using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Data.Models
{
    public class UserAchievement
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Achievement))]
        public Guid AchievementId { get; set; }
        public virtual Achievement Achievement { get; set; } = null!;
    }
}
