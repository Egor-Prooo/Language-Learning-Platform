using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Services
{
    public class ForumService : IForumService
    {
        private readonly ApplicationDbContext _context;

        // Built-in categories for a language learning platform
        private static readonly List<string> DefaultCategories = new()
        {
            "General Discussion", "Grammar Help", "Vocabulary", "Pronunciation",
            "Study Tips", "Language Exchange", "Culture & Travel", "Resources & Tools"
        };

        public ForumService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ForumPostViewModel>> GetAllPostsAsync(string? category = null, string? search = null)
        {
            // We'll use the existing ForumPost table but treat CourseId as optional
            // by using a "forum-only" sentinel CourseId or querying all posts
            var query = _context.ForumPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Content.StartsWith($"[CAT:{category}]") ||
                                         EF.Functions.Like(p.Title, $"%[{category}]%"));

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));

            var posts = await query
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return posts.Select(MapToViewModel).ToList();
        }

        public async Task<ForumPostViewModel?> GetPostByIdAsync(Guid id)
        {
            var post = await _context.ForumPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            return post == null ? null : MapToViewModel(post);
        }

        public async Task<ForumPost> CreatePostAsync(string userId, string title, string content, string category)
        {
            // We encode the category in the content using a prefix for compatibility
            // with existing schema (which requires CourseId). 
            // We use a well-known placeholder CourseId for "community forum" posts.
            var forumCourseId = await GetOrCreateForumPlaceholderCourseId();

            var post = new ForumPost
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = $"[CAT:{category}]\n{content}",
                CourseId = forumCourseId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Views = 0,
                IsPinned = false,
                IsClosed = false
            };

            _context.ForumPosts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<ForumComment> AddCommentAsync(string userId, Guid postId, string content)
        {
            var comment = new ForumComment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                Likes = 0
            };

            _context.ForumComments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<bool> DeletePostAsync(Guid postId, string userId, bool isAdmin = false)
        {
            var post = await _context.ForumPosts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return false;
            if (!isAdmin && post.UserId != userId) return false;

            _context.ForumComments.RemoveRange(post.Comments);
            _context.ForumPosts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCommentAsync(Guid commentId, string userId, bool isAdmin = false)
        {
            var comment = await _context.ForumComments.FindAsync(commentId);
            if (comment == null) return false;
            if (!isAdmin && comment.UserId != userId) return false;

            _context.ForumComments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task IncrementViewsAsync(Guid postId)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post != null)
            {
                post.Views++;
                await _context.SaveChangesAsync();
            }
        }

        public Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return Task.FromResult<IEnumerable<string>>(DefaultCategories);
        }

        public async Task<bool> TogglePinAsync(Guid postId)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null) return false;
            post.IsPinned = !post.IsPinned;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleCloseAsync(Guid postId)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null) return false;
            post.IsClosed = !post.IsClosed;
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Helpers ──────────────────────────────────────────────────

        private ForumPostViewModel MapToViewModel(ForumPost post)
        {
            var (category, cleanContent) = ParseContent(post.Content);
            var firstName = post.User?.FirstName ?? "Unknown";
            var lastName = post.User?.LastName ?? "";

            return new ForumPostViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = cleanContent,
                Category = category,
                AuthorName = $"{firstName} {lastName}".Trim(),
                AuthorInitials = GetInitials(firstName, lastName),
                AuthorId = post.UserId,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Views = post.Views,
                CommentCount = post.Comments?.Count ?? 0,
                IsPinned = post.IsPinned,
                IsClosed = post.IsClosed,
                Comments = post.Comments?
                    .OrderBy(c => c.CreatedAt)
                    .Select(c =>
                    {
                        var cf = c.User?.FirstName ?? "Unknown";
                        var cl = c.User?.LastName ?? "";
                        return new ForumCommentViewModel
                        {
                            Id = c.Id,
                            Content = c.Content,
                            AuthorName = $"{cf} {cl}".Trim(),
                            AuthorInitials = GetInitials(cf, cl),
                            AuthorId = c.UserId,
                            CreatedAt = c.CreatedAt,
                            Likes = c.Likes
                        };
                    }).ToList() ?? new()
            };
        }

        private static (string category, string content) ParseContent(string raw)
        {
            if (raw.StartsWith("[CAT:"))
            {
                var end = raw.IndexOf(']');
                if (end > 0)
                {
                    var cat = raw.Substring(5, end - 5);
                    var content = raw.Substring(end + 1).TrimStart('\n');
                    return (cat, content);
                }
            }
            return ("General Discussion", raw);
        }

        private static string GetInitials(string first, string last)
        {
            var f = first?.Length > 0 ? first[0].ToString().ToUpper() : "?";
            var l = last?.Length > 0 ? last[0].ToString().ToUpper() : "";
            return f + l;
        }

        private async Task<Guid> GetOrCreateForumPlaceholderCourseId()
        {
            // Use a fixed, well-known ID for "Community Forum" placeholder course
            var forumId = new Guid("00000000-0000-0000-0000-000000000001");

            var exists = await _context.Courses.AnyAsync(c => c.Id == forumId);
            if (!exists)
            {
                _context.Courses.Add(new Course
                {
                    Id = forumId,
                    Title = "Community Forum",
                    Language = "Community",
                    Level = "All",
                    Description = "Internal placeholder for community forum posts.",
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return forumId;
        }
    }
}