using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Services;
using LanguageLearningPlatform.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LanguageLearningPlatform.Tests
{
    // ============================================================
    //  Shared in-memory DB factory
    // ============================================================
    internal static class DbFactory
    {
        public static ApplicationDbContext Create(string name = null)
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(opts);
        }
    }

    // ============================================================
    //  ExerciseService Tests
    // ============================================================
    public class ExerciseServiceTests
    {
        private ApplicationDbContext CreateContext() => DbFactory.Create();

        private Exercise SeedExercise(ApplicationDbContext ctx,
            string type = "MultipleChoice",
            string correct = "Hola",
            string? options = null,
            int points = 10)
        {
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Test Course",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);

            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Lesson 1",
                OrderIndex = 1
            };
            ctx.Lessons.Add(lesson);

            var exercise = new Exercise
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                LessonId = lesson.Id,
                Title = "Exercise",
                Type = type,
                Content = "Question?",
                CorrectAnswer = correct,
                Options = options,
                Points = points,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Exercises.Add(exercise);
            ctx.SaveChanges();
            return exercise;
        }

        // ── Answer validation ─────────────────────────────────────

        [Fact]
        public async Task ValidateAnswer_CorrectAnswer_ReturnsIsCorrectTrue()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);
            var ex = SeedExercise(ctx, correct: "Hola");

            var result = await svc.ValidateAnswerAsync(ex.Id, "Hola");

            Assert.True(result.IsCorrect);
            Assert.Equal(ex.Points, result.PointsEarned);
        }

        [Fact]
        public async Task ValidateAnswer_CaseInsensitive_ReturnsCorrect()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);
            var ex = SeedExercise(ctx, correct: "Hola");

            var result = await svc.ValidateAnswerAsync(ex.Id, "HOLA");

            Assert.True(result.IsCorrect);
        }

        [Fact]
        public async Task ValidateAnswer_WrongAnswer_ReturnsIncorrect()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);
            var ex = SeedExercise(ctx, correct: "Hola");

            var result = await svc.ValidateAnswerAsync(ex.Id, "Adios");

            Assert.False(result.IsCorrect);
            Assert.Equal(0, result.PointsEarned);
            Assert.Equal("Hola", result.CorrectAnswer);
        }

        [Fact]
        public async Task ValidateAnswer_NumberWordNormalization_Passes()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);
            var ex = SeedExercise(ctx, correct: "3 cats");

            var result = await svc.ValidateAnswerAsync(ex.Id, "three cats");

            Assert.True(result.IsCorrect);
        }

        [Fact]
        public async Task ValidateAnswer_WithLeadingTrailingSpaces_Passes()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);
            var ex = SeedExercise(ctx, correct: "hola");

            var result = await svc.ValidateAnswerAsync(ex.Id, "  hola  ");

            Assert.True(result.IsCorrect);
        }

        [Fact]
        public async Task ValidateAnswer_InvalidExerciseId_ReturnsFalse()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);

            var result = await svc.ValidateAnswerAsync(Guid.NewGuid(), "anything");

            Assert.False(result.IsCorrect);
        }

        // ── Lesson exercises ──────────────────────────────────────

        [Fact]
        public async Task GetLessonExercises_ReturnsOrderedExercises()
        {
            var ctx = CreateContext();
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);

            var lesson = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "L", OrderIndex = 1 };
            ctx.Lessons.Add(lesson);

            for (int i = 3; i >= 1; i--)
            {
                ctx.Exercises.Add(new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson.Id,
                    Title = $"Ex {i}",
                    Type = "Translation",
                    Content = "Q",
                    CorrectAnswer = "A",
                    Points = 10,
                    OrderIndex = i,
                    CreatedAt = DateTime.UtcNow
                });
            }
            ctx.SaveChanges();

            var svc = new ExerciseService(ctx);
            var exercises = (await svc.GetLessonExercisesAsync(lesson.Id)).ToList();

            Assert.Equal(3, exercises.Count);
            Assert.True(exercises[0].OrderIndex < exercises[1].OrderIndex);
            Assert.True(exercises[1].OrderIndex < exercises[2].OrderIndex);
        }

        [Fact]
        public async Task GetLessonExercises_EmptyLesson_ReturnsEmpty()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);
            var exercises = await svc.GetLessonExercisesAsync(Guid.NewGuid());
            Assert.Empty(exercises);
        }

        // ── User stats ────────────────────────────────────────────

        [Fact]
        public async Task GetUserExerciseStats_NoResults_ReturnsZeros()
        {
            var ctx = CreateContext();
            var svc = new ExerciseService(ctx);
            var stats = await svc.GetUserExerciseStatsAsync("user-1");
            Assert.Equal(0, stats.TotalExercisesCompleted);
            Assert.Equal(0, stats.CorrectAnswers);
            Assert.Equal(0.0, stats.AccuracyRate);
        }

        [Fact]
        public async Task GetUserExerciseStats_MixedResults_CorrectAccuracy()
        {
            var ctx = CreateContext();
            var exId = Guid.NewGuid();
            ctx.UserExerciseResults.AddRange(
                new UserExerciseResult { Id = Guid.NewGuid(), UserId = "u1", ExerciseId = exId, UserAnswer = "x", IsCorrect = true, PointsEarned = 10, CompletedAt = DateTime.UtcNow },
                new UserExerciseResult { Id = Guid.NewGuid(), UserId = "u1", ExerciseId = exId, UserAnswer = "y", IsCorrect = true, PointsEarned = 10, CompletedAt = DateTime.UtcNow },
                new UserExerciseResult { Id = Guid.NewGuid(), UserId = "u1", ExerciseId = exId, UserAnswer = "z", IsCorrect = false, PointsEarned = 0, CompletedAt = DateTime.UtcNow }
            );
            ctx.SaveChanges();

            var svc = new ExerciseService(ctx);
            var stats = await svc.GetUserExerciseStatsAsync("u1");

            Assert.Equal(3, stats.TotalExercisesCompleted);
            Assert.Equal(2, stats.CorrectAnswers);
            Assert.Equal(1, stats.IncorrectAnswers);
            Assert.InRange(stats.AccuracyRate, 66.6, 66.8);
        }
    }

    // ============================================================
    //  CourseService Tests
    // ============================================================
    public class CourseServiceTests
    {
        private ApplicationDbContext CreateContext() => DbFactory.Create();

        private Guid SeedCourse(ApplicationDbContext ctx, bool published = true, string language = "Spanish")
        {
            var id = Guid.NewGuid();
            ctx.Courses.Add(new Course
            {
                Id = id,
                Title = "Course " + id.ToString()[..4],
                Language = language,
                Level = "Beginner",
                IsPublished = published,
                CreatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
            return id;
        }

        [Fact]
        public async Task GetPublishedCourses_OnlyReturnsPublished()
        {
            var ctx = CreateContext();
            SeedCourse(ctx, published: true);
            SeedCourse(ctx, published: true);
            SeedCourse(ctx, published: false);
            var svc = new CourseService(ctx);

            var courses = await svc.GetPublishedCoursesAsync();

            Assert.Equal(2, courses.Count());
            Assert.All(courses, c => Assert.True(c.IsPublished));
        }

        [Fact]
        public async Task EnrollUserInCourse_NewEnrollment_Succeeds()
        {
            var ctx = CreateContext();
            var courseId = SeedCourse(ctx);
            var svc = new CourseService(ctx);

            var result = await svc.EnrollUserInCourseAsync("user-1", courseId);

            Assert.True(result);
            Assert.True(await svc.IsUserEnrolledAsync("user-1", courseId));
        }

        [Fact]
        public async Task EnrollUserInCourse_AlreadyEnrolled_ReturnsFalse()
        {
            var ctx = CreateContext();
            var courseId = SeedCourse(ctx);
            var svc = new CourseService(ctx);

            await svc.EnrollUserInCourseAsync("user-1", courseId);
            var second = await svc.EnrollUserInCourseAsync("user-1", courseId);

            Assert.False(second);
        }

        [Fact]
        public async Task EnrollUserInCourse_CreatesProgressRecord()
        {
            var ctx = CreateContext();
            var courseId = SeedCourse(ctx);
            var svc = new CourseService(ctx);

            await svc.EnrollUserInCourseAsync("user-1", courseId);

            var progress = ctx.Progresses.FirstOrDefault(p => p.UserId == "user-1" && p.CourseId == courseId);
            Assert.NotNull(progress);
            Assert.Equal(0, progress!.CompletedLessons);
            Assert.Equal(0, progress.CompletionPercentage);
        }

        [Fact]
        public async Task UnenrollUser_ActiveEnrollment_Succeeds()
        {
            var ctx = CreateContext();
            var courseId = SeedCourse(ctx);
            var svc = new CourseService(ctx);

            await svc.EnrollUserInCourseAsync("user-1", courseId);
            var result = await svc.UnenrollUserFromCourseAsync("user-1", courseId);

            Assert.True(result);
            Assert.False(await svc.IsUserEnrolledAsync("user-1", courseId));
        }

        [Fact]
        public async Task UnenrollUser_NotEnrolled_ReturnsFalse()
        {
            var ctx = CreateContext();
            var courseId = SeedCourse(ctx);
            var svc = new CourseService(ctx);

            var result = await svc.UnenrollUserFromCourseAsync("not-enrolled", courseId);

            Assert.False(result);
        }

        [Fact]
        public async Task ReEnroll_AfterUnenroll_Succeeds()
        {
            var ctx = CreateContext();
            var courseId = SeedCourse(ctx);
            var svc = new CourseService(ctx);

            await svc.EnrollUserInCourseAsync("user-1", courseId);
            await svc.UnenrollUserFromCourseAsync("user-1", courseId);
            var result = await svc.EnrollUserInCourseAsync("user-1", courseId);

            Assert.True(result);
            Assert.True(await svc.IsUserEnrolledAsync("user-1", courseId));
        }

        [Fact]
        public async Task GetCoursesByLanguage_FiltersCorrectly()
        {
            var ctx = CreateContext();
            SeedCourse(ctx, language: "Spanish");
            SeedCourse(ctx, language: "Spanish");
            SeedCourse(ctx, language: "French");
            var svc = new CourseService(ctx);

            var courses = await svc.GetCoursesByLanguageAsync("Spanish");

            Assert.Equal(2, courses.Count());
            Assert.All(courses, c => Assert.Equal("Spanish", c.Language));
        }

        [Fact]
        public async Task GetUserEnrolledCourses_ReturnsOnlyActiveCourses()
        {
            var ctx = CreateContext();
            var c1 = SeedCourse(ctx);
            var c2 = SeedCourse(ctx);
            var svc = new CourseService(ctx);

            await svc.EnrollUserInCourseAsync("u", c1);
            await svc.EnrollUserInCourseAsync("u", c2);
            await svc.UnenrollUserFromCourseAsync("u", c2);

            var enrolled = (await svc.GetUserEnrolledCoursesAsync("u")).ToList();
            Assert.Single(enrolled);
            Assert.Equal(c1, enrolled[0].Id);
        }
    }

    // ============================================================
    //  ProgressService Tests
    // ============================================================
    public class ProgressServiceTests
    {
        private ApplicationDbContext CreateContext() => DbFactory.Create();

        private (string userId, Guid courseId) SeedProgress(ApplicationDbContext ctx, int points = 0)
        {
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);

            var progress = new Progress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                CourseId = course.Id,
                PointsEarned = points,
                CompletedLessons = 0,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };
            ctx.Progresses.Add(progress);
            ctx.SaveChanges();
            return ("u1", course.Id);
        }

        [Fact]
        public async Task GetUserCourseProgress_ReturnsCorrectRecord()
        {
            var ctx = CreateContext();
            var (userId, courseId) = SeedProgress(ctx, 50);
            var svc = new ProgressService(ctx);

            var prog = await svc.GetUserCourseProgressAsync(userId, courseId);

            Assert.NotNull(prog);
            Assert.Equal(50, prog!.PointsEarned);
        }

        [Fact]
        public async Task GetUserCourseProgress_NotFound_ReturnsNull()
        {
            var ctx = CreateContext();
            var svc = new ProgressService(ctx);

            var prog = await svc.GetUserCourseProgressAsync("nobody", Guid.NewGuid());

            Assert.Null(prog);
        }

        [Fact]
        public async Task UpdateProgress_AddsPoints()
        {
            var ctx = CreateContext();
            var (userId, courseId) = SeedProgress(ctx, 20);
            var svc = new ProgressService(ctx);

            await svc.UpdateProgressAsync(userId, courseId, 30);

            var prog = await svc.GetUserCourseProgressAsync(userId, courseId);
            Assert.Equal(50, prog!.PointsEarned);
        }

        [Fact]
        public async Task GetUserTotalPoints_SumsAcrossCourses()
        {
            var ctx = CreateContext();
            SeedProgress(ctx, 100);

            var course2 = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C2",
                Language = "French",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course2);
            ctx.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                CourseId = course2.Id,
                PointsEarned = 150,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new ProgressService(ctx);
            var total = await svc.GetUserTotalPointsAsync("u1");

            Assert.Equal(250, total);
        }

        [Fact]
        public async Task GetUserTotalPoints_NoProgress_ReturnsZero()
        {
            var ctx = CreateContext();
            var svc = new ProgressService(ctx);
            Assert.Equal(0, await svc.GetUserTotalPointsAsync("unknown-user"));
        }
    }

    // ============================================================
    //  AchievementService Tests
    // ============================================================
    public class AchievementServiceTests
    {
        private ApplicationDbContext CreateContext() => DbFactory.Create();

        private void SeedUserLevel(ApplicationDbContext ctx, string userId)
        {
            ctx.UserLevels.Add(new UserLevel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Level = 1,
                Name = "Novice",
                MinPoints = 0,
                MaxPoints = 100,
                Color = "#10B981",
                AchievedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        private Achievement SeedAchievement(ApplicationDbContext ctx,
            string triggerType, int triggerValue, int reward = 0)
        {
            var a = new Achievement
            {
                Id = Guid.NewGuid(),
                Title = $"Ach-{triggerType}-{triggerValue}",
                Description = "Test",
                Category = "Points",
                TriggerType = triggerType,
                TriggerValue = triggerValue,
                PointsReward = reward,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Achievements.Add(a);
            ctx.SaveChanges();
            return a;
        }

        [Fact]
        public async Task CheckAndAward_PointsReachedAchievement_Awarded()
        {
            var ctx = CreateContext();
            SeedUserLevel(ctx, "u1");
            var ach = SeedAchievement(ctx, "PointsReached", 50);

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);
            ctx.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                CourseId = course.Id,
                PointsEarned = 100,
                CompletedLessons = 0,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new AchievementService(ctx);
            await svc.CheckAndAwardAsync("u1");

            var awarded = ctx.UserAchievements.Any(ua => ua.UserId == "u1" && ua.AchievementId == ach.Id);
            Assert.True(awarded);
        }

        [Fact]
        public async Task CheckAndAward_BelowThreshold_NotAwarded()
        {
            var ctx = CreateContext();
            SeedUserLevel(ctx, "u1");
            var ach = SeedAchievement(ctx, "PointsReached", 500);

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);
            ctx.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                CourseId = course.Id,
                PointsEarned = 10,
                CompletedLessons = 0,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new AchievementService(ctx);
            await svc.CheckAndAwardAsync("u1");

            Assert.False(ctx.UserAchievements.Any(ua => ua.UserId == "u1"));
        }

        [Fact]
        public async Task CheckAndAward_AlreadyAwarded_NotDuplicated()
        {
            var ctx = CreateContext();
            SeedUserLevel(ctx, "u1");
            var ach = SeedAchievement(ctx, "PointsReached", 10);

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);
            ctx.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                CourseId = course.Id,
                PointsEarned = 100,
                CompletedLessons = 0,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new AchievementService(ctx);
            await svc.CheckAndAwardAsync("u1");
            await svc.CheckAndAwardAsync("u1");

            var count = ctx.UserAchievements.Count(ua => ua.UserId == "u1" && ua.AchievementId == ach.Id);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task UpdateLevel_PromotesCorrectly()
        {
            var ctx = CreateContext();
            SeedUserLevel(ctx, "u1");

            var svc = new AchievementService(ctx);
            await svc.UpdateLevelAsync("u1", 350); // should reach Elementary (300-700)

            var level = ctx.UserLevels.First(l => l.UserId == "u1");
            Assert.Equal(3, level.Level);
            Assert.Equal("Elementary", level.Name);
        }

        [Fact]
        public async Task CheckAndAward_LessonsCompleted_AwardedAtThreshold()
        {
            var ctx = CreateContext();
            SeedUserLevel(ctx, "u1");
            var ach = SeedAchievement(ctx, "LessonsCompleted", 5);

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);
            ctx.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                CourseId = course.Id,
                PointsEarned = 0,
                CompletedLessons = 5,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new AchievementService(ctx);
            await svc.CheckAndAwardAsync("u1");

            Assert.True(ctx.UserAchievements.Any(ua => ua.UserId == "u1" && ua.AchievementId == ach.Id));
        }
    }

    // ============================================================
    //  VideoLessonService Tests
    // ============================================================
    public class VideoLessonServiceTests
    {
        private ApplicationDbContext CreateContext() => DbFactory.Create();

        private (Guid lessonId, Guid courseId) SeedLesson(ApplicationDbContext ctx)
        {
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);
            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "L",
                OrderIndex = 1
            };
            ctx.Lessons.Add(lesson);
            ctx.SaveChanges();
            return (lesson.Id, course.Id);
        }

        [Fact]
        public async Task CreateVideo_StoresCorrectly()
        {
            var ctx = CreateContext();
            var (lessonId, _) = SeedLesson(ctx);
            var svc = new VideoLessonService(ctx);

            var video = await svc.CreateVideoAsync(new VideoLesson
            {
                LessonId = lessonId,
                Title = "Intro Video",
                VideoUrl = "https://youtu.be/abc123",
                VideoProvider = "YouTube",
                DurationSeconds = 300,
                OrderIndex = 1,
                IsRequired = true
            });

            Assert.NotEqual(Guid.Empty, video.Id);
            Assert.Equal("Intro Video", video.Title);
        }

        [Fact]
        public async Task GetLessonVideos_ReturnsOrderedVideos()
        {
            var ctx = CreateContext();
            var (lessonId, _) = SeedLesson(ctx);
            var svc = new VideoLessonService(ctx);

            await svc.CreateVideoAsync(new VideoLesson { LessonId = lessonId, Title = "B", VideoUrl = "u", OrderIndex = 2 });
            await svc.CreateVideoAsync(new VideoLesson { LessonId = lessonId, Title = "A", VideoUrl = "u", OrderIndex = 1 });

            var videos = (await svc.GetLessonVideosAsync(lessonId)).ToList();

            Assert.Equal(2, videos.Count);
            Assert.Equal(1, videos[0].OrderIndex);
            Assert.Equal(2, videos[1].OrderIndex);
        }

        [Fact]
        public async Task MarkVideoProgress_FirstTime_CreatesRecord()
        {
            var ctx = CreateContext();
            var (lessonId, _) = SeedLesson(ctx);
            var svc = new VideoLessonService(ctx);
            var video = await svc.CreateVideoAsync(new VideoLesson
            {
                LessonId = lessonId,
                Title = "V",
                VideoUrl = "u",
                DurationSeconds = 100
            });

            var result = await svc.MarkVideoProgressAsync(video.Id, "user-1", 80, false);

            Assert.True(result);
            var prog = ctx.UserVideoProgresses.First(p => p.VideoLessonId == video.Id && p.UserId == "user-1");
            Assert.Equal(80, prog.WatchedSeconds);
        }

        [Fact]
        public async Task MarkVideoProgress_UpdatesOnlyIfHigher()
        {
            var ctx = CreateContext();
            var (lessonId, _) = SeedLesson(ctx);
            var svc = new VideoLessonService(ctx);
            var video = await svc.CreateVideoAsync(new VideoLesson
            {
                LessonId = lessonId,
                Title = "V",
                VideoUrl = "u",
                DurationSeconds = 200
            });

            await svc.MarkVideoProgressAsync(video.Id, "u1", 90, false);
            await svc.MarkVideoProgressAsync(video.Id, "u1", 50, false); // lower – should not update

            var prog = ctx.UserVideoProgresses.First(p => p.VideoLessonId == video.Id);
            Assert.Equal(90, prog.WatchedSeconds);
        }

        [Fact]
        public async Task MarkVideoProgress_CompletedFlagSticks()
        {
            var ctx = CreateContext();
            var (lessonId, _) = SeedLesson(ctx);
            var svc = new VideoLessonService(ctx);
            var video = await svc.CreateVideoAsync(new VideoLesson
            {
                LessonId = lessonId,
                Title = "V",
                VideoUrl = "u"
            });

            await svc.MarkVideoProgressAsync(video.Id, "u1", 200, true);
            await svc.MarkVideoProgressAsync(video.Id, "u1", 0, false); // should NOT un-complete

            var prog = ctx.UserVideoProgresses.First(p => p.VideoLessonId == video.Id);
            Assert.True(prog.IsCompleted);
        }

        [Fact]
        public async Task DeleteVideo_RemovesFromDb()
        {
            var ctx = CreateContext();
            var (lessonId, _) = SeedLesson(ctx);
            var svc = new VideoLessonService(ctx);
            var video = await svc.CreateVideoAsync(new VideoLesson
            {
                LessonId = lessonId,
                Title = "V",
                VideoUrl = "u"
            });

            await svc.DeleteVideoAsync(video.Id);

            Assert.False(ctx.VideoLessons.Any(v => v.Id == video.Id));
        }

        [Fact]
        public async Task GetVideoById_WithUser_ReturnsProgressInfo()
        {
            var ctx = CreateContext();
            var (lessonId, _) = SeedLesson(ctx);
            var svc = new VideoLessonService(ctx);
            var video = await svc.CreateVideoAsync(new VideoLesson
            {
                LessonId = lessonId,
                Title = "V",
                VideoUrl = "https://youtu.be/xyz",
                DurationSeconds = 400
            });

            await svc.MarkVideoProgressAsync(video.Id, "u1", 200, false);
            var vm = await svc.GetVideoByIdAsync(video.Id, "u1");

            Assert.NotNull(vm);
            Assert.Equal(200, vm!.WatchedSeconds);
        }
    }

    // ============================================================
    //  LessonProgressService Tests
    // ============================================================
    public class LessonProgressServiceTests
    {
        private ApplicationDbContext CreateContext() => DbFactory.Create();

        /// <summary>
        /// Minimal stub achievement service that does nothing – lets us test 
        /// LessonProgressService in isolation.
        /// </summary>
        private class NoOpAchievementService : IAchievementService
        {
            public Task CheckAndAwardAsync(string userId) => Task.CompletedTask;
            public Task UpdateLevelAsync(string userId, int totalPoints) => Task.CompletedTask;
        }

        private (Guid lessonId, Guid courseId, Guid videoId, Guid exerciseId)
            SeedLessonWithContent(ApplicationDbContext ctx)
        {
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "C",
                Language = "Spanish",
                Level = "Beginner",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Courses.Add(course);

            var lesson = new Lesson { Id = Guid.NewGuid(), CourseId = course.Id, Title = "L", OrderIndex = 1 };
            ctx.Lessons.Add(lesson);

            var video = new VideoLesson
            {
                Id = Guid.NewGuid(),
                LessonId = lesson.Id,
                Title = "V",
                VideoUrl = "u",
                DurationSeconds = 100,
                IsRequired = true
            };
            ctx.VideoLessons.Add(video);

            var exercise = new Exercise
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                LessonId = lesson.Id,
                Title = "E",
                Type = "Translation",
                Content = "Q",
                CorrectAnswer = "A",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Exercises.Add(exercise);

            ctx.Progresses.Add(new Progress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                CourseId = course.Id,
                PointsEarned = 0,
                CompletedLessons = 0,
                StartedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });

            ctx.SaveChanges();
            return (lesson.Id, course.Id, video.Id, exercise.Id);
        }

        [Fact]
        public async Task TryCompleteLesson_VideoAndExerciseNotDone_ReturnsFalse()
        {
            var ctx = CreateContext();
            var (lessonId, _, _, _) = SeedLessonWithContent(ctx);
            var svc = new LessonProgressService(ctx, new NoOpAchievementService());

            var result = await svc.TryCompleteLessonAsync("u1", lessonId);

            Assert.False(result);
        }

        [Fact]
        public async Task TryCompleteLesson_VideoWatched80PctAndExerciseDone_ReturnsTrue()
        {
            var ctx = CreateContext();
            var (lessonId, _, videoId, exerciseId) = SeedLessonWithContent(ctx);

            ctx.UserVideoProgresses.Add(new UserVideoProgress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                VideoLessonId = videoId,
                WatchedSeconds = 85,
                IsCompleted = false,
                LastWatchedAt = DateTime.UtcNow
            });
            ctx.UserExerciseResults.Add(new UserExerciseResult
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                ExerciseId = exerciseId,
                UserAnswer = "A",
                IsCorrect = true,
                PointsEarned = 10,
                CompletedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new LessonProgressService(ctx, new NoOpAchievementService());
            var result = await svc.TryCompleteLessonAsync("u1", lessonId);

            Assert.True(result);
        }

        [Fact]
        public async Task TryCompleteLesson_AlreadyCompleted_ReturnsFalse()
        {
            var ctx = CreateContext();
            var (lessonId, _, videoId, exerciseId) = SeedLessonWithContent(ctx);

            ctx.UserVideoProgresses.Add(new UserVideoProgress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                VideoLessonId = videoId,
                WatchedSeconds = 100,
                IsCompleted = true,
                LastWatchedAt = DateTime.UtcNow
            });
            ctx.UserExerciseResults.Add(new UserExerciseResult
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                ExerciseId = exerciseId,
                UserAnswer = "A",
                IsCorrect = true,
                PointsEarned = 10,
                CompletedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new LessonProgressService(ctx, new NoOpAchievementService());
            await svc.TryCompleteLessonAsync("u1", lessonId); // first – succeeds
            var second = await svc.TryCompleteLessonAsync("u1", lessonId); // already complete

            Assert.False(second);
        }

        [Fact]
        public async Task TryCompleteLesson_UpdatesCourseProgress()
        {
            var ctx = CreateContext();
            var (lessonId, courseId, videoId, exerciseId) = SeedLessonWithContent(ctx);

            ctx.UserVideoProgresses.Add(new UserVideoProgress
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                VideoLessonId = videoId,
                WatchedSeconds = 100,
                IsCompleted = true,
                LastWatchedAt = DateTime.UtcNow
            });
            ctx.UserExerciseResults.Add(new UserExerciseResult
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                ExerciseId = exerciseId,
                UserAnswer = "A",
                IsCorrect = true,
                PointsEarned = 10,
                CompletedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            var svc = new LessonProgressService(ctx, new NoOpAchievementService());
            await svc.TryCompleteLessonAsync("u1", lessonId);

            var prog = ctx.Progresses.First(p => p.UserId == "u1" && p.CourseId == courseId);
            Assert.Equal(1, prog.CompletedLessons);
            Assert.Equal(100m, prog.CompletionPercentage); // 1 of 1 lesson
        }

        [Fact]
        public async Task IsLessonCompleted_ReturnsFalseInitially()
        {
            var ctx = CreateContext();
            var (lessonId, _, _, _) = SeedLessonWithContent(ctx);
            var svc = new LessonProgressService(ctx, new NoOpAchievementService());

            Assert.False(await svc.IsLessonCompletedAsync("u1", lessonId));
        }
    }

    // ============================================================
    //  ForumService Tests
    // ============================================================
    public class ForumServiceTests
    {
        private ApplicationDbContext CreateContext() => DbFactory.Create();

        [Fact]
        public async Task CreatePost_StoresCorrectly()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            var post = await svc.CreatePostAsync("user-1", "My Topic", "Some content", "Grammar Help");

            Assert.NotEqual(Guid.Empty, post.Id);
            var stored = ctx.ForumPosts.Find(post.Id);
            Assert.NotNull(stored);
            Assert.Equal("My Topic", stored!.Title);
        }

        [Fact]
        public async Task GetAllPosts_ReturnsPosts()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            await svc.CreatePostAsync("u1", "T1", "C1", "General Discussion");
            await svc.CreatePostAsync("u1", "T2", "C2", "Vocabulary");

            var posts = (await svc.GetAllPostsAsync()).ToList();
            Assert.Equal(2, posts.Count);
        }

        [Fact]
        public async Task AddComment_AttachesToPost()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            var post = await svc.CreatePostAsync("u1", "T", "C", "General Discussion");
            await svc.AddCommentAsync("u2", post.Id, "Great post!");

            var postVm = await svc.GetPostByIdAsync(post.Id);
            Assert.NotNull(postVm);
            Assert.Single(postVm!.Comments);
            Assert.Equal("Great post!", postVm.Comments[0].Content);
        }

        [Fact]
        public async Task LikeComment_IncrementsCount()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            var post = await svc.CreatePostAsync("u1", "T", "C", "Study Tips");
            var comment = await svc.AddCommentAsync("u2", post.Id, "Nice!");

            var likes = await svc.LikeCommentAsync(comment.Id);

            Assert.Equal(1, likes);
        }

        [Fact]
        public async Task DeletePost_AuthorCanDelete()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            var post = await svc.CreatePostAsync("owner", "T", "C", "General Discussion");
            var result = await svc.DeletePostAsync(post.Id, "owner");

            Assert.True(result);
            Assert.False(ctx.ForumPosts.Any(p => p.Id == post.Id));
        }

        [Fact]
        public async Task DeletePost_NonOwnerCannotDelete()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            var post = await svc.CreatePostAsync("owner", "T", "C", "General Discussion");
            var result = await svc.DeletePostAsync(post.Id, "attacker");

            Assert.False(result);
            Assert.True(ctx.ForumPosts.Any(p => p.Id == post.Id));
        }

        [Fact]
        public async Task GetCategories_ReturnsDefaultList()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            var cats = (await svc.GetCategoriesAsync()).ToList();

            Assert.Contains("General Discussion", cats);
            Assert.Contains("Grammar Help", cats);
            Assert.Contains("Vocabulary", cats);
        }

        [Fact]
        public async Task IncrementViews_CounterIncreases()
        {
            var ctx = CreateContext();
            var svc = new ForumService(ctx);

            var post = await svc.CreatePostAsync("u1", "T", "C", "Culture & Travel");
            await svc.IncrementViewsAsync(post.Id);
            await svc.IncrementViewsAsync(post.Id);

            var stored = ctx.ForumPosts.Find(post.Id);
            Assert.Equal(2, stored!.Views);
        }
    }
}