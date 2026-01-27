using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.DTOs;

// Response DTOs
public record UserDto(int Id, string Name, string Email, string Role, TrainerDto? Trainer);

public record TrainerDto(int Id, string Name, string Email);

public record UserDetailDto(int Id, string Name, string Email, string Role, TrainerDto? Trainer, IEnumerable<RoutineSummaryDto> Routines);

// Request DTOs
public record CreateUserRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress, StringLength(255)] string Email,
    [Required, StringLength(50)] string Role,
    int? TrainerId);

public record UpdateUserRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress, StringLength(255)] string Email,
    [Required, StringLength(50)] string Role,
    int? TrainerId);

