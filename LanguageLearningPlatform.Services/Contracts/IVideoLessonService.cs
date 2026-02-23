using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data.Models;

namespace LanguageLearningPlatform.Services.Contracts
{
    public interface IVideoLessonService
    {
        Task<IEnumerable<VideoLessonViewModel>> GetLessonVideosAsync(Guid lessonId, string? userId = null);
        Task<VideoLessonViewModel?> GetVideoByIdAsync(Guid videoId, string? userId = null);
        Task<bool> MarkVideoProgressAsync(Guid videoId, string userId, int watchedSeconds, bool isCompleted);
        Task<VideoLesson> CreateVideoAsync(VideoLesson video);
        Task<bool> DeleteVideoAsync(Guid videoId);
    }
}