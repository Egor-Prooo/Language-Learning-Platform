using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static LanguageLearningPlatform.Data.Seeding.Constants.Constants;

namespace LanguageLearningPlatform.Data.Seeding
{
    public class UserSeeder
    {
        private readonly UserManager<User> userManager;

        public UserSeeder(UserManager<User> userManager)
        {
            this.userManager = userManager;
        }

        public async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.Users.AnyAsync())
                return;

            var users = new[]
            {
                new User
                {
                    UserName = "john.doe@example.com",
                    Email = "john.doe@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateTime(1995, 5, 15),
                    EmailConfirmed = true,
                    IsActive = true,
                    PreferredLanguage = Constants.Constants.Spanish,
                    RegistrationDate = DateTime.UtcNow.AddDays(-30)
                },
                new User
                {
                    UserName = "jane.smith@example.com",
                    Email = "jane.smith@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    DateOfBirth = new DateTime(1998, 8, 22),
                    EmailConfirmed = true,
                    IsActive = true,
                    PreferredLanguage = Constants.Constants.French,
                    RegistrationDate = DateTime.UtcNow.AddDays(-45)
                },
                new User
                {
                    UserName = "demo@example.com",
                    Email = "demo@example.com",
                    FirstName = "Demo",
                    LastName = "User",
                    DateOfBirth = new DateTime(2000, 1, 1),
                    EmailConfirmed = true,
                    IsActive = true,
                    PreferredLanguage = Constants.Constants.German,
                    RegistrationDate = DateTime.UtcNow
                }
            };

            foreach (var user in users)
            {
                await userManager.CreateAsync(user, Constants.Constants.DefaultUserPassword);
            }

        }
    }
}
