using LanguageLearningPlatform.Core.Models;
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
        private readonly IAchievementService _achievementService;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IProgressService progressService,
            IAchievementService achievementService)
        {
            _context = context;
            _userManager = userManager;
            _progressService = progressService;
            _achievementService = achievementService;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Run achievement check on every profile load so level/streaks stay current
            await _achievementService.CheckAndAwardAsync(userId!);

            var user = await _context.Users
                .Include(u => u.UserLevel)
                .Include(u => u.UserAchievements)
                    .ThenInclude(ua => ua.Achievement)
                .Include(u => u.Progresses)
                    .ThenInclude(p => p.Course)
                .Include(u => u.ExerciseResults)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            await PopulateProfileViewBag(user, userId!);

            if (User.IsInRole("Admin"))
                return await AdminProfile(user, userId!);
            else if (User.IsInRole("Teacher"))
                return await TeacherProfile(user, userId!);
            else
                return View("StudentProfile", user);
        }

        // GET: Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var vm = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                AvatarUrl = user.ProfilePictureUrl,
                PreferredLanguage = user.PreferredLanguage
            };

            return View(vm);
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Bio = model.Bio;
            user.ProfilePictureUrl = model.AvatarUrl;
            user.PreferredLanguage = model.PreferredLanguage;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
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

            var allAchievements = await _context.Achievements
                .OrderBy(a => a.TriggerValue)
                .ToListAsync();

            var unlockedIds = userAchievements.Select(ua => ua.AchievementId).ToHashSet();

            ViewBag.UnlockedAchievements = userAchievements;
            ViewBag.LockedAchievements = allAchievements
                .Where(a => !unlockedIds.Contains(a.Id))
                .ToList();
            ViewBag.TotalPoints = await _progressService.GetUserTotalPointsAsync(userId!);

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

            var totalExercises = await _context.UserExerciseResults.CountAsync(r => r.UserId == userId);
            var correctExercises = await _context.UserExerciseResults.CountAsync(r => r.UserId == userId && r.IsCorrect);
            ViewBag.TotalExercises = totalExercises;
            ViewBag.CorrectExercises = correctExercises;
            ViewBag.AccuracyRate = totalExercises > 0 ? (double)correctExercises / totalExercises * 100 : 0;
            ViewBag.TotalPoints = await _progressService.GetUserTotalPointsAsync(userId!);

            return View();
        }

        // GET: Profile/Leaderboard
        [AllowAnonymous]
        public async Task<IActionResult> Leaderboard()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Aggregate total points per user from the Progress table
            var pointsPerUser = await _context.Progresses
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalPoints = g.Sum(p => p.PointsEarned),
                    CompletedLessons = g.Sum(p => p.CompletedLessons)
                })
                .OrderByDescending(x => x.TotalPoints)
                .Take(50)
                .ToListAsync();

            var userIds = pointsPerUser.Select(p => p.UserId).ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id) && u.IsActive)
                .Include(u => u.UserLevel)
                .Include(u => u.UserAchievements)
                .ToListAsync();

            var userDict = users.ToDictionary(u => u.Id);

            // Calculate streaks for leaderboard entries
            var allResults = await _context.UserExerciseResults
                .Where(r => userIds.Contains(r.UserId))
                .GroupBy(r => new { r.UserId, Date = r.CompletedAt.Date })
                .Select(g => new { g.Key.UserId, g.Key.Date })
                .ToListAsync();

            var resultsByUser = allResults
                .GroupBy(r => r.UserId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Date).OrderByDescending(d => d).ToList());

            var entries = new List<LeaderboardEntryViewModel>();
            int rank = 1;

            foreach (var pts in pointsPerUser)
            {
                if (!userDict.TryGetValue(pts.UserId, out var user)) continue;

                var streak = CalculateStreak(resultsByUser.GetValueOrDefault(pts.UserId) ?? new List<DateTime>());

                entries.Add(new LeaderboardEntryViewModel
                {
                    Rank = rank++,
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Initials = GetInitials(user.FirstName, user.LastName),
                    TotalPoints = pts.TotalPoints,
                    Level = user.UserLevel?.Level ?? 1,
                    LevelName = user.UserLevel?.Name ?? "Novice",
                    LevelColor = user.UserLevel?.Color ?? "#10B981",
                    CompletedLessons = pts.CompletedLessons,
                    AchievementCount = user.UserAchievements.Count,
                    CurrentStreak = streak,
                    IsCurrentUser = user.Id == currentUserId
                });
            }

            // Current user's rank if they're not in the top 50
            ViewBag.CurrentUserRank = entries.FirstOrDefault(e => e.IsCurrentUser)?.Rank;

            return View(entries);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task PopulateProfileViewBag(User user, string userId)
        {
            var totalPoints = await _progressService.GetUserTotalPointsAsync(userId);
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

            // Streak: count distinct active days in the last 7 days
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var recentActivity = await _context.Progresses
                .Where(p => p.UserId == userId && p.LastAccessedAt >= weekAgo)
                .Select(p => p.LastAccessedAt.Date)
                .Distinct()
                .CountAsync();

            ViewBag.LearningStreak = recentActivity;
        }

        private async Task<IActionResult> AdminProfile(User user, string userId)
        {
            ViewBag.TotalPlatformUsers = await _context.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalPlatformCourses = await _context.Courses.CountAsync();
            ViewBag.PublishedCourses = await _context.Courses.CountAsync(c => c.IsPublished);
            ViewBag.TotalEnrollments = await _context.CourseEnrollments.CountAsync(e => e.IsActive);
            ViewBag.TotalExercisesAttempted = await _context.UserExerciseResults.CountAsync();
            ViewBag.TotalForumPosts = await _context.ForumPosts.CountAsync();
            ViewBag.UnreadMessages = await _context.TeacherMessages.CountAsync(m => !m.IsRead);

            ViewBag.RecentUsers = await _context.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .ToListAsync();

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

        private static int CalculateStreak(List<DateTime> activityDates)
        {
            if (!activityDates.Any()) return 0;

            var today = DateTime.UtcNow.Date;
            if ((today - activityDates.First()).Days > 1) return 0;

            var streak = 0;
            var cursor = today;

            foreach (var date in activityDates)
            {
                if (date == cursor || date == cursor.AddDays(-1))
                {
                    streak++;
                    cursor = date.AddDays(-1);
                }
                else break;
            }
            return streak;
        }

        private static string GetInitials(string firstName, string lastName)
        {
            var f = firstName?.Length > 0 ? firstName[0].ToString().ToUpper() : "?";
            var l = lastName?.Length > 0 ? lastName[0].ToString().ToUpper() : "";
            return f + l;
        }
    }
}