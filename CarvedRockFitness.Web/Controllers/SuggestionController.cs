using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

public class SuggestionController(ApiService apiService, IWebHostEnvironment env) : Controller
{
    // Allow-list of accepted image types, keyed by their on-disk extension.
    // Each entry's magic bytes are checked against the uploaded content before it's saved.
    private static readonly Dictionary<string, byte[][]> AllowedImageSignatures = new()
    {
        [".jpg"]  = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"]  = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".gif"]  = [[0x47, 0x49, 0x46, 0x38, 0x37, 0x61], [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]], // "RIFF" header; WEBP marker follows at offset 8
    };
    private const long MaxImageBytes = 5 * 1024 * 1024;

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
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<IActionResult> Create(CreateSuggestionRequest model, IFormFile? image)
    {
        if (!ModelState.IsValid) return View(model);

        if (!string.IsNullOrWhiteSpace(model.BookingUrl) && !IsHttpUrl(model.BookingUrl))
        {
            ModelState.AddModelError(nameof(model.BookingUrl), "Booking link must be a valid http(s) URL.");
            return View(model);
        }

        string? imagePath = null;
        if (image is { Length: > 0 })
        {
            if (image.Length > MaxImageBytes)
            {
                ModelState.AddModelError(nameof(image), "Image must be 5 MB or smaller.");
                return View(model);
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedImageSignatures.TryGetValue(extension, out var signatures) ||
                !await MatchesSignatureAsync(image, signatures))
            {
                ModelState.AddModelError(nameof(image), "Only JPG, PNG, GIF or WEBP images are allowed.");
                return View(model);
            }

            var uploadsDir = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{extension}";
            using (var stream = new FileStream(Path.Combine(uploadsDir, fileName), FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
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

    private static async Task<bool> MatchesSignatureAsync(IFormFile file, byte[][] signatures)
    {
        var header = new byte[signatures.Max(s => s.Length)];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        return signatures.Any(sig => read >= sig.Length && header.AsSpan(0, sig.Length).SequenceEqual(sig));
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
