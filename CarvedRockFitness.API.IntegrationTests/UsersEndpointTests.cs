using System.Net;
using System.Net.Http.Json;
using CarvedRockFitness.API.DTOs;

namespace CarvedRockFitness.API.IntegrationTests;

public class UsersEndpointTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public UsersEndpointTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetUsers_ReturnsSeededUsers()
    {
        // Act
        var response = await _client.GetAsync("/users");
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();

        // Assert
        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterThan(0);
        users.Should().Contain(u => u.Name == "John Doe");
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsUser()
    {
        // Act
        var response = await _client.GetAsync("/users/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetUserById_NonExistingUser_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/users/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_ValidUser_ReturnsCreated()
    {
        // Arrange
        var newUser = new CreateUserRequest("Test User", "test@example.com", "Trainee", null);

        // Act
        var response = await _client.PostAsJsonAsync("/users", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
        createdUser.Should().NotBeNull();
        createdUser!.Name.Should().Be("Test User");
        createdUser.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task UpdateUser_ExistingUser_ReturnsNoContent()
    {
        // Arrange - first create a user
        var newUser = new CreateUserRequest("Update Test", "update@example.com", "Trainee", null);
        var createResponse = await _client.PostAsJsonAsync("/users", newUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var updateRequest = new UpdateUserRequest("Updated Name", "updated@example.com", "Trainer", null);

        // Act
        var response = await _client.PutAsJsonAsync($"/users/{createdUser!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var getResponse = await _client.GetAsync($"/users/{createdUser.Id}");
        var updatedUser = await getResponse.Content.ReadFromJsonAsync<UserDto>();
        updatedUser!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteUser_ExistingUser_ReturnsNoContent()
    {
        // Arrange - first create a user
        var newUser = new CreateUserRequest("Delete Test", "delete@example.com", "Trainee", null);
        var createResponse = await _client.PostAsJsonAsync("/users", newUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Act
        var response = await _client.DeleteAsync($"/users/{createdUser!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/users/{createdUser.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
