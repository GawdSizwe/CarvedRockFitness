using CarvedRockFitness.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace CarvedRockFitness.API.Authorization;

public class MustOwnRoutineHandler(
    IRoutineService routineService,
    ILogger<MustOwnRoutineHandler> logger) : AuthorizationHandler<MustOwnRoutineRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MustOwnRoutineRequirement requirement)
    {
        var httpContext = context.Resource as DefaultHttpContext;
        if (httpContext == null)
        {
            logger.LogWarning("Authorization context resource is not an HttpContext");
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

        var ownsRoutine = await routineService.UserOwnsRoutineAsync(routineId, userId);
        if (!ownsRoutine)
        {
            logger.LogWarning("User {UserId} does not own routine {RoutineId}", userId, routineId);
            context.Fail();
            return;
        }

        logger.LogDebug("User {UserId} authorized to access routine {RoutineId}", userId, routineId);
        context.Succeed(requirement);
    }
}
