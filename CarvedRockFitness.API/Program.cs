using CarvedRockFitness.API.Authorization;
using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Extensions;
using CarvedRockFitness.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/carvedrock-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting CarvedRock Fitness API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();

    builder.Services.AddDbContext<CarvedRockFitnessDbContext>(
        dbContextOptions => dbContextOptions.UseSqlServer(
            builder.Configuration["ConnectionStrings:CarvedRockFitnessDB"]));

    // Register application services (repositories, services, caching)
    builder.Services.AddApplicationServices();

    // Swagger/OpenAPI documentation
    builder.Services.AddSwaggerDocumentation();

    // Authorization
    builder.Services.AddScoped<IAuthorizationHandler, MustOwnRoutineHandler>();
    builder.Services.AddAuthorization(authorizationOptions =>
    {
        authorizationOptions.AddPolicy(PolicyMetadata.MustOwnRoutine,
             policyBuilder =>
             {
                 policyBuilder.RequireAuthenticatedUser();
                 policyBuilder.AddRequirements(new MustOwnRoutineRequirement());
             });
    });

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer(options => options.MapInboundClaims = false);

    var app = builder.Build();

    // Configure the HTTP request pipeline

    // Global exception handling - must be first
    app.UseGlobalExceptionHandler();

    // Swagger UI (available in all environments for demo purposes)
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CarvedRock Fitness API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });

    app.UseHttpsRedirection();

    // Request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
