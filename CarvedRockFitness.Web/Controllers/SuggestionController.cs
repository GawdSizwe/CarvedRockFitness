using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

public class SuggestionController(ApiService apiService, IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Details(int id)
    {
        var suggestion = await apiService.GetSuggestionAsync(id);
        if (suggestion == null) return NotFound();
        return View(suggestion);
    }

    [Authorize(Roles = "PaidUser,Admin")]
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Roles = "PaidUser,Admin")]
    public async Task<IActionResult> Create(CreateSuggestionRequest model, IFormFile? image)
    {
        if (!ModelState.IsValid) return View(model);

        string? imagePath = null;
        if (image is { Length: > 0 })
        {
            var uploadsDir = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            using var stream = new FileStream(Path.Combine(uploadsDir, fileName), FileMode.Create);
            await image.CopyToAsync(stream);
            imagePath = $"/uploads/{fileName}";
        }

        var dto = new CreateSuggestionDto(
            model.Title, model.Description, model.Category, model.Location,
            imagePath, model.Phone, model.Email, model.WhatsApp, model.BookingUrl,
            model.Latitude, model.Longitude);

        var success = await apiService.CreateSuggestionAsync(dto);
        if (!success)
        {
            ModelState.AddModelError("", "Failed to create suggestion. Please try again.");
            return View(model);
        }
        return RedirectToAction("Index", "Home");
    }
}

public class CreateSuggestionRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public required string Location { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WhatsApp { get; set; }
    public string? BookingUrl { get; set; }
    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }
}
