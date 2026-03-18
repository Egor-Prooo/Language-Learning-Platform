namespace LanguageLearningPlatform.Services.Contracts
{
    public interface IAchievementService
    {
        /// <summary>
        /// Checks all achievement conditions for the user and awards any that are now met.
        /// Also updates the user's level based on current total points.
        /// Call this after any action that could trigger an achievement
        /// (exercise submit, lesson complete, course complete).
        /// </summary>
        Task CheckAndAwardAsync(string userId);

        /// <summary>
        /// Recalculates the user's level from their total points and persists the change.
        /// </summary>
        Task UpdateLevelAsync(string userId, int totalPoints);
    }
}