using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Data.Seeding.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static LanguageLearningPlatform.Data.Seeding.Constants.Constants;

namespace LanguageLearningPlatform.Data.Seeding
{
    public class UserLevelSeeder : IEntitySeeder
    {
        public async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.UserLevels.AnyAsync())
                return;

            var users = await context.Users.ToListAsync();

            foreach (var user in users)
            {
                await context.UserLevels.AddAsync(new UserLevel
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Level = 1,
                    Name = Constants.Constants.DefaultUserLevelName,
                    MinPoints = Constants.Constants.DefaultMinPoints,
                    MaxPoints = Constants.Constants.DefaultMaxPoints,
                    Color = Constants.Constants.DefaultLevelColor,
                    AchievedAt = user.RegistrationDate
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
