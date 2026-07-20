using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Services;

public static class AdminUiErrorHelper
{
    public static string FromException(Exception ex) =>
        HttpResponseReader.FromException(ex);

    public static string FromApi(string? message, string fallback) =>
        string.IsNullOrWhiteSpace(message) ? fallback : message;
}
