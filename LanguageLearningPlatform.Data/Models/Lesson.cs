using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageLearningPlatform.Data.Models
{
    public class Lesson
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty; // Rich text/HTML content

        public int OrderIndex { get; set; } 

        public int DurationMinutes { get; set; }

        public bool IsLocked { get; set; } = false;

        [Required]
        [ForeignKey(nameof(Course))]
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;

        public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
