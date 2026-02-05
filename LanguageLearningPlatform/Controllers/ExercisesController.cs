using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExercisesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IExerciseService _exerciseService;

        public ExercisesController(ApplicationDbContext context, IExerciseService exerciseService)
        {
            _context = context;
            _exerciseService = exerciseService;
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitExercise([FromBody] ExerciseSubmissionRequest submission)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var exercise = await _context.Exercises.FindAsync(submission.ExerciseId);

            if (exercise == null)
            {
                return NotFound(new { error = "Exercise not found" });
            }

            // Validate the answer
            var validationResult = await _exerciseService.ValidateAnswerAsync(submission.ExerciseId, submission.UserAnswer);

            // Create the result record
            var result = new UserExerciseResult
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
            };

            _context.UserExerciseResults.Add(result);

            // Update user's course progress if correct
            if (validationResult.IsCorrect)
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

            // Get updated stats
            var stats = await _exerciseService.GetUserExerciseStatsAsync(userId);
            var totalPoints = await GetUserTotalPoints(userId);

            return Ok(new ExerciseSubmissionResponse
            {
                Success = true,
                IsCorrect = validationResult.IsCorrect,
                PointsEarned = validationResult.PointsEarned,
                TotalPoints = totalPoints,
                Feedback = validationResult.Feedback,
                CorrectAnswer = validationResult.CorrectAnswer,
                Explanation = validationResult.Explanation,
                Streak = stats.CurrentStreak,
                LevelUp = false // You can implement level-up logic here
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

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var stats = await _exerciseService.GetUserExerciseStatsAsync(userId);
            return Ok(stats);
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetExerciseHistory([FromQuery] Guid? courseId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var history = await _exerciseService.GetUserExerciseHistoryAsync(userId, courseId);
            return Ok(history);
        }

        [HttpPost("hint/{exerciseId}")]
        [Authorize]
        public async Task<IActionResult> GetHint(Guid exerciseId)
        {
            var exercise = await _exerciseService.GetExerciseByIdAsync(exerciseId);

            if (exercise == null)
            {
                return NotFound();
            }

            return Ok(new { hint = exercise.Hint });
        }

        private async Task<int> GetUserTotalPoints(string userId)
        {
            return await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsEarned);
        }
    }
}