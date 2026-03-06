using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace LanguageLearningPlatform.Data.Models
{
    public class Achievement
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public int PointsRequired { get; set; }

        [MaxLength(500)]
        public string? IconUrl { get; set; }

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // "Streak", "Points", "Completion"

        [MaxLength(50)]
        public string TriggerType { get; set; } = string.Empty;
        // e.g. "PointsReached", "LessonsCompleted", "StreakDays", "AccuracyRate", "CoursesCompleted"

        public int TriggerValue { get; set; } = 0;
        // e.g. 100 points, 10 lessons, 7 day streak

        public int PointsReward { get; set; } = 0;

        public bool IsRare { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    }
}
