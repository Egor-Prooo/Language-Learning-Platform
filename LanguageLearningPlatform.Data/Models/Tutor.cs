using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class Tutor
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Bio { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        [Required]
        public string Languages { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Specialization { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 0;

        public int TotalSessions { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal HourlyRate { get; set; } = 0;

        public bool IsAvailable { get; set; } = true;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}
