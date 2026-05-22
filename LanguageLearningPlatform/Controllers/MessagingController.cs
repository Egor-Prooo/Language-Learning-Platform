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

        public async Task<IActionResult> Index(string? teacherId = null, Guid? courseId = null)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var allMessages = await _context.TeacherMessages
                .Where(m => m.StudentId == studentId)
                .Include(m => m.Teacher)
                .Include(m => m.Course)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var msgByKey = allMessages
                .GroupBy(m => (m.TeacherId, m.CourseId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var enrollments = await _context.CourseEnrollments
                .Where(e => e.UserId == studentId && e.IsActive)
                .Include(e => e.Course)
                    .ThenInclude(c => c.CourseTeachers)
                        .ThenInclude(ct => ct.Teacher)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Creator)
                .ToListAsync();

            var courseGroups = new List<StudentCourseGroupViewModel>();

            foreach (var enrollment in enrollments)
            {
                var course = enrollment.Course;
                if (course == null) continue;

                var teacherMap = new Dictionary<string, User>();

                foreach (var ct in course.CourseTeachers)
                {
                    if (ct.Teacher != null && !teacherMap.ContainsKey(ct.Teacher.Id))
                        teacherMap[ct.Teacher.Id] = ct.Teacher;
                }

                if (course.Creator != null && !teacherMap.ContainsKey(course.Creator.Id))
                    teacherMap[course.Creator.Id] = course.Creator;

                if (!teacherMap.Any()) continue;

                var teacherItems = new List<StudentTeacherItemViewModel>();

                foreach (var teacher in teacherMap.Values)
                {
                    var convKey = (teacher.Id, course.Id);
                    var msgs = msgByKey.TryGetValue(convKey, out var m) ? m : new List<TeacherMessage>();
                    var lastMsg = msgs.OrderByDescending(x => x.SentAt).FirstOrDefault();

                    teacherItems.Add(new StudentTeacherItemViewModel
                    {
                        TeacherId = teacher.Id,
                        TeacherName = $"{teacher.FirstName} {teacher.LastName}".Trim(),
                        TeacherInitials = Initials(teacher.FirstName, teacher.LastName),
                        CourseId = course.Id,
                        HasMessages = msgs.Any(),
                        LastMessage = lastMsg?.Message ?? "",
                        LastMessageAt = lastMsg?.SentAt,
                        UnreadCount = msgs.Count(x => x.IsFromTeacher && !x.IsRead),
                        Messages = msgs.OrderBy(x => x.SentAt).ToList()
                    });
                }

                courseGroups.Add(new StudentCourseGroupViewModel
                {
                    CourseId = course.Id,
                    CourseName = course.Title,
                    CourseLanguage = course.Language,
                    CourseLevel = course.Level,
                    Teachers = teacherItems
                        .OrderByDescending(t => t.LastMessageAt)
                        .ThenBy(t => t.TeacherName)
                        .ToList()
                });
            }

            courseGroups = courseGroups.OrderBy(cg => cg.CourseName).ToList();

            if (string.IsNullOrEmpty(teacherId))
            {
                var first = courseGroups
                    .SelectMany(cg => cg.Teachers)
                    .Where(t => t.HasMessages)
                    .OrderByDescending(t => t.LastMessageAt)
                    .FirstOrDefault();

                if (first != null)
                {
                    teacherId = first.TeacherId;
                    courseId = first.CourseId;
                }
            }

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

            StudentTeacherItemViewModel? selected = null;
            if (!string.IsNullOrEmpty(teacherId) && courseId.HasValue)
            {
                selected = courseGroups
                    .SelectMany(cg => cg.Teachers)
                    .FirstOrDefault(t => t.TeacherId == teacherId && t.CourseId == courseId.Value);
            }

            ViewBag.SelectedTeacherId = teacherId;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.Selected = selected;
            ViewBag.StudentId = studentId;
            ViewBag.CourseGroups = courseGroups;

            return View(courseGroups);
        }

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

        private static string Initials(string? first, string? last)
        {
            var f = first?.Length > 0 ? first[0].ToString().ToUpper() : "?";
            var l = last?.Length > 0 ? last[0].ToString().ToUpper() : "";
            return f + l;
        }
    }

    public class StudentCourseGroupViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = "";
        public string CourseLanguage { get; set; } = "";
        public string CourseLevel { get; set; } = "";
        public List<StudentTeacherItemViewModel> Teachers { get; set; } = new();
        public int TotalUnread => Teachers.Sum(t => t.UnreadCount);
    }

    public class StudentTeacherItemViewModel
    {
        public string TeacherId { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public string TeacherInitials { get; set; } = "";
        public Guid CourseId { get; set; }
        public bool HasMessages { get; set; }
        public string LastMessage { get; set; } = "";
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public List<TeacherMessage> Messages { get; set; } = new();
    }

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