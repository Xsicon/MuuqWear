using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Services;

public static class AdminUiErrorHelper
{
    public const string ForbiddenMessage =
        "You don't have permission to view or change this data. Contact an administrator if you need access.";

    public static string FromException(Exception ex) =>
        HttpResponseReader.FromException(ex);

    public static string FromApi(string? message, string fallback)
    {
        if (IsForbiddenMessage(message))
            return ForbiddenMessage;

        return string.IsNullOrWhiteSpace(message) ? fallback : message;
    }

    public static bool IsForbiddenMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var normalized = message.Trim();

        return normalized.Contains("403", StringComparison.Ordinal)
               || normalized.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("not authorized", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("access denied", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("don't have permission", StringComparison.OrdinalIgnoreCase);
    }
}
