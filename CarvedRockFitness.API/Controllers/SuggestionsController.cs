using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CarvedRockFitness.API.Controllers;

[ApiController]
[Route("suggestions")]
[Produces("application/json")]
public class SuggestionsController(
    CarvedRockFitnessDbContext db,
    IDistributedCache cache,
    RuntimeSettingsService settings,
    ILogger<SuggestionsController> logger) : ControllerBase
{
    private const string CacheKeyPrefix = "suggestions_page_";

    private const int MaxPage = 1000;

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("general-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSuggestions([FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (page < 1 || page > MaxPage)
            return BadRequest($"page must be between 1 and {MaxPage}.");

        const int pageSize = 12;
        var cacheKey = $"{CacheKeyPrefix}{page}";

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
        {
            logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return Content(cached, "application/json");
        }

        logger.LogDebug("Cache miss for {CacheKey}, querying database", cacheKey);

        var showDemo = settings.DemoSuggestionsVisible;

        var suggestions = await db.Suggestions
            .Include(s => s.ContactDetail)
            .Where(s => !s.IsHidden && (showDemo || !s.IsDemoData))
            .OrderByDescending(s => s.IsPinned)
            .ThenByDescending(s => s.Priority)
            .ThenByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.Description,
                s.Category,
                s.Location,
                s.ImagePath,
                s.Latitude,
                s.Longitude,
                s.CreatedAt,
                s.EventDate,
                s.IsPinned,
                PostedBy = s.CreatedByName ?? s.CreatedByEmail,
                ContactDetail = s.ContactDetail == null ? null : new
                {
                    s.ContactDetail.Phone,
                    s.ContactDetail.Email,
                    s.ContactDetail.WhatsApp,
                    s.ContactDetail.BookingUrl
                }
            })
            .ToListAsync(ct);

        var json = JsonSerializer.Serialize(suggestions);
        await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, ct);

        return Content(json, "application/json");
    }

    [HttpGet("count")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCount(CancellationToken ct = default)
    {
        var showDemo = settings.DemoSuggestionsVisible;
        var count = await db.Suggestions
            .Where(s => !s.IsHidden && (showDemo || !s.IsDemoData))
            .CountAsync(ct);
        return Ok(new { count });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSuggestion(int id, CancellationToken ct = default)
    {
        var s = await db.Suggestions
            .Include(s => s.ContactDetail)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (s == null) return NotFound();

        return Ok(new
        {
            s.Id, s.Title, s.Description, s.Category, s.Location, s.ImagePath,
            s.Latitude, s.Longitude,
            s.CreatedAt, s.EventDate, s.IsPinned, s.IsHidden,
            PostedBy = s.CreatedByName ?? s.CreatedByEmail,
            ContactDetail = s.ContactDetail == null ? null : new
            {
                s.ContactDetail.Phone,
                s.ContactDetail.Email,
                s.ContactDetail.WhatsApp,
                s.ContactDetail.BookingUrl
            }
        });
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("suggestions-write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSuggestion([FromBody] CreateSuggestionRequest request, CancellationToken ct = default)
    {
        var email = User.FindFirst("email")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name  = User.FindFirst("name")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                 ?? email;
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        if (!UrlValidation.IsHttpUrlOrNull(request.ImagePath) || !UrlValidation.IsHttpUrlOrNull(request.BookingUrl))
            return BadRequest("ImagePath and BookingUrl must be http(s) URLs or relative paths.");

        var suggestion = new Suggestion
        {
            Title          = request.Title,
            Description    = request.Description,
            Category       = request.Category,
            Location       = request.Location,
            ImagePath      = request.ImagePath,
            Latitude       = request.Latitude,
            Longitude      = request.Longitude,
            CreatedAt      = DateTime.UtcNow,
            CreatedByEmail = email,
            CreatedByName  = name
        };

        db.Suggestions.Add(suggestion);
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(request.Phone) || !string.IsNullOrEmpty(request.Email)
            || !string.IsNullOrEmpty(request.WhatsApp) || !string.IsNullOrEmpty(request.BookingUrl))
        {
            db.ContactDetails.Add(new ContactDetail
            {
                SuggestionId = suggestion.Id,
                Phone = request.Phone,
                Email = request.Email,
                WhatsApp = request.WhatsApp,
                BookingUrl = request.BookingUrl
            });
            await db.SaveChangesAsync(ct);
        }

        // Invalidate cache pages 1-3
        for (int i = 1; i <= 3; i++)
            await cache.RemoveAsync($"{CacheKeyPrefix}{i}", ct);

        logger.LogInformation("Suggestion {SuggestionId} created by {Email}", suggestion.Id, LogRedaction.RedactEmail(email));
        return CreatedAtAction(nameof(GetSuggestion), new { id = suggestion.Id }, new { suggestion.Id });
    }

    [HttpPatch("{id:int}/pin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PinSuggestion(int id, CancellationToken ct = default)
    {
        var s = await db.Suggestions.FindAsync([id], ct);
        if (s == null) return NotFound();
        s.IsPinned = !s.IsPinned;
        await db.SaveChangesAsync(ct);
        for (int i = 1; i <= 3; i++) await cache.RemoveAsync($"{CacheKeyPrefix}{i}", ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/hide")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HideSuggestion(int id, CancellationToken ct = default)
    {
        var s = await db.Suggestions.FindAsync([id], ct);
        if (s == null) return NotFound();
        s.IsHidden = !s.IsHidden;
        await db.SaveChangesAsync(ct);
        for (int i = 1; i <= 3; i++) await cache.RemoveAsync($"{CacheKeyPrefix}{i}", ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/boost")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BoostSuggestion(int id, CancellationToken ct = default)
    {
        var s = await db.Suggestions.FindAsync([id], ct);
        if (s == null) return NotFound();
        s.Priority += 1;
        await db.SaveChangesAsync(ct);
        for (int i = 1; i <= 3; i++) await cache.RemoveAsync($"{CacheKeyPrefix}{i}", ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSuggestion(int id, CancellationToken ct = default)
    {
        var suggestion = await db.Suggestions.FindAsync([id], ct);
        if (suggestion == null) return NotFound();

        db.Suggestions.Remove(suggestion);
        await db.SaveChangesAsync(ct);

        for (int i = 1; i <= 3; i++)
            await cache.RemoveAsync($"{CacheKeyPrefix}{i}", ct);

        logger.LogInformation("Suggestion {SuggestionId} deleted by admin", id);
        return NoContent();
    }
}

internal static class LogRedaction
{
    // Keeps the domain (useful for triage) but masks the local part, e.g. "jo***@example.com".
    public static string RedactEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return "(none)";
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var masked = local.Length <= 2 ? "***" : $"{local[..2]}***";
        return $"{masked}{email[at..]}";
    }
}

public static class UrlValidation
{
    // Allows relative paths (e.g. "/uploads/x.jpg") or absolute http(s) URLs; rejects
    // protocol-relative ("//host/...") and non-http(s) schemes such as javascript:/data:.
    public static bool IsHttpUrlOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value.StartsWith('/') && !value.StartsWith("//")) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public record CreateSuggestionRequest(
    [property: Required, StringLength(150, MinimumLength = 1)] string Title,
    [property: Required, StringLength(4000, MinimumLength = 1)] string Description,
    [property: Required, StringLength(80)] string Category,
    [property: Required, StringLength(200)] string Location,
    [property: StringLength(500)] string? ImagePath,
    [property: Phone, StringLength(40)] string? Phone,
    [property: EmailAddress, StringLength(254)] string? Email,
    [property: StringLength(40)] string? WhatsApp,
    [property: StringLength(2000)] string? BookingUrl,
    [property: Range(-90, 90)] double? Latitude = null,
    [property: Range(-180, 180)] double? Longitude = null);
