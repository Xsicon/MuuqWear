namespace MuuqWear.Web.Constants;

public static class AdminPortalSection
{
    public const string Overview = "overview";
    public const string Orders = "orders";
    public const string Customers = "customers";
    public const string Products = "products";
    public const string Content = "content";
    public const string Affiliates = "affiliates";
    public const string Support = "support";
    public const string Careers = "careers";
    public const string System = "system";

    public static IReadOnlyList<string> All { get; } =
    [
        Overview,
        Orders,
        Customers,
        Products,
        Content,
        Affiliates,
        Support,
        Careers,
        System
    ];
}
