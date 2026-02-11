using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LanguageLearningPlatform.Services
{
    public class ExerciseService : IExerciseService
    {
        private readonly ApplicationDbContext _context;

        public ExerciseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExerciseViewModel>> GetLessonExercisesAsync(Guid lessonId)
        {
            // First, get the exercises from database
            var exercises = await _context.Exercises
                .Where(e => e.LessonId == lessonId)
                .OrderBy(e => e.OrderIndex)
                .ToListAsync(); // Materialize the query first

            // Then map to view model in memory (not in expression tree)
            return exercises.Select(e => new ExerciseViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Type = e.Type,
                Content = e.Content,
                CorrectAnswer = e.CorrectAnswer,
                Options = string.IsNullOrEmpty(e.Options)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(e.Options) ?? new List<string>(),
                Hint = e.Hint,
                Explanation = e.Explanation,
                Points = e.Points,
                DifficultyLevel = e.DifficultyLevel,
                AudioUrl = e.AudioUrl,
                ImageUrl = e.ImageUrl,
                OrderIndex = e.OrderIndex
            }).ToList();
        }

        public async Task<ExerciseViewModel?> GetExerciseByIdAsync(Guid exerciseId)
        {
            // First, get the exercise from database
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == exerciseId);

            if (exercise == null)
                return null;

            // Then map to view model in memory
            return new ExerciseViewModel
            {
                Id = exercise.Id,
                Title = exercise.Title,
                Type = exercise.Type,
                Content = exercise.Content,
                CorrectAnswer = exercise.CorrectAnswer,
                Options = string.IsNullOrEmpty(exercise.Options)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(exercise.Options) ?? new List<string>(),
                Hint = exercise.Hint,
                Explanation = exercise.Explanation,
                Points = exercise.Points,
                DifficultyLevel = exercise.DifficultyLevel,
                AudioUrl = exercise.AudioUrl,
                ImageUrl = exercise.ImageUrl,
                OrderIndex = exercise.OrderIndex
            };
        }

        public async Task<ExerciseValidationResult> ValidateAnswerAsync(Guid exerciseId, string userAnswer)
        {
            var exercise = await _context.Exercises.FindAsync(exerciseId);

            if (exercise == null)
            {
                return new ExerciseValidationResult
                {
                    IsCorrect = false,
                    PointsEarned = 0,
                    Feedback = "Exercise not found",
                    CorrectAnswer = null
                };
            }

            bool isCorrect = CompareAnswers(userAnswer, exercise.CorrectAnswer);

            return new ExerciseValidationResult
            {
                IsCorrect = isCorrect,
                PointsEarned = isCorrect ? exercise.Points : 0,
                Feedback = isCorrect ? "Correct! Well done! 🎉" : "Not quite right. Try again!",
                CorrectAnswer = isCorrect ? null : exercise.CorrectAnswer,
                Explanation = isCorrect ? exercise.Explanation : null
            };
        }

        public async Task<ExerciseStatsViewModel> GetUserExerciseStatsAsync(string userId)
        {
            // Get all user exercise results
            var results = await _context.UserExerciseResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();

            // Calculate total points from all progresses
            var totalPoints = await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsEarned);

            // Calculate streak (consecutive days with activity)
            var streak = CalculateStreak(results);

            // Calculate accuracy
            var totalExercises = results.Count;
            var correctAnswers = results.Count(r => r.IsCorrect);
            var accuracyRate = totalExercises > 0
                ? (double)correctAnswers / totalExercises * 100
                : 0;

            // Get today's exercises count
            var today = DateTime.UtcNow.Date;
            var todayExercises = results.Count(r => r.CompletedAt.Date == today);

            // Get last activity date
            var lastActivity = results.Any() ? results.First().CompletedAt : (DateTime?)null;

            return new ExerciseStatsViewModel
            {
                TotalPoints = totalPoints,
                CurrentStreak = streak,
                TotalExercisesCompleted = totalExercises,
                CorrectAnswers = correctAnswers,
                IncorrectAnswers = totalExercises - correctAnswers,
                AccuracyRate = Math.Round(accuracyRate, 2),
                TodayExercises = todayExercises,
                LastActivityDate = lastActivity
            };
        }

        public async Task<IEnumerable<UserExerciseResult>> GetUserExerciseHistoryAsync(string userId, Guid? courseId = null)
        {
            var query = _context.UserExerciseResults
                .Include(r => r.Exercise)
                    .ThenInclude(e => e.Course)
                .Where(r => r.UserId == userId);

            if (courseId.HasValue)
            {
                query = query.Where(r => r.Exercise.CourseId == courseId.Value);
            }

            return await query
                .OrderByDescending(r => r.CompletedAt)
                .Take(50)
                .ToListAsync();
        }

        private bool CompareAnswers(string userAnswer, string correctAnswer)
        {
            // Normalize both answers for comparison
            var normalizedUser = NormalizeAnswer(userAnswer);
            var normalizedCorrect = NormalizeAnswer(correctAnswer);

            return normalizedUser == normalizedCorrect;
        }

        private string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return string.Empty;

            return answer.ToLowerInvariant()
                .Trim()
                .Replace("á", "a")
                .Replace("à", "a")
                .Replace("ä", "a")
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ë", "e")
                .Replace("í", "i")
                .Replace("ì", "i")
                .Replace("ï", "i")
                .Replace("ó", "o")
                .Replace("ò", "o")
                .Replace("ö", "o")
                .Replace("ú", "u")
                .Replace("ù", "u")
                .Replace("ü", "u")
                .Replace("ñ", "n")
                .Replace("ç", "c");
        }

        private int CalculateStreak(List<UserExerciseResult> results)
        {
            if (!results.Any())
                return 0;

            var today = DateTime.UtcNow.Date;
            var streak = 0;
            var currentDate = today;

            // Group results by date
            var resultsByDate = results
                .GroupBy(r => r.CompletedAt.Date)
                .OrderByDescending(g => g.Key)
                .ToList();

            // Check if there's activity today or yesterday (to maintain streak)
            var lastActivityDate = resultsByDate.First().Key;
            if ((today - lastActivityDate).Days > 1)
            {
                // Streak is broken
                return 0;
            }

            // Count consecutive days
            foreach (var dateGroup in resultsByDate)
            {
                if (dateGroup.Key == currentDate || dateGroup.Key == currentDate.AddDays(-1))
                {
                    streak++;
                    currentDate = dateGroup.Key.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }
    }
}