using System.Security.Claims;
using CarvedRockFitness.API.Authorization;
using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarvedRockFitness.API.Tests.Authorization;

public class RoutineAuthorizationHandlerTests : IDisposable
{
    private readonly CarvedRockFitnessDbContext _db;

    public RoutineAuthorizationHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CarvedRockFitnessDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CarvedRockFitnessDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private static DefaultHttpContext HttpContextWithRouteId(int id)
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData(new RouteValueDictionary { ["id"] = id.ToString() });
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        return httpContext;
    }

    private static ClaimsPrincipal PrincipalWithSub(int userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString())], "TestAuth"));

    // ── MustOwnRoutineHandler ───────────────────────────────────────────────

    [Fact]
    public async Task MustOwnRoutineHandler_Owner_Succeeds()
    {
        var routineServiceMock = new Mock<IRoutineService>();
        routineServiceMock.Setup(s => s.UserOwnsRoutineAsync(10, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new MustOwnRoutineHandler(routineServiceMock.Object, new Mock<ILogger<MustOwnRoutineHandler>>().Object);

        var requirement = new MustOwnRoutineRequirement();
        var context = new AuthorizationHandlerContext([requirement], PrincipalWithSub(1), HttpContextWithRouteId(10));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task MustOwnRoutineHandler_NonOwner_Fails()
    {
        var routineServiceMock = new Mock<IRoutineService>();
        routineServiceMock.Setup(s => s.UserOwnsRoutineAsync(10, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new MustOwnRoutineHandler(routineServiceMock.Object, new Mock<ILogger<MustOwnRoutineHandler>>().Object);

        var requirement = new MustOwnRoutineRequirement();
        var context = new AuthorizationHandlerContext([requirement], PrincipalWithSub(2), HttpContextWithRouteId(10));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    // ── MustTrainRoutineOwnerHandler ─────────────────────────────────────────

    private async Task<(User trainer, User trainee, Routine routine)> SeedTrainerTraineeRoutineAsync()
    {
        var trainer = new User
        {
            Id = 1, Name = "Coach", Role = "Trainer", UserName = "coach@test.com",
            Email = "coach@test.com", SecurityStamp = Guid.NewGuid().ToString()
        };
        var trainee = new User
        {
            Id = 2, Name = "Athlete", Role = "Trainee", TrainerId = 1, UserName = "athlete@test.com",
            Email = "athlete@test.com", SecurityStamp = Guid.NewGuid().ToString()
        };
        var routine = new Routine { Id = 10, Name = "Leg day", Description = "Squats", UserId = 2 };

        _db.Users.AddRange(trainer, trainee);
        _db.Routines.Add(routine);
        await _db.SaveChangesAsync();
        return (trainer, trainee, routine);
    }

    [Fact]
    public async Task MustTrainRoutineOwnerHandler_AssignedTrainer_Succeeds()
    {
        await SeedTrainerTraineeRoutineAsync();
        var handler = new MustTrainRoutineOwnerHandler(_db, new Mock<ILogger<MustOwnRoutineHandler>>().Object);

        var requirement = new MustTrainRoutineOwnerRequirement("Trainer", "Trainee");
        var context = new AuthorizationHandlerContext([requirement], PrincipalWithSub(1), HttpContextWithRouteId(10));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task MustTrainRoutineOwnerHandler_UnrelatedTrainer_Fails()
    {
        await SeedTrainerTraineeRoutineAsync();
        var otherTrainer = new User
        {
            Id = 3, Name = "Other Coach", Role = "Trainer", UserName = "other@test.com",
            Email = "other@test.com", SecurityStamp = Guid.NewGuid().ToString()
        };
        _db.Users.Add(otherTrainer);
        await _db.SaveChangesAsync();

        var handler = new MustTrainRoutineOwnerHandler(_db, new Mock<ILogger<MustOwnRoutineHandler>>().Object);
        var requirement = new MustTrainRoutineOwnerRequirement("Trainer", "Trainee");
        var context = new AuthorizationHandlerContext([requirement], PrincipalWithSub(3), HttpContextWithRouteId(10));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    // ── EitherRequirementHandler ─────────────────────────────────────────────

    private static IServiceProvider BuildServiceProvider(bool firstSucceeds, bool secondSucceeds)
    {
        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(r => r.Contains(FirstMarker))))
            .ReturnsAsync(firstSucceeds ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        authServiceMock
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(r => r.Contains(SecondMarker))))
            .ReturnsAsync(secondSucceeds ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        var services = new ServiceCollection();
        services.AddSingleton(authServiceMock.Object);
        return services.BuildServiceProvider();
    }

    private static readonly MustOwnRoutineRequirement FirstMarker = new();
    private static readonly MustTrainRoutineOwnerRequirement SecondMarker = new("Trainer", "Trainee");

    [Fact]
    public async Task EitherRequirementHandler_FirstSucceeds_Succeeds()
    {
        var handler = new EitherRequirementHandler(BuildServiceProvider(firstSucceeds: true, secondSucceeds: false));
        var requirement = new EitherRequirementRequirement(FirstMarker, SecondMarker);
        var context = new AuthorizationHandlerContext([requirement], PrincipalWithSub(1), new DefaultHttpContext());

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task EitherRequirementHandler_SecondSucceeds_Succeeds()
    {
        var handler = new EitherRequirementHandler(BuildServiceProvider(firstSucceeds: false, secondSucceeds: true));
        var requirement = new EitherRequirementRequirement(FirstMarker, SecondMarker);
        var context = new AuthorizationHandlerContext([requirement], PrincipalWithSub(1), new DefaultHttpContext());

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task EitherRequirementHandler_BothFail_Fails()
    {
        var handler = new EitherRequirementHandler(BuildServiceProvider(firstSucceeds: false, secondSucceeds: false));
        var requirement = new EitherRequirementRequirement(FirstMarker, SecondMarker);
        var context = new AuthorizationHandlerContext([requirement], PrincipalWithSub(1), new DefaultHttpContext());

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
