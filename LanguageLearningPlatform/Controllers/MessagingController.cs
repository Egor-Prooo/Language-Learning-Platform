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

            _context.TeacherMessages.Add(new TeacherMessage
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                StudentId = studentId,
                CourseId = courseId,
                Message = message,
                IsFromTeacher = false,
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message sent to teacher!";
            return RedirectToAction(nameof(MyMessages));
        }

        // GET: /Messaging/MyMessages
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
                    CourseLanguage = g.First().Course?.Language ?? string.Empty,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First().Message,
                    LastMessageAt = g.Max(m => m.SentAt),
                    HasUnreadReplies = g.Any(m => m.IsFromTeacher && !m.IsRead),
                    Messages = g.OrderBy(m => m.SentAt).ToList()
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();

            // Courses the student is enrolled in but hasn't messaged anyone about yet
            var existingCourseIds = conversations.Select(c => c.CourseId).ToHashSet();

            var enrolledCourses = await _context.CourseEnrollments
                .Where(e => e.UserId == studentId && e.IsActive && !existingCourseIds.Contains(e.CourseId))
                .Include(e => e.Course)
                    .ThenInclude(c => c.Creator)
                .Include(e => e.Course)
                    .ThenInclude(c => c.CourseTeachers)
                        .ThenInclude(ct => ct.Teacher)
                .ToListAsync();

            var availableCourses = enrolledCourses
                .Where(e => e.Course != null)
                .Select(e =>
                {
                    var primaryTeacher = e.Course.CourseTeachers
                        .OrderByDescending(ct => ct.IsPrimary)
                        .FirstOrDefault()?.Teacher ?? e.Course.Creator;

                    return new AvailableCourseForMessaging
                    {
                        CourseId = e.CourseId,
                        CourseName = e.Course.Title,
                        CourseLanguage = e.Course.Language,
                        CourseLevel = e.Course.Level,
                        TeacherName = primaryTeacher != null
                            ? $"{primaryTeacher.FirstName} {primaryTeacher.LastName}".Trim()
                            : "No teacher assigned",
                        HasTeacher = primaryTeacher != null
                    };
                })
                .ToList();

            ViewBag.AvailableCourses = availableCourses;

            return View(conversations);
        }
    }

    public class StudentConversationViewModel
    {
        public string TeacherId { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseLanguage { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public bool HasUnreadReplies { get; set; }
        public List<TeacherMessage> Messages { get; set; } = new();
    }

    public class AvailableCourseForMessaging
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseLanguage { get; set; } = string.Empty;
        public string CourseLevel { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public bool HasTeacher { get; set; }
    }
}