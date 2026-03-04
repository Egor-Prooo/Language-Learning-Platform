using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class TeacherMessage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsFromTeacher { get; set; } = false; // false = from student, true = teacher reply

        public bool IsRead { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ForeignKey(nameof(Teacher))]
        public string TeacherId { get; set; } = string.Empty;
        public virtual User Teacher { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Student))]
        public string StudentId { get; set; } = string.Empty;
        public virtual User Student { get; set; } = null!;

        [ForeignKey(nameof(Course))]
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;
    }
}