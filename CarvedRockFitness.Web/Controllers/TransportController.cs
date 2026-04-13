using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

public class TransportController(ApiService apiService) : Controller
{
    public IActionResult Apply() => View();

    [HttpPost]
    public async Task<IActionResult> Apply(DriverApplyForm model)
    {
        if (!ModelState.IsValid) return View(model);

        var dto = new DriverApplicationRequest(
            model.FullName, model.Phone, model.Email, model.PreferredRoute,
            model.Vehicle, model.Capacity, model.HasPoliceClearance,
            model.LicenseNumber, model.Notes);

        var (ok, message) = await apiService.SubmitDriverApplicationAsync(dto);

        if (ok)
            TempData["ApplySuccess"] = message;
        else
            TempData["ApplyError"] = message;

        return RedirectToAction("Apply");
    }
}

public class DriverApplyForm
{
    public required string FullName        { get; set; }
    public required string Phone           { get; set; }
    public required string Email           { get; set; }
    public required string PreferredRoute  { get; set; }
    public string? Vehicle                 { get; set; }
    public int     Capacity                { get; set; } = 7;
    public bool    HasPoliceClearance      { get; set; }
    public string? LicenseNumber           { get; set; }
    public string? Notes                   { get; set; }
}
