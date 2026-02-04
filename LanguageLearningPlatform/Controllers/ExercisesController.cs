using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        public ExercisesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitExercise([FromBody] ExerciseSubmissionDto submission)
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

            // Create the result record
            var result = new UserExerciseResult
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExerciseId = submission.ExerciseId,
                UserAnswer = submission.UserAnswer,
                IsCorrect = submission.IsCorrect,
                PointsEarned = submission.IsCorrect ? exercise.Points : 0,
                TimeSpentSeconds = submission.TimeSpentSeconds,
                AttemptsCount = submission.AttemptsCount,
                CompletedAt = DateTime.UtcNow,
                Feedback = submission.IsCorrect ? "Correct!" : "Incorrect"
            };

            _context.UserExerciseResults.Add(result);

            // Update user's course progress if correct
            if (submission.IsCorrect)
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

            return Ok(new
            {
                success = true,
                pointsEarned = result.PointsEarned,
                totalPoints = await GetUserTotalPoints(userId),
                isCorrect = result.IsCorrect,
                message = result.IsCorrect ? "Excellent work! 🎉" : "Keep trying!"
            });
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

            var totalExercises = await _context.UserExerciseResults
                .Where(r => r.UserId == userId)
                .CountAsync();

            var correctExercises = await _context.UserExerciseResults
                .Where(r => r.UserId == userId && r.IsCorrect)
                .CountAsync();

            var totalPoints = await GetUserTotalPoints(userId);

            var accuracy = totalExercises > 0
                ? (double)correctExercises / totalExercises * 100
                : 0;

            return Ok(new
            {
                totalExercises,
                correctExercises,
                totalPoints,
                accuracy = Math.Round(accuracy, 2)
            });
        }

        private async Task<int> GetUserTotalPoints(string userId)
        {
            return await _context.Progresses
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsEarned);
        }
    }

    public class ExerciseSubmissionDto
    {
        public Guid ExerciseId { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int AttemptsCount { get; set; }
        public int TimeSpentSeconds { get; set; } = 0;
    }
}