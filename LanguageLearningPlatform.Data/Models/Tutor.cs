using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LanguageLearningPlatform.Data.Models
{
    public class Tutor
    {
        [Key]
        public Guid Id { get; set; }

        public string Bio { get; set; }

        public double Rating { get; set; }
    }
}
