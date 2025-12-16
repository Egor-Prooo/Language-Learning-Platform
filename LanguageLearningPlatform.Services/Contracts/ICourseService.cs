using LanguageLearningPlatform.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Services.Contracts
{
    public interface ICourseService
    {
        Task<IEnumerable<Course>> GetAllCoursesAsync();
        Task<IEnumerable<Course>> GetPublishedCoursesAsync();
        Task<Course?> GetCourseByIdAsync(Guid id);
        Task<Course?> GetCourseWithLessonsAsync(Guid id);
        Task<IEnumerable<Course>> GetCoursesByLanguageAsync(string language);
        Task<IEnumerable<Course>> GetCoursesByLevelAsync(string level);
        Task<bool> EnrollUserInCourseAsync(string userId, Guid courseId);
        Task<bool> IsUserEnrolledAsync(string userId, Guid courseId);
        Task<IEnumerable<Course>> GetUserEnrolledCoursesAsync(string userId);
    }
}
