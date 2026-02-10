namespace LanguageLearningPlatform.Core.Models
{
    public class ExerciseStatsViewModel
    {
        public int TotalPoints { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalExercisesCompleted { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double AccuracyRate { get; set; }
        public int TodayExercises { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }
}