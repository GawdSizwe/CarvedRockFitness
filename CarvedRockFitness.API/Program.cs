using CarvedRockFitness.API.Authorization;
using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Extensions;
using CarvedRockFitness.API.Middleware;
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
                    SslMode = Npgsql.SslMode.Disable
                };
                dbContextOptions.UseNpgsql(npgsql.ToString());
            }
            else
                dbContextOptions.UseSqlServer(builder.Configuration["ConnectionStrings:CarvedRockFitnessDB"]);
            dbContextOptions.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
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

    builder.Services.AddHealthChecks();

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("read", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

        options.AddPolicy("write", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

        options.RejectionStatusCode = 429;
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
        });

    var app = builder.Build();

    // Configure the HTTP request pipeline

    // Global exception handling - must be first
    app.UseGlobalExceptionHandler();

    // Swagger UI at root
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CarvedRock Fitness API v1");
        options.RoutePrefix = string.Empty;
    });

    app.UseHttpsRedirection();
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
    app.MapHealthChecks("/healthz");

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
