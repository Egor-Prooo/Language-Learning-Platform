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
    [IgnoreAntiforgeryToken]
    public class VideoLessonsController : ControllerBase
    {
        private readonly IVideoLessonService _videoService;
        private readonly ApplicationDbContext _context;
        private readonly ILessonProgressService _lessonProgressService;

        public VideoLessonsController(
            IVideoLessonService videoService,
            ApplicationDbContext context,
            ILessonProgressService lessonProgressService)
        {
            _videoService = videoService;
            _context = context;
            _lessonProgressService = lessonProgressService;
        }

        [HttpGet("lesson/{lessonId}")]
        [Authorize]
        public async Task<IActionResult> GetLessonVideos(Guid lessonId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var videos = await _videoService.GetLessonVideosAsync(lessonId, userId);
            return Ok(videos);
        }

        [HttpPost("progress")]
        [Authorize]
        public async Task<IActionResult> UpdateProgress([FromBody] VideoProgressRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _videoService.MarkVideoProgressAsync(
                request.VideoId, userId, request.WatchedSeconds, request.IsCompleted);

            if (!success) return NotFound();

            // ── After updating video progress, check whether the lesson is now complete ──
            // We need the lesson ID that this video belongs to.
            var lessonId = await _context.VideoLessons
                .Where(v => v.Id == request.VideoId)
                .Select(v => v.LessonId)
                .FirstOrDefaultAsync();

            if (lessonId != Guid.Empty)
                await _lessonProgressService.TryCompleteLessonAsync(userId, lessonId);

            return Ok(new { success = true });
        }
    }

    public class VideoProgressRequest
    {
        public Guid VideoId { get; set; }
        public int WatchedSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }
}