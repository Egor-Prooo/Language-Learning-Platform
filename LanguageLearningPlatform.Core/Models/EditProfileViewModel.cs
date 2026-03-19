using System.ComponentModel.DataAnnotations;

namespace LanguageLearningPlatform.Core.Models
{
    public class EditProfileViewModel
    {
        [MaxLength(1000)]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [MaxLength(500)]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        [MaxLength(50)]
        [Display(Name = "Preferred Language")]
        public string? PreferredLanguage { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
    }
}