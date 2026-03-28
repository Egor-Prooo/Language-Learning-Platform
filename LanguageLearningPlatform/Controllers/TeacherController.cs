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

            // Unread messages — just count + a few previews to show badge
            var unreadMessages = await _context.TeacherMessages
                .Where(m => m.TeacherId == teacherId && !m.IsRead && !m.IsFromTeacher)
                .Include(m => m.Student)
                .Include(m => m.Course)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();

            ViewBag.UnreadMessages = await _context.TeacherMessages
                .CountAsync(m => m.TeacherId == teacherId && !m.IsRead && !m.IsFromTeacher);
            ViewBag.RecentMessages = unreadMessages;

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
        public async Task<IActionResult> Messages(string? selectedStudentId = null, Guid? selectedCourseId = null)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var allCourseIds = await GetAllTeacherCourseIdsAsync(teacherId);

            // ── Load ALL enrolled students for each course ────────────────────────
            var enrollments = await _context.CourseEnrollments
                .Where(e => allCourseIds.Contains(e.CourseId) && e.IsActive)
                .Include(e => e.User)
                .Include(e => e.Course)
                .ToListAsync();

            // ── Load all messages ──────────────────────────────────────────────────
            var allMessages = await _context.TeacherMessages
                .Where(m => m.TeacherId == teacherId && allCourseIds.Contains(m.CourseId))
                .Include(m => m.Student)
                .Include(m => m.Course)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // ── Group by course ────────────────────────────────────────────────────
            var msgByCourseAndStudent = allMessages
                .GroupBy(m => (m.CourseId, m.StudentId))
                .ToDictionary(g => g.Key, g => g.ToList());

            // ── Build course groups ────────────────────────────────────────────────
            var courseGroups = enrollments
                .GroupBy(e => e.CourseId)
                .Select(cg =>
                {
                    var firstEnrollment = cg.First();
                    return new TeacherCourseGroupViewModel
                    {
                        CourseId = cg.Key,
                        CourseName = firstEnrollment.Course?.Title ?? "Unknown",
                        CourseLanguage = firstEnrollment.Course?.Language ?? "",
                        CourseLevel = firstEnrollment.Course?.Level ?? "",
                        Students = cg.Select(e =>
                        {
                            var convKey = (cg.Key, e.UserId);
                            var msgs = msgByCourseAndStudent.GetValueOrDefault(convKey) ?? new List<TeacherMessage>();
                            var lastMsg = msgs.OrderByDescending(m => m.SentAt).FirstOrDefault();
                            return new TeacherStudentItemViewModel
                            {
                                StudentId = e.UserId,
                                StudentName = $"{e.User?.FirstName} {e.User?.LastName}".Trim(),
                                StudentInitials = GetInitialsStatic(e.User?.FirstName, e.User?.LastName),
                                CourseId = cg.Key,
                                HasMessages = msgs.Any(),
                                LastMessage = lastMsg?.Message ?? "",
                                LastMessageAt = lastMsg?.SentAt,
                                UnreadCount = msgs.Count(m => !m.IsFromTeacher && !m.IsRead),
                                Messages = msgs.OrderBy(m => m.SentAt).ToList()
                            };
                        })
                        .OrderByDescending(s => s.LastMessageAt)
                        .ThenBy(s => s.StudentName)
                        .ToList()
                    };
                })
                .OrderBy(cg => cg.CourseName)
                .ToList();

            // ── Auto-select first conversation with messages ───────────────────────
            if (string.IsNullOrEmpty(selectedStudentId))
            {
                var firstWithMsg = courseGroups
                    .SelectMany(cg => cg.Students)
                    .Where(s => s.HasMessages)
                    .OrderByDescending(s => s.LastMessageAt)
                    .FirstOrDefault();

                if (firstWithMsg != null)
                {
                    selectedStudentId = firstWithMsg.StudentId;
                    selectedCourseId = firstWithMsg.CourseId;
                }
            }

            // ── Mark as read for selected conversation ─────────────────────────────
            if (!string.IsNullOrEmpty(selectedStudentId) && selectedCourseId.HasValue)
            {
                var unread = await _context.TeacherMessages
                    .Where(m => m.TeacherId == teacherId
                             && m.StudentId == selectedStudentId
                             && m.CourseId == selectedCourseId.Value
                             && !m.IsFromTeacher
                             && !m.IsRead)
                    .ToListAsync();

                if (unread.Any())
                {
                    unread.ForEach(m => m.IsRead = true);
                    await _context.SaveChangesAsync();
                }
            }

            // ── Resolve selected conversation details for right panel ──────────────
            TeacherStudentItemViewModel? selectedConv = null;
            if (!string.IsNullOrEmpty(selectedStudentId) && selectedCourseId.HasValue)
            {
                selectedConv = courseGroups
                    .SelectMany(cg => cg.Students)
                    .FirstOrDefault(s => s.StudentId == selectedStudentId && s.CourseId == selectedCourseId.Value);
            }

            ViewBag.SelectedStudentId = selectedStudentId;
            ViewBag.SelectedCourseId = selectedCourseId;
            ViewBag.SelectedConv = selectedConv;
            ViewBag.TeacherId = teacherId;

            return View(courseGroups);
        }

         //── ADD GetStudentMessages JSON API action ────────────────────────────────────
         [HttpGet]
        public async Task<IActionResult> GetStudentMessages(string studentId, Guid courseId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var messages = await _context.TeacherMessages
                .Where(m => m.TeacherId == teacherId
                         && m.StudentId == studentId
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

         //── REPLACE existing ReplyMessage() action with this ─────────────────────────
         //(Remove existing ReplyMessage - hub handles sending now.Keep empty stub if needed.)
         [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ReplyMessage(string studentId, Guid courseId, string message)
             => RedirectToAction(nameof(Messages), new { selectedStudentId = studentId, selectedCourseId = courseId });

         //── ADD these static helper ───────────────────────────────────────────────────

         private static string GetInitialsStatic(string? first, string? last)
        {
            var f = first?.Length > 0 ? first[0].ToString().ToUpper() : "?";
            var l = last?.Length > 0 ? last[0].ToString().ToUpper() : "";
            return f + l;
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
            string? opt1, string? opt2, string? opt3, string? opt4,
            string? leftA, string? rightA, string? leftB, string? rightB,
            string? leftC, string? rightC, string? leftD, string? rightD,
            string? audioUrl, string? imageUrl)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();
            if (!await IsTeacherOfCourseAsync(teacherId, lesson.CourseId)) return NotFound();

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

    // ── View Models ────────────────────────────────────────────────────────

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

    public class TeacherCourseGroupViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = "";
        public string CourseLanguage { get; set; } = "";
        public string CourseLevel { get; set; } = "";
        public List<TeacherStudentItemViewModel> Students { get; set; } = new();

        public int TotalUnread => Students.Sum(s => s.UnreadCount);
    }

    public class TeacherStudentItemViewModel
    {
        public string StudentId { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string StudentInitials { get; set; } = "";
        public Guid CourseId { get; set; }
        public bool HasMessages { get; set; }
        public string LastMessage { get; set; } = "";
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public List<TeacherMessage> Messages { get; set; } = new();
    }

    /// <summary>
    /// Represents an enrolled student who doesn't yet have an active conversation
    /// with this teacher — shown in the "Start New Conversation" sidebar section.
    /// </summary>
    public class AvailableStudentForMessaging
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseLanguage { get; set; } = string.Empty;
    }
}