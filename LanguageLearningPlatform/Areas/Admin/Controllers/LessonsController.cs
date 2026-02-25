using LanguageLearningPlatform.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Teacher")]
    public class LessonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LessonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ForCourse(Guid courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.VideoLessons)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Exercises)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            return View(course);
        }
    }
}