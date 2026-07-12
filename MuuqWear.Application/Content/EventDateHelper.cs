using System.Globalization;

namespace MuuqWear.Application.Content;

public static class EventDateHelper
{
    public static (string Month, string Day) GetDateParts(string? dateLabel)
    {
        if (string.IsNullOrWhiteSpace(dateLabel) || dateLabel == "—")
            return ("—", "—");

        if (DateTime.TryParse(
                dateLabel,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return (parsed.ToString("MMM", CultureInfo.InvariantCulture).ToUpperInvariant(),
                parsed.Day.ToString(CultureInfo.InvariantCulture));
        }

        var parts = dateLabel.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return (parts[0], parts[1].TrimEnd(','));

        return (dateLabel, "—");
    }

    public static bool TryParseEventDate(string? dateLabel, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(dateLabel))
            return false;

        return DateTime.TryParse(
            dateLabel,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out date);
    }

    public static bool IsUpcoming(string? dateLabel, DateTime utcNow)
    {
        if (!TryParseEventDate(dateLabel, out var eventDate))
            return true;

        return eventDate.Date >= utcNow.Date;
    }
}
