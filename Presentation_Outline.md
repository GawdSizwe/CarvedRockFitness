# API Authentication & RESTful Standards: Deep Dive
**Intermediate Level Presentation**

---

## 1. Introduction
- **Goal:** Explore API authentication mechanisms and RESTful API standards using the `CarvedRockFitness.API` project as a practical example.
- **Target Audience:** Intermediate developers familiar with C# and basic web API concepts.
- **Agenda:**
  1. RESTful API Standards in Practice
  2. API Authentication & Authorization
  3. Deep Dive into Custom Authorization Handlers
  4. Best Practices & Key Takeaways

---

## 2. RESTful API Standards in Practice
*Reviewing the structural foundation of a robust API.*

### Controller Design (`UsersController.cs`)
- **Routing & Attributes:**
  - `[ApiController]`: Enables automatic model validation and inferring parameter binding.
  - `[Route("users")]`: Pluralized resource naming standard for REST.
  - `[Produces("application/json")]`: Explicit content negotiation.
- **HTTP Methods & Actions:**
  - `GET /users` - Retrieve a collection.
  - `GET /users/{id:int}` - Retrieve a specific resource.
  - `POST /users` - Create a new resource. Returns `201 Created` with a `Location` header (`CreatedAtAction`).
  - `PUT /users/{id:int}` - Full update of a resource. Returns `204 No Content`.
  - `DELETE /users/{id:int}` - Remove a resource. Returns `204 No Content`.
- **Response Types & Status Codes:**
  - Explicit definitions using `[ProducesResponseType]`.
  - Handling success (`200 OK`, `201 Created`, `204 No Content`) and errors (`400 Bad Request`, `404 Not Found`).

### Dependency Injection & Services
- **Constructor Injection:** e.g., `public class UsersController(IUserService userService, ILogger<UsersController> logger)`.
- **Separation of Concerns:** Controllers handle HTTP transport; Business logic lives in services (`IUserService`).

---

## 3. API Authentication & Authorization
*Securing the API using modern ASP.NET Core mechanisms.*

### Setup in `Program.cs`
- **Authentication Scheme:**
  - Configured to use JWT Bearer tokens: `builder.Services.AddAuthentication("Bearer").AddJwtBearer(...)`.
  - JWT (JSON Web Tokens) are standard for stateless API authentication.
- **Authorization Policies:**
  - Policies allow for complex permission logic beyond simple roles.
  - Defined using `builder.Services.AddAuthorization(options => { ... })`.

### The Middleware Pipeline
- Order matters in ASP.NET Core:
  1. `app.UseExceptionHandler()`: Catch errors early.
  2. `app.UseHttpsRedirection()`: Secure transport.
  3. `app.UseRouting()` (implied).
  4. **`app.UseAuthentication()`**: Who is the user?
  5. **`app.UseAuthorization()`**: What can the user do?
  6. `app.MapControllers()`: Execute the endpoint.

---

## 4. Deep Dive: Custom Authorization Handlers
*Moving beyond basic role checks to resource-based authorization.*

### Problem Statement
- A user is authenticated, but should they be able to access *this specific* routine? We need resource-based authorization.

### The Solution: `MustOwnRoutineHandler.cs`
- **Requirement Class:** `MustOwnRoutineRequirement` acts as a marker for the policy.
- **Handler Implementation (`AuthorizationHandler<MustOwnRoutineRequirement>`):**
  1. **Extract Context:** Casts `context.Resource` to `DefaultHttpContext` to access the HTTP request.
  2. **Read Route Data:** Extracts the `id` from the route (`/routines/{id}`).
  3. **Read User Claims:** Extracts the `sub` (subject/user ID) claim from the JWT: `context.User.Claims.FirstOrDefault(c => c.Type == "sub")`.
  4. **Evaluate Rule:** Calls `routineService.UserOwnsRoutineAsync(routineId, userId)`.
  5. **Result:** Calls `context.Succeed(requirement)` if authorized, or `context.Fail()` if not.

### Why this is powerful?
- Keeps authorization logic out of the controller.
- Highly testable.
- Reusable across multiple endpoints via the `[Authorize(Policy = PolicyMetadata.MustOwnRoutine)]` attribute.

---

## 5. Best Practices & Key Takeaways

### Logging & Observability
- **Serilog Integration:** Structured logging to console and file (`logs/carvedrock-.log`).
- **Request Logging:** `app.UseSerilogRequestLogging()` provides detailed metrics on HTTP requests (method, path, status, duration).
- **Contextual Logging:** Injecting `ILogger<T>` into controllers and handlers for contextual debug/warning messages.

### Global Exception Handling
- `app.UseGlobalExceptionHandler()` in `Program.cs` ensures consistent error responses (e.g., standardizing `404 Not Found` or `500 Internal Server Error` formats) without try-catch blocks in every controller.

### CancellationToken
- Used consistently across endpoints (e.g., `public async Task<ActionResult<...>> GetUsers(CancellationToken cancellationToken)`).
- Critical for API performance: Cancels long-running database queries if the client drops the request.

---

## Conclusion
- RESTful standards make APIs predictable.
- JWT Authentication provides a scalable, stateless security model.
- Resource-based authorization (via Handlers) provides granular access control.
- Observability and cancellation tokens are hallmarks of production-ready APIs.
