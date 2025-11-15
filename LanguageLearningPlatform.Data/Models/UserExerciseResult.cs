using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Data.Models
{
    public class UserExerciseResult
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public int PointsEarned { get; set; }

        public int TimeSpentSeconds { get; set; }

        public int AttemptsCount { get; set; } = 1;

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Feedback { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Exercise))]
        public Guid ExerciseId { get; set; }
        public virtual Exercise Exercise { get; set; } = null!;
    }
}
