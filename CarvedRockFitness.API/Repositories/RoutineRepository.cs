using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Repositories;

public class RoutineRepository(CarvedRockFitnessDbContext context) : Repository<Routine>(context), IRoutineRepository
{
    public async Task<Routine?> GetRoutineWithUserAndTrainerAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.User)
            .ThenInclude(u => u.Trainer)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Routine>> GetRoutinesByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .Include(r => r.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UserOwnsRoutineAsync(int routineId, int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(r => r.Id == routineId && r.UserId == userId, cancellationToken);
    }
}

