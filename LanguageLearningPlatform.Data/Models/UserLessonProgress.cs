using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LanguageLearningPlatform.Data.Models;

namespace LanguageLearningPlatform.Data.Models
{
    public class UserLessonProgress
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Lesson))]
        public Guid LessonId { get; set; }
        public virtual Lesson Lesson { get; set; } = null!;

        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
    }
}