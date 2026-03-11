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
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IProgressService _progressService;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IProgressService progressService)
        {
            _context = context;
            _userManager = userManager;
            _progressService = progressService;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _context.Users
                .Include(u => u.UserLevel)
                .Include(u => u.UserAchievements)
                    .ThenInclude(ua => ua.Achievement)
                .Include(u => u.Progresses)
                    .ThenInclude(p => p.Course)
                .Include(u => u.ExerciseResults)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            var totalPoints = await _progressService.GetUserTotalPointsAsync(userId!);
            var enrolledCoursesCount = await _context.CourseEnrollments
                .CountAsync(e => e.UserId == userId && e.IsActive);
            var completedLessons = user.Progresses.Sum(p => p.CompletedLessons);
            var totalExercises = user.ExerciseResults.Count;
            var correctExercises = user.ExerciseResults.Count(e => e.IsCorrect);
            var accuracyRate = totalExercises > 0
                ? (double)correctExercises / totalExercises * 100
                : 0;

            ViewBag.TotalPoints = totalPoints;
            ViewBag.EnrolledCourses = enrolledCoursesCount;
            ViewBag.CompletedLessons = completedLessons;
            ViewBag.TotalExercises = totalExercises;
            ViewBag.AccuracyRate = accuracyRate;

            ViewBag.RecentAchievements = user.UserAchievements
                .OrderByDescending(ua => ua.UnlockedAt)
                .Take(5)
                .ToList();

            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var recentActivity = await _context.Progresses
                .Where(p => p.UserId == userId && p.LastAccessedAt >= weekAgo)
                .Select(p => p.LastAccessedAt.Date)
                .Distinct()
                .CountAsync();

            ViewBag.LearningStreak = recentActivity;

            // Role-based routing
            if (User.IsInRole("Admin"))
            {
                return await AdminProfile(user, userId!);
            }
            else if (User.IsInRole("Teacher"))
            {
                return await TeacherProfile(user, userId!);
            }
            else
            {
                return View("StudentProfile", user);
            }
        }

        private async Task<IActionResult> AdminProfile(User user, string userId)
        {
            // Platform-wide stats for admin
            ViewBag.TotalPlatformUsers = await _context.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalPlatformCourses = await _context.Courses.CountAsync();
            ViewBag.PublishedCourses = await _context.Courses.CountAsync(c => c.IsPublished);
            ViewBag.TotalEnrollments = await _context.CourseEnrollments.CountAsync(e => e.IsActive);
            ViewBag.TotalExercisesAttempted = await _context.UserExerciseResults.CountAsync();
            ViewBag.TotalForumPosts = await _context.ForumPosts.CountAsync();
            ViewBag.UnreadMessages = await _context.TeacherMessages.CountAsync(m => !m.IsRead);

            // Recent registrations
            ViewBag.RecentUsers = await _context.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .ToListAsync();

            // Most popular courses
            ViewBag.PopularCourses = await _context.Courses
                .Where(c => c.IsPublished)
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(5)
                .ToListAsync();

            return View("AdminProfile", user);
        }

        private async Task<IActionResult> TeacherProfile(User user, string userId)
        {
            // Teacher-specific stats
            var createdCourseIds = await _context.Courses
                .Where(c => c.CreatorId == userId)
                .Select(c => c.Id)
                .ToListAsync();

            var assignedCourseIds = await _context.CourseTeachers
                .Where(ct => ct.TeacherId == userId)
                .Select(ct => ct.CourseId)
                .ToListAsync();

            var allCourseIds = createdCourseIds.Union(assignedCourseIds).ToList();

            var teacherCourses = await _context.Courses
                .Where(c => allCourseIds.Contains(c.Id))
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .ToListAsync();

            ViewBag.TeacherCourses = teacherCourses;
            ViewBag.TotalStudents = teacherCourses
                .SelectMany(c => c.Enrollments)
                .Select(e => e.UserId)
                .Distinct()
                .Count();
            ViewBag.TotalLessonsCreated = teacherCourses.Sum(c => c.Lessons.Count);
            ViewBag.PublishedCourses = teacherCourses.Count(c => c.IsPublished);

            ViewBag.UnreadMessages = await _context.TeacherMessages
                .CountAsync(m => m.TeacherId == userId && !m.IsRead);

            ViewBag.RecentMessages = await _context.TeacherMessages
                .Where(m => m.TeacherId == userId)
                .Include(m => m.Student)
                .Include(m => m.Course)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();

            return View("TeacherProfile", user);
        }

        // GET: Profile/Achievements
        public async Task<IActionResult> Achievements()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userAchievements = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Achievement)
                .OrderByDescending(ua => ua.UnlockedAt)
                .ToListAsync();

            var allAchievements = await _context.Achievements.ToListAsync();
            var unlockedIds = userAchievements.Select(ua => ua.AchievementId).ToHashSet();

            ViewBag.UnlockedAchievements = userAchievements;
            ViewBag.LockedAchievements = allAchievements.Where(a => !unlockedIds.Contains(a.Id)).ToList();

            return View();
        }

        // GET: Profile/Stats
        public async Task<IActionResult> Stats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var progresses = await _progressService.GetUserProgressAsync(userId!);
            var exerciseResults = await _context.UserExerciseResults
                .Where(er => er.UserId == userId)
                .Include(er => er.Exercise)
                    .ThenInclude(e => e.Course)
                .OrderByDescending(er => er.CompletedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.Progresses = progresses;
            ViewBag.RecentExercises = exerciseResults;

            var statsByCourse = progresses.Select(p => new
            {
                CourseName = p.Course.Title,
                Points = p.PointsEarned,
                Completion = p.CompletionPercentage,
                Lessons = p.CompletedLessons
            }).ToList();

            ViewBag.StatsByCourse = statsByCourse;

            return View();
        }
    }
}