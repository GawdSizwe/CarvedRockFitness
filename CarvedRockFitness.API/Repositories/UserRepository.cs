using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Repositories;

public class UserRepository(CarvedRockFitnessDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetUserWithTrainerAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Trainer)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllUsersWithTrainersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Trainer)
            .ToListAsync(cancellationToken);
    }
}

