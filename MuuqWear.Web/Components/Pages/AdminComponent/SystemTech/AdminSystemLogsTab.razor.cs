using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.AdminSystemService;
using MuuqWear.Model.DTO.AdminSystem;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.SystemTech;

public partial class AdminSystemLogsTab
{
    [Inject] private IAdminSystemService AdminSystemService { get; set; } = default!;

    private List<SystemLogEntryModel> logs = [];
    private string daysFilter = "7";
    private string levelFilter = "all";
    private string search = string.Empty;
    private int currentPage = 1;
    private const int pageSize = 50;
    private int totalCount;
    private bool isLoading = true;
    private string? errorMessage;
    private CancellationTokenSource? _searchDebounce;

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));

    protected override async Task OnInitializedAsync() => await LoadLogsAsync();

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        search = e.Value?.ToString() ?? string.Empty;
        currentPage = 1;

        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;

        try
        {
            await Task.Delay(350, token);
            await LoadLogsAsync();
        }
        catch (TaskCanceledException)
        {
            // Debounced — newer input superseded this call.
        }
    }

    private async Task PreviousPage()
    {
        if (currentPage <= 1)
            return;

        currentPage--;
        await LoadLogsAsync();
    }

    private async Task NextPage()
    {
        if (currentPage >= TotalPages)
            return;

        currentPage++;
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var days = int.TryParse(daysFilter, out var parsedDays) ? parsedDays : 7;
            var result = await AdminSystemService.GetLogsAsync(
                days,
                levelFilter,
                search,
                currentPage,
                pageSize);

            if (result.Success && result.Data != null)
            {
                logs = result.Data.Items;
                totalCount = result.Data.TotalCount;
                currentPage = result.Data.Page;
            }
            else
            {
                logs = [];
                totalCount = 0;
                errorMessage = AdminUiErrorHelper.FromApi(result.Message, "Unable to load logs.");
            }
        }
        catch (Exception ex)
        {
            logs = [];
            totalCount = 0;
            errorMessage = AdminUiErrorHelper.FromException(ex);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private static string GetLevelClass(string level) => level switch
    {
        "Error" => "error",
        "Warning" => "warning",
        _ => "info"
    };
}
