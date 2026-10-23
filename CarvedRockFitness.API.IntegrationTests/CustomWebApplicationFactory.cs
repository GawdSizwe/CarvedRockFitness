using CarvedRockFitness.API.DbContexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarvedRockFitness.API.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove all EF Core and SQL Server related services
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<CarvedRockFitnessDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                d.ServiceType.FullName?.Contains("SqlServer") == true ||
                d.ImplementationType?.FullName?.Contains("SqlServer") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with InMemory database
            services.AddDbContext<CarvedRockFitnessDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Register a fake authentication scheme that authenticates every request as an
            // Admin, and make it the default (overriding Program.cs's "Bearer" JWT scheme), so
            // tests can exercise [Authorize(Roles = "Admin")] endpoints without a real
            // OpenIddict-issued token.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarvedRockFitnessDbContext>();
        db.Database.EnsureCreated();
    }
}
