// Register.cshtml.cs
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace LanguageLearningPlatform.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            [StringLength(100)]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            [StringLength(100)]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    DateOfBirth = DateTime.UtcNow.AddYears(-18), // Default age
                    RegistrationDate = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true // Set to true for development
                };

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Create initial user level
                    var context = HttpContext.RequestServices.GetRequiredService<LanguageLearningPlatform.Data.ApplicationDbContext>();
                    var userLevel = new UserLevel
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Level = 1,
                        Name = "Novice",
                        MinPoints = 0,
                        MaxPoints = 100,
                        Color = "#10B981",
                        AchievedAt = DateTime.UtcNow
                    };
                    context.UserLevels.Add(userLevel);
                    await context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}

/* Register.cshtml */
/*
@page
@model RegisterModel
@{
    ViewData["Title"] = "Register";
}

<div class="container my-5">
    <div class="row justify-content-center">
        <div class="col-md-6 col-lg-5">
            <div class="course-card">
                <div class="course-card-body">
                    <div class="text-center mb-4">
                        <h1 class="h3 fw-bold">Create Your Account</h1>
                        <p class="text-secondary">Join thousands of language learners</p>
                    </div>

                    <form id="registerForm" asp-route-returnUrl="@Model.ReturnUrl" method="post">
                        <div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>
                        
                        <div class="row g-3 mb-3">
                            <div class="col-md-6">
                                <label asp-for="Input.FirstName" class="form-label">First Name</label>
                                <input asp-for="Input.FirstName" class="form-control form-select-custom" placeholder="John" />
                                <span asp-validation-for="Input.FirstName" class="text-danger small"></span>
                            </div>
                            <div class="col-md-6">
                                <label asp-for="Input.LastName" class="form-label">Last Name</label>
                                <input asp-for="Input.LastName" class="form-control form-select-custom" placeholder="Doe" />
                                <span asp-validation-for="Input.LastName" class="text-danger small"></span>
                            </div>
                        </div>

                        <div class="mb-3">
                            <label asp-for="Input.Email" class="form-label">Email</label>
                            <input asp-for="Input.Email" class="form-control form-select-custom" placeholder="name@example.com" />
                            <span asp-validation-for="Input.Email" class="text-danger small"></span>
                        </div>

                        <div class="mb-3">
                            <label asp-for="Input.Password" class="form-label">Password</label>
                            <input asp-for="Input.Password" class="form-control form-select-custom" placeholder="••••••••" />
                            <span asp-validation-for="Input.Password" class="text-danger small"></span>
                        </div>

                        <div class="mb-4">
                            <label asp-for="Input.ConfirmPassword" class="form-label">Confirm Password</label>
                            <input asp-for="Input.ConfirmPassword" class="form-control form-select-custom" placeholder="••••••••" />
                            <span asp-validation-for="Input.ConfirmPassword" class="text-danger small"></span>
                        </div>

                        <button id="registerSubmit" type="submit" class="btn btn-enroll w-100 mb-3">
                            <i class="fas fa-user-plus me-2"></i>Create Account
                        </button>

                        <div class="text-center">
                            <p class="text-secondary small mb-0">
                                Already have an account? 
                                <a asp-page="./Login" asp-route-returnUrl="@Model.ReturnUrl" class="text-decoration-none fw-semibold">
                                    Sign in
                                </a>
                            </p>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
*/