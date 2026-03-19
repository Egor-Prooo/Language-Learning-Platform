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
                courses = courses.Where(c => c.Language == language);

            if (!string.IsNullOrEmpty(level))
                courses = courses.Where(c => c.Level == level);

            ViewBag.SelectedLanguage = language;
            ViewBag.SelectedLevel = level;

            return View(courses);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var course = await _courseService.GetCourseWithLessonsAsync(id);
            if (course == null) return NotFound();

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
            if (userId == null) return Unauthorized();

            var success = await _courseService.EnrollUserInCourseAsync(userId, id);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Successfully enrolled in the course!"
                : "You are already enrolled in this course.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Courses/Unenroll/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unenroll(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var success = await _courseService.UnenrollUserFromCourseAsync(userId, id);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "You have been unenrolled from the course. Your progress has been saved."
                : "Could not unenroll — you may not be enrolled in this course.";

            return RedirectToAction(nameof(MyCourses));
        }

        // GET: Courses/MyCourses
        [Authorize]
        public async Task<IActionResult> MyCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var courses = await _courseService.GetUserEnrolledCoursesAsync(userId);
            var progresses = await _progressService.GetUserProgressAsync(userId);
            var progressDict = progresses.ToDictionary(p => p.CourseId, p => p);

            ViewBag.Progresses = progressDict;

            // Split into active and completed
            var activeCourses = courses.Where(c =>
                !progressDict.ContainsKey(c.Id) ||
                progressDict[c.Id].CompletionPercentage < 100).ToList();

            var completedCourses = courses.Where(c =>
                progressDict.ContainsKey(c.Id) &&
                progressDict[c.Id].CompletionPercentage >= 100).ToList();

            ViewBag.ActiveCourses = activeCourses;
            ViewBag.CompletedCourses = completedCourses;

            return View(courses);
        }
    }
}