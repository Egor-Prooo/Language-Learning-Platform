using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.UserLevel)
                .Include(u => u.Progresses)
                .OrderByDescending(u => u.RegistrationDate)
                .ToListAsync();

            var usersWithRoles = new List<(User User, IList<string> Roles)>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add((user, roles));
            }

            ViewBag.UsersWithRoles = usersWithRoles;

            return View(users);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var user = await _context.Users
                .Include(u => u.UserLevel)
                .Include(u => u.Progresses)
                    .ThenInclude(p => p.Course)
                .Include(u => u.UserAchievements)
                    .ThenInclude(ua => ua.Achievement)
                .Include(u => u.ExerciseResults)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRoles = roles;

            var enrollments = await _context.CourseEnrollments
                .Where(e => e.UserId == id)
                .Include(e => e.Course)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;

            return View(user);
        }

        // POST: Admin/Users/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/AssignRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["SuccessMessage"] = $"Role '{role}' assigned successfully!";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/Users/RemoveRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
                TempData["SuccessMessage"] = $"Role '{role}' removed successfully!";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/Users/CreateTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(
            string FirstName,
            string LastName,
            string Email,
            string? Specialization,
            string? TeacherBio,
            string TempPassword)
        {
            if (string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(TempPassword))
            {
                TempData["ErrorMessage"] = "First name, last name, email and password are required.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _userManager.FindByEmailAsync(Email);
            if (existing != null)
            {
                TempData["ErrorMessage"] = $"A user with the email '{Email}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            var teacher = new User
            {
                UserName = Email,
                Email = Email,
                FirstName = FirstName,
                LastName = LastName,
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                RegistrationDate = DateTime.UtcNow,
                EmailConfirmed = true,
                IsActive = true,
                IsVerifiedTeacher = true,
                Specialization = Specialization,
                TeacherBio = TeacherBio
            };

            var result = await _userManager.CreateAsync(teacher, TempPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Could not create teacher account: {errors}";
                return RedirectToAction(nameof(Index));
            }

            await EnsureRoleExistsAsync("Teacher");
            await _userManager.AddToRoleAsync(teacher, "Teacher");

            await _context.UserLevels.AddAsync(new UserLevel
            {
                Id = Guid.NewGuid(),
                UserId = teacher.Id,
                Level = 1,
                Name = "Beginner",
                MinPoints = 0,
                MaxPoints = 100,
                Color = "#10B981",
                AchievedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Teacher account for {FirstName} {LastName} created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            var roleManager = HttpContext.RequestServices
                .GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}