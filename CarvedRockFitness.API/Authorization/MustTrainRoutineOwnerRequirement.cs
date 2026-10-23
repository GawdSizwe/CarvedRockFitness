using Microsoft.AspNetCore.Authorization;

namespace CarvedRockFitness.API.Authorization;

public class MustTrainRoutineOwnerRequirement(string trainerRoleName, string traineeRoleName) : IAuthorizationRequirement
{
    public string TrainerRoleName { get; } = trainerRoleName;
    public string TraineeRoleName { get; } = traineeRoleName;
}