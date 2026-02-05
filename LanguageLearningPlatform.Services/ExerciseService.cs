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
            });
        }

        public async Task<Exercise?> GetExerciseByIdAsync(Guid id)
        {
            return await _context.Exercises
                .Include(e => e.Course)
                .Include(e => e.Lesson)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ExerciseResultDto> ValidateAnswerAsync(Guid exerciseId, string userAnswer)
        {
            var exercise = await GetExerciseByIdAsync(exerciseId);

            if (exercise == null)
            {
                return new ExerciseResultDto
                {
                    IsCorrect = false,
                    Feedback = "Exercise not found"
                };
            }

            var isCorrect = CompareAnswers(exercise.CorrectAnswer, userAnswer);

            return new ExerciseResultDto
            {
                IsCorrect = isCorrect,
                Feedback = isCorrect
                    ? GetPositiveFeedback()
                    : "Not quite right. Try again!",
                CorrectAnswer = isCorrect ? null : exercise.CorrectAnswer,
                Explanation = isCorrect ? exercise.Explanation : null,
                PointsEarned = isCorrect ? exercise.Points : 0
            };
        }

        public async Task<bool> SubmitExerciseAnswerAsync(Guid exerciseId, string userId, string userAnswer)
        {
            var exercise = await GetExerciseByIdAsync(exerciseId);
            if (exercise == null) return false;

            var isCorrect = CompareAnswers(exercise.CorrectAnswer, userAnswer);

            // Get attempts count for this exercise
            var previousAttempts = await _context.UserExerciseResults
                .Where(r => r.UserId == userId && r.ExerciseId == exerciseId)
                .CountAsync();

            var result = new UserExerciseResult
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExerciseId = exerciseId,
                UserAnswer = userAnswer,
                IsCorrect = isCorrect,
                PointsEarned = isCorrect ? exercise.Points : 0,
                AttemptsCount = previousAttempts + 1,
                CompletedAt = DateTime.UtcNow,
                Feedback = isCorrect ? GetPositiveFeedback() : "Try again!"
            };

            _context.UserExerciseResults.Add(result);

            // Update user progress if correct
            if (isCorrect)
            {
                var progress = await _context.Progresses
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == exercise.CourseId);

                if (progress != null)
                {
                    progress.PointsEarned += exercise.Points;
                    progress.LastAccessedAt = DateTime.UtcNow;
                    _context.Progresses.Update(progress);
                }
            }

            await _context.SaveChangesAsync();
            return isCorrect;
        }

        public async Task<IEnumerable<UserExerciseResult>> GetUserExerciseHistoryAsync(string userId, Guid? courseId = null)
        {
            var query = _context.UserExerciseResults
                .Where(r => r.UserId == userId)
                .Include(r => r.Exercise)
                    .ThenInclude(e => e.Course)
                .OrderByDescending(r => r.CompletedAt);

            if (courseId.HasValue)
            {
                query = (IOrderedQueryable<UserExerciseResult>)query.Where(r => r.Exercise.CourseId == courseId.Value);
            }

            return await query.Take(50).ToListAsync();
        }

        public async Task<ExerciseStatsDto> GetUserExerciseStatsAsync(string userId)
        {
            var results = await _context.UserExerciseResults
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var totalExercises = results.Count;
            var correctExercises = results.Count(r => r.IsCorrect);
            var totalPoints = results.Sum(r => r.PointsEarned);
            var accuracyRate = totalExercises > 0
                ? (double)correctExercises / totalExercises * 100
                : 0;

            // Calculate streak (consecutive days with activity)
            var streak = CalculateStreak(results);

            return new ExerciseStatsDto
            {
                TotalExercises = totalExercises,
                CorrectExercises = correctExercises,
                TotalPoints = totalPoints,
                AccuracyRate = Math.Round(accuracyRate, 2),
                CurrentStreak = streak
            };
        }

        private bool CompareAnswers(string correctAnswer, string userAnswer)
        {
            return NormalizeAnswer(userAnswer) == NormalizeAnswer(correctAnswer);
        }

        private string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return string.Empty;

            return answer.ToLower()
                .Trim()
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("ñ", "n")
                .Replace("ü", "u")
                .Replace("à", "a")
                .Replace("è", "e")
                .Replace("ì", "i")
                .Replace("ò", "o")
                .Replace("ù", "u")
                .Replace("ç", "c")
                .Replace("ä", "a")
                .Replace("ö", "o")
                .Replace("ß", "ss");
        }

        private string GetPositiveFeedback()
        {
            var feedbacks = new[]
            {
                "Excellent! 🎉",
                "Perfect! Well done! ⭐",
                "Amazing work! 🌟",
                "You're doing great! 👏",
                "Fantastic! Keep it up! 🚀",
                "Brilliant! 💯",
                "Outstanding! 🏆",
                "Superb! You're a natural! 🎯"
            };

            return feedbacks[new Random().Next(feedbacks.Length)];
        }

        private int CalculateStreak(List<UserExerciseResult> results)
        {
            if (!results.Any()) return 0;

            var streak = 0;
            var currentDate = DateTime.UtcNow.Date;
            var orderedDates = results
                .Select(r => r.CompletedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            foreach (var date in orderedDates)
            {
                if (date == currentDate || date == currentDate.AddDays(-streak))
                {
                    streak++;
                    currentDate = date;
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