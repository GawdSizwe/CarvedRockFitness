using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

public class RatingController(ApiService apiService) : Controller
{
    // ── Driver Ratings ─────────────────────────
    public async Task<IActionResult> Drivers()
    {
        var data = await apiService.GetAllDriverRatingsAsync();
        ViewBag.DriverRatings = data;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> RateDriver(int driverId, int stars, string? reviewerName, string? comment)
    {
        var (ok, message) = await apiService.RateDriverAsync(driverId, stars, reviewerName, comment);
        TempData[ok ? "RateSuccess" : "RateError"] = message;
        return RedirectToAction("Drivers");
    }

    // ── Academy Reviews ────────────────────────
    public async Task<IActionResult> Academy()
    {
        var data = await apiService.GetAcademyReviewsAsync();
        ViewBag.Reviews = data;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ReviewAcademy(int stars, string? reviewerName, string? reviewerRole, string? comment)
    {
        var (ok, message) = await apiService.SubmitAcademyReviewAsync(stars, reviewerName, reviewerRole, comment);
        TempData[ok ? "ReviewSuccess" : "ReviewError"] = message;
        return RedirectToAction("Academy");
    }
}
