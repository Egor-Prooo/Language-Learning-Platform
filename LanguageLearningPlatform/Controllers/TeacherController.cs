using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IProgressService _progressService;

        public TeacherController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IProgressService progressService)
        {
            _context = context;
            _userManager = userManager;
            _progressService = progressService;
        }

        // GET: /Teacher/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var teacherCourses = await _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .ToListAsync();

            var totalStudents = teacherCourses.SelectMany(c => c.Enrollments).Select(e => e.UserId).Distinct().Count();
            var totalLessons = teacherCourses.Sum(c => c.Lessons.Count);
            var publishedCourses = teacherCourses.Count(c => c.IsPublished);

            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalLessons = totalLessons;
            ViewBag.PublishedCourses = publishedCourses;
            ViewBag.TotalCourses = teacherCourses.Count;

            // Recent messages from students
            var recentMessages = await _context.TeacherMessages
                .Where(m => m.TeacherId == teacherId && !m.IsRead)
                .Include(m => m.Student)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();

            ViewBag.UnreadMessages = recentMessages.Count;
            ViewBag.RecentMessages = recentMessages;

            return View(teacherCourses);
        }

        // GET: /Teacher/Students
        public async Task<IActionResult> Students(Guid? courseId = null)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var teacherCourseIds = await _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            var enrollmentsQuery = _context.CourseEnrollments
                .Where(e => teacherCourseIds.Contains(e.CourseId) && e.IsActive)
                .Include(e => e.User)
                .Include(e => e.Course)
                .AsQueryable();

            if (courseId.HasValue)
                enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseId == courseId.Value);

            var enrollments = await enrollmentsQuery.ToListAsync();

            // Get progress for each enrollment
            var progressData = await _context.Progresses
                .Where(p => teacherCourseIds.Contains(p.CourseId))
                .ToListAsync();

            ViewBag.TeacherCourses = await _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .ToListAsync();
            ViewBag.SelectedCourseId = courseId;
            ViewBag.ProgressData = progressData.ToDictionary(p => (p.UserId, p.CourseId), p => p);

            return View(enrollments);
        }

        // GET: /Teacher/CourseDetails/id
        public async Task<IActionResult> CourseDetails(Guid id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var course = await _context.Courses
                .Where(c => c.Id == id && c.CreatorId == teacherId)
                .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.Exercises)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync();

            if (course == null) return NotFound();

            var progresses = await _context.Progresses
                .Where(p => p.CourseId == id)
                .ToListAsync();

            ViewBag.Progresses = progresses.ToDictionary(p => p.UserId, p => p);

            return View(course);
        }

        // GET: /Teacher/CreateCourse
        public IActionResult CreateCourse() => View();

        // POST: /Teacher/CreateCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course model)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = model.Title,
                Language = model.Language,
                Level = model.Level,
                Description = model.Description,
                EstimatedHours = model.EstimatedHours,
                IsPublished = false,
                CreatorId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Course created successfully!";
            return RedirectToAction(nameof(CourseDetails), new { id = course.Id });
        }

        // POST: /Teacher/TogglePublish/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(Guid id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id && c.CreatorId == teacherId);

            if (course == null) return NotFound();

            course.IsPublished = !course.IsPublished;
            course.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = course.IsPublished ? "Course published!" : "Course unpublished.";
            return RedirectToAction(nameof(CourseDetails), new { id });
        }

        // GET: /Teacher/AddLesson/courseId
        public async Task<IActionResult> AddLesson(Guid courseId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.CreatorId == teacherId);
            if (course == null) return NotFound();

            ViewBag.CourseId = courseId;
            ViewBag.CourseName = course.Title;
            return View();
        }

        // POST: /Teacher/AddLesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLesson(Guid courseId, string title, string description, string content, int durationMinutes)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.CreatorId == teacherId);

            if (course == null) return NotFound();

            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Content = content,
                DurationMinutes = durationMinutes,
                CourseId = courseId,
                OrderIndex = course.Lessons.Count + 1
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lesson added successfully!";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        // GET: /Teacher/Messages
        public async Task<IActionResult> Messages()
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var messages = await _context.TeacherMessages
                .Where(m => m.TeacherId == teacherId)
                .Include(m => m.Student)
                .Include(m => m.Course)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            // Mark all as read
            var unread = messages.Where(m => !m.IsRead).ToList();
            unread.ForEach(m => m.IsRead = true);
            if (unread.Any()) await _context.SaveChangesAsync();

            // Group by conversation (student + course)
            var conversations = messages
                .GroupBy(m => (m.StudentId, m.CourseId))
                .Select(g => new TeacherConversationViewModel
                {
                    StudentId = g.Key.StudentId,
                    CourseId = g.Key.CourseId,
                    StudentName = $"{g.First().Student?.FirstName} {g.First().Student?.LastName}".Trim(),
                    CourseName = g.First().Course?.Title ?? "Unknown",
                    LastMessage = g.OrderByDescending(m => m.SentAt).First().Message,
                    LastMessageAt = g.Max(m => m.SentAt),
                    UnreadCount = g.Count(m => !m.IsRead),
                    Messages = g.OrderBy(m => m.SentAt).ToList()
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();

            return View(conversations);
        }

        // POST: /Teacher/ReplyMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(string studentId, Guid courseId, string message)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var reply = new TeacherMessage
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                StudentId = studentId,
                CourseId = courseId,
                Message = message,
                IsFromTeacher = true,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.TeacherMessages.Add(reply);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reply sent!";
            return RedirectToAction(nameof(Messages));
        }

        // GET: /Teacher/StudentProgress/studentId
        public async Task<IActionResult> StudentProgress(string studentId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var student = await _context.Users.FindAsync(studentId);
            if (student == null) return NotFound();

            var teacherCourseIds = await _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            var progresses = await _context.Progresses
                .Where(p => p.UserId == studentId && teacherCourseIds.Contains(p.CourseId))
                .Include(p => p.Course)
                .ToListAsync();

            var exerciseResults = await _context.UserExerciseResults
                .Where(r => r.UserId == studentId)
                .Include(r => r.Exercise)
                    .ThenInclude(e => e.Course)
                .Where(r => teacherCourseIds.Contains(r.Exercise.CourseId))
                .OrderByDescending(r => r.CompletedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.ExerciseResults = exerciseResults;

            return View(progresses);
        }
    }

    public class TeacherConversationViewModel
    {
        public string StudentId { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public List<TeacherMessage> Messages { get; set; } = new();
    }
}