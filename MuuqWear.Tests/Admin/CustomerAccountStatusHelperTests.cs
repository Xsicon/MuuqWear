using MuuqWear.Model.Customer;
using MuuqWear.Web.Helpers;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class CustomerAccountStatusHelperTests
{
    [Theory]
    [InlineData("suspended", true)]
    [InlineData("Suspended", true)]
    [InlineData("SUSPENDED", true)]
    [InlineData("active", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSuspended_matches_status_case_insensitively(string? status, bool expected)
    {
        var customer = new CustomerModel { AccountStatus = status! };

        Assert.Equal(expected, CustomerAccountStatusHelper.IsSuspended(customer));
    }

    [Fact]
    public void GetStatusLabel_returns_active_for_non_suspended()
    {
        var customer = new CustomerModel { AccountStatus = "active" };

        Assert.Equal("Active", CustomerAccountStatusHelper.GetStatusLabel(customer));
    }

    [Fact]
    public void GetStatusLabel_includes_end_date_when_suspended()
    {
        var until = new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc);
        var customer = new CustomerModel { AccountStatus = "suspended", SuspendedUntil = until };

        var label = CustomerAccountStatusHelper.GetStatusLabel(customer);

        Assert.StartsWith("Suspended until ", label);
        Assert.Contains(until.ToLocalTime().ToString("MMM dd, yyyy"), label);
    }

    [Fact]
    public void GetStatusLabel_falls_back_when_end_date_missing()
    {
        var customer = new CustomerModel { AccountStatus = "suspended" };

        Assert.Equal("Suspended", CustomerAccountStatusHelper.GetStatusLabel(customer));
    }

    [Theory]
    [InlineData(1, "1 day")]
    [InlineData(30, "30 days")]
    [InlineData(180, "6 months")]
    [InlineData(365, "12 months")]
    public void GetDurationLabel_uses_known_durations(int days, string expected)
    {
        Assert.Equal(expected, CustomerAccountStatusHelper.GetDurationLabel(days));
    }

    [Fact]
    public void GetDurationLabel_falls_back_for_unknown_duration()
    {
        Assert.Equal("45 days", CustomerAccountStatusHelper.GetDurationLabel(45));
    }
}
