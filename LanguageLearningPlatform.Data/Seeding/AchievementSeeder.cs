using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Data.Seeding.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static LanguageLearningPlatform.Data.Seeding.Constants.Constants;

namespace LanguageLearningPlatform.Data.Seeding
{
    public class AchievementSeeder : IEntitySeeder
    {
        public async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.Achievements.AnyAsync())
                return;

            var achievements = new[]
            {
                new Achievement
                {
                    Id = Guid.NewGuid(),
                    Title = "First Steps",
                    Description = "Complete your first lesson",
                    PointsReward = 50,
                    Category = Constants.Constants.Completion,
                    IsRare = false
                },
                new Achievement
                {
                    Id = Guid.NewGuid(),
                    Title = "Perfectionist",
                    Description = "10 correct answers in a row",
                    PointsReward = 150,
                    Category = Constants.Constants.Accuracy,
                    IsRare = true
                }
            };

            await context.Achievements.AddRangeAsync(achievements);
            await context.SaveChangesAsync();
        }

    }
}
