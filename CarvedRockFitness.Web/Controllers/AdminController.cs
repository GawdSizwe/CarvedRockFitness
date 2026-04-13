using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ApiService apiService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var usersTask       = apiService.GetUsersAsync();
        var suggestionsTask = apiService.GetSuggestionsAsync();
        var bannersTask     = apiService.GetAllBannersAsync();
        var academyTask     = apiService.GetAcademyRegistrationsAsync();
        var driversTask     = apiService.GetAllDriversAdminAsync();
        var driverAppsTask  = apiService.GetDriverApplicationsAsync();
        var reviewsTask     = apiService.GetAllAcademyReviewsAdminAsync();
        var driverRatingsTask = apiService.GetAllDriverRatingsAsync();
        await Task.WhenAll(usersTask, suggestionsTask, bannersTask, academyTask, driversTask, driverAppsTask, reviewsTask, driverRatingsTask);

        ViewBag.Users           = usersTask.Result;
        ViewBag.Suggestions     = suggestionsTask.Result;
        ViewBag.Banners         = bannersTask.Result;
        ViewBag.Academy         = academyTask.Result;
        ViewBag.Drivers         = driversTask.Result;
        ViewBag.DriverApps      = driverAppsTask.Result;
        ViewBag.AcademyReviews  = reviewsTask.Result;
        ViewBag.DriverRatings   = driverRatingsTask.Result;
        ViewBag.DemoVisible     = await apiService.GetDemoVisibleAsync();
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
    public async Task<IActionResult> ToggleDemoData()
    {
        var newValue = await apiService.ToggleDemoDataAsync();

        TempData["Success"] = newValue
            ? "Demo listings are now VISIBLE in the feed."
            : "Demo listings are now HIDDEN — only real client listings show.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> BanUser(string id)
    {
        await apiService.BanUserAsync(id);
        TempData["Success"] = "User ban status updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await apiService.DeleteIdentityUserAsync(id);
        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAcademyStatus(int id, string status, string? assignedRoute)
    {
        await apiService.UpdateAcademyStatusAsync(id, status, assignedRoute);
        TempData["Success"] = "Registration status updated.";
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

    [HttpPost]
    public async Task<IActionResult> ApproveDriverApplication(int id)
    {
        await apiService.ApproveDriverApplicationAsync(id);
        TempData["Success"] = "Application approved — driver added to active roster.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RejectDriverApplication(int id)
    {
        await apiService.RejectDriverApplicationAsync(id);
        TempData["Success"] = "Application rejected.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDriverStatus(int id, string status)
    {
        await apiService.UpdateDriverStatusAsync(id, status);
        TempData["Success"] = "Driver status updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ApproveReview(int id)
    {
        await apiService.ApproveAcademyReviewAsync(id);
        TempData["Success"] = "Review approved and is now public.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteReview(int id)
    {
        await apiService.DeleteAcademyReviewAsync(id);
        TempData["Success"] = "Review deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDriverRating(int id)
    {
        await apiService.DeleteDriverRatingAsync(id);
        TempData["Success"] = "Rating removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateDriver(string fullName, string phone, string route,
        string? vehicle, int capacity, bool policeClearance, string? licenseNumber)
    {
        var dto = new CreateDriverRequest(fullName, phone, route, vehicle, capacity, policeClearance, licenseNumber);
        await apiService.CreateDriverAsync(dto);
        TempData["Success"] = $"Driver {fullName} added to {route} route.";
        return RedirectToAction(nameof(Index));
    }
}
