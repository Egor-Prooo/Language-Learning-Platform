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
        Task<ExerciseViewModel?> GetExerciseByIdAsync(Guid exerciseId);
        Task<ExerciseValidationResult> ValidateAnswerAsync(Guid exerciseId, string userAnswer);
        Task<ExerciseStatsViewModel> GetUserExerciseStatsAsync(string userId);
        Task<IEnumerable<UserExerciseResult>> GetUserExerciseHistoryAsync(string userId, Guid? courseId = null);
    }

    public class ExerciseValidationResult
    {
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
    }
}
