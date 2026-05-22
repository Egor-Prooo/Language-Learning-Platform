using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class ExercisesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IExerciseService _exerciseService;
        private readonly ILessonProgressService _lessonProgressService;
        private readonly IAchievementService _achievementService;

        public ExercisesController(ApplicationDbContext context, IExerciseService exerciseService,
            ILessonProgressService lessonProgressService, IAchievementService achievementService)
        {
            _context = context;
            _exerciseService = exerciseService;
            _lessonProgressService = lessonProgressService;
            _achievementService = achievementService;
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitExercise([FromBody] ExerciseSubmissionRequest submission)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var exercise = await _context.Exercises.FindAsync(submission.ExerciseId);
            if (exercise == null)
                return NotFound(new { error = "Exercise not found" });

            // ── Validate answer ───────────────────────────────────────────────────
            var validationResult = await _exerciseService.ValidateAnswerAsync(
                submission.ExerciseId, submission.UserAnswer);

            // ── Persist result ────────────────────────────────────────────────────
            _context.UserExerciseResults.Add(new UserExerciseResult
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExerciseId = submission.ExerciseId,
                UserAnswer = submission.UserAnswer,
                IsCorrect = validationResult.IsCorrect,
                PointsEarned = validationResult.PointsEarned,
                TimeSpentSeconds = submission.TimeSpentSeconds,
                AttemptsCount = submission.AttemptsCount,
                CompletedAt = DateTime.UtcNow,
                Feedback = validationResult.Feedback
            });

            // ── Update course points on correct answer ────────────────────────────
            if (validationResult.IsCorrect)
            {
                var progress = await _context.Progresses
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == exercise.CourseId);

                if (progress != null)
                {
                    progress.PointsEarned += exercise.Points;
                    progress.LastAccessedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            // ── Try to complete the parent lesson ─────────────────────────────────
            if (exercise.LessonId.HasValue)
                await _lessonProgressService.TryCompleteLessonAsync(userId, exercise.LessonId.Value);
            else
                await _achievementService.CheckAndAwardAsync(userId);

            // ── Build response ────────────────────────────────────────────────────
            var stats = await _exerciseService.GetUserExerciseStatsAsync(userId);
            var totalPoints = await GetUserTotalPointsAsync(userId);

            return Ok(new ExerciseSubmissionResponse
            {
                Success = true,
                IsCorrect = validationResult.IsCorrect,
                PointsEarned = validationResult.PointsEarned,
                TotalPoints = totalPoints,
                Feedback = validationResult.IsCorrect
                                    ? "Correct! Well done! 🎉"
                                    : "Not quite right. Try again!",
                CorrectAnswer = validationResult.IsCorrect ? null : validationResult.CorrectAnswer,
                Explanation = validationResult.IsCorrect ? validationResult.Explanation : null,
                Streak = stats.CurrentStreak,
                LevelUp = false
            });
        }

        [HttpGet("lesson/{lessonId}")]
        [Authorize]
        public async Task<IActionResult> GetLessonExercises(Guid lessonId)
        {
            var exercises = await _exerciseService.GetLessonExercisesAsync(lessonId);
            return Ok(exercises);
        }

        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var stats = await _exerciseService.GetUserExerciseStatsAsync(userId);
            return Ok(stats);
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetExerciseHistory([FromQuery] Guid? courseId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = _context.UserExerciseResults
                .Include(r => r.Exercise)
                    .ThenInclude(e => e.Course)
                .Where(r => r.UserId == userId);

            if (courseId.HasValue)
                query = query.Where(r => r.Exercise.CourseId == courseId.Value);

            var results = await query
                .OrderByDescending(r => r.CompletedAt)
                .Take(200)
                .ToListAsync();

            // Project to a slim DTO so the exerciseId is always present and camelCased
            // correctly regardless of the global serialiser configuration.
            var dto = results.Select(r => new
            {
                exerciseId = r.ExerciseId,
                isCorrect = r.IsCorrect,
                userAnswer = r.UserAnswer,
                pointsEarned = r.PointsEarned,
                completedAt = r.CompletedAt,
                feedback = r.Feedback,
                explanation = r.Exercise?.Explanation,
            });

            return Ok(dto);
        }

        [HttpPost("hint/{exerciseId}")]
        [Authorize]
        public async Task<IActionResult> GetHint(Guid exerciseId)
        {
            var exercise = await _exerciseService.GetExerciseByIdAsync(exerciseId);
            if (exercise == null) return NotFound();

            return Ok(new { hint = exercise.Hint });
        }

        private async Task<int> GetUserTotalPointsAsync(string userId)
            => await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsEarned);
    }
}