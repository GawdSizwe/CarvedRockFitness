using CarvedRockFitness.API.Controllers;
using CarvedRockFitness.API.DTOs;
using CarvedRockFitness.API.Exceptions;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CarvedRockFitness.API.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _sut = new UsersController(_userServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsOkWithUsers()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new(1, "John", "john@test.com", "Admin", null),
            new(2, "Jane", "jane@test.com", "Trainer", null)
        };
        _userServiceMock.Setup(x => x.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _sut.GetUsers(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsOkWithUser()
    {
        // Arrange
        var user = new UserDto(1, "John", "john@test.com", "Admin", null);
        _userServiceMock.Setup(x => x.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserById(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Name.Should().Be("John");
    }

    [Fact]
    public async Task GetUserById_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetUserByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        // Act & Assert
        await _sut.Invoking(c => c.GetUserById(999, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateUserRequest("John", "john@test.com", "Trainee", null);
        var createdUser = new UserDto(1, "John", "john@test.com", "Trainee", null);
        _userServiceMock.Setup(x => x.CreateUserAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _sut.CreateUser(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UsersController.GetUserById));
        var returnedUser = createdResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Id.Should().Be(1);
    }

    [Fact]
    public async Task UpdateUser_ExistingUser_ReturnsNoContent()
    {
        // Arrange
        var request = new UpdateUserRequest("John Updated", "john@test.com", "Trainer", null);
        _userServiceMock.Setup(x => x.UpdateUserAsync(1, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateUser(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_ExistingUser_ReturnsNoContent()
    {
        // Arrange
        _userServiceMock.Setup(x => x.DeleteUserAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteUser(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}

