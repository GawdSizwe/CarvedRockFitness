using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Controllers;

[ApiController]
[Route("drivers")]
[Produces("application/json")]
public class DriversController(
    CarvedRockFitnessDbContext db,
    RuntimeSettingsService settings,
    ILogger<DriversController> logger) : ControllerBase
{
    // ── Public: list active drivers (for the Academy transport section) ──
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDrivers(CancellationToken ct)
    {
        var showDemo = settings.DemoSuggestionsVisible;

        var drivers = await db.Drivers
            .Where(d => d.Status == "Active" && (showDemo || !d.IsDemoData))
            .OrderBy(d => d.Route)
            .Select(d => new
            {
                d.Id, d.FullName, d.Phone, d.Route,
                d.Vehicle, d.Capacity, d.PoliceClearance, d.Status
            })
            .ToListAsync(ct);

        return Ok(drivers);
    }

    // ── Public: submit a driver application ──
    [HttpPost("apply")]
    [AllowAnonymous]
    public async Task<IActionResult> Apply([FromBody] DriverApplyRequest request, CancellationToken ct)
    {
        var app = new DriverApplication
        {
            FullName           = request.FullName,
            Phone              = request.Phone,
            Email              = request.Email,
            PreferredRoute     = request.PreferredRoute,
            Vehicle            = request.Vehicle,
            Capacity           = request.Capacity,
            HasPoliceClearance = request.HasPoliceClearance,
            LicenseNumber      = request.LicenseNumber,
            Notes              = request.Notes,
            Status             = "Pending",
            CreatedAt          = DateTime.UtcNow
        };

        db.DriverApplications.Add(app);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Driver application {Id} from {Name} for route {Route}", app.Id, app.FullName, app.PreferredRoute);
        return Ok(new { id = app.Id, message = $"Application received from {app.FullName}! We will be in touch within 48 hours." });
    }

    // ── Admin: list all drivers ──
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDrivers(CancellationToken ct)
    {
        var drivers = await db.Drivers
            .OrderBy(d => d.Route)
            .Select(d => new
            {
                d.Id, d.FullName, d.Phone, d.Route,
                d.Vehicle, d.Capacity, d.PoliceClearance,
                d.LicenseNumber, d.Status, d.IsDemoData, d.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(drivers);
    }

    // ── Admin: list all applications ──
    [HttpGet("applications")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetApplications(CancellationToken ct)
    {
        var apps = await db.DriverApplications
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id, a.FullName, a.Phone, a.Email, a.PreferredRoute,
                a.Vehicle, a.Capacity, a.HasPoliceClearance,
                a.LicenseNumber, a.Notes, a.Status, a.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(apps);
    }

    // ── Admin: approve application → promote to active driver ──
    [HttpPost("applications/{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveApplication(int id, CancellationToken ct)
    {
        var application = await db.DriverApplications.FindAsync([id], ct);
        if (application == null) return NotFound();

        application.Status = "Approved";

        var driver = new Driver
        {
            FullName       = application.FullName,
            Phone          = application.Phone,
            Route          = application.PreferredRoute,
            Vehicle        = application.Vehicle,
            Capacity       = application.Capacity,
            PoliceClearance = application.HasPoliceClearance,
            LicenseNumber  = application.LicenseNumber,
            Status         = "Active",
            CreatedAt      = DateTime.UtcNow
        };

        db.Drivers.Add(driver);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Application {Id} approved — driver {Name} created as driver {DriverId}", id, driver.FullName, driver.Id);
        return Ok(new { driverId = driver.Id });
    }

    // ── Admin: reject application ──
    [HttpPost("applications/{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectApplication(int id, CancellationToken ct)
    {
        var application = await db.DriverApplications.FindAsync([id], ct);
        if (application == null) return NotFound();
        application.Status = "Rejected";
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Admin: update driver status ──
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDriverStatus(int id, [FromBody] UpdateDriverStatusRequest request, CancellationToken ct)
    {
        var driver = await db.Drivers.FindAsync([id], ct);
        if (driver == null) return NotFound();
        driver.Status = request.Status;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Admin: create driver manually ──
    [HttpPost("admin/create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverRequest request, CancellationToken ct)
    {
        var driver = new Driver
        {
            FullName        = request.FullName,
            Phone           = request.Phone,
            Route           = request.Route,
            Vehicle         = request.Vehicle,
            Capacity        = request.Capacity,
            PoliceClearance = request.PoliceClearance,
            LicenseNumber   = request.LicenseNumber,
            Status          = "Active",
            CreatedAt       = DateTime.UtcNow
        };

        db.Drivers.Add(driver);
        await db.SaveChangesAsync(ct);
        return Ok(new { id = driver.Id });
    }
}

public record DriverApplyRequest(
    string FullName, string Phone, string Email, string PreferredRoute,
    string? Vehicle = null, int Capacity = 7,
    bool HasPoliceClearance = false, string? LicenseNumber = null, string? Notes = null);

public record UpdateDriverStatusRequest(string Status);

public record CreateDriverRequest(
    string FullName, string Phone, string Route,
    string? Vehicle = null, int Capacity = 7,
    bool PoliceClearance = false, string? LicenseNumber = null);
