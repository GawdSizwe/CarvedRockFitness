using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Repositories;
using CarvedRockFitness.API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CarvedRockFitness.API.Tests.Services;

public class RoutineServiceTests
{
    private readonly Mock<IRoutineRepository> _routineRepositoryMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<RoutineService>> _loggerMock;
    private readonly RoutineService _sut;

    public RoutineServiceTests()
    {
        _routineRepositoryMock = new Mock<IRoutineRepository>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<RoutineService>>();

        object? cacheValue = null;
        _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        _sut = new RoutineService(_routineRepositoryMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetRoutineByIdAsync_ExistingRoutine_ReturnsRoutineWithUserAndTrainer()
    {
        // Arrange
        var trainer = new User { Id = 2, Name = "Trainer", Email = "trainer@test.com", Role = "Trainer" };
        var user = new User { Id = 1, Name = "John", Email = "john@test.com", Role = "Trainee", Trainer = trainer };
        var routine = new Routine 
        { 
            Id = 1, 
            Name = "Morning Yoga", 
            Description = "Relaxing yoga", 
            UserId = 1,
            User = user 
        };

        _routineRepositoryMock.Setup(x => x.GetRoutineWithUserAndTrainerAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(routine);

        // Act
        var result = await _sut.GetRoutineByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Morning Yoga");
        result.Trainee.Name.Should().Be("John");
        result.Trainer.Should().NotBeNull();
        result.Trainer!.Name.Should().Be("Trainer");
    }

    [Fact]
    public async Task GetRoutineByIdAsync_NonExistingRoutine_ReturnsNull()
    {
        // Arrange
        _routineRepositoryMock.Setup(x => x.GetRoutineWithUserAndTrainerAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Routine?)null);

        // Act
        var result = await _sut.GetRoutineByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRoutinesByUserIdAsync_ReturnsUserRoutines()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@test.com", Role = "Trainee" };
        var routines = new List<Routine>
        {
            new() { Id = 1, Name = "Morning Yoga", Description = "Relaxing", UserId = 1, User = user },
            new() { Id = 2, Name = "Evening Run", Description = "Cardio", UserId = 1, User = user }
        };

        _routineRepositoryMock.Setup(x => x.GetRoutinesByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(routines);

        // Act
        var result = await _sut.GetRoutinesByUserIdAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Morning Yoga");
    }

    [Fact]
    public async Task UserOwnsRoutineAsync_UserOwnsRoutine_ReturnsTrue()
    {
        // Arrange
        _routineRepositoryMock.Setup(x => x.UserOwnsRoutineAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UserOwnsRoutineAsync(1, 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserOwnsRoutineAsync_UserDoesNotOwnRoutine_ReturnsFalse()
    {
        // Arrange
        _routineRepositoryMock.Setup(x => x.UserOwnsRoutineAsync(1, 999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.UserOwnsRoutineAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }
}

