namespace MuuqWear.Web.Constants;

public static class AdminPortalPolicies
{
    public const string Overview = "AdminSection:overview";
    public const string Orders = "AdminSection:orders";
    public const string Customers = "AdminSection:customers";
    public const string Products = "AdminSection:products";
    public const string Content = "AdminSection:content";
    public const string Affiliates = "AdminSection:affiliates";
    public const string Support = "AdminSection:support";
    public const string Careers = "AdminSection:careers";
    public const string System = "AdminSection:system";

    public static string ForSection(string section) => $"AdminSection:{section}";
}
