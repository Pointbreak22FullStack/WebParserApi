using Identity;
using Identity.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public LoginRequest Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByNameAsync(Input.Login);
            if (user == null)
            {
                ErrorMessage = "Неверный логин или пароль.";
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Input.Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ErrorMessage = "Неверный логин или пароль.";
                return Page();
            }

            return RedirectToPage("/Index");
        }
    }
}
