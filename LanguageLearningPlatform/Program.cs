using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Data.Seeding;
using LanguageLearningPlatform.Services;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDefaultIdentity<User>(options => {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddRoles<IdentityRole>() // Add role support
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // Register services
            builder.Services.AddScoped<ICourseService, CourseService>();
            builder.Services.AddScoped<IProgressService, ProgressService>();
            builder.Services.AddScoped<IExerciseService, ExerciseService>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await DatabaseSeeder.SeedAsync(context, userManager);
                await SeedRolesAndAdminAsync(roleManager, userManager);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "admin",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            await app.RunAsync();
        }

        private static async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            // Create roles
            string[] roleNames = { "Admin", "Teacher", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user
            var adminEmail = "admin@lingolearn.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    EmailConfirmed = true,
                    IsActive = true,
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
