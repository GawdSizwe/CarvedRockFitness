using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Persist Data Protection keys to a durable volume outside Development so cookies/antiforgery
// tokens survive restarts and redeploys instead of invalidating every user's session each time
// (see fly.toml: the "web_keys" volume is mounted at Web:DataProtectionKeysPath).
var keysPath = builder.Configuration["Web:DataProtectionKeysPath"];
if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(keysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("CarvedRockFitness.Web");
}

builder.Services.AddControllersWithViews(options =>
{
    // Require a valid antiforgery token on every unsafe (POST/PUT/DELETE) request by default
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
}).AddRazorRuntimeCompilation();

// Cookie + OIDC authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "GymMeetups.Web";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Events.OnValidatePrincipal = async ctx =>
    {
        var expText = ctx.Properties.GetTokenValue("expires_at");
        if (expText != null && DateTimeOffset.TryParse(expText, out var exp) && exp < DateTimeOffset.UtcNow.AddMinutes(5))
        {
            var refreshToken = ctx.Properties.GetTokenValue("refresh_token");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var oidcOptions = ctx.HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<OpenIdConnectOptions>>()
                    .Get(OpenIdConnectDefaults.AuthenticationScheme);
                var tokenEndpoint = $"{oidcOptions.Authority}/connect/token";
                var http = ctx.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
                var body = new Dictionary<string, string>
                {
                    ["grant_type"]    = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"]     = oidcOptions.ClientId!
                };
                var resp = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body));
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
                    if (json != null)
                    {
                        ctx.Properties.UpdateTokenValue("access_token",  json.RootElement.GetProperty("access_token").GetString()!);
                        ctx.Properties.UpdateTokenValue("refresh_token", json.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString()! : refreshToken);
                        ctx.Properties.UpdateTokenValue("expires_at",    DateTimeOffset.UtcNow.AddSeconds(json.RootElement.GetProperty("expires_in").GetInt32()).ToString("o"));
                        ctx.ShouldRenew = true;
                    }
                }
                else
                {
                    // Refresh failed — sign out the cookie so the user gets a clean re-login
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        }
    };
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];       // https://localhost:5002
    options.ClientId = "gym-web";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.SaveTokens = true;                                          // Store access token in cookie
    options.UseTokenLifetime = false;                                   // Let cookie manage expiry
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("roles");
    options.Scope.Add("offline_access");
    options.Scope.Add("gym-api");
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
    options.ClaimActions.MapJsonKey(ClaimTypes.Role, "role");
    options.ClaimActions.MapJsonKey(ClaimTypes.Role, "roles");
    options.RequireHttpsMetadata = false;                               // Allow HTTP in dev

    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.ResponseMode = OpenIdConnectResponseMode.Query;
});

// Typed HttpClient for the API — automatically attaches the access token
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<PayFastService>();
builder.Services.AddHttpClient("GymApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});
builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Oidc:Authority"]!);
});

var app = builder.Build();

var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
    headers["X-Frame-Options"] = "DENY";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' cdn.jsdelivr.net unpkg.com; " +
        "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net fonts.googleapis.com unpkg.com; " +
        "font-src 'self' cdn.jsdelivr.net fonts.gstatic.com; " +
        "img-src 'self' data: *.tile.openstreetmap.org; " +
        "connect-src 'self' nominatim.openstreetmap.org; " +
        "frame-ancestors 'none'";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
