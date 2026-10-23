using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Authorization;

public class MustTrainRoutineOwnerHandler(CarvedRockFitnessDbContext carvedRockFitnessDbContext, ILogger<MustOwnRoutineHandler> logger) : AuthorizationHandler<MustTrainRoutineOwnerRequirement>
{
    private readonly CarvedRockFitnessDbContext _carvedRockFitnessDbContext = carvedRockFitnessDbContext;
    protected override async Task HandleRequirementAsync (AuthorizationHandlerContext context, MustTrainRoutineOwnerRequirement requirement)
    {
        var httpContext = (context.Resource as DefaultHttpContext);
        if (httpContext == null) { 
            context.Fail();
            return;
        }
        
        var routeValues = httpContext.GetRouteData().Values;
        if (!routeValues.TryGetValue("id", out var idValue))
        {
            logger.LogWarning("Route does not contain 'id' parameter");
            context.Fail();
            return;
        }

        if (!int.TryParse(idValue as string, out var routineId))
        {
            logger.LogWarning("Could not parse routine id from route value: {IdValue}", idValue);
            context.Fail();
            return;
        }

        if (!int.TryParse(context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value, out var userId))
        {
            logger.LogWarning("Could not parse user id from 'sub' claim");
            context.Fail();
            return;
        }

        var routine = await _carvedRockFitnessDbContext.Routines.Include(r => r.User).ThenInclude(u => u.Trainer).FirstOrDefaultAsync(r => r.Id == routineId);
        if (routine?.User == null || routine.User.TrainerId != userId || routine.User.Role != requirement.TraineeRoleName || routine.User.Trainer?.Role != requirement.TrainerRoleName)
        {
            logger.LogWarning("User {UserId} does not own routine {RoutineId}", userId, routineId);
            context.Fail();
            return;
        }

        logger.LogDebug("User {UserId} authorized to access routine {RoutineId}", userId, routineId);
        context.Succeed(requirement);
    }
}

