namespace LanguageLearningPlatform.Core.Models
{
    public class InteractiveExerciseViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string? Hint { get; set; }
        public int Points { get; set; }
        public int DifficultyLevel { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public int OrderIndex { get; set; }

        // Interactive exercise specific properties
        public List<WordBankItem>? WordBank { get; set; }
        public List<MatchingPair>? MatchingPairs { get; set; }
        public SpeakingExerciseData? SpeakingData { get; set; }
        public ListeningExerciseData? ListeningData { get; set; }
    }

    public class WordBankItem
    {
        public string Word { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    public class MatchingPair
    {
        public string Left { get; set; } = string.Empty;
        public string Right { get; set; } = string.Empty;
        public Guid PairId { get; set; }
    }

    public class SpeakingExerciseData
    {
        public string TargetPhrase { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double MinimumAccuracy { get; set; } = 0.7;
    }

    public class ListeningExerciseData
    {
        public string AudioUrl { get; set; } = string.Empty;
        public string Transcript { get; set; } = string.Empty;
        public List<string> Questions { get; set; } = new();
    }

    public class ExerciseSubmissionRequest
    {
        public Guid ExerciseId { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
        public int TimeSpentSeconds { get; set; }
        public int AttemptsCount { get; set; } = 1;
    }

    public class ExerciseSubmissionResponse
    {
        public bool Success { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public int TotalPoints { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public int Streak { get; set; }
        public bool LevelUp { get; set; }
    }
}