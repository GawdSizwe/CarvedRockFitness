using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

public class AccountController : Controller
{
    public IActionResult Login(string? returnUrl = null)
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = returnUrl ?? "/"
        }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult AccessDenied() => View();

    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Profile()
    {
        return View();
    }
}
