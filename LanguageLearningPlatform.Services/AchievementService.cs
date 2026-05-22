using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly ApplicationDbContext _context;

        private static readonly (int Level, string Name, int Min, int Max, string Color)[] LevelDefs =
        {
            (1, "Novice",        0,     100,    "#10B981"),
            (2, "Beginner",      100,   300,    "#3B82F6"),
            (3, "Elementary",    300,   700,    "#8B5CF6"),
            (4, "Intermediate",  700,   1500,   "#F59E0B"),
            (5, "Upper-Inter",   1500,  3000,   "#EF4444"),
            (6, "Advanced",      3000,  6000,   "#EC4899"),
            (7, "Master",        6000,  999999, "#F97316"),
        };

        public AchievementService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task CheckAndAwardAsync(string userId)
        {
            var totalPoints = await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsEarned);

            var completedLessons = await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.CompletedLessons);

            var completedCourses = await _context.Progresses
                .Where(p => p.UserId == userId && p.CompletionPercentage >= 100)
                .CountAsync();

            var exerciseResults = await _context.UserExerciseResults
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var totalExercises = exerciseResults.Count;
            var correctExercises = exerciseResults.Count(r => r.IsCorrect);
            var accuracyRate = totalExercises > 0
                ? (double)correctExercises / totalExercises * 100
                : 0;

            var streak = CalculateStreak(exerciseResults);

            var allAchievements = await _context.Achievements.ToListAsync();

            var earnedIds = (await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AchievementId)
                .ToListAsync()).ToHashSet();

            var newAwards = new List<UserAchievement>();
            var bonusPoints = 0;

            foreach (var achievement in allAchievements.Where(a => !earnedIds.Contains(a.Id)))
            {
                var earned = achievement.TriggerType switch
                {
                    "PointsReached" => totalPoints >= achievement.TriggerValue,
                    "LessonsCompleted" => completedLessons >= achievement.TriggerValue,
                    "StreakDays" => streak >= achievement.TriggerValue,
                    "AccuracyRate" => totalExercises >= 10 && accuracyRate >= achievement.TriggerValue,
                    "CoursesCompleted" => completedCourses >= achievement.TriggerValue,
                    _ => false
                };

                if (!earned) continue;

                newAwards.Add(new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AchievementId = achievement.Id,
                    UnlockedAt = DateTime.UtcNow
                });

                bonusPoints += achievement.PointsReward;
            }

            if (newAwards.Any())
            {
                _context.UserAchievements.AddRange(newAwards);

                if (bonusPoints > 0)
                {
                    var firstProgress = await _context.Progresses
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    if (firstProgress != null)
                        firstProgress.PointsEarned += bonusPoints;
                }

                await _context.SaveChangesAsync();

                totalPoints += bonusPoints;
            }

            await UpdateLevelAsync(userId, totalPoints);
        }

        public async Task UpdateLevelAsync(string userId, int totalPoints)
        {
            var userLevel = await _context.UserLevels
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (userLevel == null) return;

            var def = LevelDefs.LastOrDefault(l => totalPoints >= l.Min);
            if (def == default) return;

            if (userLevel.Level == def.Level) return;

            userLevel.Level = def.Level;
            userLevel.Name = def.Name;
            userLevel.MinPoints = def.Min;
            userLevel.MaxPoints = def.Max;
            userLevel.Color = def.Color;
            userLevel.AchievedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }


        private static int CalculateStreak(List<UserExerciseResult> results)
        {
            if (!results.Any()) return 0;

            var today = DateTime.UtcNow.Date;

            var byDate = results
                .GroupBy(r => r.CompletedAt.Date)
                .OrderByDescending(g => g.Key)
                .ToList();

            if ((today - byDate.First().Key).Days > 1) return 0;

            var streak = 0;
            var cursor = today;

            foreach (var group in byDate)
            {
                if (group.Key == cursor || group.Key == cursor.AddDays(-1))
                {
                    streak++;
                    cursor = group.Key.AddDays(-1);
                }
                else break;
            }

            return streak;
        }
    }
}