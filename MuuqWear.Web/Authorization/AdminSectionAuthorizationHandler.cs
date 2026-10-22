using Microsoft.AspNetCore.Authorization;
using MuuqWear.Web.Constants;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.Authorization;

public sealed class AdminSectionAuthorizationHandler
    : AuthorizationHandler<AdminSectionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminSectionRequirement requirement)
    {
        var role = AdminPortalUserContext.GetRole(context.User);
        if (AdminPortalRoles.CanAccess(role, requirement.Section))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
