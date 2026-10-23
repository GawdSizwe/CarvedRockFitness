using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.Controllers;

[ApiController]
[Route("academy")]
[Produces("application/json")]
public class AcademyController(
    CarvedRockFitnessDbContext db,
    RuntimeSettingsService settings,
    ILogger<AcademyController> logger) : ControllerBase
{
    [HttpGet("stats")]
    [AllowAnonymous]
    [EnableRateLimiting("general-read")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var showDemo = settings.DemoSuggestionsVisible;

        var query = db.AcademyRegistrations
            .Where(r => showDemo || !r.IsDemoData);

        var totalPlayers   = await query.CountAsync(ct);
        var activePlayers  = await query.CountAsync(r => r.Status == "Active", ct);
        var sportsOffered  = await query.Select(r => r.Sport).Distinct().CountAsync(ct);
        var transportUsers = await query.CountAsync(r => r.NeedsTransport, ct);
        var routes         = await query.Where(r => r.AssignedRoute != null)
                                        .Select(r => r.AssignedRoute).Distinct().CountAsync(ct);
        var pending        = await query.CountAsync(r => r.Status == "Pending", ct);

        return Ok(new
        {
            totalPlayers,
            activePlayers,
            sportsOffered,
            transportUsers,
            routes,
            pending
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("academy-register")]
    public async Task<IActionResult> Register([FromBody] AcademyRegisterRequest request, CancellationToken ct)
    {
        if (!request.ConsentGiven)
            return BadRequest("Consent to process the player's and parent/guardian's information is required.");

        var reg = new AcademyRegistration
        {
            PlayerName     = request.PlayerName,
            Age            = request.Age,
            Sport          = request.Sport,
            ParentName     = request.ParentName,
            ParentEmail    = request.ParentEmail,
            ParentPhone    = request.ParentPhone,
            NeedsTransport = request.NeedsTransport,
            PickupAddress  = request.PickupAddress,
            Notes          = request.Notes,
            Status         = "Pending",
            CreatedAt      = DateTime.UtcNow,
            ConsentGivenAt = DateTime.UtcNow
        };

        db.AcademyRegistrations.Add(reg);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Academy registration {Id} for {Player} ({Sport})", reg.Id, reg.PlayerName, reg.Sport);
        return Ok(new { id = reg.Id, message = $"Registration received for {reg.PlayerName}!" });
    }

    [HttpGet("registrations")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("admin")]
    public async Task<IActionResult> GetRegistrations(CancellationToken ct)
    {
        var showDemo = settings.DemoSuggestionsVisible;

        var list = await db.AcademyRegistrations
            .Where(r => showDemo || !r.IsDemoData)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.PlayerName, r.Age, r.Sport,
                r.ParentName, r.ParentEmail, r.ParentPhone,
                r.NeedsTransport, r.PickupAddress, r.AssignedRoute,
                r.Status, r.IsDemoData, r.Notes, r.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpPatch("registrations/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var reg = await db.AcademyRegistrations.FindAsync([id], ct);
        if (reg == null) return NotFound();
        reg.Status = request.Status;
        if (request.AssignedRoute != null) reg.AssignedRoute = request.AssignedRoute;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record AcademyRegisterRequest(
    [property: Required, StringLength(150, MinimumLength = 1)] string PlayerName,
    [property: Range(3, 25)] int Age,
    [property: Required, StringLength(80)] string Sport,
    [property: Required, StringLength(150, MinimumLength = 1)] string ParentName,
    [property: Required, EmailAddress, StringLength(254)] string ParentEmail,
    [property: Required, Phone, StringLength(40)] string ParentPhone,
    bool ConsentGiven,
    bool NeedsTransport = false,
    [property: StringLength(300)] string? PickupAddress = null,
    [property: StringLength(2000)] string? Notes = null);

public record UpdateStatusRequest(
    [property: Required, StringLength(40)] string Status,
    [property: StringLength(150)] string? AssignedRoute = null);
