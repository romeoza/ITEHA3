using Microsoft.AspNetCore.Authorization;

namespace Vulindlela.Booking.Api.Business;

public sealed class OwnsRecordRequirement : IAuthorizationRequirement { }

public sealed class OwnsRecordHandler : AuthorizationHandler<OwnsRecordRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnsRecordRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}