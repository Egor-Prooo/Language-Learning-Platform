using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Services.Contracts
{
    public interface IExerciseService
    {
        Task<IEnumerable<ExerciseViewModel>> GetLessonExercisesAsync(Guid lessonId);
        Task<Exercise?> GetExerciseByIdAsync(Guid id);
        Task<bool> SubmitExerciseAnswerAsync(Guid exerciseId, string userId, string userAnswer);
        Task<ExerciseResultDto> ValidateAnswerAsync(Guid exerciseId, string userAnswer);
        Task<IEnumerable<UserExerciseResult>> GetUserExerciseHistoryAsync(string userId, Guid? courseId = null);
        Task<ExerciseStatsDto> GetUserExerciseStatsAsync(string userId);
    }

    public class ExerciseResultDto
    {
        public bool IsCorrect { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public int PointsEarned { get; set; }
    }

    public class ExerciseStatsDto
    {
        public int TotalExercises { get; set; }
        public int CorrectExercises { get; set; }
        public int TotalPoints { get; set; }
        public double AccuracyRate { get; set; }
        public int CurrentStreak { get; set; }
    }
}
