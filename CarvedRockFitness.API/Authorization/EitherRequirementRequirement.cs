using Microsoft.AspNetCore.Authorization;

namespace CarvedRockFitness.API.Authorization;

public class EitherRequirementRequirement(IAuthorizationRequirement firstRequirement, IAuthorizationRequirement secondRequirement) : IAuthorizationRequirement
{
    public IAuthorizationRequirement FirstRequirement { get; } = firstRequirement;
    public IAuthorizationRequirement SecondRequirement { get; } = secondRequirement;
}
