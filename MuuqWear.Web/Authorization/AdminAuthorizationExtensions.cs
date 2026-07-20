using Microsoft.AspNetCore.Authorization;
using MuuqWear.Web.Authorization;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.Authorization;

public static class AdminAuthorizationExtensions
{
    public static IServiceCollection AddAdminPortalAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, AdminSectionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var section in AdminPortalSection.All)
            {
                options.AddPolicy(
                    AdminPortalPolicies.ForSection(section),
                    policy => policy.AddRequirements(new AdminSectionRequirement(section)));
            }
        });

        return services;
    }
}
