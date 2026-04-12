# KasiMeetups (CarvedRock Fitness)

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![OpenIddict](https://img.shields.io/badge/OpenIddict-5.4-blue?style=for-the-badge)
![Fly.io](https://img.shields.io/badge/Fly.io-Deployed-8B5CF6?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Swagger](https://img.shields.io/badge/OpenAPI-3.0-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![Tests](https://img.shields.io/badge/Tests-26_Passing-success?style=for-the-badge)

**Enterprise-grade RESTful API for Fitness Routine Management**

*Demonstrating Clean Architecture, SOLID Principles, and Modern .NET Best Practices*

[Architecture](#-architecture) •
[Quick Start](#-quick-start) •
[API Documentation](#-api-documentation) •
[Testing Strategy](#-testing-strategy) •
[Technical Decisions](#-technical-decisions)

</div>

---

## 👤 Author

<table>
<tr>
<td align="center">
<b>Sizwe Mazibuko</b><br/>
<sub>Software Engineer</sub><br/>
</td>
</tr>
</table>


**Responsibilities:**
- System architecture design and implementation
- Establishment of coding standards and best practices
- Test strategy and quality assurance
- Technical documentation

---

## 📋 Executive Summary

CarvedRock Fitness API is a **production-ready** backend service built with enterprise-grade architecture patterns. This project demonstrates proficiency in:

| Area | Implementation |
|------|----------------|
| **Architecture** | Clean Architecture with Repository + Service Layer patterns |
| **Security** | JWT Authentication with Policy-based Authorization |
| **Performance** | In-memory caching with intelligent invalidation |
| **Quality** | 26 automated tests (Unit + Integration) with 100% critical path coverage |
| **Observability** | Structured logging with Serilog, request tracing |
| **Documentation** | OpenAPI 3.0 specification with interactive Swagger UI |

---

## 🏗️ Architecture

### Three-Service Architecture (Production - Fly.io)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CLIENT LAYER                                      │
│                    ┌─────────────────────────────────┐                    │
│                    │    https://gawdsizwe-web.fly.dev  │                    │
│                    │         (Blazor Web App)          │                    │
│                    └──────────────┬────────────────────┘                    │
└───────────────────────────────────┼─────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          AUTHENTICATION LAYER                               │
│                    ┌─────────────────────────────────┐                    │
│                    │   https://gawdsizwe-auth.fly.dev  │                    │
│                    │      (OpenIddict OIDC Server)     │                    │
│                    │   • Authorization endpoint         │                    │
│                    │   • Token endpoint                 │                    │
│                    │   • UserInfo endpoint              │                    │
│                    └──────────────┬────────────────────┘                    │
└───────────────────────────────────┼─────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           API LAYER                                         │
│                    ┌─────────────────────────────────┐                    │
│                    │   https://gawdsizwe-api.fly.dev   │                    │
│                    │       (REST API + Swagger)        │                    │
│                    │   • JWT Bearer authentication      │                    │
│                    │   • PostgreSQL persistence         │                    │
│                    │   • Rate limiting + caching        │                    │
│                    └───────────────────────────────────┘                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        DATABASE LAYER                                       │
│                    ┌─────────────────────────────────┐                    │
│                    │     Fly.io Postgres Cluster     │                    │
│                    └───────────────────────────────────┘                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Live Endpoints

| Service | URL | Health Check |
|---------|-----|--------------|
| Web App | https://gawdsizwe-web.fly.dev | /healthz |
| Auth Server | https://gawdsizwe-auth.fly.dev | /healthz |
| API | https://gawdsizwe-api.fly.dev | /healthz |

### Design Principles

This solution adheres to **SOLID principles** and implements the following architectural patterns:

- **Single Responsibility** - Each class has one reason to change
- **Open/Closed** - Open for extension, closed for modification via interfaces
- **Liskov Substitution** - All implementations are interchangeable
- **Interface Segregation** - Focused, cohesive interfaces
- **Dependency Inversion** - High-level modules depend on abstractions

### Layered Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            PRESENTATION LAYER                                │
│  ┌───────────────┐  ┌───────────────┐  ┌─────────────────────────────────┐  │
│  │  Controllers  │  │     DTOs      │  │  Middleware (Exception Handler) │  │
│  │  (Thin, no    │  │  (Request/    │  │  (Global error handling with    │  │
│  │   business    │  │   Response    │  │   RFC 7807 Problem Details)     │  │
│  │   logic)      │  │   contracts)  │  │                                 │  │
│  └───────┬───────┘  └───────────────┘  └─────────────────────────────────┘  │
│          │                                                                   │
├──────────┼──────────────────────────────────────────────────────────────────┤
│          ▼                    BUSINESS LOGIC LAYER                           │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                            SERVICES                                   │   │
│  │  ┌─────────────────────┐      ┌─────────────────────┐                │   │
│  │  │    IUserService     │      │   IRoutineService   │                │   │
│  │  │    UserService      │      │   RoutineService    │                │   │
│  │  └─────────────────────┘      └─────────────────────┘                │   │
│  │                                                                       │   │
│  │  Responsibilities:                                                    │   │
│  │  • Business rule validation    • Entity-to-DTO mapping               │   │
│  │  • Caching logic               • Cross-cutting orchestration         │   │
│  └───────┬──────────────────────────────────────────────────────────────┘   │
│          │                                                                   │
├──────────┼──────────────────────────────────────────────────────────────────┤
│          ▼                    DATA ACCESS LAYER                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                          REPOSITORIES                                 │   │
│  │  ┌─────────────────────┐      ┌─────────────────────┐                │   │
│  │  │  IRepository<T>     │      │  IUserRepository    │                │   │
│  │  │  Repository<T>      │      │  IRoutineRepository │                │   │
│  │  └─────────────────────┘      └─────────────────────┘                │   │
│  │                                                                       │   │
│  │  Responsibilities:                                                    │   │
│  │  • Data persistence           • Query optimization (eager loading)   │   │
│  │  • Unit of Work pattern       • Database abstraction                 │   │
│  └───────┬──────────────────────────────────────────────────────────────┘   │
│          │                                                                   │
├──────────┼──────────────────────────────────────────────────────────────────┤
│          ▼                     INFRASTRUCTURE                                │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────────────────────┐   │
│  │   DbContext   │  │   Entities    │  │        SQL Server             │   │
│  │   (EF Core)   │  │   (Domain)    │  │   (Persistent Storage)        │   │
│  └───────────────┘  └───────────────┘  └───────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
CarvedRockFitness/
│
├── 📁 CarvedRockFitness.API/                    # Main API Project
│   │
│   ├── 📁 Controllers/                          # HTTP Endpoints
│   │   ├── UsersController.cs                   # User CRUD operations
│   │   └── RoutinesController.cs                # Routine operations (authorized)
│   │
│   ├── 📁 Services/                             # Business Logic Layer
│   │   ├── IUserService.cs                      # User service contract
│   │   ├── UserService.cs                       # User business logic + caching
│   │   ├── IRoutineService.cs                   # Routine service contract
│   │   └── RoutineService.cs                    # Routine business logic + caching
│   │
│   ├── 📁 Repositories/                         # Data Access Layer
│   │   ├── IRepository.cs                       # Generic repository interface
│   │   ├── Repository.cs                        # Generic repository implementation
│   │   ├── IUserRepository.cs                   # User-specific queries
│   │   ├── UserRepository.cs                    # User data access
│   │   ├── IRoutineRepository.cs                # Routine-specific queries
│   │   └── RoutineRepository.cs                 # Routine data access
│   │
│   ├── 📁 DTOs/                                 # Data Transfer Objects
│   │   ├── UserDtos.cs                          # UserDto, CreateUserRequest, etc.
│   │   ├── RoutineDtos.cs                       # RoutineDto, TraineeDto, etc.
│   │   └── ErrorDtos.cs                         # ErrorResponse (RFC 7807)
│   │
│   ├── 📁 Entities/                             # Domain Models
│   │   ├── User.cs                              # User entity
│   │   └── Routine.cs                           # Routine entity
│   │
│   ├── 📁 Authorization/                        # Security
│   │   ├── MustOwnRoutineHandler.cs             # Resource-based authorization
│   │   ├── MustOwnRoutineRequirement.cs         # Authorization requirement
│   │   └── PolicyMetadata.cs                    # Policy constants
│   │
│   ├── 📁 Middleware/                           # Cross-Cutting Concerns
│   │   └── ExceptionHandlingMiddleware.cs       # Global exception handler
│   │
│   ├── 📁 Exceptions/                           # Custom Exceptions
│   │   └── ApiException.cs                      # NotFoundException, BadRequestException, etc.
│   │
│   ├── 📁 Extensions/                           # DI Configuration
│   │   └── ServiceCollectionExtensions.cs       # Service registration helpers
│   │
│   ├── 📁 DbContexts/                           # Entity Framework
│   │   └── CarvedRockFitnessDbContext.cs        # EF Core context + seed data
│   │
│   └── Program.cs                               # Application bootstrap
│
├── 📁 CarvedRockFitness.API.Tests/              # Unit Tests (19 tests)
│   ├── 📁 Services/
│   │   ├── UserServiceTests.cs                  # 7 tests
│   │   └── RoutineServiceTests.cs               # 5 tests
│   └── 📁 Controllers/
│       └── UsersControllerTests.cs              # 6 tests
│
├── 📁 CarvedRockFitness.API.IntegrationTests/   # Integration Tests (7 tests)
│   ├── CustomWebApplicationFactory.cs           # Test server configuration
│   └── UsersEndpointTests.cs                    # End-to-end HTTP tests
│
├── 📄 TESTING.md                                # Test documentation
└── 📄 README.md                                 # This file
```

---

## 🚀 Quick Start

### Prerequisites

| Requirement | Version | Purpose |
|-------------|---------|---------|
| .NET SDK | 9.0+ | Runtime and build tools |
| SQL Server | 2019+ | Database (LocalDB acceptable) |
| IDE | VS 2022 / VS Code / Rider | Development environment |

### Installation

```bash
# 1. Clone repository
git clone https://github.com/smazibuko/carvedrock-fitness-api.git
cd carvedrock-fitness-api

# 2. Configure database connection (using User Secrets)
dotnet user-secrets set "ConnectionStrings:CarvedRockFitnessDB" \
    "Server=(localdb)\\mssqllocaldb;Database=CarvedRockFitness;Trusted_Connection=True;"

# 3. Apply database migrations
dotnet ef database update --project CarvedRockFitness.API

# 4. Run the application
dotnet run --project CarvedRockFitness.API

# 5. Access Swagger UI
# Navigate to: https://localhost:7009
```

### Verify Installation

```bash
# Run all tests (should show 26 passing)
dotnet test

# Expected output:
# Passed! - Failed: 0, Passed: 19 - CarvedRockFitness.API.Tests.dll
# Passed! - Failed: 0, Passed: 7  - CarvedRockFitness.API.IntegrationTests.dll
```

---

## 📚 API Documentation

### Interactive Documentation

**Swagger UI** is available at the application root:
```
https://localhost:7009
```

### Endpoints Summary

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/users` | Retrieve all users | ❌ |
| `GET` | `/users/{id}` | Retrieve user by ID | ❌ |
| `POST` | `/users` | Create new user | ❌ |
| `PUT` | `/users/{id}` | Update existing user | ❌ |
| `DELETE` | `/users/{id}` | Delete user | ❌ |
| `GET` | `/routines/{id}` | Retrieve routine (owner only) | ✅ |

### Authentication

The API uses **JWT Bearer tokens**:

```http
GET /routines/1 HTTP/1.1
Host: localhost:7009
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Error Response Format

All errors conform to **RFC 7807 Problem Details**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "User with id 999 not found",
  "traceId": "00-abc123def456-00"
}
```

---

## 🧪 Testing Strategy

### Test Pyramid Implementation

```
                    ┌─────────────┐
                    │   Manual    │  ← HTTP file for exploratory testing
                    │   Testing   │
                    └──────┬──────┘
                           │
                ┌──────────┴──────────┐
                │    Integration      │  ← 7 tests (WebApplicationFactory)
                │       Tests         │     Full HTTP pipeline validation
                └──────────┬──────────┘
                           │
        ┌──────────────────┴──────────────────┐
        │            Unit Tests               │  ← 19 tests (xUnit + Moq)
        │   Services (12) + Controllers (6)   │     Isolated component testing
        └─────────────────────────────────────┘
```

### Test Coverage Summary

| Component | Tests | Coverage Focus |
|-----------|-------|----------------|
| `UserService` | 7 | CRUD operations, caching, exceptions |
| `RoutineService` | 5 | Queries, ownership validation |
| `UsersController` | 6 | HTTP responses, status codes |
| `Integration` | 7 | End-to-end request/response |
| **Total** | **26** | |

### Running Tests

```bash
# All tests
dotnet test

# With verbosity
dotnet test --verbosity normal

# With code coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test CarvedRockFitness.API.Tests
```

---

## 🔧 Technical Decisions

### Why Repository Pattern?

| Benefit | Explanation |
|---------|-------------|
| **Testability** | Services can be unit tested with mocked repositories |
| **Flexibility** | Database provider can be swapped without changing business logic |
| **Single Responsibility** | Data access logic isolated from business rules |
| **Query Optimization** | Complex queries encapsulated in repository methods |

### Why Service Layer?

| Benefit | Explanation |
|---------|-------------|
| **Business Logic Isolation** | Rules don't leak into controllers or repositories |
| **Caching Strategy** | Centralized cache management with proper invalidation |
| **Transaction Management** | Service methods define transaction boundaries |
| **Cross-Cutting Concerns** | Logging, validation applied consistently |

### Why DTOs?

| Benefit | Explanation |
|---------|-------------|
| **API Contract Stability** | Internal model changes don't break API consumers |
| **Security** | Prevent over-posting attacks |
| **Flexibility** | Shape responses for specific use cases |
| **Validation** | Request validation attributes on DTOs |

---

## ⚡ Performance Optimizations

### Caching Strategy

```csharp
// In-memory cache with 5-minute TTL
private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

// Cache keys
- "all_users"       → Invalidated on create/update/delete
- "user_{id}"       → Invalidated on update/delete
- "routine_{id}"    → Invalidated on update/delete
```

### Query Optimizations

- **Eager Loading**: Related entities loaded in single query
- **Projection**: Only required fields selected via DTOs
- **Async/Await**: Non-blocking database operations throughout

---

## 📊 Observability

### Structured Logging (Serilog)

**Console Output:**
```
[14:32:15 INF] UserService: Fetching all users from database
[14:32:15 INF] HTTP GET /users responded 200 in 45.23 ms
```

**File Output:** `logs/carvedrock-{date}.log`
```
2026-01-15 14:32:15.123 +02:00 [INF] UserService: Fetching user {UserId} from database
```

### Log Levels

| Level | Usage |
|-------|-------|
| `Debug` | Cache hits, detailed flow |
| `Information` | Business operations, database calls |
| `Warning` | Authorization failures, validation issues |
| `Error` | Exceptions with stack traces |

---

## 🛠️ Technology Stack

| Category | Technology | Version |
|----------|------------|---------|
| **Runtime** | .NET | 9.0 |
| **Language** | C# | 13 |
| **Web Framework** | ASP.NET Core | 9.0 |
| **ORM** | Entity Framework Core | 9.0 |
| **Database** | SQL Server | 2019+ |
| **Logging** | Serilog | 8.0 |
| **API Docs** | Swashbuckle | 7.2 |
| **Testing** | xUnit | 2.9 |
| **Mocking** | Moq | 4.20 |
| **Assertions** | FluentAssertions | 7.0 |
| **Integration Tests** | WebApplicationFactory | 9.0 |

---

## � Deployment (Fly.io)

### Fly.io CLI Commands

```powershell
# Deploy all services
$env:PATH += ";$env:USERPROFILE\.fly\bin"

# Deploy Auth Server
cd CarvedRockFitness.Auth
flyctl deploy --remote-only --app gawdsizwe-auth --wait-timeout 300

# Deploy API
cd ..\CarvedRockFitness.API
flyctl deploy --remote-only --app gawdsizwe-api --wait-timeout 300

# Deploy Web
cd ..\CarvedRockFitness.Web
flyctl deploy --remote-only --app gawdsizwe-web --wait-timeout 300

# View logs
flyctl logs --app gawdsizwe-auth
flyctl logs --app gawdsizwe-api
flyctl logs --app gawdsizwe-web

# Database access
flyctl postgres connect -a gym-db -d <database_name>
```

### Production Secrets (Fly.io)

| Service | Secret | Purpose |
|---------|--------|---------|
| Auth | `DATABASE_URL` | PostgreSQL connection string |
| Auth | `SIGNING_CERT_PASSWORD` | RSA certificate password |
| API | `DATABASE_URL` | PostgreSQL connection string |
| API | `Auth:Authority` | `https://gawdsizwe-auth.fly.dev` |
| Web | `OIDC:Authority` | `https://gawdsizwe-auth.fly.dev` |
| Web | `Api:BaseUrl` | `https://gawdsizwe-api.fly.dev` |
| Web | `PayFast:MerchantId` | **REQUIRED** - PayFast merchant ID |
| Web | `PayFast:MerchantKey` | **REQUIRED** - PayFast merchant key |

### Known Issues & Fixes Applied

| Issue | Root Cause | Fix Applied |
|-------|------------|-------------|
| **OIDC redirect_uri mismatch (ID2043)** | Web app detected `http://` behind Fly proxy | Added `ForwardedHeaders` middleware, cleared `KnownNetworks`/`KnownProxies` |
| **Token endpoint HTTP method invalid (ID2084)** | `UseHttpsRedirection` converted POST→GET via 301 | Removed `UseHttpsRedirection`, added `ForwardedHeaders` to Auth |
| **ResponseMode form_post failure** | Fly proxy blocks POST callbacks | Changed to `ResponseMode.Query` |
| **API database nvarchar error** | Migrations written for SQL Server, not PostgreSQL | Wrapped `EnsureCreated` in try/catch, skipped SQL Server migrations |
| **PayFast 400 Bad Request** | Missing `PayFast:MerchantId` and `PayFast:MerchantKey` secrets | **TODO**: Set these Fly.io secrets |

### Checking Signed-Up Users (Database)

```powershell
# Connect to database
flyctl postgres connect -a gym-db -d <dbname>

# List users
SELECT "Id", "UserName", "Email", "EmailConfirmed", "IsPaidUser" FROM "AspNetUsers";

# Promote to Admin (replace USER_ID with actual ID)
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT 'USER_ID_HERE', "Id" FROM "AspNetRoles" WHERE "Name" = 'Admin';

# Mark as paid
UPDATE "AspNetUsers" SET "IsPaidUser" = true WHERE "Id" = 'USER_ID_HERE';
```

---

## �📝 License

This project is licensed under the MIT License.

---

## 📞 Contact

**Sizwe Mazibuko** - Software Developer / Tech Lead

- 📧 Email: smazibuko01@gmail.com
- 🐙 GitHub: [github.com/GawdSizwe](https://github.com/GawdSizwe)

---

<div align="center">

**Designed & Developed by Sizwe Mazibuko**

*CarvedRock Fitness API - Enterprise-Grade .NET Solution*

© 2026 CarvedRock Fitness. All Rights Reserved.

</div>
