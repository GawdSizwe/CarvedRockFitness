using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.DTOs;

// Response DTOs
public record RoutineDto(int Id, string Name, string Description, TraineeDto Trainee, TrainerDto? Trainer);

public record RoutineSummaryDto(int Id, string Name, string Description);

public record TraineeDto(int Id, string Name, string Email);

// Request DTOs
public record CreateRoutineRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(500)] string Description,
    [Required] int UserId);

public record UpdateRoutineRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(500)] string Description);

