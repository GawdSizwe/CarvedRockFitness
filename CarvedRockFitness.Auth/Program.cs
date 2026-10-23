using CarvedRockFitness.Auth.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;

// ── Serilog bootstrap ──────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("OpenIddict", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "GymMeetups.Auth")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllersWithViews(options =>
{
    // Require a valid antiforgery token on every unsafe (POST/PUT/DELETE) request by default
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Rate-limit the login form and OIDC token endpoint to slow down credential stuffing / brute force
builder.Services.AddRateLimiter(options =>
{
    static string GetIp(HttpContext ctx) =>
        ctx.Request.Headers["Fly-Client-IP"].FirstOrDefault()
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "anon";

    options.AddPolicy("auth-write", context => RateLimitPartition.GetSlidingWindowLimiter(
        GetIp(context),
        _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(5),
            SegmentsPerWindow = 5, QueueLimit = 0
        }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Resolves the Postgres SSL mode from DB_SSL_MODE (or Database:SslMode config), defaulting to
// "Prefer" — i.e. encrypt when the server supports it, rather than always disabling TLS.
static Npgsql.SslMode ResolveSslMode(IConfiguration configuration)
{
    var raw = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? configuration["Database:SslMode"];
    return !string.IsNullOrEmpty(raw) && Enum.TryParse<Npgsql.SslMode>(raw, true, out var parsed)
        ? parsed
        : Npgsql.SslMode.Prefer;
}

// EF Core + Identity
var pgConnAuth = Environment.GetEnvironmentVariable("DATABASE_URL_AUTH");
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    if (!string.IsNullOrEmpty(pgConnAuth))
    {
        // Convert postgres://user:pass@host:port/db URI to Npgsql connection string
        var uri = new Uri(pgConnAuth);
        var userInfo = uri.UserInfo.Split(':');
        var npgsql = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = ResolveSslMode(builder.Configuration)
        };
        options.UseNpgsql(npgsql.ToString());
    }
    else
        options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDB"));
    options.UseOpenIddict();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // Bank-level password policy — mirrors the UI requirements checklist
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequiredUniqueChars    = 4;

    // Lock an account out after repeated failed attempts to slow down password guessing
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Identity cookie to stay on the Auth server
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "GymAuth.Session";
});

// OpenIddict — the OIDC server
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AuthDbContext>();
    })
    .AddServer(options =>
    {
        // OAuth2/OIDC endpoints
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserinfoEndpointUris("/connect/userinfo")
               .SetLogoutEndpointUris("/connect/logout");

        // Supported flows
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange(); // PKCE for the Web UI

        options.AllowRefreshTokenFlow();          // Silent token refresh
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        options.AllowClientCredentialsFlow(); // Machine-to-machine

        // Disable encryption so the API's standard JWT Bearer middleware can validate tokens
        options.DisableAccessTokenEncryption();

        // Scopes this server can grant
        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess,
            "gym-api");

        // RSA signing — load from file, auto-generate on first boot, or use dev ephemeral keys
        var certPath     = builder.Configuration["Auth:SigningCertPath"] ?? "/certs/signing.pfx";
        var certPassword = Environment.GetEnvironmentVariable("SIGNING_CERT_PASSWORD");
        if (string.IsNullOrEmpty(certPassword))
        {
            if (!builder.Environment.IsDevelopment())
                throw new InvalidOperationException(
                    "SIGNING_CERT_PASSWORD must be set in non-Development environments.");
            certPassword = "GymMeetupsAuth-Dev-Only!";
        }
        if (!File.Exists(certPath) && Directory.Exists(Path.GetDirectoryName(certPath)!))
        {
            // First boot in production — generate and persist a self-signed RSA cert
            using var rsa = System.Security.Cryptography.RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                "CN=GymMeetupsAuth", rsa,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            var generated = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(10));
            var pfxBytes = generated.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, certPassword);
            File.WriteAllBytes(certPath, pfxBytes);
            Log.Information("OpenIddict: generated and persisted new RSA signing cert to {CertPath}", certPath);
        }
        if (File.Exists(certPath))
        {
            var signingCert = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
            options.AddSigningCertificate(signingCert)
                   .AddEncryptionCertificate(signingCert);
            Log.Information("OpenIddict: using RSA certificate from {CertPath}", certPath);
        }
        else
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
            Log.Warning("OpenIddict: using DEVELOPMENT ephemeral RSA keys. Set Auth:SigningCertPath for production.");
        }

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough()
               .EnableLogoutEndpointPassthrough()
               .DisableTransportSecurityRequirement(); // Allow HTTP in dev
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed OIDC clients and roles on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await context.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "FreeUser", "PaidUser", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // ── Seed test users — local development only, never in Staging/Production ──
    if (app.Environment.IsDevelopment())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testUsers = new[]
        {
            new { FullName = "Admin User",   Email = "admin@gymmeetups.local",   Password = Guid.NewGuid().ToString("N") + "Aa1!", Role = "Admin",    IsPaidUser = true  },
            new { FullName = "Paid Member",  Email = "paid@gymmeetups.local",    Password = Guid.NewGuid().ToString("N") + "Aa1!", Role = "PaidUser", IsPaidUser = true  },
            new { FullName = "Free Member",  Email = "free@gymmeetups.local",    Password = Guid.NewGuid().ToString("N") + "Aa1!", Role = "FreeUser", IsPaidUser = false },
        };

        foreach (var seed in testUsers)
        {
            if (await userManager.FindByEmailAsync(seed.Email) == null)
            {
                var user = new ApplicationUser
                {
                    FullName   = seed.FullName,
                    UserName   = seed.Email,
                    Email      = seed.Email,
                    IsPaidUser = seed.IsPaidUser,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, seed.Password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, seed.Role);
                Log.Information("Dev seed: created {Email} with a random password (see logs only in Development)", seed.Email);
                if (result.Succeeded)
                    Log.Information("Dev seed password for {Email}: {Password}", seed.Email, seed.Password);
            }
        }
    }

    // Register gym-api scope with resource so 'aud' claim is populated in access tokens
    var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
    if (await scopeManager.FindByNameAsync("gym-api") == null)
    {
        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "gym-api",
            DisplayName = "Gym API",
            Resources = { "gym-api" }
        });
    }

    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    var gymWebDescriptor = new OpenIddictApplicationDescriptor
    {
        ClientId = "gym-web",
        ClientType = OpenIddictConstants.ClientTypes.Public,
        DisplayName = "Gym Meetups Web",
        RedirectUris =
        {
            new Uri("https://localhost:5001/signin-oidc"),
            new Uri("https://gawdsizwe-web.fly.dev/signin-oidc")
        },
        PostLogoutRedirectUris =
        {
            new Uri("https://localhost:5001/signout-callback-oidc"),
            new Uri("https://gawdsizwe-web.fly.dev/signout-callback-oidc")
        },
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.Logout,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
            OpenIddictConstants.Permissions.Prefixes.Scope + "gym-api"
        }
    };

    var existing = await manager.FindByClientIdAsync("gym-web");
    if (existing == null)
    {
        await manager.CreateAsync(gymWebDescriptor);
    }
    else
    {
        await manager.UpdateAsync(existing, gymWebDescriptor);
    }
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

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
        "img-src 'self' data:; " +
        "frame-ancestors 'none'";
    await next();
});

app.UseStaticFiles();
app.UseRouting();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/healthz");

app.Run();
