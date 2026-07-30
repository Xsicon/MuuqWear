using MuuqWear.Application.Shared;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

internal static class SupportPaginatedLoader
{
    public const int DefaultPageSize = 100;
    public const int MaxPages = 20;

    public static async Task<(List<T> Items, string? Error, string? TruncationWarning)> LoadAllPagesAsync<T>(
        Func<int, int, Task<Response<PaginatedResponse<T>>>> fetchPage,
        int pageSize = DefaultPageSize)
    {
        var all = new List<T>();
        var page = 1;
        string? error = null;

        while (page <= MaxPages)
        {
            var result = await fetchPage(page, pageSize);
            if (!result.Success || result.Data == null)
            {
                error = result.Message ?? "Failed to load data.";
                break;
            }

            all.AddRange(result.Data.Data);

            if (!result.Data.HasMore)
                break;

            page++;
        }

        string? truncation = null;
        if (page > MaxPages)
            truncation = $"Showing the first {MaxPages * pageSize:N0} records. Use search or filters to narrow results.";

        return (all, page == 1 ? error : null, truncation);
    }
}
