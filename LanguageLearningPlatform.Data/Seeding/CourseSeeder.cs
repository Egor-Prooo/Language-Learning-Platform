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

            var courses = new List<Course>
            {
               new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Beginner Spanish",
                    Language = Constants.Constants.Spanish,
                    Level = Constants.Constants.Beginner,
                    Description = "Start your Spanish learning journey with essential vocabulary, basic grammar, and everyday conversations. Perfect for absolute beginners!",
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
                    Description = "Advance your Spanish skills with complex grammar structures, expanded vocabulary, and real-world conversation practice.",
                    LanguageCode = "ES",
                    EstimatedHours = 35,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-55)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Spanish",
                    Language = Constants.Constants.Spanish,
                    Level = "Advanced",
                    Description = "Master Spanish with advanced literature, professional communication, and native-level fluency exercises.",
                    LanguageCode = "ES",
                    EstimatedHours = 50,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-50)
                },

                // French Courses
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Beginner French",
                    Language = Constants.Constants.French,
                    Level = Constants.Constants.Beginner,
                    Description = "Learn French from scratch with pronunciation basics, essential phrases, and foundational grammar concepts.",
                    LanguageCode = "FR",
                    EstimatedHours = 22,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-45)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Intermediate French",
                    Language = Constants.Constants.French,
                    Level = Constants.Constants.Intermediate,
                    Description = "Develop conversational fluency with intermediate grammar, idiomatic expressions, and practical dialogue scenarios.",
                    LanguageCode = "FR",
                    EstimatedHours = 38,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-40)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced French",
                    Language = Constants.Constants.French,
                    Level = "Advanced",
                    Description = "Achieve mastery with sophisticated vocabulary, literary analysis, and professional-level French communication.",
                    LanguageCode = "FR",
                    EstimatedHours = 48,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-35)
                },

                // German Courses
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Beginner German",
                    Language = Constants.Constants.German,
                    Level = Constants.Constants.Beginner,
                    Description = "Start learning German with basic grammar, essential vocabulary, and fundamental sentence structures.",
                    LanguageCode = "DE",
                    EstimatedHours = 25,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Intermediate German",
                    Language = Constants.Constants.German,
                    Level = Constants.Constants.Intermediate,
                    Description = "Build on your German foundation with complex cases, modal verbs, and practical conversation exercises.",
                    LanguageCode = "DE",
                    EstimatedHours = 40,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced German",
                    Language = Constants.Constants.German,
                    Level = "Advanced",
                    Description = "Master German with advanced literature, business communication, and near-native proficiency exercises.",
                    LanguageCode = "DE",
                    EstimatedHours = 52,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },

                // Japanese Courses
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Beginner Japanese",
                    Language = Constants.Constants.Japanese,
                    Level = Constants.Constants.Beginner,
                    Description = "Master the Japanese writing systems and basic grammar structures. Learn essential phrases for daily communication.",
                    LanguageCode = "JA",
                    EstimatedHours = 30,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Intermediate Japanese",
                    Language = Constants.Constants.Japanese,
                    Level = Constants.Constants.Intermediate,
                    Description = "Expand your Japanese skills with kanji learning, intermediate grammar patterns, and cultural contexts.",
                    LanguageCode = "JA",
                    EstimatedHours = 45,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Japanese",
                    Language = Constants.Constants.Japanese,
                    Level = "Advanced",
                    Description = "Achieve advanced proficiency with complex kanji, business Japanese, and nuanced cultural communication.",
                    LanguageCode = "JA",
                    EstimatedHours = 60,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },

                // Italian Courses
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Beginner Italian",
                    Language = "Italian",
                    Level = Constants.Constants.Beginner,
                    Description = "Learn essential Italian for travel, dining, shopping, and basic conversations. Perfect for your next trip to Italy!",
                    LanguageCode = "IT",
                    EstimatedHours = 18,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Intermediate Italian",
                    Language = "Italian",
                    Level = Constants.Constants.Intermediate,
                    Description = "Develop fluency with everyday Italian conversations, intermediate grammar, and cultural insights.",
                    LanguageCode = "IT",
                    EstimatedHours = 36,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Italian",
                    Language = "Italian",
                    Level = "Advanced",
                    Description = "Master Italian with advanced literature, regional dialects, and professional communication skills.",
                    LanguageCode = "IT",
                    EstimatedHours = 46,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },

                // Chinese Courses
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Beginner Chinese",
                    Language = "Chinese",
                    Level = Constants.Constants.Beginner,
                    Description = "Start your Mandarin journey with pinyin, basic characters, tones, and fundamental conversational skills.",
                    LanguageCode = "ZH",
                    EstimatedHours = 28,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-18)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Intermediate Chinese",
                    Language = "Chinese",
                    Level = Constants.Constants.Intermediate,
                    Description = "Build your character recognition, master complex grammar structures, and improve conversational fluency.",
                    LanguageCode = "ZH",
                    EstimatedHours = 42,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Chinese",
                    Language = "Chinese",
                    Level = "Advanced",
                    Description = "Achieve mastery with advanced characters, business Chinese, and sophisticated reading comprehension.",
                    LanguageCode = "ZH",
                    EstimatedHours = 55,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-9)
                }
            };

            await context.Courses.AddRangeAsync(courses);
            await context.SaveChangesAsync();
        }
    }
}
