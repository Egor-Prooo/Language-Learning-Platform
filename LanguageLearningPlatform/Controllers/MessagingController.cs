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

        // ── Student Messenger ─────────────────────────────────────────────────

        /// <summary>
        /// Main student messaging page.
        /// Left panel: list of teacher conversations (teacher name + course name).
        /// Right panel: chat for the selected conversation.
        /// </summary>
        public async Task<IActionResult> Index(string? teacherId = null, Guid? courseId = null)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // ── Load existing conversations (grouped by teacher + course) ──────
            var rawMessages = await _context.TeacherMessages
                .Where(m => m.StudentId == studentId)
                .Include(m => m.Teacher)
                .Include(m => m.Course)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var conversations = rawMessages
                .GroupBy(m => (m.TeacherId, m.CourseId))
                .Select(g =>
                {
                    var first = g.First();
                    return new StudentConversationViewModel
                    {
                        TeacherId = g.Key.TeacherId,
                        CourseId = g.Key.CourseId,
                        TeacherName = $"{first.Teacher?.FirstName} {first.Teacher?.LastName}".Trim(),
                        TeacherInitials = Initials(first.Teacher?.FirstName, first.Teacher?.LastName),
                        CourseName = first.Course?.Title ?? "Unknown",
                        CourseLanguage = first.Course?.Language ?? "",
                        CourseLevel = first.Course?.Level ?? "",
                        LastMessage = g.OrderByDescending(m => m.SentAt).First().Message,
                        LastMessageAt = g.Max(m => m.SentAt),
                        UnreadCount = g.Count(m => m.IsFromTeacher && !m.IsRead),
                        Messages = g.OrderBy(m => m.SentAt).ToList()
                    };
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();

            // ── Load courses where student can start a new conversation ────────
            var existingKeys = conversations.Select(c => (c.TeacherId, c.CourseId)).ToHashSet();

            var enrollments = await _context.CourseEnrollments
                .Where(e => e.UserId == studentId && e.IsActive)
                .Include(e => e.Course)
                    .ThenInclude(c => c.CourseTeachers)
                        .ThenInclude(ct => ct.Teacher)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Creator)
                .ToListAsync();

            var newConversationOptions = enrollments
                .Select(e =>
                {
                    var course = e.Course;
                    var teacher = course?.CourseTeachers
                                       .OrderByDescending(ct => ct.IsPrimary)
                                       .FirstOrDefault()?.Teacher
                                   ?? course?.Creator;

                    if (teacher == null || course == null) return null;
                    if (existingKeys.Contains((teacher.Id, course.Id))) return null;

                    return new NewConversationOption
                    {
                        TeacherId = teacher.Id,
                        TeacherName = $"{teacher.FirstName} {teacher.LastName}".Trim(),
                        TeacherInitials = Initials(teacher.FirstName, teacher.LastName),
                        CourseId = course.Id,
                        CourseName = course.Title,
                        CourseLanguage = course.Language,
                        CourseLevel = course.Level
                    };
                })
                .Where(o => o != null)
                .Cast<NewConversationOption>()
                .ToList();

            // ── Auto-select first conversation if none specified ───────────────
            if (string.IsNullOrEmpty(teacherId) && conversations.Any())
            {
                teacherId = conversations.First().TeacherId;
                courseId = conversations.First().CourseId;
            }

            // ── Mark incoming messages as read ────────────────────────────────
            if (!string.IsNullOrEmpty(teacherId) && courseId.HasValue)
            {
                var unread = await _context.TeacherMessages
                    .Where(m => m.StudentId == studentId
                             && m.TeacherId == teacherId
                             && m.CourseId == courseId.Value
                             && m.IsFromTeacher
                             && !m.IsRead)
                    .ToListAsync();

                if (unread.Any())
                {
                    unread.ForEach(m => m.IsRead = true);
                    await _context.SaveChangesAsync();
                }
            }

            // ── Build selected-conversation data ──────────────────────────────
            StudentConversationViewModel? selected = null;
            NewConversationOption? newConv = null;

            if (!string.IsNullOrEmpty(teacherId) && courseId.HasValue)
            {
                selected = conversations.FirstOrDefault(
                    c => c.TeacherId == teacherId && c.CourseId == courseId.Value);

                if (selected == null)
                    newConv = newConversationOptions.FirstOrDefault(
                        o => o.TeacherId == teacherId && o.CourseId == courseId.Value);
            }

            ViewBag.SelectedTeacherId = teacherId;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.Selected = selected;
            ViewBag.NewConv = newConv;
            ViewBag.StudentId = studentId;
            ViewBag.NewConvOptions = newConversationOptions;

            return View(conversations);
        }

        // ── Redirect from "Ask the Teacher" button on course page ─────────────
        public async Task<IActionResult> ContactTeacher(Guid courseId)
        {
            var course = await _context.Courses
                .Include(c => c.CourseTeachers).ThenInclude(ct => ct.Teacher)
                .Include(c => c.Creator)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            var teacher = course.CourseTeachers
                              .OrderByDescending(ct => ct.IsPrimary)
                              .FirstOrDefault()?.Teacher
                          ?? course.Creator;

            if (teacher == null)
            {
                TempData["ErrorMessage"] = "This course has no assigned teacher.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            return RedirectToAction(nameof(Index),
                new { teacherId = teacher.Id, courseId });
        }

        // ── JSON API: get paginated message history for a conversation ─────────
        [HttpGet]
        public async Task<IActionResult> GetMessages(string teacherId, Guid courseId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var messages = await _context.TeacherMessages
                .Where(m => m.StudentId == studentId
                         && m.TeacherId == teacherId
                         && m.CourseId == courseId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    id = m.Id.ToString(),
                    message = m.Message,
                    isFromTeacher = m.IsFromTeacher,
                    sentAt = m.SentAt.ToString("HH:mm"),
                    sentDate = m.SentAt.Date.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Json(messages);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string Initials(string? first, string? last)
        {
            var f = first?.Length > 0 ? first[0].ToString().ToUpper() : "?";
            var l = last?.Length > 0 ? last[0].ToString().ToUpper() : "";
            return f + l;
        }
    }

    // ── View-model types ──────────────────────────────────────────────────────

    public class StudentConversationViewModel
    {
        public string TeacherId { get; set; } = "";
        public Guid CourseId { get; set; }
        public string TeacherName { get; set; } = "";
        public string TeacherInitials { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string CourseLanguage { get; set; } = "";
        public string CourseLevel { get; set; } = "";
        public string LastMessage { get; set; } = "";
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public List<TeacherMessage> Messages { get; set; } = new();
    }

    public class NewConversationOption
    {
        public string TeacherId { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public string TeacherInitials { get; set; } = "";
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = "";
        public string CourseLanguage { get; set; } = "";
        public string CourseLevel { get; set; } = "";
    }
}