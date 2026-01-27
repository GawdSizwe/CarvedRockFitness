using CarvedRockFitness.API.Authorization;
using CarvedRockFitness.API.DTOs;
using CarvedRockFitness.API.Exceptions;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRockFitness.API.Controllers;

/// <summary>
/// Manages fitness routine operations
/// </summary>
[ApiController]
[Route("routines")]
[Produces("application/json")]
public class RoutinesController(IRoutineService routineService, ILogger<RoutinesController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves a specific routine by ID
    /// </summary>
    /// <param name="id">The routine ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The routine details including trainee and trainer information</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = PolicyMetadata.MustOwnRoutine)]
    [ProducesResponseType(typeof(RoutineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RoutineDto>> GetRoutineById(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting routine {RoutineId}", id);
        var routine = await routineService.GetRoutineByIdAsync(id, cancellationToken);

        if (routine == null)
            throw new NotFoundException($"Routine with id {id} not found");

        return Ok(routine);
    }
}
