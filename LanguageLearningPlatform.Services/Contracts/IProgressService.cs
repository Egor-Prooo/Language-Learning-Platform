using LanguageLearningPlatform.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Services.Contracts
{
    public interface IProgressService
    {
        Task<Progress?> GetUserCourseProgressAsync(string userId, Guid courseId);
        Task<IEnumerable<Progress>> GetUserProgressAsync(string userId);
        Task UpdateProgressAsync(string userId, Guid courseId, int pointsEarned);
        Task<int> GetUserTotalPointsAsync(string userId);

    }
}
