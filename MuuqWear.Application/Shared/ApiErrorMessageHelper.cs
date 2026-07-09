using System.Net;
using System.Text.Json;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Produces safe, user-facing error messages from HTTP API responses.
/// </summary>
public static class ApiErrorMessageHelper
{
    private const int MaxMessageLength = 300;

    public static async Task<string> FromResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return string.Empty;

        var statusMessage = $"Server error: {response.StatusCode}";

        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return statusMessage;

            var parsed = TryParseMessage(body);
            if (!string.IsNullOrWhiteSpace(parsed))
                return parsed!;
        }
        catch
        {
            // Fall back to the generic status message.
        }

        return statusMessage;
    }

    internal static string? TryParseMessage(string body)
    {
        if (LooksLikeInternalDetails(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (TryReadMessageProperty(document.RootElement, out var message))
                return IsSafeUserMessage(message) ? message : null;
        }
        catch (JsonException)
        {
            if (IsSafeUserMessage(body))
                return body.Trim();
        }

        return null;
    }

    private static bool TryReadMessageProperty(JsonElement element, out string message)
    {
        message = string.Empty;

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.Equals("message", StringComparison.OrdinalIgnoreCase))
                continue;

            message = property.Value.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(message);
        }

        return false;
    }

    private static bool LooksLikeInternalDetails(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (text.Contains('<') && text.Contains('>'))
            return true;

        return text.Contains("StackTrace", StringComparison.OrdinalIgnoreCase)
               || (text.Contains(" at ", StringComparison.Ordinal)
                   && text.Contains(".cs:line", StringComparison.OrdinalIgnoreCase))
               || (text.Contains("System.", StringComparison.Ordinal)
                   && text.Contains("Exception", StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsSafeUserMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if (trimmed.Length > MaxMessageLength)
            return false;

        return !LooksLikeInternalDetails(trimmed);
    }
}
