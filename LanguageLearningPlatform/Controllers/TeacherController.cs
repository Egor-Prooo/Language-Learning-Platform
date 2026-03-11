using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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

            var createdCourseIds = await _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            var assignedCourseIds = await _context.CourseTeachers
                .Where(ct => ct.TeacherId == teacherId)
                .Select(ct => ct.CourseId)
                .ToListAsync();

            var allCourseIds = createdCourseIds.Union(assignedCourseIds).ToList();

            var teacherCourses = await _context.Courses
                .Where(c => allCourseIds.Contains(c.Id))
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .ToListAsync();

            ViewBag.CreatedCourseIds = createdCourseIds.ToHashSet();
            ViewBag.TotalStudents = teacherCourses.SelectMany(c => c.Enrollments).Select(e => e.UserId).Distinct().Count();
            ViewBag.TotalLessons = teacherCourses.Sum(c => c.Lessons.Count);
            ViewBag.PublishedCourses = teacherCourses.Count(c => c.IsPublished);
            ViewBag.TotalCourses = teacherCourses.Count;

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

        // ── Students ─────────────────────────────────────────────────

        // GET: /Teacher/Students
        public async Task<IActionResult> Students(Guid? courseId = null)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var allCourseIds = await GetAllTeacherCourseIdsAsync(teacherId);

            var enrollmentsQuery = _context.CourseEnrollments
                .Where(e => allCourseIds.Contains(e.CourseId) && e.IsActive)
                .Include(e => e.User)
                .Include(e => e.Course)
                .AsQueryable();

            if (courseId.HasValue)
                enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseId == courseId.Value);

            var enrollments = await enrollmentsQuery.ToListAsync();
            var progressData = await _context.Progresses
                .Where(p => allCourseIds.Contains(p.CourseId))
                .ToListAsync();

            ViewBag.TeacherCourses = await _context.Courses.Where(c => allCourseIds.Contains(c.Id)).ToListAsync();
            ViewBag.SelectedCourseId = courseId;
            ViewBag.ProgressData = progressData.ToDictionary(p => (p.UserId, p.CourseId), p => p);

            return View(enrollments);
        }

        // ── Course management ─────────────────────────────────────────

        // GET: /Teacher/CourseDetails/id
        public async Task<IActionResult> CourseDetails(Guid id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (!await IsTeacherOfCourseAsync(teacherId, id)) return NotFound();

            var course = await _context.Courses
                .Where(c => c.Id == id)
                .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.Exercises)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync();

            if (course == null) return NotFound();

            ViewBag.IsCreator = await _context.Courses.AnyAsync(c => c.Id == id && c.CreatorId == teacherId);

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

        // POST: /Teacher/TogglePublish/id  (creator only)
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

            if (!await IsTeacherOfCourseAsync(teacherId, courseId)) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
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

            if (!await IsTeacherOfCourseAsync(teacherId, courseId)) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId);

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

        // ── Messaging ─────────────────────────────────────────────────

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

            _context.TeacherMessages.Add(new TeacherMessage
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                StudentId = studentId,
                CourseId = courseId,
                Message = message,
                IsFromTeacher = true,
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reply sent!";
            return RedirectToAction(nameof(Messages));
        }

        // ── Student progress ──────────────────────────────────────────

        // GET: /Teacher/StudentProgress/studentId
        public async Task<IActionResult> StudentProgress(string studentId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var allCourseIds = await GetAllTeacherCourseIdsAsync(teacherId);

            var student = await _context.Users.FindAsync(studentId);
            if (student == null) return NotFound();

            var progresses = await _context.Progresses
                .Where(p => p.UserId == studentId && allCourseIds.Contains(p.CourseId))
                .Include(p => p.Course)
                .ToListAsync();

            var exerciseResults = await _context.UserExerciseResults
                .Where(r => r.UserId == studentId)
                .Include(r => r.Exercise)
                    .ThenInclude(e => e.Course)
                .Where(r => allCourseIds.Contains(r.Exercise.CourseId))
                .OrderByDescending(r => r.CompletedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.ExerciseResults = exerciseResults;

            return View(progresses);
        }

        // ── Helpers ──────────────────────────────────────────────────

        private async Task<bool> IsTeacherOfCourseAsync(string teacherId, Guid courseId)
        {
            var isCreator = await _context.Courses.AnyAsync(c => c.Id == courseId && c.CreatorId == teacherId);
            var isAssigned = await _context.CourseTeachers.AnyAsync(ct => ct.CourseId == courseId && ct.TeacherId == teacherId);
            return isCreator || isAssigned;
        }

        private async Task<List<Guid>> GetAllTeacherCourseIdsAsync(string teacherId)
        {
            var createdIds = await _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            var assignedIds = await _context.CourseTeachers
                .Where(ct => ct.TeacherId == teacherId)
                .Select(ct => ct.CourseId)
                .ToListAsync();

            return createdIds.Union(assignedIds).ToList();
        }

        // GET: /Teacher/ManageExercises/lessonId
        public async Task<IActionResult> ManageExercises(Guid lessonId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises.OrderBy(e => e.OrderIndex))
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();
            if (!await IsTeacherOfCourseAsync(teacherId, lesson.CourseId)) return NotFound();

            return View(lesson);
        }

        // GET: /Teacher/AddExercise/lessonId
        public async Task<IActionResult> AddExercise(Guid lessonId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();
            if (!await IsTeacherOfCourseAsync(teacherId, lesson.CourseId)) return NotFound();

            ViewBag.Lesson = lesson;
            ViewBag.LessonId = lessonId;
            ViewBag.CourseName = lesson.Course.Title;
            return View();
        }

        // POST: /Teacher/AddExercise
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExercise(
            Guid lessonId, string title, string type, string content,
            string correctAnswer, string? hint, string? explanation,
            int points, int difficultyLevel,
            // Multiple choice options
            string? opt1, string? opt2, string? opt3, string? opt4,
            // Matching pairs
            string? leftA, string? rightA, string? leftB, string? rightB,
            string? leftC, string? rightC, string? leftD, string? rightD,
            // Audio / image
            string? audioUrl, string? imageUrl)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();
            if (!await IsTeacherOfCourseAsync(teacherId, lesson.CourseId)) return NotFound();

            // Build options JSON
            string? optionsJson = null;

            if (type == "MultipleChoice")
            {
                var opts = new[] { opt1, opt2, opt3, opt4 }
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToList();
                if (opts.Any())
                    optionsJson = System.Text.Json.JsonSerializer.Serialize(opts);
            }
            else if (type == "FillInBlank" || type == "FillInTheBlank")
            {
                // Word bank: reuse opt1-opt4 as word bank items
                var words = new[] { opt1, opt2, opt3, opt4 }
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToList();
                if (words.Any())
                    optionsJson = System.Text.Json.JsonSerializer.Serialize(words);
            }
            else if (type == "Matching")
            {
                var pairs = new List<object>();
                if (!string.IsNullOrWhiteSpace(leftA) && !string.IsNullOrWhiteSpace(rightA))
                    pairs.Add(new { Left = leftA, Right = rightA, PairId = Guid.NewGuid() });
                if (!string.IsNullOrWhiteSpace(leftB) && !string.IsNullOrWhiteSpace(rightB))
                    pairs.Add(new { Left = leftB, Right = rightB, PairId = Guid.NewGuid() });
                if (!string.IsNullOrWhiteSpace(leftC) && !string.IsNullOrWhiteSpace(rightC))
                    pairs.Add(new { Left = leftC, Right = rightC, PairId = Guid.NewGuid() });
                if (!string.IsNullOrWhiteSpace(leftD) && !string.IsNullOrWhiteSpace(rightD))
                    pairs.Add(new { Left = leftD, Right = rightD, PairId = Guid.NewGuid() });

                if (pairs.Any())
                    optionsJson = System.Text.Json.JsonSerializer.Serialize(pairs);

                // Matching exercises always have this placeholder as the correct answer
                correctAnswer = "matched";
            }

            var exercise = new Exercise
            {
                Id = Guid.NewGuid(),
                CourseId = lesson.CourseId,
                LessonId = lessonId,
                Title = title,
                Type = type,
                Content = content,
                CorrectAnswer = correctAnswer,
                Options = optionsJson,
                Hint = string.IsNullOrWhiteSpace(hint) ? null : hint,
                Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation,
                Points = points,
                DifficultyLevel = difficultyLevel,
                AudioUrl = string.IsNullOrWhiteSpace(audioUrl) ? null : audioUrl,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
                OrderIndex = lesson.Exercises.Count + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Exercise added successfully!";
            return RedirectToAction(nameof(ManageExercises), new { lessonId });
        }

        // POST: /Teacher/DeleteExercise
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExercise(Guid exerciseId, Guid lessonId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var exercise = await _context.Exercises
                .Include(e => e.Lesson)
                .FirstOrDefaultAsync(e => e.Id == exerciseId);

            if (exercise == null) return NotFound();
            if (!await IsTeacherOfCourseAsync(teacherId, exercise.CourseId)) return NotFound();

            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Exercise deleted.";
            return RedirectToAction(nameof(ManageExercises), new { lessonId });
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