using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Data.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager)
        {
            await context.Database.MigrateAsync();

            await new UserSeeder(userManager).SeedAsync(context);
            await new CourseSeeder().SeedAsync(context);
            await new AchievementSeeder().SeedAsync(context);
            await new UserLevelSeeder().SeedAsync(context);
        }
    }
}
