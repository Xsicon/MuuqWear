using MuuqWear.Model.Customer;

namespace MuuqWear.Web.Helpers;

public static class CustomerAccountStatusHelper
{
    public static readonly IReadOnlyList<(int Days, string Label)> SuspensionDurations =
    [
        (1, "1 day"),
        (3, "3 days"),
        (7, "7 days"),
        (14, "14 days"),
        (30, "30 days"),
        (60, "60 days"),
        (90, "90 days"),
        (180, "6 months"),
        (365, "12 months")
    ];

    public static bool IsSuspended(CustomerModel customer) =>
        string.Equals(customer.AccountStatus, "suspended", StringComparison.OrdinalIgnoreCase);

    public static string GetStatusLabel(CustomerModel customer)
    {
        if (!IsSuspended(customer))
            return "Active";

        if (customer.SuspendedUntil.HasValue)
            return $"Suspended until {customer.SuspendedUntil.Value.ToLocalTime():MMM dd, yyyy}";

        return "Suspended";
    }

    public static string GetDurationLabel(int days) =>
        SuspensionDurations.FirstOrDefault(d => d.Days == days).Label ?? $"{days} days";
}
