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

        public int PointsReward { get; set; } = 0;

        public bool IsRare { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    }
}
