using System.Diagnostics;
using System.Text.Json;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Shared HTTP response parsing with sanitized error messages for UI display.
/// </summary>
public static class HttpResponseReader
{
    public const string ConnectionErrorMessage = "Unable to connect to server. Please try again.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<Response<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await ApiErrorMessageHelper.FromResponseAsync(response);
            return Response<T>.Fail(message);
        }

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return Response<T>.Fail("Failed to parse response");
        }

        // Endpoints that complete without returning a payload (204/empty body).
        if (string.IsNullOrWhiteSpace(body))
            return new Response<T> { Success = true };

        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
        }

        using (document)
        {
            if (document != null && IsWrappedResponse(document.RootElement))
            {
                try
                {
                    var envelope = JsonSerializer.Deserialize<Response<T>>(body, JsonOptions);
                    if (envelope != null)
                    {
                        // An envelope without an explicit flag isn't a failure; the 2xx status decides.
                        if (!TryReadBoolean(document.RootElement, "success", out _))
                            envelope.Success = true;

                        return envelope;
                    }
                }
                catch (JsonException)
                {
                }

                // The envelope is recognizable but its payload doesn't fit T, so keep the
                // server's verdict and let the caller work without the data.
                return new Response<T>
                {
                    Success = !TryReadBoolean(document.RootElement, "success", out var success) || success,
                    Message = ReadString(document.RootElement, "message")
                };
            }

            if (document != null)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                    if (data != null)
                        return Response<T>.SuccessResponse(data);
                }
                catch (JsonException)
                {
                }
            }
        }

        // A 2xx whose body can't be mapped is still a completed call. Callers treat a missing
        // payload as "refetch", which is far better than reporting a failure that didn't happen.
        Debug.WriteLine(
            $"[HttpResponseReader] Unmapped {typeof(T).Name} payload: {Truncate(body)}");

        return new Response<T> { Success = true };
    }

    /// <summary>
    /// Distinguishes the <see cref="Response{T}"/> envelope from a bare payload. Without this a
    /// bare DTO still deserializes into an envelope, producing a false failure with no message.
    /// </summary>
    private static bool IsWrappedResponse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals("success", StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals("data", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadBoolean(JsonElement root, string name, out bool value)
    {
        value = false;

        foreach (var property in root.EnumerateObject())
        {
            if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return false;

            value = property.Value.GetBoolean();
            return true;
        }

        return false;
    }

    private static string ReadString(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string Truncate(string body) =>
        body.Length <= 300 ? body : body[..300] + "…";

    public static string FromException(Exception _) => ConnectionErrorMessage;
}
