using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;


namespace LanguageLearningPlatform.Services
{
    public class ProgressService : IProgressService
    {
        private readonly ApplicationDbContext _context;

        public ProgressService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Progress?> GetUserCourseProgressAsync(string userId, Guid courseId)
        {
            return await _context.Progresses
                .Include(p => p.Course)
                    .ThenInclude(c => c.Lessons)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == courseId);
        }

        public async Task<IEnumerable<Progress>> GetUserProgressAsync(string userId)
        {
            return await _context.Progresses
                .Where(p => p.UserId == userId)
                .Include(p => p.Course)
                .OrderByDescending(p => p.LastAccessedAt)
                .ToListAsync();
        }

        public async Task UpdateProgressAsync(string userId, Guid courseId, int pointsEarned)
        {
            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == courseId);

            if (progress == null)
                return;

            progress.PointsEarned += pointsEarned;
            progress.LastAccessedAt = DateTime.UtcNow;

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course != null && course.Lessons.Any())
            {
                progress.CompletionPercentage = (decimal)progress.CompletedLessons / course.Lessons.Count * 100;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUserTotalPointsAsync(string userId)
        {
            return await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsEarned);
        }
    }
}
