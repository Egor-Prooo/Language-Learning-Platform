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

        // GET: /Messaging/MyMessages
        public async Task<IActionResult> MyMessages(
            string? selectedTeacherId = null,
            Guid? selectedCourseId = null)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // All existing messages for this student
            var allMessages = await _context.TeacherMessages
                .Where(m => m.StudentId == studentId)
                .Include(m => m.Teacher)
                .Include(m => m.Course)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Build conversations from messages
            var conversations = allMessages
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

            // Enrolled courses without active conversations → available to start new chat
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
                        HasTeacher = primaryTeacher != null,
                        TeacherId = primaryTeacher?.Id ?? string.Empty
                    };
                })
                .ToList();

            // Mark messages as read for selected conversation
            if (!string.IsNullOrEmpty(selectedTeacherId) && selectedCourseId.HasValue)
            {
                var unreadMessages = await _context.TeacherMessages
                    .Where(m => m.StudentId == studentId
                             && m.TeacherId == selectedTeacherId
                             && m.CourseId == selectedCourseId.Value
                             && m.IsFromTeacher
                             && !m.IsRead)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    unreadMessages.ForEach(m => m.IsRead = true);
                    await _context.SaveChangesAsync();
                }
            }

            // Auto-select first conversation if none specified but conversations exist
            if (string.IsNullOrEmpty(selectedTeacherId) && conversations.Any())
            {
                selectedTeacherId = conversations.First().TeacherId;
                selectedCourseId = conversations.First().CourseId;
            }

            ViewBag.SelectedTeacherId = selectedTeacherId;
            ViewBag.SelectedCourseId = selectedCourseId;
            ViewBag.AvailableCourses = availableCourses;

            return View(conversations);
        }

        // GET: /Messaging/ContactTeacher/courseId
        public async Task<IActionResult> ContactTeacher(Guid courseId)
        {
            // Just redirect to MyMessages — the new messenger UI handles everything
            var course = await _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.CourseTeachers)
                    .ThenInclude(ct => ct.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            var courseTeacher = course.CourseTeachers
                .OrderByDescending(ct => ct.IsPrimary)
                .FirstOrDefault();

            var teacher = courseTeacher?.Teacher ?? course.Creator;

            if (teacher == null)
            {
                TempData["ErrorMessage"] = "This course has no assigned teacher.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            return RedirectToAction(nameof(MyMessages),
                new { selectedTeacherId = teacher.Id, selectedCourseId = courseId });
        }

        // POST: /Messaging/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(Guid courseId, string message,
            string? selectedTeacherId = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(MyMessages),
                    new { selectedTeacherId, selectedCourseId = courseId });
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

            return RedirectToAction(nameof(MyMessages),
                new { selectedTeacherId = teacherId, selectedCourseId = courseId });
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
        public string TeacherId { get; set; } = string.Empty;
        public bool HasTeacher { get; set; }
    }
}