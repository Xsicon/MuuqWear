using System.Security.Claims;
using MuuqWear.Model.Orders;
using MuuqWear.Web.Constants;
using MuuqWear.Web.ServerAuth;

namespace MuuqWear.Web.Helpers;

public static class OrderAccess
{
    public static bool CanView(OrderModel? order, ClaimsPrincipal? user)
    {
        if (order is null || user?.Identity?.IsAuthenticated != true)
            return false;
        if (AdminPortalRoles.IsAllowed(AdminPortalUserContext.GetRole(user)))
            return true;

        var (uid, email, _) = VerifiedAuthSession.Shopper(user);
        if (!string.IsNullOrWhiteSpace(order.CustomerId))
        {
            if (string.Equals(order.CustomerId, uid, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return !string.IsNullOrWhiteSpace(email)
            && string.Equals(order.Email, email, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsStaff(ClaimsPrincipal? user) =>
        AdminPortalRoles.IsAllowed(AdminPortalUserContext.GetRole(user ?? new ClaimsPrincipal()));
}
