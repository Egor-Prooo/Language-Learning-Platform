using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;

        public CourseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Course>> GetAllCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> GetPublishedCoursesAsync()
        {
            return await _context.Courses
                .Where(c => c.IsPublished)
                .Include(c => c.Lessons)
                .ToListAsync();
        }

        public async Task<Course?> GetCourseByIdAsync(Guid id)
        {
            return await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Course?> GetCourseWithLessonsAsync(Guid id)
        {
            return await _context.Courses
                .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.Exercises)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Course>> GetCoursesByLanguageAsync(string language)
        {
            return await _context.Courses
                .Where(c => c.Language == language && c.IsPublished)
                .Include(c => c.Lessons)
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> GetCoursesByLevelAsync(string level)
        {
            return await _context.Courses
                .Where(c => c.Level == level && c.IsPublished)
                .Include(c => c.Lessons)
                .ToListAsync();
        }

        public async Task<bool> EnrollUserInCourseAsync(string userId, Guid courseId)
        {
            var existingEnrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

            if (existingEnrollment != null)
                return false;

            var enrollment = new CourseEnrollment
            {
                UserId = userId,
                CourseId = courseId,
                EnrollmentDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.CourseEnrollments.Add(enrollment);

            var progress = new Progress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CourseId = courseId,
                CompletedLessons = 0,
                CompletionPercentage = 0,
                PointsEarned = 0,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };

            _context.Progresses.Add(progress);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserEnrolledAsync(string userId, Guid courseId)
        {
            return await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == courseId && e.IsActive);
        }

        public async Task<IEnumerable<Course>> GetUserEnrolledCoursesAsync(string userId)
        {
            return await _context.CourseEnrollments
                .Where(e => e.UserId == userId && e.IsActive)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Lessons)
                .Select(e => e.Course)
                .ToListAsync();
        }
    }
}
