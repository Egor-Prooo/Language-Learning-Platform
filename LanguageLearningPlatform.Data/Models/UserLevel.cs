using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Data.Models
{
    public class UserLevel
    {
        [Key]
        public Guid Id { get; set; }

        public int Level { get; set; } = 1;

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = "Beginner";

        public int MinPoints { get; set; }

        public int MaxPoints { get; set; }

        [MaxLength(500)]
        public string? BadgeUrl { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public DateTime AchievedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;
    }
}
