using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Teacher")]
    public class VideoLessonsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVideoLessonService _videoService;

        public VideoLessonsController(ApplicationDbContext context, IVideoLessonService videoService)
        {
            _context = context;
            _videoService = videoService;
        }

        // GET: Admin/VideoLessons/ForLesson/lessonId
        public async Task<IActionResult> ForLesson(Guid lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.VideoLessons.OrderBy(v => v.OrderIndex))
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();

            return View(lesson);
        }

        // GET: Admin/VideoLessons/Create?lessonId=...
        public async Task<IActionResult> Create(Guid lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();

            ViewBag.Lesson = lesson;
            return View();
        }

        // POST: Admin/VideoLessons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Guid lessonId,
            string title,
            string description,
            string videoUrl,
            string videoProvider,
            int durationSeconds,
            int orderIndex,
            bool isRequired)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(videoUrl))
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == lessonId);

                ViewBag.Lesson = lesson;
                ModelState.AddModelError(string.Empty, "Title and Video URL are required.");
                return View(new VideoLesson { LessonId = lessonId });
            }

            var video = new VideoLesson
            {
                LessonId = lessonId,
                Title = title,
                Description = description ?? string.Empty,
                VideoUrl = videoUrl,
                VideoProvider = videoProvider ?? "YouTube",
                DurationSeconds = durationSeconds,
                OrderIndex = orderIndex,
                IsRequired = isRequired
            };

            await _videoService.CreateVideoAsync(video);
            TempData["SuccessMessage"] = "Video added successfully!";
            return RedirectToAction(nameof(ForLesson), new { lessonId });
        }

        // POST: Admin/VideoLessons/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid lessonId)
        {
            await _videoService.DeleteVideoAsync(id);
            TempData["SuccessMessage"] = "Video deleted.";
            return RedirectToAction(nameof(ForLesson), new { lessonId });
        }
    }
}