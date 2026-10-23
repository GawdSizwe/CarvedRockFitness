using CarvedRockFitness.API.Authorization;
using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Extensions;
using CarvedRockFitness.API.Middleware;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Configure Serilog — structured logging for scalable/distributed observability
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "GymMeetups.API")
    // Human-readable console (dev)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting CarvedRock Fitness API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // API controllers only
    builder.Services.AddControllers();

    // Runtime-togglable settings (seeded from appsettings, toggled via /admin/settings)
    builder.Services.AddSingleton(new RuntimeSettingsService
    {
        DemoSuggestionsVisible = builder.Configuration.GetValue("SeedData:DemoSuggestionsVisible", false)
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

    var pgConn = Environment.GetEnvironmentVariable("DATABASE_URL");
    builder.Services.AddDbContext<CarvedRockFitnessDbContext>(
        dbContextOptions =>
        {
            if (!string.IsNullOrEmpty(pgConn))
            {
                var uri = new Uri(pgConn);
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
                dbContextOptions.UseNpgsql(npgsql.ToString());
            }
            else
                dbContextOptions.UseSqlServer(builder.Configuration["ConnectionStrings:CarvedRockFitnessDB"]);
        });

    // Distributed cache — memory for dev, Redis for production (set Cache:Redis in config)
    var redisConnectionString = builder.Configuration["Cache:Redis"];
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "GymMeetups:";
        });
        Log.Information("Cache: Redis at {RedisConnection}", redisConnectionString);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        Log.Information("Cache: In-memory (dev mode — set Cache:Redis for production)");
    }

    // CORS — allow the Web frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WebPolicy", policy =>
            policy.WithOrigins(
                    builder.Configuration["Cors:WebOrigin"] ?? "https://localhost:5001",
                    "http://localhost:5001")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });

    // Register application services (repositories, services, caching)
    builder.Services.AddApplicationServices();

    // Health checks — liveness (self) + readiness (DB connectivity)
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"))
        .AddDbContextCheck<CarvedRockFitnessDbContext>("database");

    // Per-endpoint rate limiting — sliding window, IP-keyed, with Retry-After + structured logging
    builder.Services.AddRateLimiter(options =>
    {
        // "Fly-Client-IP" is set by the Fly.io edge proxy to the real end-user IP and cannot be
        // spoofed by callers; the Web app forwards the same header when it proxies requests here,
        // so limits apply per end user even though every Web-originated call shares one connection.
        static string GetIp(HttpContext ctx) =>
            ctx.Request.Headers["Fly-Client-IP"].FirstOrDefault()
            ?? ctx.Connection.RemoteIpAddress?.ToString()
            ?? "anon";

        // For authenticated write actions, key on the user's subject claim too so one logged-in
        // user can't bypass the IP-based limit by rotating IPs (and so shared-IP users aren't
        // penalized by someone else's writes).
        static string GetUserOrIp(HttpContext ctx)
        {
            var sub = ctx.User.FindFirst("sub")?.Value;
            return !string.IsNullOrEmpty(sub) ? $"user:{sub}" : $"ip:{GetIp(ctx)}";
        }

        // General reads — 100 req/min per IP
        options.AddPolicy("general-read", context => RateLimitPartition.GetSlidingWindowLimiter(
            GetIp(context),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100, Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6, QueueLimit = 0
            }));

        // Admin endpoints — 30 req/min per authenticated admin
        options.AddPolicy("admin", context => RateLimitPartition.GetSlidingWindowLimiter(
            GetUserOrIp(context),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30, Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6, QueueLimit = 0
            }));

        // Suggestions POST — 5 per 10 min per authenticated user
        options.AddPolicy("suggestions-write", context => RateLimitPartition.GetSlidingWindowLimiter(
            GetUserOrIp(context),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5, Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 5, QueueLimit = 0
            }));

        // Ratings POST — 10 per 10 min per IP
        options.AddPolicy("ratings-write", context => RateLimitPartition.GetSlidingWindowLimiter(
            GetIp(context),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10, Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 5, QueueLimit = 0
            }));

        // Academy registration POST — 3 per 10 min per IP
        options.AddPolicy("academy-register", context => RateLimitPartition.GetSlidingWindowLimiter(
            GetIp(context),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 3, Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 5, QueueLimit = 0
            }));

        // Driver applications POST — 5 per 10 min per IP
        options.AddPolicy("drivers-write", context => RateLimitPartition.GetSlidingWindowLimiter(
            GetIp(context),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5, Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 5, QueueLimit = 0
            }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.Headers["Retry-After"] = "60";
            await context.HttpContext.Response.WriteAsync(
                "Rate limit exceeded. Please try again later.", cancellationToken);
            Log.Warning("Rate limit exceeded: {Method} {Path} from {IP}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                GetIp(context.HttpContext));
        };
    });

    // Swagger/OpenAPI documentation
    builder.Services.AddSwaggerDocumentation();

    // Authorization handlers (reused from existing API policies)
    builder.Services.AddScoped<IAuthorizationHandler, MustOwnRoutineHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, MustTrainRoutineOwnerHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, EitherRequirementHandler>();

    builder.Services.AddAuthorization(authorizationOptions =>
    {
        authorizationOptions.AddPolicy(PolicyMetadata.MustOwnRoutine,
             policyBuilder =>
             {
                 policyBuilder.RequireAuthenticatedUser();
                 policyBuilder.AddRequirements(new MustOwnRoutineRequirement());
             });

        authorizationOptions.AddPolicy(PolicyMetadata.MustOwnRoutineOrTrainOwner,
             policyBuilder =>
             {
                 policyBuilder.RequireAuthenticatedUser();
                 policyBuilder.AddRequirements(new EitherRequirementRequirement(
                     new MustOwnRoutineRequirement(),
                     new MustTrainRoutineOwnerRequirement("Trainer", "Trainee")));
             });
    });

    // JWT Bearer — tokens issued by CarvedRockFitness.Auth (OpenIddict)
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"]; // e.g. https://localhost:5002
            options.Audience = "gym-api";
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = false; // Fly.io proxy handles HTTPS
            options.TokenValidationParameters.RoleClaimType = "role";
            options.TokenValidationParameters.NameClaimType = "name";
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Error("JWT validation failed: {Error}", ctx.Exception.Message);
                    return Task.CompletedTask;
                },
                OnChallenge = ctx =>
                {
                    Log.Warning("JWT challenge: {Error} — {ErrorDescription}", ctx.Error, ctx.ErrorDescription);
                    return Task.CompletedTask;
                }
            };
        });

    var app = builder.Build();

    // Auto-apply schema changes on startup (PostgreSQL only — this raw SQL uses Postgres syntax
    // and is skipped for SQL Server/LocalDB and the InMemory provider used by integration tests;
    // those rely on the EF Core migrations in Migrations/ instead, applied via `dotnet ef database update`).
    if (!string.IsNullOrEmpty(pgConn))
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CarvedRockFitnessDbContext>();
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "AppSettings" (
                "Key"       TEXT PRIMARY KEY,
                "Value"     TEXT NOT NULL,
                "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "AcademyRegistrations" (
                "Id"             SERIAL PRIMARY KEY,
                "PlayerName"     TEXT NOT NULL,
                "Age"            INTEGER NOT NULL DEFAULT 0,
                "Sport"          TEXT NOT NULL,
                "ParentName"     TEXT NOT NULL,
                "ParentEmail"    TEXT NOT NULL,
                "ParentPhone"    TEXT NOT NULL,
                "NeedsTransport" BOOLEAN NOT NULL DEFAULT FALSE,
                "PickupAddress"  TEXT,
                "Notes"          TEXT,
                "Status"         TEXT NOT NULL DEFAULT 'Pending',
                "AssignedRoute"  TEXT,
                "IsDemoData"     BOOLEAN NOT NULL DEFAULT FALSE,
                "CreatedAt"      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );
            """);

        // Seed academy demo data if table is empty
        // Demo/seed data is for local development and sandbox demos only — never seeded in
        // Staging/Production, where real registrations, drivers etc. should be entered by admins.
        var count = db.AcademyRegistrations.Count();
        if (count == 0 && app.Environment.IsDevelopment())
        {
            db.Database.ExecuteSqlRaw("""
                INSERT INTO "AcademyRegistrations" ("PlayerName","Age","Sport","ParentName","ParentEmail","ParentPhone","NeedsTransport","PickupAddress","Status","AssignedRoute","IsDemoData","CreatedAt") VALUES
                ('Sipho Dlamini',12,'Football','Nomsa Dlamini','nomsa@email.com','083 111 2001',TRUE,'Tembisa CBD','Active','Tembisa CBD',TRUE,'2025-02-01'),
                ('Thabo Mokoena',14,'Football','Grace Mokoena','grace@email.com','072 222 3002',TRUE,'Ivory Park','Active','Ivory Park',TRUE,'2025-02-03'),
                ('Lerato Khumalo',10,'Cricket','Peter Khumalo','peter@email.com','061 333 4003',FALSE,NULL,'Active',NULL,TRUE,'2025-02-05'),
                ('Naledi Sithole',11,'Athletics','Maria Sithole','maria@email.com','082 444 5004',TRUE,'Kwa-Thema','Active','Kwa-Thema',TRUE,'2025-02-08'),
                ('Kabelo Mthembu',16,'Football','James Mthembu','james@email.com','073 555 6005',TRUE,'Rabie Ridge','Active','Rabie Ridge',TRUE,'2025-02-10'),
                ('Zanele Nkosi',9,'Tennis','Ruth Nkosi','ruth@email.com','084 666 7006',FALSE,NULL,'Active',NULL,TRUE,'2025-02-12'),
                ('Bongani Zulu',13,'Football','David Zulu','david@email.com','065 777 8007',TRUE,'Ebony Park','Active','Ebony Park',TRUE,'2025-02-15'),
                ('Mpho Mahlangu',15,'Cricket','Sarah Mahlangu','sarah@email.com','071 888 9008',TRUE,'Clayville','Confirmed','Clayville',TRUE,'2025-03-01'),
                ('Ayanda Ndlovu',8,'Athletics','Linda Ndlovu','linda@email.com','062 999 1009',FALSE,NULL,'Pending',NULL,TRUE,'2025-03-05'),
                ('Tshepo Molefe',17,'Football','Anna Molefe','anna@email.com','083 101 2010',TRUE,'Tembisa CBD','Active','Tembisa CBD',TRUE,'2025-03-08'),
                ('Palesa Maseko',11,'Tennis','Joseph Maseko','joseph@email.com','074 202 3011',FALSE,NULL,'Active',NULL,TRUE,'2025-03-10'),
                ('Siyabonga Cele',14,'Cricket','Thandi Cele','thandi@email.com','081 303 4012',TRUE,'Ivory Park','Active','Ivory Park',TRUE,'2025-03-12');
                """);
        }

        // Drivers table
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Drivers" (
                "Id"              SERIAL PRIMARY KEY,
                "FullName"        TEXT NOT NULL,
                "Phone"           TEXT NOT NULL,
                "Route"           TEXT NOT NULL,
                "Vehicle"         TEXT,
                "Capacity"        INTEGER NOT NULL DEFAULT 7,
                "PoliceClearance" BOOLEAN NOT NULL DEFAULT FALSE,
                "LicenseNumber"   TEXT,
                "Status"          TEXT NOT NULL DEFAULT 'Active',
                "IsDemoData"      BOOLEAN NOT NULL DEFAULT FALSE,
                "CreatedAt"       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );
            """);

        // DriverApplications table
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "DriverApplications" (
                "Id"                 SERIAL PRIMARY KEY,
                "FullName"           TEXT NOT NULL,
                "Phone"              TEXT NOT NULL,
                "Email"              TEXT NOT NULL,
                "PreferredRoute"     TEXT NOT NULL,
                "Vehicle"            TEXT,
                "Capacity"           INTEGER NOT NULL DEFAULT 7,
                "HasPoliceClearance" BOOLEAN NOT NULL DEFAULT FALSE,
                "LicenseNumber"      TEXT,
                "Notes"              TEXT,
                "Status"             TEXT NOT NULL DEFAULT 'Pending',
                "CreatedAt"          TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );
            """);

        // Seed demo drivers if empty
        if (!db.Drivers.Any() && app.Environment.IsDevelopment())
        {
            db.Database.ExecuteSqlRaw("""
                INSERT INTO "Drivers" ("FullName","Phone","Route","Vehicle","Capacity","PoliceClearance","LicenseNumber","Status","IsDemoData","CreatedAt") VALUES
                ('Siphamandla Ndlovu','082 301 4421','Tembisa CBD','Toyota Quantum (White)',12,TRUE,'GP 23 XY 7654','Active',TRUE,'2025-01-10'),
                ('Bonginkosi Mthembu','073 812 5503','Ivory Park','Toyota HiAce (Silver)',14,TRUE,'GP 45 AB 1122','Active',TRUE,'2025-01-15'),
                ('Themba Radebe','061 904 3312','Kwa-Thema','Nissan NV350 (Blue)',10,TRUE,'GP 67 CD 3344','Active',TRUE,'2025-01-20'),
                ('Nkosinathi Dlamini','083 456 7788','Rabie Ridge','Toyota Quantum (Black)',12,FALSE,'GP 89 EF 5566','Active',TRUE,'2025-02-01'),
                ('Mthokozisi Cele','074 223 9901','Ebony Park','Toyota HiAce (White)',14,TRUE,'GP 12 GH 7788','Active',TRUE,'2025-02-05'),
                ('Lungelo Khumalo','082 774 1123','Clayville','Ford Transit (Grey)',9,TRUE,'GP 34 IJ 9900','Active',TRUE,'2025-02-10');
                """);
        }

        // DriverRatings table
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "DriverRatings" (
                "Id"           SERIAL PRIMARY KEY,
                "DriverId"     INTEGER NOT NULL,
                "ReviewerName" TEXT NOT NULL DEFAULT 'Anonymous',
                "Stars"        INTEGER NOT NULL DEFAULT 5,
                "Comment"      TEXT,
                "IsDemoData"   BOOLEAN NOT NULL DEFAULT FALSE,
                "CreatedAt"    TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );
            """);

        // AcademyReviews table
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "AcademyReviews" (
                "Id"           SERIAL PRIMARY KEY,
                "ReviewerName" TEXT NOT NULL DEFAULT 'Anonymous',
                "ReviewerRole" TEXT,
                "Stars"        INTEGER NOT NULL DEFAULT 5,
                "Comment"      TEXT NOT NULL DEFAULT '',
                "Approved"     BOOLEAN NOT NULL DEFAULT FALSE,
                "IsDemoData"   BOOLEAN NOT NULL DEFAULT FALSE,
                "CreatedAt"    TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );
            """);

        // Seed demo driver ratings (referencing driver IDs 1–6)
        if (!db.DriverRatings.Any() && app.Environment.IsDevelopment())
        {
            db.Database.ExecuteSqlRaw("""
                INSERT INTO "DriverRatings" ("DriverId","ReviewerName","Stars","Comment","IsDemoData","CreatedAt") VALUES
                (1,'Nomsa Dlamini',5,'Siphamandla is always on time and very professional. Our kids feel safe.',TRUE,'2025-03-01'),
                (1,'Grace Mokoena',5,'Excellent driver — communicates well on WhatsApp. Highly recommend.',TRUE,'2025-03-05'),
                (1,'Maria Sithole',4,'Very reliable, only missed once due to a flat tyre.',TRUE,'2025-03-10'),
                (2,'Peter Khumalo',5,'Bonginkosi is fantastic with the kids. Always smiling.',TRUE,'2025-03-02'),
                (2,'Ruth Nkosi',4,'Good driver, vehicle is always clean.',TRUE,'2025-03-08'),
                (3,'James Mthembu',5,'Themba is punctual and the kids love him.',TRUE,'2025-03-03'),
                (3,'David Zulu',5,'Best driver on the route. Never late.',TRUE,'2025-03-09'),
                (4,'Sarah Mahlangu',3,'Nkosinathi is okay but sometimes texts while driving.',TRUE,'2025-03-04'),
                (4,'Linda Ndlovu',4,'Generally good but communication could be better.',TRUE,'2025-03-11'),
                (5,'Anna Molefe',5,'Mthokozisi is wonderful. My son looks forward to the ride!',TRUE,'2025-03-06'),
                (5,'Joseph Maseko',5,'Always early, very safe driver.',TRUE,'2025-03-12'),
                (6,'Thandi Cele',5,'Lungelo goes above and beyond for the children.',TRUE,'2025-03-07');
                """);
        }

        // Seed demo academy reviews
        if (!db.AcademyReviews.Any() && app.Environment.IsDevelopment())
        {
            db.Database.ExecuteSqlRaw("""
                INSERT INTO "AcademyReviews" ("ReviewerName","ReviewerRole","Stars","Comment","Approved","IsDemoData","CreatedAt") VALUES
                ('Nomsa Dlamini','Parent',5,'YVEDA Academy has completely transformed my son. The coaches are incredible and the transport service gives me peace of mind.',TRUE,TRUE,'2025-03-01'),
                ('Coach Baloyi','Coach',5,'World-class facilities and a genuine commitment to youth development. Proud to be part of this.',TRUE,TRUE,'2025-03-05'),
                ('Grace Mokoena','Parent',5,'My daughter went from shy to a confident athlete in 3 months. Cannot thank YVEDA enough.',TRUE,TRUE,'2025-03-08'),
                ('Sipho Radebe','Player',5,'The best academy in Tembisa! The coaches push us hard but always believe in us.',TRUE,TRUE,'2025-03-10'),
                ('Peter Khumalo','Parent',4,'Great programme overall. Would love to see more cricket equipment.',TRUE,TRUE,'2025-03-12'),
                ('Maria Sithole','Parent',5,'Safe transport, professional coaches, great kids programme. 10/10.',TRUE,TRUE,'2025-03-15'),
                ('Thabo Nkosi','Player',5,'YVEDA changed my life. I got scouted because of this academy!',TRUE,TRUE,'2025-03-18'),
                ('Linda Cele','Parent',4,'Very happy with the progress my child has made. Transport is reliable.',TRUE,TRUE,'2025-03-20');
                """);
        }
    }

    // Load the persisted demo-visibility toggle (if any) so it survives restarts/redeploys;
    // falls back to the appsettings-configured default when no row exists or the table isn't
    // present yet (e.g. local SQL Server dev before running the AddAppSettings migration).
    using (var scope = app.Services.CreateScope())
    {
        var settings = scope.ServiceProvider.GetRequiredService<RuntimeSettingsService>();
        var store = scope.ServiceProvider.GetRequiredService<AppSettingsStore>();
        var persisted = await store.TryGetBoolAsync(RuntimeSettingsService.DemoSuggestionsVisibleKey);
        if (persisted.HasValue)
            settings.DemoSuggestionsVisible = persisted.Value;
    }

    // Configure the HTTP request pipeline

    // Global exception handling - must be first
    app.UseGlobalExceptionHandler();

    // Swagger UI at root — only in Development, or when explicitly opted into via config
    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("Swagger:Enabled", false))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CarvedRock Fitness API v1");
            options.RoutePrefix = string.Empty;
        });
    }

    app.UseCors("WebPolicy");

    // Request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Liveness — is the process up?
    app.MapHealthChecks("/healthz/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Name == "self",
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Readiness — is the DB reachable?
    app.MapHealthChecks("/healthz/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Name == "database",
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Overall — all checks combined (Grafana polls this)
    app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }

