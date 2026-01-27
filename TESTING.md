# CarvedRock Fitness API - Testing Guide

## Overview

The solution includes **26 automated tests** organized into two test projects:

| Project | Type | Tests | Purpose |
|---------|------|-------|---------|
| `CarvedRockFitness.API.Tests` | Unit Tests | 19 | Test individual components in isolation |
| `CarvedRockFitness.API.IntegrationTests` | Integration Tests | 7 | Test full HTTP request/response pipeline |

---

## Running Tests

### Run All Tests
```powershell
dotnet test
```

### Run with Verbose Output
```powershell
dotnet test --verbosity normal
```

### Run Specific Test Project
```powershell
# Unit tests only
dotnet test CarvedRockFitness.API.Tests

# Integration tests only
dotnet test CarvedRockFitness.API.IntegrationTests
```

### Run with Code Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

---

## Unit Tests (`CarvedRockFitness.API.Tests`)

Unit tests use **mocking** to isolate the component under test. Dependencies are replaced with mock objects using the **Moq** library.

### Testing Stack
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **FluentAssertions** - Readable assertions

### Test Structure

```
CarvedRockFitness.API.Tests/
├── Services/
│   ├── UserServiceTests.cs      (7 tests)
│   └── RoutineServiceTests.cs   (5 tests)
└── Controllers/
    └── UsersControllerTests.cs  (6 tests)
```

### Service Tests Pattern

```csharp
public class UserServiceTests
{
    // Dependencies (mocked)
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    
    // System Under Test
    private readonly UserService _sut;

    public UserServiceTests()
    {
        // Arrange - Setup mocks
        _userRepositoryMock = new Mock<IUserRepository>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<UserService>>();

        // Configure cache to always miss (forces DB calls)
        object? cacheValue = null;
        _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);
        _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Create the service with mocked dependencies
        _sut = new UserService(
            _userRepositoryMock.Object, 
            _cacheMock.Object, 
            _loggerMock.Object);
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
        _userRepositoryMock
            .Setup(x => x.GetAllUsersWithTrainersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Name == "John");
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.DeleteUserAsync(999))
            .Should().ThrowAsync<NotFoundException>();
    }
}
```

### UserService Tests (7)

| Test | Description |
|------|-------------|
| `GetAllUsersAsync_ReturnsAllUsers` | Verifies all users are returned |
| `GetUserByIdAsync_ExistingUser_ReturnsUser` | Returns user when found |
| `GetUserByIdAsync_NonExistingUser_ReturnsNull` | Returns null for invalid ID |
| `CreateUserAsync_ValidRequest_CreatesAndReturnsUser` | Creates user and returns DTO |
| `UpdateUserAsync_ExistingUser_UpdatesSuccessfully` | Updates user properties |
| `UpdateUserAsync_NonExistingUser_ThrowsNotFoundException` | Throws when user not found |
| `DeleteUserAsync_NonExistingUser_ThrowsNotFoundException` | Throws when user not found |

### RoutineService Tests (5)

| Test | Description |
|------|-------------|
| `GetRoutineByIdAsync_ExistingRoutine_ReturnsRoutineWithUserAndTrainer` | Returns full routine graph |
| `GetRoutineByIdAsync_NonExistingRoutine_ReturnsNull` | Returns null for invalid ID |
| `GetRoutinesByUserIdAsync_ReturnsUserRoutines` | Filters routines by user |
| `UserOwnsRoutineAsync_UserOwnsRoutine_ReturnsTrue` | Ownership check passes |
| `UserOwnsRoutineAsync_UserDoesNotOwnRoutine_ReturnsFalse` | Ownership check fails |

### Controller Tests (6)

| Test | Description |
|------|-------------|
| `GetUsers_ReturnsOkWithUsers` | Returns 200 OK with user list |
| `GetUserById_ExistingUser_ReturnsOkWithUser` | Returns 200 OK with user |
| `GetUserById_NonExistingUser_ThrowsNotFoundException` | Throws 404 exception |
| `CreateUser_ValidRequest_ReturnsCreatedAtAction` | Returns 201 Created |
| `UpdateUser_ExistingUser_ReturnsNoContent` | Returns 204 No Content |
| `DeleteUser_ExistingUser_ReturnsNoContent` | Returns 204 No Content |

---

## Integration Tests (`CarvedRockFitness.API.IntegrationTests`)

Integration tests spin up the **actual API** using `WebApplicationFactory` and make **real HTTP requests**. The database is swapped with an **in-memory database** seeded with test data.

### Test Infrastructure

#### CustomWebApplicationFactory

```csharp
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> 
    where TProgram : class
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove SQL Server provider
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<CarvedRockFitnessDbContext>) ||
                d.ServiceType.FullName?.Contains("SqlServer") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Add InMemory database
            services.AddDbContext<CarvedRockFitnessDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarvedRockFitnessDbContext>();
        db.Database.EnsureCreated(); // Applies seed data from OnModelCreating
    }
}
```

#### Integration Test Pattern

```csharp
public class UsersEndpointTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public UsersEndpointTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();  // Seed the in-memory database
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessAndCorrectContentType()
    {
        // Act - Make real HTTP request
        var response = await _client.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
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
    }
}
```

### Integration Tests (7)

| Test | HTTP | Description |
|------|------|-------------|
| `GetUsers_ReturnsSuccessAndCorrectContentType` | GET /users | Verifies 200 + JSON content type |
| `GetUsers_ReturnsSeededUsers` | GET /users | Confirms seed data is present |
| `GetUserById_ExistingUser_ReturnsUser` | GET /users/1 | Returns specific user |
| `GetUserById_NonExistingUser_ReturnsNotFound` | GET /users/99999 | Returns 404 Not Found |
| `CreateUser_ValidUser_ReturnsCreated` | POST /users | Creates user, returns 201 |
| `UpdateUser_ExistingUser_ReturnsNoContent` | PUT /users/{id} | Updates user, returns 204 |
| `DeleteUser_ExistingUser_ReturnsNoContent` | DELETE /users/{id} | Deletes user, returns 204 |

---

## Manual Testing with HTTP Files

The `CarvedRockFitness.API.http` file provides ready-to-use requests for manual testing in VS Code or Visual Studio.

### Prerequisites
- Install **REST Client** extension (VS Code) or use Visual Studio's built-in support
- Run the API: `dotnet run --project CarvedRockFitness.API`

### Quick Test Workflow

1. **Start the API**
   ```powershell
   dotnet run --project CarvedRockFitness.API
   ```

2. **Open** `CarvedRockFitness.API/CarvedRockFitness.API.http`

3. **Click "Send Request"** above any `###` section

### Available Test Requests

| Section | Requests |
|---------|----------|
| **Swagger** | Get OpenAPI spec |
| **Users** | GET all, GET by ID, POST, PUT, DELETE |
| **Routines** | GET with auth, GET without auth (401), ownership tests |
| **Error Handling** | 404 responses, invalid endpoints |

---

## Test Data (Seed Data)

The in-memory database is seeded with data from `CarvedRockFitnessDbContext.OnModelCreating()`:

### Users (10)

| ID | Name | Role | Trainer |
|----|------|------|---------|
| 1 | John Doe | Admin | - |
| 2 | Jane Smith | Trainer | - |
| 3 | Mike Johnson | Trainer | - |
| 4 | Emily Davis | Trainee | Jane Smith (2) |
| 5 | Chris Brown | Trainee | Jane Smith (2) |
| 6-10 | ... | Trainee | Varies |

### Routines (30)

Routines are assigned to trainees (users 4-10). Each trainee owns multiple routines.

Example ownership:
- User 4 (Emily) → Routines 1, 8, 15, 22, 29
- User 5 (Chris) → Routines 2, 9, 16, 23, 30

---

## Best Practices Demonstrated

1. **Arrange-Act-Assert** pattern in all tests
2. **Isolated unit tests** with mocked dependencies
3. **Integration tests** with real HTTP pipeline
4. **Fluent assertions** for readable test code
5. **Fixture sharing** with `IClassFixture<T>`
6. **Unique database per test class** to prevent conflicts
7. **Seed data** for predictable test scenarios

