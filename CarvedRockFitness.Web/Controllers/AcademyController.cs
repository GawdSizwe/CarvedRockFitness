using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

public class AcademyController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Register() => View();

    [HttpPost]
    public IActionResult Register(AcademyRegisterRequest model)
    {
        if (!ModelState.IsValid) return View(model);
        TempData["RegisterSuccess"] = $"Registration received for {model.PlayerName}! We will contact {model.ParentEmail} within 24 hours.";
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
}
