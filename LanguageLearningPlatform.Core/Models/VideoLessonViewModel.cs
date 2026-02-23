namespace LanguageLearningPlatform.Core.Models
{
    public class VideoLessonViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string EmbedUrl { get; set; } = string.Empty;
        public string VideoProvider { get; set; } = "YouTube";
        public int DurationSeconds { get; set; }
        public string DurationFormatted => DurationSeconds > 0
            ? $"{DurationSeconds / 60}:{(DurationSeconds % 60):D2}"
            : "—";
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }
        public bool IsCompleted { get; set; }
        public int WatchedSeconds { get; set; }
        public double WatchProgress => DurationSeconds > 0
            ? Math.Min((double)WatchedSeconds / DurationSeconds * 100, 100)
            : 0;
    }
}