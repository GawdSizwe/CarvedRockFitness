using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.DTOs;
using CarvedRockFitness.API.Exceptions;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Controllers;

/// <summary>
/// Manages user operations including CRUD functionality
/// </summary>
[ApiController]
[Route("users")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserService userService, ILogger<UsersController> logger, CarvedRockFitnessDbContext db) : ControllerBase
{
    /// <summary>
    /// Retrieves all users
    /// </summary>
    /// <returns>List of all users with their trainer information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting all users");
        var users = await userService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Returns a flat member summary for the admin dashboard
    /// </summary>
    [HttpGet("members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(CancellationToken ct)
    {
        var members = await db.Users
            .Select(u => new
            {
                u.Id,
                Name      = u.Name,
                Email     = u.Email ?? "",
                Role      = u.Role,
                u.IsPaidUser,
                IsBanned  = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
            })
            .OrderBy(u => u.Name)
            .ToListAsync(ct);
        return Ok(members);
    }

    /// <summary>
    /// Retrieves a specific user by ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserById(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting user {UserId}", id);
        var user = await userService.GetUserByIdAsync(id, cancellationToken);
        
        if (user == null)
            throw new NotFoundException($"User with id {id} not found");

        return Ok(user);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">The user creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating new user with email {Email}", request.Email);
        var user = await userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="id">The user ID to update</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating user {UserId}", id);
        await userService.UpdateUserAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="id">The user ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting user {UserId}", id);
        await userService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Toggles a user's banned (lockout) status
    /// </summary>
    [HttpPatch("{id:int}/ban")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(int id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user == null) return NotFound();
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            user.LockoutEnd    = null;
            user.LockoutEnabled = false;
        }
        else
        {
            user.LockoutEnabled = true;
            user.LockoutEnd     = DateTimeOffset.UtcNow.AddYears(100);
        }
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
