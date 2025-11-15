using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class Exercise
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Course))]
        public string CourseId { get; set; }
        public Course Course { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }

        public string Content { get; set; }

        public string CorrectAnswer { get; set; }

        public int Points { get; set; }
    }
}
