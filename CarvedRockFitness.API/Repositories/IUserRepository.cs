using CarvedRockFitness.API.Entities;

namespace CarvedRockFitness.API.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetUserWithTrainerAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllUsersWithTrainersAsync(CancellationToken cancellationToken = default);
}

