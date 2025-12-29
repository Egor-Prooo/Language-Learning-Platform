using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LanguageLearningPlatform.Data.Models
{
    public class Course
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Language { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Level { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? LanguageCode { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public int EstimatedHours { get; set; } = 0;

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(Creator))]
        public string? CreatorId { get; set; }
        public virtual User? Creator { get; set; }

        // Navigation properties
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public virtual ICollection<Progress> Progresses { get; set; } = new List<Progress>();
        public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
        public virtual ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public virtual ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
        public virtual ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();
        public virtual ICollection<TeacherLesson> TeacherLessons { get; set; } = new List<TeacherLesson>();

    }
}
