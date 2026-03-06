using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningPlatform.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AchievementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AchievementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Achievements
        public async Task<IActionResult> Index()
        {
            var achievements = await _context.Achievements
                .Include(a => a.UserAchievements)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(achievements);
        }

        // GET: Admin/Achievements/Create
        public IActionResult Create()
        {
            return View("CreateEdit", new Achievement());
        }

        // GET: Admin/Achievements/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement == null) return NotFound();

            return View("CreateEdit", achievement);
        }

        // POST: Admin/Achievements/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Achievement achievement)
        {
            if (!ModelState.IsValid)
                return View("CreateEdit", achievement);

            var isNew = achievement.Id == Guid.Empty;

            if (isNew)
            {
                achievement.Id = Guid.NewGuid();
                achievement.CreatedAt = DateTime.UtcNow;
                _context.Achievements.Add(achievement);
                TempData["SuccessMessage"] = "Achievement created successfully!";
            }
            else
            {
                var existing = await _context.Achievements.FindAsync(achievement.Id);
                if (existing == null) return NotFound();

                existing.Title = achievement.Title;
                existing.Description = achievement.Description;
                existing.Category = achievement.Category;
                existing.PointsRequired = achievement.PointsRequired;
                existing.PointsReward = achievement.PointsReward;
                existing.IsRare = achievement.IsRare;
                existing.IconUrl = achievement.IconUrl;
                existing.TriggerType = achievement.TriggerType;
                existing.TriggerValue = achievement.TriggerValue;

                _context.Achievements.Update(existing);
                TempData["SuccessMessage"] = "Achievement updated successfully!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Achievements/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var achievement = await _context.Achievements
                .Include(a => a.UserAchievements)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (achievement == null) return NotFound();

            if (achievement.UserAchievements.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{achievement.Title}' — it has been unlocked by {achievement.UserAchievements.Count} user(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.Achievements.Remove(achievement);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Achievement deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Achievements/ToggleRare/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRare(Guid id)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement == null) return NotFound();

            achievement.IsRare = !achievement.IsRare;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Achievement marked as {(achievement.IsRare ? "Rare" : "Common")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}