using System.Net.Http.Json;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Shared HTTP response parsing with sanitized error messages for UI display.
/// </summary>
public static class HttpResponseReader
{
    public const string ConnectionErrorMessage = "Unable to connect to server. Please try again.";

    public static async Task<Response<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await ApiErrorMessageHelper.FromResponseAsync(response);
            return Response<T>.Fail(message);
        }

        try
        {
            var result = await response.Content.ReadFromJsonAsync<Response<T>>();
            return result ?? Response<T>.Fail("Empty response");
        }
        catch
        {
            return Response<T>.Fail("Failed to parse response");
        }
    }

    public static string FromException(Exception _) => ConnectionErrorMessage;
}
