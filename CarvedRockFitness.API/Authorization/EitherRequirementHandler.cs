using Microsoft.AspNetCore.Authorization;

namespace CarvedRockFitness.API.Authorization;

public class EitherRequirementHandler(IServiceProvider serviceProvider) : AuthorizationHandler<EitherRequirementRequirement>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EitherRequirementRequirement requirement)
    {
        var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

        var firstRequirementResult = await authorizationService.AuthorizeAsync(context.User, context.Resource, [requirement.FirstRequirement]);
        if (firstRequirementResult.Succeeded)
        {
            context.Succeed(requirement);
            return;
        }

        var secondRequirementResult = await authorizationService.AuthorizeAsync(context.User, context.Resource, [requirement.SecondRequirement]);
        if (secondRequirementResult.Succeeded)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}

