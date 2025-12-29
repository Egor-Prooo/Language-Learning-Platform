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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get statistics
            var totalUsers = await _context.Users.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var publishedCourses = await _context.Courses.CountAsync(c => c.IsPublished);
            var totalEnrollments = await _context.CourseEnrollments.CountAsync();
            var totalLessons = await _context.Lessons.CountAsync();
            var totalExercises = await _context.Exercises.CountAsync();
            var totalAchievements = await _context.Achievements.CountAsync();

            // Recent users
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .ToListAsync();

            // Recent enrollments
            var recentEnrollments = await _context.CourseEnrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .OrderByDescending(e => e.EnrollmentDate)
                .Take(5)
                .ToListAsync();

            // Popular courses
            var popularCourses = await _context.Courses
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.PublishedCourses = publishedCourses;
            ViewBag.TotalEnrollments = totalEnrollments;
            ViewBag.TotalLessons = totalLessons;
            ViewBag.TotalExercises = totalExercises;
            ViewBag.TotalAchievements = totalAchievements;
            ViewBag.RecentUsers = recentUsers;
            ViewBag.RecentEnrollments = recentEnrollments;
            ViewBag.PopularCourses = popularCourses;

            return View();
        }
    }
}
