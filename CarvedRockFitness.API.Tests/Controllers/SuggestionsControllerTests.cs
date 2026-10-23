using CarvedRockFitness.API.Controllers;
using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CarvedRockFitness.API.Tests.Controllers;

public class SuggestionsControllerTests : IDisposable
{
    private readonly CarvedRockFitnessDbContext _db;
    private readonly SuggestionsController _sut;
    private readonly Mock<IDistributedCache> _cacheMock;

    public SuggestionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<CarvedRockFitnessDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new CarvedRockFitnessDbContext(options);
        _cacheMock = new Mock<IDistributedCache>();

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((byte[]?)null);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                  It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var settings = new RuntimeSettingsService { DemoSuggestionsVisible = true };
        var logger = new Mock<ILogger<SuggestionsController>>().Object;

        _sut = new SuggestionsController(_db, _cacheMock.Object, settings, logger);
    }

    // ── helpers ──────────────────────────────────────────────────────────
    private User SeedUser(int id = 1)
    {
        var user = new User
        {
            Id = id, Name = "Test User", UserName = $"user{id}@test.com",
            Email = $"user{id}@test.com", SecurityStamp = Guid.NewGuid().ToString(),
            Role = "PaidUser"
        };
        _db.Users.Add(user);
        return user;
    }

    private Suggestion SeedSuggestion(int id, string title, bool isHidden = false,
        bool isPinned = false, int priority = 0, bool isDemoData = false)
    {
        var s = new Suggestion
        {
            Id = id, Title = title, Description = "desc", Category = "Football",
            Location = "Soweto", CreatedAt = DateTime.UtcNow.AddMinutes(-id),
            IsHidden = isHidden, IsPinned = isPinned, Priority = priority,
            IsDemoData = isDemoData
        };
        _db.Suggestions.Add(s);
        return s;
    }

    public void Dispose() => _db.Dispose();

    // ── feed ordering ─────────────────────────────────────────────────────
    [Fact]
    public async Task GetSuggestions_OrdersByPinned_ThenPriority_ThenDate()
    {
        SeedUser();
        SeedSuggestion(1, "Normal Old",    isPinned: false, priority: 0);
        SeedSuggestion(2, "High Priority", isPinned: false, priority: 5);
        SeedSuggestion(3, "Pinned",        isPinned: true,  priority: 0);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestions(1) as ContentResult;

        result.Should().NotBeNull();
        var json = result!.Content!;
        // Pinned must appear before High Priority, which must appear before Normal
        json.IndexOf("Pinned").Should().BeLessThan(json.IndexOf("High Priority"));
        json.IndexOf("High Priority").Should().BeLessThan(json.IndexOf("Normal Old"));
    }

    // ── hidden filter ────────────────────────────────────────────────────
    [Fact]
    public async Task GetSuggestions_ExcludesHiddenListings()
    {
        SeedUser();
        SeedSuggestion(1, "Visible");
        SeedSuggestion(2, "Hidden One", isHidden: true);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestions(1) as ContentResult;

        result!.Content.Should().Contain("Visible");
        result.Content.Should().NotContain("Hidden One");
    }

    // ── demo data filter ─────────────────────────────────────────────────
    [Fact]
    public async Task GetSuggestions_WhenDemoHidden_ExcludesDemoListings()
    {
        SeedUser();
        SeedSuggestion(1, "Real Listing", isDemoData: false);
        SeedSuggestion(2, "Demo Listing", isDemoData: true);
        await _db.SaveChangesAsync();

        var hiddenSettings = new RuntimeSettingsService { DemoSuggestionsVisible = false };
        var sut = new SuggestionsController(
            _db, _cacheMock.Object, hiddenSettings,
            new Mock<ILogger<SuggestionsController>>().Object);

        var result = await sut.GetSuggestions(1) as ContentResult;

        result!.Content.Should().Contain("Real Listing");
        result.Content.Should().NotContain("Demo Listing");
    }

    [Fact]
    public async Task GetSuggestions_WhenDemoVisible_IncludesDemoListings()
    {
        SeedUser();
        SeedSuggestion(1, "Real Listing", isDemoData: false);
        SeedSuggestion(2, "Demo Listing", isDemoData: true);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestions(1) as ContentResult;

        result!.Content.Should().Contain("Real Listing");
        result.Content.Should().Contain("Demo Listing");
    }

    // ── pin toggle ───────────────────────────────────────────────────────
    [Fact]
    public async Task PinSuggestion_TogglesIsPinned()
    {
        SeedUser();
        SeedSuggestion(10, "Pin Me");
        await _db.SaveChangesAsync();

        var result = await _sut.PinSuggestion(10);

        result.Should().BeOfType<NoContentResult>();
        var s = await _db.Suggestions.FindAsync(10);
        s!.IsPinned.Should().BeTrue();

        await _sut.PinSuggestion(10);
        await _db.Entry(s).ReloadAsync();
        s.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task PinSuggestion_NonExistent_ReturnsNotFound()
    {
        var result = await _sut.PinSuggestion(999);
        result.Should().BeOfType<NotFoundResult>();
    }

    // ── hide toggle ──────────────────────────────────────────────────────
    [Fact]
    public async Task HideSuggestion_TogglesIsHidden()
    {
        SeedUser();
        SeedSuggestion(20, "Hide Me");
        await _db.SaveChangesAsync();

        var result = await _sut.HideSuggestion(20);

        result.Should().BeOfType<NoContentResult>();
        var s = await _db.Suggestions.FindAsync(20);
        s!.IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task HideSuggestion_NonExistent_ReturnsNotFound()
    {
        var result = await _sut.HideSuggestion(999);
        result.Should().BeOfType<NotFoundResult>();
    }

    // ── boost ────────────────────────────────────────────────────────────
    [Fact]
    public async Task BoostSuggestion_IncrementsPriority()
    {
        SeedUser();
        SeedSuggestion(30, "Boost Me", priority: 2);
        await _db.SaveChangesAsync();

        await _sut.BoostSuggestion(30);
        await _sut.BoostSuggestion(30);

        var s = await _db.Suggestions.FindAsync(30);
        s!.Priority.Should().Be(4);
    }

    [Fact]
    public async Task BoostSuggestion_NonExistent_ReturnsNotFound()
    {
        var result = await _sut.BoostSuggestion(999);
        result.Should().BeOfType<NotFoundResult>();
    }

    // ── get single ───────────────────────────────────────────────────────
    [Fact]
    public async Task GetSuggestion_ExistingId_ReturnsOk()
    {
        SeedUser();
        SeedSuggestion(40, "Find Me");
        await _db.SaveChangesAsync();

        var result = await _sut.GetSuggestion(40) as OkObjectResult;
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSuggestion_NonExistingId_ReturnsNotFound()
    {
        var result = await _sut.GetSuggestion(999);
        result.Should().BeOfType<NotFoundResult>();
    }
}
