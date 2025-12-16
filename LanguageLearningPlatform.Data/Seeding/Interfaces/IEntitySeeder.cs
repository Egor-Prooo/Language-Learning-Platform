using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Data.Seeding.Interfaces
{
    public interface IEntitySeeder
    {
        Task SeedAsync(ApplicationDbContext context);
    }
}
