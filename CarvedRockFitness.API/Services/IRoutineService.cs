using CarvedRockFitness.API.DTOs;

namespace CarvedRockFitness.API.Services;

public interface IRoutineService
{
    Task<RoutineDto?> GetRoutineByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoutineSummaryDto>> GetRoutinesByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> UserOwnsRoutineAsync(int routineId, int userId, CancellationToken cancellationToken = default);
}

