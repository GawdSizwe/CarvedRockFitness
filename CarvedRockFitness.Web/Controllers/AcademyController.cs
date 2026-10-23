using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.Web.Controllers;

public class AcademyController(ApiService apiService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var statsTask   = apiService.GetAcademyStatsAsync();
        var driversTask = apiService.GetActiveDriversAsync();
        var reviewsTask = apiService.GetAcademyReviewsAsync();
        await Task.WhenAll(statsTask, driversTask, reviewsTask);
        ViewBag.Stats   = statsTask.Result;
        ViewBag.Drivers = driversTask.Result;
        ViewBag.Reviews = reviewsTask.Result;
        return View();
    }

    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(AcademyRegisterRequest model)
    {
        if (!ModelState.IsValid) return View(model);

        var dto = new AcademyRegisterDto(
            model.PlayerName, model.Age, model.Sport,
            model.ParentName, model.ParentEmail, model.ParentPhone, model.ConsentGiven,
            model.NeedsTransport, model.PickupAddress, model.Notes);

        var (ok, message) = await apiService.RegisterAcademyPlayerAsync(dto);

        if (ok)
            TempData["RegisterSuccess"] = $"{message} We will contact {model.ParentEmail} within 24 hours.";
        else
            TempData["RegisterError"] = message;

        return RedirectToAction(nameof(Index));
    }
}

public class AcademyRegisterRequest
{
    public required string PlayerName    { get; set; }
    public required string ParentName    { get; set; }
    public required string ParentEmail   { get; set; }
    public required string ParentPhone   { get; set; }
    public required string Sport         { get; set; }
    public int    Age                    { get; set; }
    public bool   NeedsTransport         { get; set; }
    public string? PickupAddress         { get; set; }
    public string? Notes                 { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must consent to the processing of your and your child's information to register.")]
    public bool ConsentGiven             { get; set; }
}
