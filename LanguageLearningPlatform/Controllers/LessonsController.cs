using LanguageLearningPlatform.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Web.Controllers
{
    public class LessonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LessonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Lessons/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises.OrderBy(e => e.OrderIndex))
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if user is enrolled
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == lesson.CourseId && e.IsActive);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "You must be enrolled in this course to view lessons.";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            // Get other lessons in the course for navigation
            var courseLessons = await _context.Lessons
                .Where(l => l.CourseId == lesson.CourseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            ViewBag.CourseLessons = courseLessons;
            ViewBag.CurrentLessonIndex = courseLessons.FindIndex(l => l.Id == id);

            return View(lesson);
        }

    }
}
