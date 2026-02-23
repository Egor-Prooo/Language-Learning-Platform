using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class UserVideoProgress
    {
        [Key]
        public Guid Id { get; set; }

        public bool IsCompleted { get; set; } = false;

        public int WatchedSeconds { get; set; } = 0;

        public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(VideoLesson))]
        public Guid VideoLessonId { get; set; }
        public virtual VideoLesson VideoLesson { get; set; } = null!;
    }
}