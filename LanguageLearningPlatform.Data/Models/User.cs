using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        public DateTime DateOfBirth { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? PreferredLanguage { get; set; }

        // Teacher-specific properties
        [MaxLength(2000)]
        public string? TeacherBio { get; set; }

        [MaxLength(500)]
        public string? Specialization { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 0;

        public int TotalSessions { get; set; } = 0;

        public bool IsVerifiedTeacher { get; set; } = false;

        // Navigation properties
        public virtual ICollection<Course> EnrolledCourses { get; set; } = new List<Course>();
        public virtual ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
        public virtual ICollection<Progress> Progresses { get; set; } = new List<Progress>();
        public virtual ICollection<UserExerciseResult> ExerciseResults { get; set; } = new List<UserExerciseResult>();
        public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
        public virtual ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();
        public virtual ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();
        public virtual ICollection<TeacherLesson> TeacherLessons { get; set; } = new List<TeacherLesson>();
        public virtual UserLevel UserLevel { get; set; }
    }
}
