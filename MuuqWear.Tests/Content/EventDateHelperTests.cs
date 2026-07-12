using MuuqWear.Application.Content;
using Xunit;

namespace MuuqWear.Tests.Content;

public class EventDateHelperTests
{
    [Fact]
    public void GetDateParts_parses_standard_label()
    {
        var (month, day) = EventDateHelper.GetDateParts("May 14, 2025");

        Assert.Equal("MAY", month);
        Assert.Equal("14", day);
    }

    [Fact]
    public void IsUpcoming_returns_false_for_past_dates()
    {
        var utcNow = new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc);

        Assert.False(EventDateHelper.IsUpcoming("May 14, 2025", utcNow));
    }
}
