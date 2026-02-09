using LanguageLearningPlatform.Core.Models;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LanguageLearningPlatform.Web.Controllers
{
    public class LessonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LessonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Lessons/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises.OrderBy(e => e.OrderIndex))
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if user is enrolled
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == lesson.CourseId && e.IsActive);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "You must be enrolled in this course to view lessons.";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            // Get other lessons in the course for navigation
            var courseLessons = await _context.Lessons
                .Where(l => l.CourseId == lesson.CourseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            // Prepare exercises for view
            var exercises = lesson.Exercises.OrderBy(e => e.OrderIndex).Select(e => new ExerciseViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Type = e.Type,
                Content = e.Content,
                CorrectAnswer = e.CorrectAnswer,
                Options = string.IsNullOrEmpty(e.Options)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(e.Options) ?? new List<string>(),
                Hint = e.Hint,
                Explanation = e.Explanation,
                Points = e.Points,
                DifficultyLevel = e.DifficultyLevel,
                AudioUrl = e.AudioUrl,
                ImageUrl = e.ImageUrl,
                OrderIndex = e.OrderIndex
            }).ToList();

            ViewBag.Exercises = exercises;
            ViewBag.CourseLessons = courseLessons;
            ViewBag.CurrentLessonIndex = courseLessons.FindIndex(l => l.Id == id);

            return View(lesson);
        }

        public async Task<IActionResult> Exercises(Guid id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises.OrderBy(e => e.OrderIndex))
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check enrollment
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == lesson.CourseId && e.IsActive);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "You must be enrolled in this course to view lessons.";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            // Map to InteractiveExerciseViewModel
            var exercises = lesson.Exercises.OrderBy(e => e.OrderIndex)
                .Select(e => MapToInteractiveViewModel(e, lesson.Course.Language))
                .ToList();

            ViewBag.LessonTitle = lesson.Title;
            ViewBag.LessonId = lesson.Id;

            return View(exercises);
        }

        private InteractiveExerciseViewModel MapToInteractiveViewModel(Exercise e, string courseLanguage)
        {
            var model = new InteractiveExerciseViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Type = e.Type,
                Content = e.Content,
                Hint = e.Hint,
                Points = e.Points,
                DifficultyLevel = e.DifficultyLevel,
                AudioUrl = e.AudioUrl,
                ImageUrl = e.ImageUrl,
                OrderIndex = e.OrderIndex,
                Options = new List<string>()
            };

            // Parse options based on exercise type
            if (!string.IsNullOrEmpty(e.Options))
            {
                try
                {
                    if (e.Type == "MultipleChoice")
                    {
                        model.Options = JsonSerializer.Deserialize<List<string>>(e.Options) ?? new List<string>();
                    }
                    else if (e.Type == "FillInBlank" || e.Type == "FillInTheBlank")
                    {
                        // Word bank - expects JSON array of strings
                        var words = JsonSerializer.Deserialize<List<string>>(e.Options);
                        model.WordBank = words?.Select((w, index) => new WordBankItem
                        {
                            Word = w,
                            Position = index
                        }).ToList();
                    }
                    else if (e.Type == "Matching")
                    {
                        // For matching exercises, parse the MatchingPair objects
                        // Expected format: [{"Left": "Hello", "Right": "Hola", "PairId": "guid"}, ...]
                        model.MatchingPairs = JsonSerializer.Deserialize<List<MatchingPair>>(e.Options);
                    }
                }
                catch (JsonException)
                {
                    // Handle malformed JSON gracefully
                    model.Options = new List<string>();
                }
            }

            // Setup speaking exercise data
            if (e.Type == "Speaking")
            {
                model.SpeakingData = new SpeakingExerciseData
                {
                    TargetPhrase = e.CorrectAnswer,
                    Language = MapLanguageToCode(courseLanguage),
                    MinimumAccuracy = 0.8 // 80% accuracy required
                };
            }

            // Setup listening exercise data
            if (e.Type == "Listening" && !string.IsNullOrEmpty(e.AudioUrl))
            {
                model.ListeningData = new ListeningExerciseData
                {
                    AudioUrl = e.AudioUrl,
                    Transcript = e.CorrectAnswer,
                    Questions = new List<string>() // Can be populated from Options if needed
                };
            }

            return model;
        }

        private string MapLanguageToCode(string language)
        {
            return language?.ToLower() switch
            {
                "spanish" => "es-ES",
                "french" => "fr-FR",
                "german" => "de-DE",
                "japanese" => "ja-JP",
                "italian" => "it-IT",
                "chinese" => "zh-CN",
                _ => "en-US"
            };
        }
    }
}
