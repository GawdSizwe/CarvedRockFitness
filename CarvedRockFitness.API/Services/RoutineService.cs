using CarvedRockFitness.API.DTOs;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace CarvedRockFitness.API.Services;

public class RoutineService(
    IRoutineRepository routineRepository,
    IMemoryCache cache,
    ILogger<RoutineService> logger) : IRoutineService
{
    private const string RoutineCacheKeyPrefix = "routine_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<RoutineDto?> GetRoutineByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{RoutineCacheKeyPrefix}{id}";

        if (cache.TryGetValue(cacheKey, out RoutineDto? cachedRoutine))
        {
            logger.LogDebug("Returning cached routine {RoutineId}", id);
            return cachedRoutine;
        }

        logger.LogInformation("Fetching routine {RoutineId} from database", id);
        var routine = await routineRepository.GetRoutineWithUserAndTrainerAsync(id, cancellationToken);

        if (routine == null)
            return null;

        var routineDto = MapToDto(routine);
        cache.Set(cacheKey, routineDto, CacheDuration);
        return routineDto;
    }

    public async Task<IEnumerable<RoutineSummaryDto>> GetRoutinesByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching routines for user {UserId}", userId);
        var routines = await routineRepository.GetRoutinesByUserIdAsync(userId, cancellationToken);
        return routines.Select(r => new RoutineSummaryDto(r.Id, r.Name, r.Description));
    }

    public async Task<bool> UserOwnsRoutineAsync(int routineId, int userId, CancellationToken cancellationToken = default)
    {
        return await routineRepository.UserOwnsRoutineAsync(routineId, userId, cancellationToken);
    }

    private static RoutineDto MapToDto(Routine routine) => new(
        routine.Id,
        routine.Name,
        routine.Description,
        new TraineeDto(routine.User.Id, routine.User.Name, routine.User.Email),
        routine.User.Trainer != null 
            ? new TrainerDto(routine.User.Trainer.Id, routine.User.Trainer.Name, routine.User.Trainer.Email) 
            : null
    );
}

