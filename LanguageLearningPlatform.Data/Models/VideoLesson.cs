using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class VideoLesson
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string VideoUrl { get; set; } = string.Empty;

        [MaxLength(50)]
        public string VideoProvider { get; set; } = "YouTube"; 

        public int DurationSeconds { get; set; } = 0;

        public int OrderIndex { get; set; } = 0;

        public bool IsRequired { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ForeignKey(nameof(Lesson))]
        public Guid LessonId { get; set; }
        public virtual Lesson Lesson { get; set; } = null!;

        public virtual ICollection<UserVideoProgress> UserProgresses { get; set; } = new List<UserVideoProgress>();
    }
}