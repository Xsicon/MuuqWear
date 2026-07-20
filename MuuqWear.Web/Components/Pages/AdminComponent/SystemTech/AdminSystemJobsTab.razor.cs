using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.AdminSystemService;
using MuuqWear.Model.DTO.AdminSystem;

namespace MuuqWear.Web.Components.Pages.AdminComponent.SystemTech;

public partial class AdminSystemJobsTab
{
    [Inject] private IAdminSystemService AdminSystemService { get; set; } = default!;

    private List<BackgroundJobModel> jobs = [];
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        var result = await AdminSystemService.GetJobsAsync();

        if (result.Success && result.Data != null)
            jobs = result.Data;
        else
            errorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Unable to load background jobs."
                : result.Message;

        isLoading = false;
    }

    private static string GetStatusClass(string status) =>
        status.ToLowerInvariant() switch
        {
            "running" => "info",
            "failed" => "error",
            "completed" => "info",
            _ => "warning"
        };
}
