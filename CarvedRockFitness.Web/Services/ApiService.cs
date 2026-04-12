using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CarvedRockFitness.Web.Services;

public class ApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
{
    private async Task<HttpClient> GetClientAsync()
    {
        var client = httpClientFactory.CreateClient("GymApi");
        var token = await httpContextAccessor.HttpContext!
            .GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
        var client = await GetClientAsync();
        var response = await client.GetAsync("/users/members");
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<UserSummaryDto>>(json, JsonOptions) ?? [];
    }

    public async Task<bool> BanUserAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.PatchAsync($"/users/{id}/ban", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteIdentityUserAsync(int id)
    {
        var client = await GetClientAsync();
        var response = await client.DeleteAsync($"/users/{id}");
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

public record UserSummaryDto(int Id, string Name, string? Email, string Role, bool IsPaidUser, bool IsBanned = false);

public record AdBannerDto(int Id, string BusinessName, string ImageUrl, string? LinkUrl, string? TagLine, bool IsActive = true, bool IsDemoData = false);
