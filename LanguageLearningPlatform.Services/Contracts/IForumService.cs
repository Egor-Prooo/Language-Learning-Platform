using LanguageLearningPlatform.Data.Models;

namespace LanguageLearningPlatform.Services.Contracts
{
    public class ForumPostViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorInitials { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int Views { get; set; }
        public int CommentCount { get; set; }
        public int Likes { get; set; }
        public bool IsPinned { get; set; }
        public bool IsClosed { get; set; }
        public List<ForumCommentViewModel> Comments { get; set; } = new();
    }

    public class ForumCommentViewModel
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorInitials { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Likes { get; set; }
    }

    public interface IForumService
    {
        Task<IEnumerable<ForumPostViewModel>> GetAllPostsAsync(string? category = null, string? search = null);
        Task<ForumPostViewModel?> GetPostByIdAsync(Guid id);
        Task<ForumPost> CreatePostAsync(string userId, string title, string content, string category);
        Task<ForumComment> AddCommentAsync(string userId, Guid postId, string content);
        Task<bool> DeletePostAsync(Guid postId, string userId, bool isAdmin = false);
        Task<bool> DeleteCommentAsync(Guid commentId, string userId, bool isAdmin = false);
        Task IncrementViewsAsync(Guid postId);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<bool> TogglePinAsync(Guid postId);
        Task<bool> ToggleCloseAsync(Guid postId);
    }
}