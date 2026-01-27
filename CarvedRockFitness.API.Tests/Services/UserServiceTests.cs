using CarvedRockFitness.API.DTOs;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Exceptions;
using CarvedRockFitness.API.Repositories;
using CarvedRockFitness.API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CarvedRockFitness.API.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<UserService>>();

        // Setup cache to always miss (return false for TryGetValue)
        object? cacheValue = null;
        _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        _sut = new UserService(_userRepositoryMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Name = "John", Email = "john@test.com", Role = "Admin" },
            new() { Id = 2, Name = "Jane", Email = "jane@test.com", Role = "Trainer" }
        };
        _userRepositoryMock.Setup(x => x.GetAllUsersWithTrainersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Name == "John");
        result.Should().Contain(u => u.Name == "Jane");
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@test.com", Role = "Admin" };
        _userRepositoryMock.Setup(x => x.GetUserWithTrainerAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John");
        result.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetUserWithTrainerAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_CreatesAndReturnsUser()
    {
        // Arrange
        var request = new CreateUserRequest("John", "john@test.com", "Trainee", null);
        var createdUser = new User { Id = 1, Name = "John", Email = "john@test.com", Role = "Trainee" };

        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(x => x.GetUserWithTrainerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _sut.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("John");
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ExistingUser_UpdatesSuccessfully()
    {
        // Arrange
        var existingUser = new User { Id = 1, Name = "John", Email = "john@test.com", Role = "Trainee" };
        var updateRequest = new UpdateUserRequest("John Updated", "john.updated@test.com", "Trainer", null);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _sut.UpdateUserAsync(1, updateRequest);

        // Assert
        _userRepositoryMock.Verify(x => x.Update(It.Is<User>(u => 
            u.Name == "John Updated" && u.Email == "john.updated@test.com")), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        var updateRequest = new UpdateUserRequest("John", "john@test.com", "Trainee", null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateUserAsync(999, updateRequest))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("User with id 999 not found");
    }

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_DeletesSuccessfully()
    {
        // Arrange
        var existingUser = new User { Id = 1, Name = "John", Email = "john@test.com", Role = "Trainee" };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _sut.DeleteUserAsync(1);

        // Assert
        _userRepositoryMock.Verify(x => x.Remove(existingUser), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.DeleteUserAsync(999))
            .Should().ThrowAsync<NotFoundException>();
    }
}

