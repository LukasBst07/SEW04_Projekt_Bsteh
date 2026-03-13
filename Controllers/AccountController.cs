using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ==================== LOGIN ====================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Schon eingeloggt? Ab zum Dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Dashboard", "Game");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Dashboard", "Game");
            }

            ModelState.AddModelError(string.Empty, "E-Mail oder Passwort ist falsch.");
            return View(model);
        }

        // ==================== REGISTER ====================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Dashboard", "Game");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Rolle "Spieler" zuweisen
                await _userManager.AddToRoleAsync(user, "Spieler");

                // Direkt einloggen
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Dashboard", "Game");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==================== LOGOUT ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}