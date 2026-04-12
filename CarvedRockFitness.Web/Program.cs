using CarvedRockFitness.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

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
                    ctx.RejectPrincipal();
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
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
