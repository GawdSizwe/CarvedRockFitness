using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ApiService apiService, IConfiguration config, IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index()
    {
        var usersTask       = apiService.GetUsersAsync();
        var suggestionsTask = apiService.GetSuggestionsAsync();
        var bannersTask     = apiService.GetAllBannersAsync();
        await Task.WhenAll(usersTask, suggestionsTask, bannersTask);

        ViewBag.Users       = usersTask.Result;
        ViewBag.Suggestions = suggestionsTask.Result;
        ViewBag.Banners     = bannersTask.Result;
        ViewBag.DemoVisible = config.GetValue("SeedData:DemoSuggestionsVisible", true);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSuggestion(int id)
    {
        await apiService.DeleteSuggestionAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> PinSuggestion(int id)
    {
        await apiService.PinSuggestionAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> HideSuggestion(int id)
    {
        await apiService.HideSuggestionAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> BoostSuggestion(int id)
    {
        await apiService.BoostSuggestionAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult ToggleDemoData()
    {
        var appsettingsPath = Path.Combine(env.ContentRootPath, "..", "CarvedRockFitness.API", "appsettings.json");
        appsettingsPath = Path.GetFullPath(appsettingsPath);

        var current = config.GetValue("SeedData:DemoSuggestionsVisible", true);
        var newValue = !current;

        if (System.IO.File.Exists(appsettingsPath))
        {
            var json = System.IO.File.ReadAllText(appsettingsPath);
            var updated = System.Text.RegularExpressions.Regex.Replace(
                json,
                "\"DemoSuggestionsVisible\"\\s*:\\s*(true|false)",
                $"\"DemoSuggestionsVisible\": {newValue.ToString().ToLower()}");
            System.IO.File.WriteAllText(appsettingsPath, updated);
        }

        TempData["Success"] = newValue
            ? "Demo listings are now VISIBLE in the feed."
            : "Demo listings are now HIDDEN — only real client listings show.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> BanUser(int id)
    {
        await apiService.BanUserAsync(id);
        TempData["Success"] = "User ban status updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await apiService.DeleteIdentityUserAsync(id);
        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBanner(string businessName, string imageUrl, string? linkUrl, string? tagLine)
    {
        await apiService.CreateBannerAsync(businessName, imageUrl, linkUrl, tagLine);
        TempData["Success"] = $"Banner for '{businessName}' added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleBanner(int id)
    {
        await apiService.ToggleBannerAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBanner(int id)
    {
        await apiService.DeleteBannerAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
