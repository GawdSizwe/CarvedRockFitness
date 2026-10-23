using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

[Authorize]
public class PaymentController(
    PayFastService payFast,
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<PaymentController> logger) : Controller
{
    [HttpGet]
    public IActionResult Checkout(string plan = "premium")
    {
        var amount = plan == "academy"
            ? config.GetValue("PayFast:AcademyFeeAmount", "300.00")
            : config.GetValue("PayFast:PremiumPlanAmount", "149.00");

        ViewBag.Amount      = $"R {amount}";
        ViewBag.Plan        = plan;
        ViewBag.IsSandbox   = config.GetValue("PayFast:Sandbox", true);

        var email = User.FindFirst("email")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                 ?? "user@sportmeetups.co.za";

        var baseUrl    = $"{Request.Scheme}://{Request.Host}";
        // The payment ID round-trips the plan and the paying user's email so the anonymous ITN
        // webhook (which PayFast calls directly, with no session/cookie) can later recover who
        // to mark as paid without needing a separate "pending payment" table.
        var paymentId  = PaymentIdCodec.Encode(plan, email);
        var nameParts  = (User.Identity?.Name ?? "User").Split(' ');
        var firstName  = nameParts[0];
        var lastName   = nameParts.Length > 1 ? nameParts[1] : "-";

        var redirect = payFast.BuildPayment(new PayFastPaymentRequest(
            PaymentId:       paymentId,
            FirstName:       firstName,
            LastName:        lastName,
            Email:           email,
            Amount:          decimal.Parse(amount!, CultureInfo.InvariantCulture),
            ItemName:        plan == "academy" ? "YVEDA Academy Transport Fee" : "Sport & Health Premium Listing",
            ItemDescription: plan == "academy" ? "Monthly kids transport to YVEDA" : "Post unlimited sport & health listings",
            ReturnUrl:       $"{baseUrl}/Payment/Success?plan={plan}",
            CancelUrl:       $"{baseUrl}/Payment/Cancel",
            NotifyUrl:       $"{baseUrl}/Payment/Notify"
        ));

        ViewBag.Redirect = redirect;
        return View();
    }

    [HttpGet]
    public IActionResult Success(string plan = "premium")
    {
        TempData["PaymentSuccess"] = plan == "academy"
            ? "Payment confirmed — your child's transport to YVEDA is booked!"
            : "Payment confirmed — you are now a Premium member! You can post unlimited listings.";
        return RedirectToAction("Profile", "Account");
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["PaymentCancelled"] = "Payment was cancelled. No charge was made.";
        return RedirectToAction("Profile", "Account");
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Notify(CancellationToken ct)
    {
        var paymentStatus = Request.Form["payment_status"].ToString();
        var paymentId     = Request.Form["m_payment_id"].ToString();

        // 1. Signature must match what we'd have generated ourselves.
        if (!payFast.VerifyItnSignature(Request.Form))
        {
            logger.LogWarning("PayFast ITN signature INVALID for payment {PaymentId}", paymentId);
            return BadRequest("Invalid signature");
        }

        // 2. PayFast itself must confirm the notification is genuine (defence-in-depth beyond
        //    the local signature check, per PayFast's integration guide).
        if (!await payFast.ServerValidateAsync(Request.Form, ct))
        {
            logger.LogWarning("PayFast ITN server validation FAILED for payment {PaymentId}", paymentId);
            return BadRequest("Server validation failed");
        }

        var decoded = PaymentIdCodec.Decode(paymentId);
        if (decoded == null)
        {
            logger.LogWarning("PayFast ITN had an unrecognised m_payment_id {PaymentId}", paymentId);
            return BadRequest("Unrecognised payment id");
        }
        var (plan, email) = decoded.Value;

        // 3. Never trust the client-submitted amount; compare PayFast's own reported gross
        //    amount against our server-side configured price for the plan.
        var expectedAmountRaw = plan == "academy"
            ? config.GetValue("PayFast:AcademyFeeAmount", "300.00")
            : config.GetValue("PayFast:PremiumPlanAmount", "149.00");
        var expectedAmount = decimal.Parse(expectedAmountRaw!, CultureInfo.InvariantCulture);
        var grossAmountRaw = Request.Form["amount_gross"].ToString();
        if (!decimal.TryParse(grossAmountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var grossAmount) ||
            Math.Abs(grossAmount - expectedAmount) > 0.01m)
        {
            logger.LogWarning(
                "PayFast ITN amount mismatch for payment {PaymentId}: expected {Expected}, got {Actual}",
                paymentId, expectedAmount, grossAmountRaw);
            return BadRequest("Amount mismatch");
        }

        logger.LogInformation("PayFast ITN verified — payment {PaymentId} status {Status}", paymentId, paymentStatus);

        if (paymentStatus == "COMPLETE")
        {
            await MarkUserPaidAsync(email, plan, ct);
        }

        return Ok();
    }

    private async Task MarkUserPaidAsync(string email, string plan, CancellationToken ct)
    {
        var secret = config["ServiceAuth:PaymentsWebhookSecret"];
        if (string.IsNullOrEmpty(secret))
        {
            logger.LogError(
                "ServiceAuth:PaymentsWebhookSecret is not configured — cannot mark {Email} as paid for plan {Plan}",
                email, plan);
            return;
        }

        var client = httpClientFactory.CreateClient("AuthApi");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/internal/payments/mark-paid");
        request.Headers.Add("X-Service-Secret", secret);
        request.Content = JsonContent.Create(new { email, plan });

        using var response = await client.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Marked {Email} as paid for plan {Plan}", email, plan);
        }
        else
        {
            logger.LogError(
                "Failed to mark {Email} as paid for plan {Plan}: Auth responded {StatusCode}",
                email, plan, response.StatusCode);
        }
    }
}

/// <summary>
/// Encodes/decodes the PayFast "m_payment_id" so the anonymous ITN webhook can recover which
/// plan was purchased and which user to credit, without needing a separate persisted
/// "pending payment" record. Not a security boundary by itself — the ITN handler always
/// re-validates the signature, calls PayFast's server-side validation, and checks the amount
/// before acting on the decoded value.
/// </summary>
internal static class PaymentIdCodec
{
    public static string Encode(string plan, string email)
    {
        var payload = $"{plan}|{email}|{DateTime.UtcNow.Ticks}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static (string Plan, string Email)? Decode(string encoded)
    {
        try
        {
            var base64 = encoded.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = payload.Split('|');
            return parts.Length >= 2 ? (parts[0], parts[1]) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
