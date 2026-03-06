using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class CourseTeacher
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Course))]
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Teacher))]
        public string TeacherId { get; set; } = string.Empty;
        public virtual User Teacher { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public bool IsPrimary { get; set; } = false; // marks the main teacher
    }
}