using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Services
{
    public class LessonProgressService : ILessonProgressService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAchievementService _achievementService;

        public LessonProgressService(ApplicationDbContext context, IAchievementService achievementService)
        {
            _context = context;
            _achievementService = achievementService;
        }

        public async Task<bool> IsLessonCompletedAsync(string userId, Guid lessonId)
        {
            return await _context.UserLessonProgresses
                .AnyAsync(p => p.UserId == userId && p.LessonId == lessonId && p.IsCompleted);
        }

        public async Task<bool> TryCompleteLessonAsync(string userId, Guid lessonId)
        {
            if (await IsLessonCompletedAsync(userId, lessonId))
                return false;

            var lesson = await _context.Lessons
                .Include(l => l.VideoLessons)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return false;

            if (lesson.VideoLessons.Any())
            {
                var videoIds = lesson.VideoLessons.Select(v => v.Id).ToList();

                var videoProgresses = await _context.UserVideoProgresses
                    .Where(p => p.UserId == userId && videoIds.Contains(p.VideoLessonId))
                    .ToListAsync();

                foreach (var video in lesson.VideoLessons)
                {
                    var vp = videoProgresses.FirstOrDefault(p => p.VideoLessonId == video.Id);

                    if (vp == null) return false; 

                    if (!vp.IsCompleted)
                    {
                        double watchPct = video.DurationSeconds > 0
                            ? (double)vp.WatchedSeconds / video.DurationSeconds * 100
                            : 0;

                        if (watchPct < 80) return false;
                    }
                }
            }

            if (lesson.Exercises.Any())
            {
                var exerciseIds = lesson.Exercises.Select(e => e.Id).ToList();

                var attemptedIds = await _context.UserExerciseResults
                    .Where(r => r.UserId == userId && exerciseIds.Contains(r.ExerciseId))
                    .Select(r => r.ExerciseId)
                    .Distinct()
                    .ToListAsync();

                if (attemptedIds.Count < exerciseIds.Count) return false;
            }

            if (!lesson.VideoLessons.Any() && !lesson.Exercises.Any())
                return false;

            _context.UserLessonProgresses.Add(new UserLessonProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            });

            var courseProgress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == lesson.CourseId);

            if (courseProgress != null)
            {
                courseProgress.CompletedLessons++;
                courseProgress.LastAccessedAt = DateTime.UtcNow;

                var totalLessons = await _context.Lessons
                    .CountAsync(l => l.CourseId == lesson.CourseId);

                courseProgress.CompletionPercentage = totalLessons > 0
                    ? (decimal)courseProgress.CompletedLessons / totalLessons * 100
                    : 0;

                if (courseProgress.CompletionPercentage >= 100 && courseProgress.CompletedAt == null)
                    courseProgress.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await _achievementService.CheckAndAwardAsync(userId);

            return true;
        }
    }
}