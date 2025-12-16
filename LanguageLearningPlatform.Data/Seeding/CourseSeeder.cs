using LanguageLearningPlatform.Data.Seeding.Interfaces;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static LanguageLearningPlatform.Data.Seeding.Constants.Constants;

namespace LanguageLearningPlatform.Data.Seeding
{
    public class CourseSeeder : IEntitySeeder
    {
        public async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.Courses.AnyAsync())
                return;

            var courses = new[]
            {
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Spanish for Beginners",
                    Language = Constants.Constants.Spanish,
                    Level = Constants.Constants.Beginner,
                    Description = "Start your Spanish learning journey.",
                    LanguageCode = "ES",
                    EstimatedHours = 20,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Intermediate Spanish",
                    Language = Constants.Constants.Spanish,
                    Level = Constants.Constants.Intermediate,
                    Description = "Advance your Spanish skills.",
                    LanguageCode = "ES",
                    EstimatedHours = 35,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                }
            };

            await context.Courses.AddRangeAsync(courses);
            await context.SaveChangesAsync();

        }
    }
}
