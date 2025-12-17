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

            if (user == null)
            {
                return NotFound();
            }

            // Get statistics
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

            // Get recent achievements (last 5)
            ViewBag.RecentAchievements = user.UserAchievements
                .OrderByDescending(ua => ua.UnlockedAt)
                .Take(5)
                .ToList();

            // Get learning streak (simplified - days with activity in last 7 days)
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var recentActivity = await _context.Progresses
                .Where(p => p.UserId == userId && p.LastAccessedAt >= weekAgo)
                .Select(p => p.LastAccessedAt.Date)
                .Distinct()
                .CountAsync();

            ViewBag.LearningStreak = recentActivity;

            return View(user);
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

            // Calculate stats by course
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