namespace MuuqWear.Web.Helpers;

public static class AdminDateTimeHelper
{
    /// <summary>
    /// Normalizes API timestamps for consistent UTC comparisons and relative-time display.
    /// </summary>
    public static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
