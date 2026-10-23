using System.Security.Cryptography;
using System.Text;

namespace CarvedRockFitness.Web.Services;

public class PayFastService(IConfiguration config, IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
{
    private bool Sandbox   => config.GetValue("PayFast:Sandbox", true);
    private string BaseUrl => Sandbox
        ? "https://sandbox.payfast.co.za/eng/process"
        : "https://www.payfast.co.za/eng/process";
    private string ValidateUrl => Sandbox
        ? "https://sandbox.payfast.co.za/eng/query/validate"
        : "https://www.payfast.co.za/eng/query/validate";

    // PayFast's publicly documented sandbox test credentials are only used as a fallback in
    // Development, and only while Sandbox mode is on; production must always supply real values.
    private string MerchantId => config["PayFast:MerchantId"]
        ?? (env.IsDevelopment() && Sandbox ? "10000100" : throw MissingConfig("PayFast:MerchantId"));
    private string MerchantKey => config["PayFast:MerchantKey"]
        ?? (env.IsDevelopment() && Sandbox ? "46f0cd694581a" : throw MissingConfig("PayFast:MerchantKey"));
    private string PassPhrase => config["PayFast:PassPhrase"] ?? "";

    private static InvalidOperationException MissingConfig(string key) =>
        new($"{key} must be configured (no fallback is used outside Development sandbox mode).");

    public PayFastRedirect BuildPayment(PayFastPaymentRequest req)
    {
        var data = new Dictionary<string, string>
        {
            ["merchant_id"]      = MerchantId,
            ["merchant_key"]     = MerchantKey,
            ["return_url"]       = req.ReturnUrl,
            ["cancel_url"]       = req.CancelUrl,
            ["notify_url"]       = req.NotifyUrl,
            ["name_first"]       = req.FirstName,
            ["name_last"]        = req.LastName,
            ["email_address"]    = req.Email,
            ["m_payment_id"]     = req.PaymentId,
            ["amount"]           = req.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["item_name"]        = req.ItemName,
            ["item_description"] = req.ItemDescription,
        };

        if (!string.IsNullOrWhiteSpace(PassPhrase))
            data["passphrase"] = PassPhrase;

        var signature = GenerateSignature(data);
        data.Remove("passphrase");
        data["signature"] = signature;

        return new PayFastRedirect(BaseUrl, data, Sandbox);
    }

    public bool VerifyItnSignature(IFormCollection form)
    {
        var data = new Dictionary<string, string>();
        foreach (var key in form.Keys)
        {
            if (key == "signature") continue;
            var val = form[key].ToString();
            if (!string.IsNullOrEmpty(val))
                data[key] = val;
        }

        if (!string.IsNullOrWhiteSpace(PassPhrase))
            data["passphrase"] = PassPhrase;

        var expected = GenerateSignature(data);
        var received = form["signature"].ToString();
        return string.Equals(expected, received, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Posts the raw ITN payload back to PayFast so PayFast itself confirms the notification is
    /// genuine (defence-in-depth beyond just re-checking the MD5 signature locally, per PayFast's
    /// integration guide).
    /// </summary>
    public async Task<bool> ServerValidateAsync(IFormCollection form, CancellationToken ct = default)
    {
        var fields = form.Keys.ToDictionary(k => k, k => form[k].ToString());
        using var content = new FormUrlEncodedContent(fields);
        var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsync(ValidateUrl, content, ct);
        if (!response.IsSuccessStatusCode) return false;
        var body = await response.Content.ReadAsStringAsync(ct);
        return body.Trim().Equals("VALID", StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateSignature(Dictionary<string, string> data)
    {
        var queryString = string.Join("&",
            data.Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        return Convert.ToHexString(hash).ToLower();
    }
}

public record PayFastPaymentRequest(
    string PaymentId, string FirstName, string LastName, string Email,
    decimal Amount, string ItemName, string ItemDescription,
    string ReturnUrl, string CancelUrl, string NotifyUrl);

public record PayFastRedirect(string ActionUrl, Dictionary<string, string> Fields, bool IsSandbox);
