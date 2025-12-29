using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<UserLevel> UserLevels { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<UserExerciseResult> UserExerciseResults { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<ForumComment> ForumComments { get; set; }
        public DbSet<TeacherLesson> TeacherLessons { get; set; }
        public DbSet<CourseSection> CourseSections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserAchievement>()
               .HasOne(ua => ua.User)
               .WithMany(u => u.UserAchievements)
               .HasForeignKey(ua => ua.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAchievement>()
                .HasOne(ua => ua.Achievement)
                .WithMany(a => a.UserAchievements)
                .HasForeignKey(ua => ua.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);

            // Chat Message relationships
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Tutor)
                .WithMany(t => t.ChatMessages)
                .HasForeignKey(cm => cm.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            //// Course Creator relationship
            //modelBuilder.Entity<Course>()
            //    .HasOne(c => c.Creator)
            //    .WithMany(u => u.CreatedCourses)
            //    .HasForeignKey(c => c.CreatorId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //// Forum Post relationships
            //modelBuilder.Entity<ForumPost>()
            //    .HasOne(fp => fp.Course)
            //    .WithMany(c => c.ForumPosts)
            //    .HasForeignKey(fp => fp.CourseId)
            //    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ForumPost>()
                .HasOne(fp => fp.User)
                .WithMany(u => u.ForumPosts)
                .HasForeignKey(fp => fp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Forum Comment relationships
            modelBuilder.Entity<ForumComment>()
                .HasOne(fc => fc.Post)
                .WithMany(fp => fp.Comments)
                .HasForeignKey(fc => fc.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ForumComment>()
                .HasOne(fc => fc.User)
                .WithMany(u => u.ForumComments)
                .HasForeignKey(fc => fc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            //// Teacher Lesson relationships
            //modelBuilder.Entity<TeacherLesson>()
            //    .HasOne(tl => tl.Course)
            //    .WithMany(c => c.TeacherLessons)
            //    .HasForeignKey(tl => tl.CourseId)
            //    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherLesson>()
                .HasOne(tl => tl.Teacher)
                .WithMany(u => u.TeacherLessons)
                .HasForeignKey(tl => tl.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Course Section relationships
            modelBuilder.Entity<CourseSection>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.Sections)
                .HasForeignKey(cs => cs.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            modelBuilder.Entity<Exercise>()
                .HasIndex(e => e.CourseId);

            modelBuilder.Entity<Progress>()
                .HasIndex(p => new { p.UserId, p.CourseId });

            modelBuilder.Entity<ForumPost>()
                .HasIndex(fp => fp.CourseId);

            modelBuilder.Entity<ForumPost>()
                .HasIndex(fp => fp.CreatedAt);
        }
    }
}
