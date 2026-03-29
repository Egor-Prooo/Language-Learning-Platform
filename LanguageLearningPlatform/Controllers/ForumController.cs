using LanguageLearningPlatform.Services.Contracts;
using LanguageLearningPlatform.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    public class ForumController : Controller
    {
        private readonly IForumService _forumService;
        private readonly IHubContext<ForumHub> _hub;

        public ForumController(IForumService forumService, IHubContext<ForumHub> hub)
        {
            _forumService = forumService;
            _hub = hub;
        }

        // GET: /Forum
        public async Task<IActionResult> Index(string? category, string? search)
        {
            var posts = await _forumService.GetAllPostsAsync(category, search);
            var categories = await _forumService.GetCategoriesAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.Search = search;

            return View(posts);
        }

        // GET: /Forum/Post/id
        public async Task<IActionResult> Post(Guid id)
        {
            var post = await _forumService.GetPostByIdAsync(id);
            if (post == null) return NotFound();

            await _forumService.IncrementViewsAsync(id);

            var categories = await _forumService.GetCategoriesAsync();
            ViewBag.Categories = categories;

            return View(post);
        }

        // GET: /Forum/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _forumService.GetCategoriesAsync();
            return View();
        }

        // POST: /Forum/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title, string content, string category)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                ViewBag.Categories = await _forumService.GetCategoriesAsync();
                ModelState.AddModelError("", "Title and content are required.");
                return View();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var post = await _forumService.CreatePostAsync(userId, title, content, category ?? "General Discussion");

            TempData["SuccessMessage"] = "Your post has been published!";
            return RedirectToAction(nameof(Post), new { id = post.Id });
        }

        // POST: /Forum/Comment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(Guid postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Post), new { id = postId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var comment = await _forumService.AddCommentAsync(userId, postId, content);

            // Reload the saved comment with author info via GetPostByIdAsync
            // (AddCommentAsync returns a bare ForumComment; we broadcast the minimal
            //  payload the client needs rather than re-fetching the full post).
            var firstName = User.Identity?.Name ?? "";
            var userFullName = $"{User.FindFirstValue("FirstName") ?? ""} {User.FindFirstValue("LastName") ?? ""}".Trim();
            if (string.IsNullOrWhiteSpace(userFullName))
                userFullName = User.Identity?.Name ?? "User";

            // Build initials from claims that the Identity cookie carries
            // (FirstName / LastName custom claims added in Program.cs via ClaimsTransformation
            //  or ApplicationUser principal). Fall back to the comment author name gracefully.
            var initials = userFullName.Length > 0
                ? string.Concat(userFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(w => char.ToUpper(w[0]).ToString()))
                : "?";

            // Broadcast the new comment to everyone currently viewing this post
            await _hub.Clients
                .Group(ForumHub.PostGroup(postId.ToString()))
                .SendAsync("ReceiveComment", new
                {
                    id = comment.Id.ToString(),
                    content = content,
                    authorName = userFullName,
                    authorInitials = initials,
                    authorId = userId,
                    createdAt = comment.CreatedAt.ToString("MMM dd, yyyy · HH:mm"),
                    likes = 0
                });

            TempData["SuccessMessage"] = "Reply posted!";
            return RedirectToAction(nameof(Post), new { id = postId });
        }

        // POST: /Forum/LikeComment  (AJAX)
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LikeComment(Guid commentId)
        {
            var newCount = await _forumService.LikeCommentAsync(commentId);

            // Broadcast the updated like count so other viewers see it immediately.
            // We don't know the postId from this endpoint, so we broadcast to all
            // clients and let each client ignore updates for comments it doesn't own.
            await _hub.Clients.All.SendAsync("CommentLikeUpdated", new
            {
                commentId = commentId.ToString(),
                likes = newCount
            });

            return Json(new { success = true, likes = newCount });
        }

        // POST: /Forum/LikePost  (AJAX)
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LikePost(Guid postId)
        {
            var newCount = await _forumService.LikePostAsync(postId);

            await _hub.Clients
                .Group(ForumHub.PostGroup(postId.ToString()))
                .SendAsync("PostLikeUpdated", new
                {
                    postId = postId.ToString(),
                    likes = newCount
                });

            return Json(new { success = true, likes = newCount });
        }

        // POST: /Forum/DeletePost
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var success = await _forumService.DeletePostAsync(postId, userId, isAdmin);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Post deleted." : "You can only delete your own posts.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Forum/DeleteComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(Guid commentId, Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            await _forumService.DeleteCommentAsync(commentId, userId, isAdmin);

            // Notify viewers to remove the comment from the UI
            await _hub.Clients
                .Group(ForumHub.PostGroup(postId.ToString()))
                .SendAsync("CommentDeleted", commentId.ToString());

            return RedirectToAction(nameof(Post), new { id = postId });
        }

        // POST: /Forum/TogglePin  (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePin(Guid postId)
        {
            await _forumService.TogglePinAsync(postId);
            return RedirectToAction(nameof(Post), new { id = postId });
        }

        // POST: /Forum/ToggleClose  (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleClose(Guid postId)
        {
            await _forumService.ToggleCloseAsync(postId);
            return RedirectToAction(nameof(Post), new { id = postId });
        }
    }
}