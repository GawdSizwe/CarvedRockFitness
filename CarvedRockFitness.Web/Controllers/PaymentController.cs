using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.Web.Controllers;

[Authorize]
public class PaymentController(PayFastService payFast, IConfiguration config, ILogger<PaymentController> logger) : Controller
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

        var baseUrl    = $"{Request.Scheme}://{Request.Host}";
        var paymentId  = $"{plan}-{User.Identity!.Name}-{DateTime.UtcNow.Ticks}";
        var nameParts  = (User.Identity.Name ?? "User").Split(' ');
        var firstName  = nameParts[0];
        var lastName   = nameParts.Length > 1 ? nameParts[1] : "-";
        var email      = User.FindFirst("email")?.Value
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                      ?? "user@sportmeetups.co.za";

        var redirect = payFast.BuildPayment(new PayFastPaymentRequest(
            PaymentId:       paymentId,
            FirstName:       firstName,
            LastName:        lastName,
            Email:           email,
            Amount:          decimal.Parse(amount!),
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
    public IActionResult Notify()
    {
        var paymentStatus = Request.Form["payment_status"].ToString();
        var paymentId     = Request.Form["m_payment_id"].ToString();

        if (!payFast.VerifyItnSignature(Request.Form))
        {
            logger.LogWarning("PayFast ITN signature INVALID for payment {PaymentId}", paymentId);
            return BadRequest("Invalid signature");
        }

        logger.LogInformation("PayFast ITN verified — payment {PaymentId} status {Status}", paymentId, paymentStatus);

        if (paymentStatus == "COMPLETE")
        {
            logger.LogInformation("Payment COMPLETE for {PaymentId} — mark user as PaidUser", paymentId);
        }

        return Ok();
    }
}
