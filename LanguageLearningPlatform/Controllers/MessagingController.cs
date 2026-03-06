using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    [Authorize]
    public class MessagingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Messaging/ContactTeacher/courseId
        public async Task<IActionResult> ContactTeacher(Guid courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Creator)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            // Find primary assigned teacher, fall back to course creator
            var courseTeacher = await _context.CourseTeachers
                .Include(ct => ct.Teacher)
                .Where(ct => ct.CourseId == courseId)
                .OrderByDescending(ct => ct.IsPrimary)
                .FirstOrDefaultAsync();

            var teacher = courseTeacher?.Teacher ?? course.Creator;

            if (teacher == null)
            {
                TempData["ErrorMessage"] = "This course has no assigned teacher.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var previousMessages = await _context.TeacherMessages
                .Where(m => m.StudentId == studentId && m.CourseId == courseId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.Course = course;
            ViewBag.Teacher = teacher;
            ViewBag.PreviousMessages = previousMessages;

            return View();
        }

        // POST: /Messaging/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(Guid courseId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(ContactTeacher), new { courseId });
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                TempData["ErrorMessage"] = "Course not found.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            // Find assigned teacher or fall back to creator
            var courseTeacher = await _context.CourseTeachers
                .Where(ct => ct.CourseId == courseId)
                .OrderByDescending(ct => ct.IsPrimary)
                .FirstOrDefaultAsync();

            var teacherId = courseTeacher?.TeacherId ?? course.CreatorId;

            if (teacherId == null)
            {
                TempData["ErrorMessage"] = "No teacher found for this course.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var msg = new TeacherMessage
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                StudentId = studentId,
                CourseId = courseId,
                Message = message,
                IsFromTeacher = false,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.TeacherMessages.Add(msg);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message sent to teacher!";
            return RedirectToAction(nameof(ContactTeacher), new { courseId });
        }

        // GET: /Messaging/MyMessages — student inbox showing teacher replies
        public async Task<IActionResult> MyMessages()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var messages = await _context.TeacherMessages
                .Where(m => m.StudentId == studentId)
                .Include(m => m.Teacher)
                .Include(m => m.Course)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => (m.TeacherId, m.CourseId))
                .Select(g => new StudentConversationViewModel
                {
                    TeacherId = g.Key.TeacherId,
                    CourseId = g.Key.CourseId,
                    TeacherName = $"{g.First().Teacher?.FirstName} {g.First().Teacher?.LastName}".Trim(),
                    CourseName = g.First().Course?.Title ?? "Unknown",
                    LastMessage = g.OrderByDescending(m => m.SentAt).First().Message,
                    LastMessageAt = g.Max(m => m.SentAt),
                    HasUnreadReplies = g.Any(m => m.IsFromTeacher && !m.IsRead),
                    Messages = g.OrderBy(m => m.SentAt).ToList()
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();

            return View(conversations);
        }
    }

    public class StudentConversationViewModel
    {
        public string TeacherId { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public bool HasUnreadReplies { get; set; }
        public List<TeacherMessage> Messages { get; set; } = new();
    }
}