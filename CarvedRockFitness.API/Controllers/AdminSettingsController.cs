using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;

namespace CarvedRockFitness.API.Controllers;

[ApiController]
[Route("admin/settings")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin")]
[Produces("application/json")]
public class AdminSettingsController(
    RuntimeSettingsService settings,
    AppSettingsStore settingsStore,
    IDistributedCache cache) : ControllerBase
{
    [HttpGet("demo-visible")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDemoVisible()
        => Ok(new { demoVisible = settings.DemoSuggestionsVisible });

    [HttpPost("demo-visible/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleDemoVisible(CancellationToken ct)
    {
        settings.DemoSuggestionsVisible = !settings.DemoSuggestionsVisible;
        await settingsStore.SetBoolAsync(RuntimeSettingsService.DemoSuggestionsVisibleKey, settings.DemoSuggestionsVisible, ct);

        // Clear cached suggestion pages so the feed reflects the change immediately
        for (int i = 1; i <= 10; i++)
            await cache.RemoveAsync($"suggestions_page_{i}", ct);

        return Ok(new { demoVisible = settings.DemoSuggestionsVisible });
    }
}
