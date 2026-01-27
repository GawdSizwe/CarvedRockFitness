using CarvedRockFitness.API.DTOs;
using CarvedRockFitness.API.Entities;
using CarvedRockFitness.API.Exceptions;
using CarvedRockFitness.API.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace CarvedRockFitness.API.Services;

public class UserService(
    IUserRepository userRepository,
    IMemoryCache cache,
    ILogger<UserService> logger) : IUserService
{
    private const string AllUsersCacheKey = "all_users";
    private const string UserCacheKeyPrefix = "user_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(AllUsersCacheKey, out IEnumerable<UserDto>? cachedUsers) && cachedUsers != null)
        {
            logger.LogDebug("Returning cached users list");
            return cachedUsers;
        }

        logger.LogInformation("Fetching all users from database");
        var users = await userRepository.GetAllUsersWithTrainersAsync(cancellationToken);
        var userDtos = users.Select(MapToDto).ToList();

        cache.Set(AllUsersCacheKey, userDtos, CacheDuration);
        return userDtos;
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserCacheKeyPrefix}{id}";

        if (cache.TryGetValue(cacheKey, out UserDto? cachedUser))
        {
            logger.LogDebug("Returning cached user {UserId}", id);
            return cachedUser;
        }

        logger.LogInformation("Fetching user {UserId} from database", id);
        var user = await userRepository.GetUserWithTrainerAsync(id, cancellationToken);
        
        if (user == null)
            return null;

        var userDto = MapToDto(user);
        cache.Set(cacheKey, userDto, CacheDuration);
        return userDto;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating new user with email {Email}", request.Email);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Role = request.Role,
            TrainerId = request.TrainerId
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        logger.LogInformation("User {UserId} created successfully", user.Id);

        // Reload with trainer info
        var createdUser = await userRepository.GetUserWithTrainerAsync(user.Id, cancellationToken);
        return MapToDto(createdUser!);
    }

    public async Task UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating user {UserId}", id);

        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"User with id {id} not found");

        user.Name = request.Name;
        user.Email = request.Email;
        user.Role = request.Role;
        user.TrainerId = request.TrainerId;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(cancellationToken);

        InvalidateCache(id);
        logger.LogInformation("User {UserId} updated successfully", id);
    }

    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting user {UserId}", id);

        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"User with id {id} not found");

        userRepository.Remove(user);
        await userRepository.SaveChangesAsync(cancellationToken);

        InvalidateCache(id);
        logger.LogInformation("User {UserId} deleted successfully", id);
    }

    private void InvalidateCache(int? userId = null)
    {
        cache.Remove(AllUsersCacheKey);
        if (userId.HasValue)
            cache.Remove($"{UserCacheKeyPrefix}{userId}");
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.Trainer != null ? new TrainerDto(user.Trainer.Id, user.Trainer.Name, user.Trainer.Email) : null
    );
}

