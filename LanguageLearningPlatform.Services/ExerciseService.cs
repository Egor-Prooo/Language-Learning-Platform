using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

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
            var exercises = await _context.Exercises
                .Where(e => e.LessonId == lessonId)
                .OrderBy(e => e.OrderIndex)
                .ToListAsync();

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
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == exerciseId);

            if (exercise == null)
                return null;

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
            var results = await _context.UserExerciseResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();

            var totalPoints = await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsEarned);

            var streak = CalculateStreak(results);

            var totalExercises = results.Count;
            var correctAnswers = results.Count(r => r.IsCorrect);
            var accuracyRate = totalExercises > 0
                ? (double)correctAnswers / totalExercises * 100
                : 0;

            var today = DateTime.UtcNow.Date;
            var todayExercises = results.Count(r => r.CompletedAt.Date == today);
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
            var normalizedUser = NormalizeAnswer(userAnswer);
            var normalizedCorrect = NormalizeAnswer(correctAnswer);
            return normalizedUser == normalizedCorrect;
        }

        private string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return string.Empty;

            var normalized = answer.ToLowerInvariant()
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

            // Normalise number words → digits so "three cats" and "3 cats" compare equal
            return NormalizeNumbers(normalized);
        }

        // Maps number words to their digit equivalents.
        // Covers 0-19, tens (20-90), and common large values.
        private static readonly Dictionary<string, string> WordToDigit =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["zero"] = "0",
                ["one"] = "1",
                ["two"] = "2",
                ["three"] = "3",
                ["four"] = "4",
                ["five"] = "5",
                ["six"] = "6",
                ["seven"] = "7",
                ["eight"] = "8",
                ["nine"] = "9",
                ["ten"] = "10",
                ["eleven"] = "11",
                ["twelve"] = "12",
                ["thirteen"] = "13",
                ["fourteen"] = "14",
                ["fifteen"] = "15",
                ["sixteen"] = "16",
                ["seventeen"] = "17",
                ["eighteen"] = "18",
                ["nineteen"] = "19",
                ["twenty"] = "20",
                ["thirty"] = "30",
                ["forty"] = "40",
                ["fifty"] = "50",
                ["sixty"] = "60",
                ["seventy"] = "70",
                ["eighty"] = "80",
                ["ninety"] = "90",
                ["hundred"] = "100",
                ["thousand"] = "1000",
            };

        private static string NormalizeNumbers(string input)
        {
            var result = input;
            foreach (var kvp in WordToDigit)
            {
                result = Regex.Replace(
                    result,
                    $@"\b{Regex.Escape(kvp.Key)}\b",
                    kvp.Value,
                    RegexOptions.IgnoreCase);
            }
            return result;
        }

        private int CalculateStreak(List<UserExerciseResult> results)
        {
            if (!results.Any())
                return 0;

            var today = DateTime.UtcNow.Date;
            var streak = 0;
            var currentDate = today;

            var resultsByDate = results
                .GroupBy(r => r.CompletedAt.Date)
                .OrderByDescending(g => g.Key)
                .ToList();

            var lastActivityDate = resultsByDate.First().Key;
            if ((today - lastActivityDate).Days > 1)
                return 0;

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