// LanguageLearningPlatform/Controllers/LessonsApiController.cs
// Add this new file alongside the existing LessonsController.cs

using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    [ApiController]
    [Route("api/lessons")]
    [IgnoreAntiforgeryToken]
    [Authorize]
    public class LessonsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILessonProgressService _lessonProgressService;

        public LessonsApiController(
            ApplicationDbContext context,
            ILessonProgressService lessonProgressService)
        {
            _context = context;
            _lessonProgressService = lessonProgressService;
        }

        // POST api/lessons/{lessonId}/complete
        [HttpPost("{lessonId}/complete")]
        public async Task<IActionResult> CompleteLesson(Guid lessonId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Check enrollment
            var lesson = await _context.Lessons
                .Include(l => l.Exercises)
                .Include(l => l.VideoLessons)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return NotFound(new { success = false, message = "Lesson not found." });

            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == lesson.CourseId && e.IsActive);

            if (!isEnrolled)
                return Forbid();

            // Check if already completed
            var alreadyCompleted = await _lessonProgressService.IsLessonCompletedAsync(userId, lessonId);

            // Try to mark complete (checks all conditions internally)
            var justCompleted = await _lessonProgressService.TryCompleteLessonAsync(userId, lessonId);

            // Determine how many exercises were attempted vs total
            var totalExercises = lesson.Exercises.Count;
            var attemptedExercises = totalExercises > 0
                ? await _context.UserExerciseResults
                    .Where(r => r.UserId == userId && lesson.Exercises.Select(e => e.Id).Contains(r.ExerciseId))
                    .Select(r => r.ExerciseId)
                    .Distinct()
                    .CountAsync()
                : 0;

            var allCorrect = totalExercises > 0
                ? await _context.UserExerciseResults
                    .Where(r => r.UserId == userId
                             && lesson.Exercises.Select(e => e.Id).Contains(r.ExerciseId)
                             && r.IsCorrect)
                    .Select(r => r.ExerciseId)
                    .Distinct()
                    .CountAsync() == totalExercises
                : true;

            // Build next lesson URL
            string? nextLessonUrl = null;
            var courseLessons = await _context.Lessons
                .Where(l => l.CourseId == lesson.CourseId)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new { l.Id, l.OrderIndex })
                .ToListAsync();

            var currentIndex = courseLessons.FindIndex(l => l.Id == lessonId);
            if (currentIndex >= 0 && currentIndex < courseLessons.Count - 1)
            {
                var nextLesson = courseLessons[currentIndex + 1];
                nextLessonUrl = $"/Lessons/Details/{nextLesson.Id}";
            }

            if (justCompleted || alreadyCompleted)
            {
                return Ok(new
                {
                    success = true,
                    status = "completed",
                    message = alreadyCompleted && !justCompleted
                        ? "Lesson was already marked complete."
                        : "Lesson marked as complete!",
                    nextLessonUrl
                });
            }

            // Not fully completed — check why
            if (totalExercises > 0 && attemptedExercises < totalExercises)
            {
                return Ok(new
                {
                    success = false,
                    status = "incomplete",
                    message = $"You have {totalExercises - attemptedExercises} unanswered exercise(s). Answer all exercises to complete the lesson.",
                    nextLessonUrl = (string?)null
                });
            }

            if (totalExercises > 0 && !allCorrect)
            {
                return Ok(new
                {
                    success = false,
                    status = "incomplete",
                    message = "Some exercises were not answered correctly. Complete all exercises correctly to finish the lesson.",
                    nextLessonUrl = (string?)null
                });
            }

            // Has videos that aren't watched enough
            return Ok(new
            {
                success = false,
                status = "incomplete",
                message = "Make sure you've watched all required videos (at least 80%) before finishing.",
                nextLessonUrl = (string?)null
            });
        }

        // GET api/lessons/{lessonId}/status
        [HttpGet("{lessonId}/status")]
        public async Task<IActionResult> GetLessonStatus(Guid lessonId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var lesson = await _context.Lessons
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();

            var isCompleted = await _lessonProgressService.IsLessonCompletedAsync(userId, lessonId);

            var totalExercises = lesson.Exercises.Count;
            var correctlyAnswered = totalExercises > 0
                ? await _context.UserExerciseResults
                    .Where(r => r.UserId == userId
                             && lesson.Exercises.Select(e => e.Id).Contains(r.ExerciseId)
                             && r.IsCorrect)
                    .Select(r => r.ExerciseId)
                    .Distinct()
                    .CountAsync()
                : 0;

            var attempted = totalExercises > 0
                ? await _context.UserExerciseResults
                    .Where(r => r.UserId == userId && lesson.Exercises.Select(e => e.Id).Contains(r.ExerciseId))
                    .Select(r => r.ExerciseId)
                    .Distinct()
                    .CountAsync()
                : 0;

            return Ok(new
            {
                isCompleted,
                totalExercises,
                correctlyAnswered,
                attempted,
                allCorrect = totalExercises == 0 || correctlyAnswered == totalExercises
            });
        }
    }
}
