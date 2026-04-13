using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace CarvedRockFitness.API.Controllers;

[ApiController]
[Route("admin/settings")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminSettingsController(RuntimeSettingsService settings, IDistributedCache cache) : ControllerBase
{
    [HttpGet("demo-visible")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDemoVisible()
        => Ok(new { demoVisible = settings.DemoSuggestionsVisible });

    [HttpPost("demo-visible/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleDemoVisible()
    {
        settings.DemoSuggestionsVisible = !settings.DemoSuggestionsVisible;

        // Clear cached suggestion pages so the feed reflects the change immediately
        for (int i = 1; i <= 10; i++)
            await cache.RemoveAsync($"suggestions_page_{i}");

        return Ok(new { demoVisible = settings.DemoSuggestionsVisible });
    }
}
