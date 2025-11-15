using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class Exercise
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Course))]
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;

        [ForeignKey(nameof(Lesson))]
        public Guid? LessonId { get; set; }
        public virtual Lesson? Lesson { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty; 

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;

        public string? Options { get; set; } // JSON for multiple choice

        [MaxLength(500)]
        public string? Hint { get; set; }

        [MaxLength(1000)]
        public string? Explanation { get; set; }

        public int Points { get; set; } = 10;

        public int OrderIndex { get; set; } = 0;

        public int DifficultyLevel { get; set; } = 1;

        [MaxLength(500)]
        public string? AudioUrl { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserExerciseResult> UserResults { get; set; } = new List<UserExerciseResult>();
    }
}
