using CarvedRockFitness.Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.Auth.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [EnableRateLimiting("auth-write")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: false, lockoutOnFailure: true);
                if (result.Succeeded)
                    return RedirectToLocal(returnUrl);
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "This account has been temporarily locked due to repeated failed sign-in attempts. Please try again later.");
                    return View(model);
                }
            }
            ModelState.AddModelError("", "Invalid email or password.");
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "FreeUser");
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required, Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
