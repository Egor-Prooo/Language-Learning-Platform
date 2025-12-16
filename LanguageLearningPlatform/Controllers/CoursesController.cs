
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IProgressService _progressService;

        public CoursesController(ICourseService courseService, IProgressService progressService)
        {
            _courseService = courseService;
            _progressService = progressService;
        }

        // GET: Courses
        public async Task<IActionResult> Index(string? language, string? level)
        {
            var courses = await _courseService.GetPublishedCoursesAsync();

            if (!string.IsNullOrEmpty(language))
            {
                courses = courses.Where(c => c.Language == language);
            }

            if (!string.IsNullOrEmpty(level))
            {
                courses = courses.Where(c => c.Level == level);
            }

            ViewBag.SelectedLanguage = language;
            ViewBag.SelectedLevel = level;

            return View(courses);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var course = await _courseService.GetCourseWithLessonsAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.IsEnrolled = await _courseService.IsUserEnrolledAsync(userId!, id);
                ViewBag.Progress = await _progressService.GetUserCourseProgressAsync(userId!, id);
            }

            return View(course);
        }

        // POST: Courses/Enroll/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var success = await _courseService.EnrollUserInCourseAsync(userId, id);

            if (success)
            {
                TempData["SuccessMessage"] = "Successfully enrolled in the course!";
            }
            else
            {
                TempData["ErrorMessage"] = "You are already enrolled in this course.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Courses/MyCourses
        [Authorize]
        public async Task<IActionResult> MyCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var courses = await _courseService.GetUserEnrolledCoursesAsync(userId);
            var progresses = await _progressService.GetUserProgressAsync(userId);

            ViewBag.Progresses = progresses.ToDictionary(p => p.CourseId, p => p);

            return View(courses);
        }
    }
}
