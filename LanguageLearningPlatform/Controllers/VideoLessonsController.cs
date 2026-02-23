using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class VideoLessonsController : ControllerBase
    {
        private readonly IVideoLessonService _videoService;

        public VideoLessonsController(IVideoLessonService videoService)
        {
            _videoService = videoService;
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

            return success ? Ok(new { success = true }) : NotFound();
        }
    }

    public class VideoProgressRequest
    {
        public Guid VideoId { get; set; }
        public int WatchedSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }
}