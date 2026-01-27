using CarvedRockFitness.API.Entities;

namespace CarvedRockFitness.API.Repositories;

public interface IRoutineRepository : IRepository<Routine>
{
    Task<Routine?> GetRoutineWithUserAndTrainerAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Routine>> GetRoutinesByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> UserOwnsRoutineAsync(int routineId, int userId, CancellationToken cancellationToken = default);
}

