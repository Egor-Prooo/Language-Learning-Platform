namespace LanguageLearningPlatform.Core.Models
{
    public class LeaderboardEntryViewModel
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int Level { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public string LevelColor { get; set; } = "#10B981";
        public int CompletedLessons { get; set; }
        public int AchievementCount { get; set; }
        public int CurrentStreak { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}