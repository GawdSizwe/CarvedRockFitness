using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Controllers;

[ApiController]
[Produces("application/json")]
public class RatingsController(
    CarvedRockFitnessDbContext db,
    RuntimeSettingsService settings,
    ILogger<RatingsController> logger) : ControllerBase
{
    // ── Driver Ratings ─────────────────────────────────────────

    [HttpGet("drivers/{driverId:int}/ratings")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDriverRatings(int driverId, CancellationToken ct)
    {
        var showDemo = settings.DemoSuggestionsVisible;
        var ratings = await db.DriverRatings
            .Where(r => r.DriverId == driverId && (showDemo || !r.IsDemoData))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.ReviewerName, r.Stars, r.Comment, r.CreatedAt })
            .ToListAsync(ct);

        var avg = ratings.Any() ? Math.Round(ratings.Average(r => r.Stars), 1) : 0.0;
        return Ok(new { average = avg, count = ratings.Count, ratings });
    }

    [HttpGet("drivers/ratings/all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllDriverRatings(CancellationToken ct)
    {
        var showDemo = settings.DemoSuggestionsVisible;

        var drivers = await db.Drivers
            .Where(d => d.Status == "Active")
            .ToListAsync(ct);

        var allRatings = await db.DriverRatings
            .Where(r => showDemo || !r.IsDemoData)
            .ToListAsync(ct);

        var result = drivers.Select(d =>
        {
            var driverRatings = allRatings.Where(r => r.DriverId == d.Id).ToList();
            return new
            {
                d.Id, d.FullName, d.Route, d.Vehicle,
                Average  = driverRatings.Any() ? Math.Round(driverRatings.Average(r => r.Stars), 1) : 0.0,
                Count    = driverRatings.Count,
                Latest   = driverRatings.OrderByDescending(r => r.CreatedAt)
                               .Take(2)
                               .Select(r => new { r.ReviewerName, r.Stars, r.Comment })
            };
        }).OrderBy(d => d.Route);

        return Ok(result);
    }

    [HttpPost("drivers/{driverId:int}/rate")]
    [AllowAnonymous]
    public async Task<IActionResult> RateDriver(int driverId, [FromBody] RateDriverRequest request, CancellationToken ct)
    {
        if (request.Stars < 1 || request.Stars > 5)
            return BadRequest("Stars must be 1–5.");

        var driver = await db.Drivers.FindAsync([driverId], ct);
        if (driver == null) return NotFound("Driver not found.");

        var rating = new DriverRating
        {
            DriverId     = driverId,
            ReviewerName = string.IsNullOrWhiteSpace(request.ReviewerName) ? "Anonymous" : request.ReviewerName,
            Stars        = request.Stars,
            Comment      = request.Comment,
            CreatedAt    = DateTime.UtcNow
        };

        db.DriverRatings.Add(rating);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Driver {DriverId} rated {Stars} stars by {Name}", driverId, rating.Stars, rating.ReviewerName);
        return Ok(new { id = rating.Id });
    }

    [HttpDelete("drivers/ratings/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDriverRating(int id, CancellationToken ct)
    {
        var rating = await db.DriverRatings.FindAsync([id], ct);
        if (rating == null) return NotFound();
        db.DriverRatings.Remove(rating);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Academy Reviews ─────────────────────────────────────────

    [HttpGet("academy/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAcademyReviews(CancellationToken ct)
    {
        var showDemo = settings.DemoSuggestionsVisible;
        var reviews = await db.AcademyReviews
            .Where(r => r.Approved && (showDemo || !r.IsDemoData))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.ReviewerName, r.ReviewerRole, r.Stars, r.Comment, r.CreatedAt })
            .ToListAsync(ct);

        var avg = reviews.Any() ? Math.Round(reviews.Average(r => r.Stars), 1) : 0.0;
        return Ok(new { average = avg, count = reviews.Count, reviews });
    }

    [HttpPost("academy/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitAcademyReview([FromBody] AcademyReviewRequest request, CancellationToken ct)
    {
        if (request.Stars < 1 || request.Stars > 5)
            return BadRequest("Stars must be 1–5.");

        var review = new AcademyReview
        {
            ReviewerName = string.IsNullOrWhiteSpace(request.ReviewerName) ? "Anonymous" : request.ReviewerName,
            ReviewerRole = request.ReviewerRole,
            Stars        = request.Stars,
            Comment      = request.Comment ?? string.Empty,
            Approved     = false,
            CreatedAt    = DateTime.UtcNow
        };

        db.AcademyReviews.Add(review);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Academy review submitted by {Name} — {Stars} stars (pending approval)", review.ReviewerName, review.Stars);
        return Ok(new { id = review.Id, message = "Thank you! Your review will appear after moderation." });
    }

    [HttpGet("academy/reviews/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAcademyReviews(CancellationToken ct)
    {
        var reviews = await db.AcademyReviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.ReviewerName, r.ReviewerRole, r.Stars, r.Comment, r.Approved, r.IsDemoData, r.CreatedAt })
            .ToListAsync(ct);
        return Ok(reviews);
    }

    [HttpPost("academy/reviews/{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveReview(int id, CancellationToken ct)
    {
        var review = await db.AcademyReviews.FindAsync([id], ct);
        if (review == null) return NotFound();
        review.Approved = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("academy/reviews/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReview(int id, CancellationToken ct)
    {
        var review = await db.AcademyReviews.FindAsync([id], ct);
        if (review == null) return NotFound();
        db.AcademyReviews.Remove(review);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record RateDriverRequest(int Stars, string? ReviewerName = null, string? Comment = null);
public record AcademyReviewRequest(int Stars, string? ReviewerName = null, string? ReviewerRole = null, string? Comment = null);
