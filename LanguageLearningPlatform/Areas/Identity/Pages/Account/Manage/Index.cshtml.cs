#nullable disable

using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LanguageLearningPlatform.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public IndexModel(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult OnGet()
        {
            // Redirect to the custom MVC Profile controller
            return RedirectToAction("Index", "Profile", new { area = "" });
        }

        public IActionResult OnPost()
        {
            return RedirectToAction("Index", "Profile", new { area = "" });
        }
    }
}