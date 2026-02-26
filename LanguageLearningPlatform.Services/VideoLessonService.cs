using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Services
{
    public class VideoLessonService : IVideoLessonService
    {
        private readonly ApplicationDbContext _context;

        public VideoLessonService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VideoLessonViewModel>> GetLessonVideosAsync(Guid lessonId, string? userId = null)
        {
            var videos = await _context.VideoLessons
                .Where(v => v.LessonId == lessonId)
                .OrderBy(v => v.OrderIndex)
                .ToListAsync();

            var result = new List<VideoLessonViewModel>();

            foreach (var video in videos)
            {
                var vm = MapToViewModel(video);

                if (userId != null)
                {
                    var progress = await _context.UserVideoProgresses
                        .FirstOrDefaultAsync(p => p.VideoLessonId == video.Id && p.UserId == userId);

                    if (progress != null)
                    {
                        vm.IsCompleted = progress.IsCompleted;
                        vm.WatchedSeconds = progress.WatchedSeconds;
                    }
                }

                result.Add(vm);
            }

            return result;
        }

        public async Task<VideoLessonViewModel?> GetVideoByIdAsync(Guid videoId, string? userId = null)
        {
            var video = await _context.VideoLessons.FindAsync(videoId);
            if (video == null) return null;

            var vm = MapToViewModel(video);

            if (userId != null)
            {
                var progress = await _context.UserVideoProgresses
                    .FirstOrDefaultAsync(p => p.VideoLessonId == videoId && p.UserId == userId);

                if (progress != null)
                {
                    vm.IsCompleted = progress.IsCompleted;
                    vm.WatchedSeconds = progress.WatchedSeconds;
                }
            }

            return vm;
        }

        public async Task<bool> MarkVideoProgressAsync(Guid videoId, string userId, int watchedSeconds, bool isCompleted)
        {
            var video = await _context.VideoLessons.FindAsync(videoId);
            if (video == null) return false;

            var progress = await _context.UserVideoProgresses
                .FirstOrDefaultAsync(p => p.VideoLessonId == videoId && p.UserId == userId);

            if (progress == null)
            {
                progress = new UserVideoProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    VideoLessonId = videoId,
                    WatchedSeconds = watchedSeconds,
                    IsCompleted = isCompleted,
                    LastWatchedAt = DateTime.UtcNow
                };
                _context.UserVideoProgresses.Add(progress);
            }
            else
            {
                // Only update if new progress is greater
                if (watchedSeconds > progress.WatchedSeconds)
                    progress.WatchedSeconds = watchedSeconds;

                if (isCompleted)
                    progress.IsCompleted = true;

                progress.LastWatchedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<VideoLesson> CreateVideoAsync(VideoLesson video)
        {
            if (video.LessonId == Guid.Empty)
                throw new ArgumentException("LessonId must not be empty.");

            video.Id = Guid.NewGuid();
            video.CreatedAt = DateTime.UtcNow;
            _context.VideoLessons.Add(video);
            await _context.SaveChangesAsync();
            return video;
        }

        public async Task<bool> DeleteVideoAsync(Guid videoId)
        {
            var video = await _context.VideoLessons.FindAsync(videoId);
            if (video == null) return false;

            _context.VideoLessons.Remove(video);
            await _context.SaveChangesAsync();
            return true;
        }

        private VideoLessonViewModel MapToViewModel(VideoLesson video)
        {
            return new VideoLessonViewModel
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                VideoUrl = video.VideoUrl,
                EmbedUrl = BuildEmbedUrl(video.VideoUrl, video.VideoProvider),
                VideoProvider = video.VideoProvider,
                DurationSeconds = video.DurationSeconds,
                OrderIndex = video.OrderIndex,
                IsRequired = video.IsRequired
            };
        }

        private string BuildEmbedUrl(string videoUrl, string provider)
        {
            try
            {
                return provider switch
                {
                    "YouTube" => BuildYouTubeEmbed(videoUrl),
                    "Vimeo" => BuildVimeoEmbed(videoUrl),
                    _ => videoUrl // Direct URL — used in <video> tag
                };
            }
            catch
            {
                return videoUrl;
            }
        }

        private string BuildYouTubeEmbed(string url)
        {
            // Handle youtu.be/ID and youtube.com/watch?v=ID and already-embed URLs
            if (url.Contains("embed/")) return url;

            string? videoId = null;

            if (url.Contains("youtu.be/"))
            {
                videoId = url.Split("youtu.be/")[1].Split('?')[0];
            }
            else if (url.Contains("youtube.com/watch"))
            {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                videoId = query["v"];
            }

            return videoId != null
                ? $"https://www.youtube.com/embed/{videoId}?rel=0&modestbranding=1"
                : url;
        }

        private string BuildVimeoEmbed(string url)
        {
            if (url.Contains("player.vimeo.com")) return url;

            var parts = url.TrimEnd('/').Split('/');
            var videoId = parts.Last();
            return $"https://player.vimeo.com/video/{videoId}";
        }
    }
}