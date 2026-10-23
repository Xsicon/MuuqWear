namespace MuuqWear.Application.Shared;

public static class ShopperAccount
{
    public static bool CanUseStorefront(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return true;

        return role.Equals("user", StringComparison.OrdinalIgnoreCase)
            || role.Equals("customer", StringComparison.OrdinalIgnoreCase);
    }
}
