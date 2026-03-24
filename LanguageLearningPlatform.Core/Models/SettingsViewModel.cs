using System.ComponentModel.DataAnnotations;

namespace LanguageLearningPlatform.Core.Models
{
    public class SettingsViewModel
    {
        // ── Profile tab ──────────────────────────────
        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [MaxLength(500)]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        [MaxLength(50)]
        [Display(Name = "Preferred Language")]
        public string? PreferredLanguage { get; set; }

        // ── Security tab ─────────────────────────────
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [MinLength(6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmNewPassword { get; set; }

        // ── Read-only display info ────────────────────
        public string? Email { get; set; }
        public string? ActiveTab { get; set; } = "profile";
    }
}