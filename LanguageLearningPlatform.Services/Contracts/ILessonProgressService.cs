namespace LanguageLearningPlatform.Services.Contracts
{
    public interface ILessonProgressService
    {
        /// <summary>
        /// Checks whether the lesson completion conditions are met for the user.
        /// Conditions: all required videos watched >= 80 % AND all exercises attempted.
        /// If met and not previously recorded, marks the lesson complete, increments
        /// Progress.CompletedLessons, recalculates course completion %, then triggers
        /// achievement/level checks.
        /// Returns true only when the lesson transitions to complete for the first time.
        /// </summary>
        Task<bool> TryCompleteLessonAsync(string userId, Guid lessonId);

        /// <summary>Returns whether the lesson is already marked complete for this user.</summary>
        Task<bool> IsLessonCompletedAsync(string userId, Guid lessonId);
    }
}