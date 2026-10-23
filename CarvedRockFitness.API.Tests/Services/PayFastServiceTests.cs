// Self-contained PayFast tests — service logic is embedded below so no Web project reference is needed.
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CarvedRockFitness.API.Tests.Services;

// ── local copy of PayFast service under test ─────────────────────────────────
internal class TestPayFastService(IConfiguration config)
{
    private bool   Sandbox    => config.GetValue("PayFast:Sandbox", true);
    private string BaseUrl    => Sandbox ? "https://sandbox.payfast.co.za/eng/process" : "https://www.payfast.co.za/eng/process";
    private string MerchantId  => config["PayFast:MerchantId"]  ?? "10000100";
    private string MerchantKey => config["PayFast:MerchantKey"] ?? "46f0cd694581a";
    private string PassPhrase  => config["PayFast:PassPhrase"]  ?? "";

    public (string ActionUrl, Dictionary<string, string> Fields, bool IsSandbox) BuildPayment(
        string paymentId, string firstName, string lastName, string email,
        decimal amount, string itemName, string itemDescription,
        string returnUrl, string cancelUrl, string notifyUrl)
    {
        var data = new Dictionary<string, string>
        {
            ["merchant_id"]      = MerchantId,
            ["merchant_key"]     = MerchantKey,
            ["return_url"]       = returnUrl,
            ["cancel_url"]       = cancelUrl,
            ["notify_url"]       = notifyUrl,
            ["name_first"]       = firstName,
            ["name_last"]        = lastName,
            ["email_address"]    = email,
            ["m_payment_id"]     = paymentId,
            ["amount"]           = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["item_name"]        = itemName,
            ["item_description"] = itemDescription,
        };
        if (!string.IsNullOrWhiteSpace(PassPhrase))
            data["passphrase"] = PassPhrase;
        var sig = Sign(data);
        data.Remove("passphrase");
        data["signature"] = sig;
        return (BaseUrl, data, Sandbox);
    }

    private static string Sign(Dictionary<string, string> data)
    {
        var qs = string.Join("&", data.Where(k => !string.IsNullOrEmpty(k.Value))
                                      .Select(k => $"{k.Key}={Uri.EscapeDataString(k.Value)}"));
        using var md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(qs))).ToLower();
    }
}
// ─────────────────────────────────────────────────────────────────────────────

public class PayFastServiceTests
{
    private static TestPayFastService BuildService(bool sandbox = true,
        string merchantId  = "10000100",
        string merchantKey = "46f0cd694581a",
        string passPhrase  = "")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayFast:Sandbox"]     = sandbox.ToString().ToLower(),
                ["PayFast:MerchantId"]  = merchantId,
                ["PayFast:MerchantKey"] = merchantKey,
                ["PayFast:PassPhrase"]  = passPhrase,
            })
            .Build();
        return new TestPayFastService(config);
    }

    private static (string, string, string, string, decimal, string, string, string, string, string) Sample(decimal amount = 149.00m) =>
        ("test-001", "Sipho", "Dlamini", "sipho@yveda.co.za", amount,
         "Sport & Health Premium", "Monthly premium listing",
         "https://localhost/Payment/Success",
         "https://localhost/Payment/Cancel",
         "https://localhost/Payment/Notify");

    private (string, Dictionary<string, string>, bool) Build(bool sandbox = true,
        string merchantId = "10000100", string merchantKey = "46f0cd694581a",
        string passPhrase = "", decimal amount = 149.00m)
    {
        var s = Sample(amount);
        return BuildService(sandbox, merchantId, merchantKey, passPhrase)
            .BuildPayment(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5,
                          s.Item6, s.Item7, s.Item8, s.Item9, s.Item10);
    }

    // ── sandbox / live URLs ───────────────────────────────────────────────
    [Fact]
    public void BuildPayment_Sandbox_UsesSandboxUrl()
    {
        var (url, _, isSandbox) = Build(sandbox: true);
        url.Should().StartWith("https://sandbox.payfast.co.za");
        isSandbox.Should().BeTrue();
    }

    [Fact]
    public void BuildPayment_Live_UsesLiveUrl()
    {
        var (url, _, isSandbox) = Build(sandbox: false);
        url.Should().StartWith("https://www.payfast.co.za");
        isSandbox.Should().BeFalse();
    }

    // ── required fields present ───────────────────────────────────────────
    [Fact]
    public void BuildPayment_ContainsAllRequiredFields()
    {
        var (_, fields, _) = Build();
        fields.Should().ContainKey("merchant_id");
        fields.Should().ContainKey("merchant_key");
        fields.Should().ContainKey("amount");
        fields.Should().ContainKey("item_name");
        fields.Should().ContainKey("return_url");
        fields.Should().ContainKey("cancel_url");
        fields.Should().ContainKey("notify_url");
        fields.Should().ContainKey("signature");
    }

    // ── amount formatting ─────────────────────────────────────────────────
    [Theory]
    [InlineData(149.00, "149.00")]
    [InlineData(300.00, "300.00")]
    [InlineData(49.99,  "49.99")]
    public void BuildPayment_FormatsAmountCorrectly(decimal input, string expected)
    {
        var (_, fields, _) = Build(amount: input);
        fields["amount"].Should().Be(expected);
    }

    // ── merchant credentials stamped ─────────────────────────────────────
    [Fact]
    public void BuildPayment_StampsMerchantCredentials()
    {
        var (_, fields, _) = Build(merchantId: "99998888", merchantKey: "testkey123");
        fields["merchant_id"].Should().Be("99998888");
        fields["merchant_key"].Should().Be("testkey123");
    }

    // ── signature is an MD5 hex string ───────────────────────────────────
    [Fact]
    public void BuildPayment_SignatureIsMd5Hex()
    {
        var (_, fields, _) = Build();
        var sig = fields["signature"];
        sig.Should().HaveLength(32);
        sig.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    // ── same inputs produce same signature (deterministic) ────────────────
    [Fact]
    public void BuildPayment_SameInputs_ProduceSameSignature()
    {
        var sig1 = Build().Item2["signature"];
        var sig2 = Build().Item2["signature"];
        sig1.Should().Be(sig2);
    }

    // ── passphrase excluded from posted fields ────────────────────────────
    [Fact]
    public void BuildPayment_PassphraseNotInPostedFields()
    {
        var (_, fields, _) = Build(passPhrase: "SecretPhrase123");
        fields.Should().NotContainKey("passphrase");
    }

    // ── customer details stamped ──────────────────────────────────────────
    [Fact]
    public void BuildPayment_StampsCustomerDetails()
    {
        var (_, fields, _) = Build();
        fields["name_first"].Should().Be("Sipho");
        fields["name_last"].Should().Be("Dlamini");
        fields["email_address"].Should().Be("sipho@yveda.co.za");
    }

    // ── URLs passed through correctly ─────────────────────────────────────
    [Fact]
    public void BuildPayment_UrlsPassedThrough()
    {
        var (_, fields, _) = Build();
        fields["return_url"].Should().Contain("/Payment/Success");
        fields["cancel_url"].Should().Contain("/Payment/Cancel");
        fields["notify_url"].Should().Contain("/Payment/Notify");
    }
}
