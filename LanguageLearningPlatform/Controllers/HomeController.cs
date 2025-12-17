using System.Diagnostics;
using System.Security.Claims;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ICourseService _courseService;
        private readonly IProgressService _progressService;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            ICourseService courseService,
            IProgressService progressService)
        {
            _logger = logger;
            _context = context;
            _courseService = courseService;
            _progressService = progressService;
        }

        public async Task<IActionResult> Index()
        {
            // Get platform statistics
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.IsPublished);
            ViewBag.TotalUsers = await _context.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalLessons = await _context.Lessons.CountAsync();

            // Get featured courses (top 3 most enrolled)
            var featuredCourses = await _context.Courses
                .Where(c => c.IsPublished)
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(3)
                .ToListAsync();

            ViewBag.FeaturedCourses = featuredCourses;

            // If user is authenticated, show their stats
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userProgress = await _progressService.GetUserProgressAsync(userId!);
                var totalPoints = await _progressService.GetUserTotalPointsAsync(userId!);
                var enrolledCoursesCount = await _context.CourseEnrollments
                    .CountAsync(e => e.UserId == userId && e.IsActive);

                ViewBag.UserTotalPoints = totalPoints;
                ViewBag.UserEnrolledCourses = enrolledCoursesCount;
                ViewBag.UserCompletedLessons = userProgress.Sum(p => p.CompletedLessons);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}