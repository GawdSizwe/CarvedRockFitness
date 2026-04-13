using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Controllers;

[ApiController]
[Route("banners")]
[Produces("application/json")]
public class AdBannersController(CarvedRockFitnessDbContext db, RuntimeSettingsService settings) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanners(CancellationToken ct = default)
    {
        try
        {
            var showDemo = settings.DemoSuggestionsVisible;

            var banners = await db.AdBanners
                .Where(b => b.IsActive && (showDemo || !b.IsDemoData))
                .OrderBy(b => b.CreatedAt)
                .Select(b => new { b.Id, b.BusinessName, b.ImageUrl, b.LinkUrl, b.TagLine })
                .ToListAsync(ct);

            return Ok(banners);
        }
        catch
        {
            return Ok(Array.Empty<object>());
        }
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBanners(CancellationToken ct = default)
    {
        var banners = await db.AdBanners
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
        return Ok(banners);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBanner([FromBody] CreateBannerRequest req, CancellationToken ct = default)
    {
        var banner = new AdBanner
        {
            BusinessName = req.BusinessName,
            ImageUrl      = req.ImageUrl,
            LinkUrl       = req.LinkUrl,
            TagLine       = req.TagLine,
            IsActive      = true,
            IsDemoData    = false,
            CreatedAt     = DateTime.UtcNow
        };
        db.AdBanners.Add(banner);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAllBanners), new { }, new { banner.Id });
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleBanner(int id, CancellationToken ct = default)
    {
        var banner = await db.AdBanners.FindAsync([id], ct);
        if (banner == null) return NotFound();
        banner.IsActive = !banner.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBanner(int id, CancellationToken ct = default)
    {
        var banner = await db.AdBanners.FindAsync([id], ct);
        if (banner == null) return NotFound();
        db.AdBanners.Remove(banner);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CreateBannerRequest(string BusinessName, string ImageUrl, string? LinkUrl, string? TagLine);
