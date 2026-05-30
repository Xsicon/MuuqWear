using MuuqWear.Model.AdminSettingsUser;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminSettingsComponent
{

    // ─── STATE ────────────────────────────────────────────────
    private List<AdminSettingsUserModel> users = new();
    private bool isLoading = true;
    private string activeTab = "users";
    private string modalError = string.Empty;
    private bool isSaving = false;

    // ─── INVITE STATE ─────────────────────────────────────────
    private bool isInviteModalOpen = false;
    private InviteAdminSettingsUserModel inviteForm = new();

    // ─── EDIT STATE ───────────────────────────────────────────
    private bool isEditModalOpen = false;
    private AdminSettingsUserModel? editingUser = null;
    private UpdateAdminSettingsUserModel editForm = new();

    // ─── DEACTIVATE STATE ─────────────────────────────────────
    private bool isDeactivateModalOpen = false;
    private AdminSettingsUserModel? deactivatingUser = null;

    // ─── INTEGRATIONS STATE ───────────────────────────────────
    private List<IntegrationModel> integrations = new()
{
    new IntegrationModel
    {
        Name      = "Stripe",
        Subtitle  = "Last Sync: Today 2:00 AM",
        IsHealthy = true,
        IsStatic  = true
    },
    new IntegrationModel
    {
        Name      = "Supabase",
        Subtitle  = "Checking...",
        IsHealthy = true,
        IsStatic  = false
    },
    new IntegrationModel
    {
        Name      = "Email Service",
        Subtitle  = "SendGrid",
        IsHealthy = true,
        IsStatic  = true
    }
};

    // ─── ROLES ────────────────────────────────────────────────
    private record RoleOption(string Value, string Label);

    private List<RoleOption> AvailableRoles => new()
{
    new("admin",          "Admin"),
    new("support_team",   "Support Team"),
    new("sales_team",     "Sales Team"),
    new("content_team",   "Content Team"),
    new("affiliate_team", "Affiliate Team")
};

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }

    // ─── LOAD USERS ───────────────────────────────────────────
    private async Task LoadUsers()
    {
        isLoading = true;
        StateHasChanged();

        var result = await AdminSettingService.GetAll();
        if (result.Success && result.Data != null)
            users = result.Data;

        isLoading = false;
        StateHasChanged();
    }

    // ─── TAB ──────────────────────────────────────────────────
    private async Task SetTab(string tab)
    {
        activeTab = tab;

        if (tab == "integrations")
            await LoadIntegrations();
        else if (tab == "users" && !users.Any())
            await LoadUsers();

        StateHasChanged();
    }

    // ─── INTEGRATIONS ─────────────────────────────────────────
    private async Task LoadIntegrations()
    {
        await CheckSupabaseHealth();
    }

    //  single responsibility — only checks Supabase health
    private async Task CheckSupabaseHealth()
    {
        var supabase = integrations.First(i => i.Name == "Supabase");
        supabase.IsLoading = true;
        StateHasChanged();

        var result = await AdminSettingService.CheckSupabaseHealth();

        supabase.IsLoading = false;
        supabase.IsHealthy = result.Success && result.Data?.IsHealthy == true;
        supabase.Subtitle = supabase.IsHealthy
            ? $"Status: Healthy · Checked {DateTime.Now:hh:mm tt}"
            : "Status: Unhealthy";

        StateHasChanged();
    }

    //  single responsibility — handles Test button per integration
    private async Task HandleTest(IntegrationModel integration)
    {
        if (integration.IsStatic)
        {
            integration.Subtitle = "Test functionality coming soon.";
            StateHasChanged();
            return;
        }

        if (integration.Name == "Supabase")
            await CheckSupabaseHealth();
    }

    // ─── INVITE ───────────────────────────────────────────────
    private void OpenInviteModal()
    {
        inviteForm = new();
        modalError = string.Empty;
        isInviteModalOpen = true;
    }

    private void CloseInviteModal()
    {
        isInviteModalOpen = false;
        modalError = string.Empty;
    }

    private async Task ConfirmInvite()
    {
        modalError = string.Empty;
        isSaving = true;
        StateHasChanged();

        var result = await AdminSettingService.Invite(inviteForm);

        if (result.Success && result.Data != null)
        {
            users.Insert(0, result.Data);
            CloseInviteModal();
        }
        else
        {
            modalError = result.Message ?? "Failed to invite user.";
        }

        isSaving = false;
        StateHasChanged();
    }

    // ─── EDIT ─────────────────────────────────────────────────
    private void OpenEditModal(AdminSettingsUserModel user)
    {
        editingUser = user;
        editForm = new UpdateAdminSettingsUserModel
        {
            FullName = user.FullName ?? string.Empty,
            Role = user.Role ?? string.Empty
        };
        modalError = string.Empty;
        isEditModalOpen = true;
    }

    private void CloseEditModal()
    {
        isEditModalOpen = false;
        editingUser = null;
        modalError = string.Empty;
    }

    private async Task ConfirmEdit()
    {
        modalError = string.Empty;
        isSaving = true;
        StateHasChanged();

        var result = await AdminSettingService.Update(
            editingUser!.Id, editForm);

        if (result.Success && result.Data != null)
        {
            var index = users.FindIndex(u => u.Id == editingUser!.Id);
            if (index != -1) users[index] = result.Data;
            CloseEditModal();
        }
        else
        {
            modalError = result.Message ?? "Failed to update user.";
        }

        isSaving = false;
        StateHasChanged();
    }

    // ─── DEACTIVATE ───────────────────────────────────────────
    private void OpenDeactivateModal(AdminSettingsUserModel user)
    {
        deactivatingUser = user;
        modalError = string.Empty;
        isDeactivateModalOpen = true;
    }

    private void CloseDeactivateModal()
    {
        isDeactivateModalOpen = false;
        deactivatingUser = null;
        modalError = string.Empty;
    }

    private async Task ConfirmDeactivate()
    {
        modalError = string.Empty;
        isSaving = true;
        StateHasChanged();

        var result = await AdminSettingService.Deactivate(
            deactivatingUser!.Id);

        if (result.Success)
        {
            users.RemoveAll(u => u.Id == deactivatingUser!.Id);
            CloseDeactivateModal();
        }
        else
        {
            modalError = result.Message ?? "Failed to deactivate user.";
        }

        isSaving = false;
        StateHasChanged();
    }

    // ─── HELPERS ──────────────────────────────────────────────
    private string GetRoleDisplay(string? role) => role switch
    {
        "admin" => "Admin",
        "support_team" => "Support Team",
        "sales_team" => "Sales Team",
        "content_team" => "Content Team",
        "affiliate_team" => "Affiliate Team",
        _ => role ?? "Unknown"
    };
}