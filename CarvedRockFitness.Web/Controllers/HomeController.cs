using CarvedRockFitness.Web.Models;
using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiService _apiService;

    public HomeController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        List<SuggestionDto> suggestions = [];
        List<AdBannerDto>   banners     = [];
        try
        {
            suggestions = await _apiService.GetSuggestionsAsync(page);
        }
        catch (HttpRequestException)
        {
            TempData["ApiWarning"] = "We're having trouble loading the feed right now. Please try again in a moment.";
        }
        catch (Exception)
        {
            TempData["ApiWarning"] = "Something went wrong loading the feed. Our team has been notified.";
        }

        try
        {
            banners = await _apiService.GetBannersAsync();
        }
        catch
        {
            // Banners are non-critical — silently degrade
        }
        ViewBag.CurrentPage = page;
        ViewBag.Banners     = banners;
        return View(suggestions);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = HttpContext.TraceIdentifier;
        var exFeature  = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var statusCode = HttpContext.Response.StatusCode;

        if (statusCode == 404)
            return RedirectToAction("Index");

        return View(new ErrorViewModel { RequestId = requestId });
    }
}
