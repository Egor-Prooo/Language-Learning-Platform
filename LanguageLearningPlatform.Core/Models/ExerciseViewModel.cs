using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Core.Models
{
    public class ExerciseViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string? Hint { get; set; }
        public string? Explanation { get; set; }
        public int Points { get; set; }
        public int DifficultyLevel { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public int OrderIndex { get; set; }
    }
}
