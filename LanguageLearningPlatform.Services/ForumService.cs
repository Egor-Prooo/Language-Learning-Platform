using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Services
{
    public class ForumService : IForumService
    {
        private readonly ApplicationDbContext _context;

        private static readonly Guid ForumCourseId = new Guid("00000000-0000-0000-0000-000000000001");

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
            var query = _context.ForumPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Where(p => p.CourseId == ForumCourseId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Content.StartsWith($"[CAT:{category}]"));

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
                .FirstOrDefaultAsync(p => p.Id == id && p.CourseId == ForumCourseId);

            return post == null ? null : MapToViewModel(post);
        }

        public async Task<ForumPost> CreatePostAsync(string userId, string title, string content, string category)
        {
            await EnsureForumPlaceholderCourseExists();

            var post = new ForumPost
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = $"[CAT:{category}]\n{content}",
                CourseId = ForumCourseId,
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

        public async Task<int> LikeCommentAsync(Guid commentId)
        {
            var comment = await _context.ForumComments.FindAsync(commentId);
            if (comment == null) return 0;

            comment.Likes++;
            await _context.SaveChangesAsync();
            return comment.Likes;
        }

        public async Task<int> LikePostAsync(Guid postId)
        {
            // Post-level likes are stored on the post's own first comment, 
            // or we can track them via the Views field incremented separately.
            // Since ForumPost doesn't have a Likes column we add a synthetic
            // "like" comment owned by the system, or we simply count comments
            // that start with "[LIKE]". Cleaner: we just add a Likes column to
            // the ForumPost migration. For now we use an EF shadow approach —
            // increment a dedicated field if present, otherwise use Views as proxy.
            //
            // The cleanest path without a migration is to store likes as special
            // ForumComment records. We do that here:

            // Check if a likes-comment placeholder exists
            const string LikesMarker = "[SYSTEM:LIKES]";

            var likesComment = await _context.ForumComments
                .FirstOrDefaultAsync(c => c.PostId == postId && c.Content == LikesMarker);

            if (likesComment == null)
            {
                // First like — create the placeholder
                var post = await _context.ForumPosts.FindAsync(postId);
                if (post == null) return 0;

                likesComment = new ForumComment
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    UserId = post.UserId, // system marker owned by author
                    Content = LikesMarker,
                    CreatedAt = DateTime.UtcNow,
                    Likes = 1
                };
                _context.ForumComments.Add(likesComment);
            }
            else
            {
                likesComment.Likes++;
            }

            await _context.SaveChangesAsync();
            return likesComment.Likes;
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
            => Task.FromResult<IEnumerable<string>>(DefaultCategories);

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

        // ── Helpers ──────────────────────────────────────────────────────────

        private const string LikesMarkerConst = "[SYSTEM:LIKES]";

        private ForumPostViewModel MapToViewModel(ForumPost post)
        {
            var (category, cleanContent) = ParseContent(post.Content);
            var firstName = post.User?.FirstName ?? "Unknown";
            var lastName = post.User?.LastName ?? "";

            // Separate real comments from the system likes placeholder
            var realComments = post.Comments?
                .Where(c => c.Content != LikesMarkerConst)
                .ToList() ?? new List<ForumComment>();

            var likesPlaceholder = post.Comments?
                .FirstOrDefault(c => c.Content == LikesMarkerConst);

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
                CommentCount = realComments.Count,
                Likes = likesPlaceholder?.Likes ?? 0,
                IsPinned = post.IsPinned,
                IsClosed = post.IsClosed,
                Comments = realComments
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
                    }).ToList()
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

        private async Task EnsureForumPlaceholderCourseExists()
        {
            var exists = await _context.Courses.AnyAsync(c => c.Id == ForumCourseId);
            if (!exists)
            {
                _context.Courses.Add(new Course
                {
                    Id = ForumCourseId,
                    Title = "Community Forum",
                    Language = "Community",
                    Level = "All",
                    Description = "Internal placeholder for community forum posts.",
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}