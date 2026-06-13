using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Model.AffiliateApplication;

namespace MuuqWear.Web.Components.Pages.AdminComponent;
public partial class AdminAffiliatesComponent
{
    // Tab state
    private string activeTab = "pending";

    // Data
    private List<AffiliateApplicationModel> pendingApplications = new();
    private List<AffiliateApplicationModel> activeApplications = new();
    private List<AffiliateApplicationModel> waitListApplications = new();
    private int pendingCount = 0;
    private int totalActiveDays = 0;

    // Modal state
    private bool showViewModal = false;
    private AffiliateApplicationModel? selectedApplication = null;

    // UI state
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadApplications();
    }

    private async Task LoadApplications()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            // Load pending and active in parallel
            var pendingTask = AffiliateService.GetAllApplications("pending");
            var activeTask = AffiliateService.GetAllApplications("approved");
            var waitListTask = AffiliateService.GetAllApplications("waitlisted");
            var countTask = AffiliateService.GetPendingCount();

            await Task.WhenAll(pendingTask, activeTask, waitListTask, countTask);

            // Handle pending
            var pendingResult = await pendingTask;
            if (pendingResult.Success && pendingResult.Data != null)
            {
                pendingApplications = pendingResult.Data;
            }

            // Handle active
            var activeResult = await activeTask;
            if (activeResult.Success && activeResult.Data != null)
            {
                activeApplications = activeResult.Data;
                totalActiveDays = CalculateTotalActiveDays(activeApplications);
            }
            var waitlistResult = await waitListTask;
            if (waitlistResult.Success && waitlistResult.Data != null)
            {
                waitListApplications = waitlistResult.Data;
            }
            // Handle count
            var countResult = await countTask;
            if (countResult.Success)
            {
                pendingCount = countResult.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [AdminAffiliates] Load error: {ex.Message}");
            errorMessage = "Failed to load applications";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private int CalculateTotalActiveDays(List<AffiliateApplicationModel> applications)
    {
        if (!applications.Any()) return 0;

        var oldestApproval = applications
            .Where(a => a.ReviewedAt.HasValue)
            .Min(a => a.ReviewedAt);

        if (oldestApproval == null) return 0;

        return (DateTime.UtcNow - oldestApproval.Value).Days;
    }

    /// <summary>
    /// Handle admin actions (approve, reject, etc.)
    /// </summary>
    private async Task HandleAction((Guid Id, string Action) data)
    {
        var (applicationId, action) = data;

        if (action.ToLower() == "view")
        {
            OpenViewModal(applicationId);
            return;
        }

        try
        {

            // Call backend
            var result = await AffiliateService.ApproveApplication(applicationId);

            if (result.Success)
            {

                // Reload applications to reflect changes
                await LoadApplications();
            }
            else
            {
                Console.WriteLine($" [Admin] Update failed: {result.Message}");
                errorMessage = result.Message;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Admin] HandleAction error: {ex.Message}");
            errorMessage = "Failed to approve application status";
            StateHasChanged();
        }
    }


    private void OpenViewModal(Guid applicationId)
    {
        // Find application from all lists
        selectedApplication = pendingApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? activeApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? waitListApplications.FirstOrDefault(a => a.Id == applicationId);

        if (selectedApplication != null)
        {
            showViewModal = true;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Close view modal
    /// </summary>
    private void CloseViewModal()
    {
        showViewModal = false;
        selectedApplication = null;
        StateHasChanged();
    }

}
