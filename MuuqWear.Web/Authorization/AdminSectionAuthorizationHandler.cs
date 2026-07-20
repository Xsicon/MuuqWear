using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.Authorization;

public sealed class AdminSectionAuthorizationHandler
    : AuthorizationHandler<AdminSectionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminSectionRequirement requirement)
    {
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (AdminPortalRoles.CanAccess(role, requirement.Section))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
