using CarvedRockFitness.Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace CarvedRockFitness.Auth.Controllers;

/// <summary>
/// Internal, service-to-service endpoint used by the Web app's PayFast ITN handler to mark a
/// user as paid once the payment notification has been fully verified (signature + PayFast
/// server-side validation + amount check). Not reachable by browsers: it is guarded by a shared
/// secret (ServiceAuth:PaymentsWebhookSecret) rather than user authentication, since the caller
/// here is the Web app itself, not a logged-in user.
/// </summary>
[ApiController]
[Route("internal/payments")]
[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public class InternalPaymentsController(
    UserManager<ApplicationUser> userManager,
    IConfiguration config,
    ILogger<InternalPaymentsController> logger) : ControllerBase
{
    [HttpPost("mark-paid")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-write")]
    public async Task<IActionResult> MarkPaid([FromBody] MarkPaidRequest request)
    {
        var expectedSecret = config["ServiceAuth:PaymentsWebhookSecret"];
        var providedSecret = Request.Headers["X-Service-Secret"].ToString();
        if (string.IsNullOrEmpty(expectedSecret) ||
            !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(providedSecret),
                System.Text.Encoding.UTF8.GetBytes(expectedSecret)))
        {
            logger.LogWarning("Rejected mark-paid call with an invalid service secret for {Email}", request.Email);
            return Unauthorized();
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            logger.LogWarning("mark-paid called for unknown email {Email}", request.Email);
            return NotFound();
        }

        user.IsPaidUser = true;
        await userManager.UpdateAsync(user);

        if (!await userManager.IsInRoleAsync(user, "PaidUser"))
            await userManager.AddToRoleAsync(user, "PaidUser");
        if (await userManager.IsInRoleAsync(user, "FreeUser"))
            await userManager.RemoveFromRoleAsync(user, "FreeUser");

        logger.LogInformation("Marked {Email} as PaidUser (plan: {Plan})", request.Email, request.Plan);
        return Ok();
    }
}

public record MarkPaidRequest(string Email, string Plan);
