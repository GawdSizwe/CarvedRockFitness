using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CarvedRockFitness.Web.Services;

public class ApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<ApiService> logger)
{
    // Forwards the browser's real IP (as seen by the Fly edge) so the API's per-client
    // rate limiters key on the actual end user rather than this Web instance's own address.
    private void ForwardClientIp(HttpClient client)
    {
        var ctx = httpContextAccessor.HttpContext;
        var clientIp = ctx?.Request.Headers["Fly-Client-IP"].FirstOrDefault()
                     ?? ctx?.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            client.DefaultRequestHeaders.Remove("Fly-Client-IP");
            client.DefaultRequestHeaders.Add("Fly-Client-IP", clientIp);
        }
    }

    private async Task<HttpClient> GetAuthClientAsync()
    {
        var client = httpClientFactory.CreateClient("AuthApi");
        var ctx = httpContextAccessor.HttpContext;
        var token = ctx != null ? await ctx.GetTokenAsync("access_token") : null;
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        ForwardClientIp(client);
        return client;
    }

    private async Task<HttpClient> GetClientAsync()
    {
        var client = httpClientFactory.CreateClient("GymApi");
        ForwardClientIp(client);
        var ctx = httpContextAccessor.HttpContext;
        if (ctx == null)
        {
            logger.LogWarning("HttpContext is null");
            return client;
        }
        var token = await ctx.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("Access token is null or empty. User authenticated: {IsAuthenticated}", ctx.User?.Identity?.IsAuthenticated ?? false);
        }
        else
        {
            logger.LogInformation("Access token found (length: {Length})", token.Length);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    public async Task<List<SuggestionDto>> GetSuggestionsAsync(int page = 1)
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync($"/suggestions?page={page}");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<SuggestionDto>>(json, JsonOptions) ?? [];
    }

    public async Task<int> GetSuggestionsCountAsync()
    {
        try
        {
            var client = await GetClientAsync();
            var response = await client.GetAsync("/suggestions/count");
            if (!response.IsSuccessStatusCode) return 0;
            var json = await response.Content.ReadAsStringAsync();
            var doc  = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("count").GetInt32();
        }
        catch { return 0; }
    }

    public async Task<SuggestionDto?> GetSuggestionAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync($"/suggestions/{id}");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SuggestionDto>(json, JsonOptions);
    }

    public async Task<bool> CreateSuggestionAsync(CreateSuggestionDto dto)
    {
        var client = await GetClientAsync();
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/suggestions", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserSummaryDto>> GetUsersAsync()
    {
        var client = await GetAuthClientAsync();
        var response = await client.GetAsync("/api/users");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<UserSummaryDto>>(json, JsonOptions) ?? [];
    }

    public async Task<bool> BanUserAsync(string id)
    {
        var client = await GetAuthClientAsync();
        var response = await client.PatchAsync($"/api/users/{id}/ban", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteIdentityUserAsync(string id)
    {
        var client = await GetAuthClientAsync();
        var response = await client.DeleteAsync($"/api/users/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteSuggestionAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.DeleteAsync($"/suggestions/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PinSuggestionAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PatchAsync($"/suggestions/{id}/pin", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> HideSuggestionAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PatchAsync($"/suggestions/{id}/hide", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> BoostSuggestionAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PatchAsync($"/suggestions/{id}/boost", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AdBannerDto>> GetBannersAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/banners");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AdBannerDto>>(json, JsonOptions) ?? [];
    }

    public async Task<List<AdBannerDto>> GetAllBannersAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/banners/all");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AdBannerDto>>(json, JsonOptions) ?? [];
    }

    public async Task<bool> CreateBannerAsync(string businessName, string imageUrl, string? linkUrl, string? tagLine)
    {
        var client = await GetClientAsync();
        var payload = new { businessName, imageUrl, linkUrl, tagLine };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/banners", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleBannerAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PatchAsync($"/banners/{id}/toggle", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBannerAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.DeleteAsync($"/banners/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> GetDemoVisibleAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/admin/settings/demo-visible");
        if (!response.IsSuccessStatusCode) return false;
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("demoVisible", out var prop) && prop.GetBoolean();
    }

    public async Task<bool> ToggleDemoDataAsync()
    {
        var client = await GetClientAsync();
        var response = await client.PostAsync("/admin/settings/demo-visible/toggle", null);
        if (!response.IsSuccessStatusCode) return false;
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("demoVisible", out var prop) && prop.GetBoolean();
    }

    // ── Academy ────────────────────────────────────────
    public async Task<AcademyStatsDto?> GetAcademyStatsAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/academy/stats");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AcademyStatsDto>(json, JsonOptions);
    }

    public async Task<(bool ok, string message)> RegisterAcademyPlayerAsync(AcademyRegisterDto dto)
    {
        var client = await GetClientAsync();
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/academy/register", content);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return (false, "Registration failed. Please try again.");
        var doc = JsonDocument.Parse(json);
        var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? "Registered!" : "Registered!";
        return (true, msg);
    }

    public async Task<List<AcademyRegistrationDto>> GetAcademyRegistrationsAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/academy/registrations");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AcademyRegistrationDto>>(json, JsonOptions) ?? [];
    }

    public async Task<bool> UpdateAcademyStatusAsync(int id, string status, string? assignedRoute = null)
    {
        var client = await GetClientAsync();
        var body = new { status, assignedRoute };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PatchAsync($"/academy/registrations/{id}/status", content);
        return response.IsSuccessStatusCode;
    }

    // ── Drivers ────────────────────────────────────────
    public async Task<List<DriverDto>> GetActiveDriversAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/drivers");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DriverDto>>(json, JsonOptions) ?? [];
    }

    public async Task<(bool ok, string message)> SubmitDriverApplicationAsync(DriverApplicationRequest dto)
    {
        var client = await GetClientAsync();
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/drivers/apply", content);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return (false, "Application failed. Please try again.");
        var doc = JsonDocument.Parse(json);
        var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? "Submitted!" : "Submitted!";
        return (true, msg);
    }

    public async Task<List<DriverDto>> GetAllDriversAdminAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/drivers/admin/all");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DriverDto>>(json, JsonOptions) ?? [];
    }

    public async Task<List<DriverApplicationDto>> GetDriverApplicationsAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/drivers/applications");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DriverApplicationDto>>(json, JsonOptions) ?? [];
    }

    public async Task<bool> ApproveDriverApplicationAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PostAsync($"/drivers/applications/{id}/approve", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectDriverApplicationAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PostAsync($"/drivers/applications/{id}/reject", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateDriverStatusAsync(int id, string status)
    {
        var client = await GetClientAsync();
        var body = new { status };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PatchAsync($"/drivers/{id}/status", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CreateDriverAsync(CreateDriverRequest dto)
    {
        var client = await GetClientAsync();
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/drivers/admin/create", content);
        return response.IsSuccessStatusCode;
    }

    // ── Ratings ────────────────────────────────────────────────
    public async Task<DriverRatingsDto?> GetAllDriverRatingsAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/drivers/ratings/all");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return new DriverRatingsDto(JsonSerializer.Deserialize<List<DriverRatingSummaryDto>>(json, JsonOptions) ?? []);
    }

    public async Task<(bool ok, string message)> RateDriverAsync(int driverId, int stars, string? name, string? comment)
    {
        var client = await GetClientAsync();
        var body   = new { stars, reviewerName = name, comment };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/drivers/{driverId}/rate", content);
        return response.IsSuccessStatusCode ? (true, "Thank you for your rating!") : (false, "Rating failed. Please try again.");
    }

    public async Task<AcademyReviewsResultDto?> GetAcademyReviewsAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/academy/reviews");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AcademyReviewsResultDto>(json, JsonOptions);
    }

    public async Task<(bool ok, string message)> SubmitAcademyReviewAsync(int stars, string? name, string? role, string? comment)
    {
        var client = await GetClientAsync();
        var body   = new { stars, reviewerName = name, reviewerRole = role, comment };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/academy/reviews", content);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return (false, "Submission failed.");
        try { var doc = JsonDocument.Parse(json); return (true, doc.RootElement.GetProperty("message").GetString() ?? "Submitted!"); }
        catch { return (true, "Submitted!"); }
    }

    public async Task<List<AcademyReviewAdminDto>> GetAllAcademyReviewsAdminAsync()
    {
        var client = await GetClientAsync();
        var response = await client.GetAsync("/academy/reviews/all");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AcademyReviewAdminDto>>(json, JsonOptions) ?? [];
    }

    public async Task<bool> ApproveAcademyReviewAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PostAsync($"/academy/reviews/{id}/approve", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAcademyReviewAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.DeleteAsync($"/academy/reviews/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteDriverRatingAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.DeleteAsync($"/drivers/ratings/{id}");
        return response.IsSuccessStatusCode;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}

// DTOs for API responses
public record SuggestionDto(int Id, string Title, string Description, string Category,
    string Location, string? ImagePath, DateTime CreatedAt, DateTime? EventDate, bool IsPinned,
    string? PostedBy, ContactDetailDto? ContactDetail,
    double? Latitude = null, double? Longitude = null);

public record ContactDetailDto(string? Phone, string? Email, string? WhatsApp, string? BookingUrl);

public record CreateSuggestionDto(string Title, string Description, string Category,
    string Location, string? ImagePath, string? Phone, string? Email, string? WhatsApp, string? BookingUrl,
    double? Latitude = null, double? Longitude = null);

public record UserSummaryDto(string Id, string Name, string? Email, string Role, bool IsPaidUser, bool IsBanned = false);

public record AdBannerDto(int Id, string BusinessName, string ImageUrl, string? LinkUrl, string? TagLine, bool IsActive = true, bool IsDemoData = false);

public record AcademyStatsDto(int TotalPlayers, int ActivePlayers, int SportsOffered, int TransportUsers, int Routes, int Pending);

public record AcademyRegisterDto(
    string PlayerName, int Age, string Sport,
    string ParentName, string ParentEmail, string ParentPhone, bool ConsentGiven,
    bool NeedsTransport = false, string? PickupAddress = null, string? Notes = null);

public record AcademyRegistrationDto(
    int Id, string PlayerName, int Age, string Sport,
    string ParentName, string ParentEmail, string ParentPhone,
    bool NeedsTransport, string? PickupAddress, string? AssignedRoute,
    string Status, bool IsDemoData, string? Notes, DateTime CreatedAt);

public record DriverDto(
    int Id, string FullName, string Phone, string Route,
    string? Vehicle, int Capacity, bool PoliceClearance, string Status,
    bool IsDemoData = false, string? LicenseNumber = null, DateTime CreatedAt = default);

public record DriverApplicationDto(
    int Id, string FullName, string Phone, string Email, string PreferredRoute,
    string? Vehicle, int Capacity, bool HasPoliceClearance,
    string? LicenseNumber, string? Notes, string Status, DateTime CreatedAt);

public record DriverApplicationRequest(
    string FullName, string Phone, string Email, string PreferredRoute,
    string? Vehicle = null, int Capacity = 7,
    bool HasPoliceClearance = false, string? LicenseNumber = null, string? Notes = null);

public record CreateDriverRequest(
    string FullName, string Phone, string Route,
    string? Vehicle = null, int Capacity = 7,
    bool PoliceClearance = false, string? LicenseNumber = null);

public record DriverRatingItemDto(string ReviewerName, int Stars, string? Comment);
public record DriverRatingSummaryDto(
    int Id, string FullName, string Route, string? Vehicle,
    double Average, int Count, List<DriverRatingItemDto> Latest);
public record DriverRatingsDto(List<DriverRatingSummaryDto> Drivers);

public record AcademyReviewItemDto(int Id, string ReviewerName, string? ReviewerRole, int Stars, string Comment, DateTime CreatedAt);
public record AcademyReviewsResultDto(double Average, int Count, List<AcademyReviewItemDto> Reviews);
public record AcademyReviewAdminDto(int Id, string ReviewerName, string? ReviewerRole, int Stars, string Comment, bool Approved, bool IsDemoData, DateTime CreatedAt);
