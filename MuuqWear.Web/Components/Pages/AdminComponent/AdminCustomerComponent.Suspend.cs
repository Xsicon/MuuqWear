using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MuuqWear.Model.Customer;
using MuuqWear.Web.Helpers;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCustomerComponent
{
    private bool isAccountActionBusy;
    private bool showSuspendForm;
    private int suspendDurationDays = 30;
    private string suspendReason = string.Empty;
    private string accountActionError = string.Empty;
    private string statusFilter = string.Empty;
    private string pendingToast = string.Empty;
    private bool suppressDetailsAutoSelect;

    private static readonly HashSet<string> AllowedStatusFilters =
        new(StringComparer.OrdinalIgnoreCase) { "", "active", "suspended" };

    private async Task HandleStatusFilterChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        statusFilter = AllowedStatusFilters.Contains(value) ? value : string.Empty;
        currentPage = 1;
        selectedCustomerForDetails = null;
        ClearNotesState();
        loadedCacheKey = null;
        await LoadCustomersAsync(force: true);
    }

    private void BeginSuspendForm()
    {
        showSuspendForm = true;
        suspendDurationDays = 30;
        suspendReason = string.Empty;
        accountActionError = string.Empty;
    }

    private void CancelSuspendForm()
    {
        showSuspendForm = false;
        suspendReason = string.Empty;
        accountActionError = string.Empty;
    }

    private CustomerModel? GetActiveAccountCustomer() =>
        selectedCustomer ?? selectedCustomerForDetails;

    private async Task SuspendActiveCustomerAsync()
    {
        var customer = GetActiveAccountCustomer();
        if (customer == null || isAccountActionBusy)
            return;

        isAccountActionBusy = true;
        accountActionError = string.Empty;
        StateHasChanged();

        try
        {
            var result = await CustomerService.Suspend(customer.Id, new SuspendCustomerModel
            {
                DurationDays = suspendDurationDays,
                Reason = string.IsNullOrWhiteSpace(suspendReason) ? null : suspendReason.Trim()
            });

            if (!result.Success)
            {
                accountActionError = AdminUiErrorHelper.FromApi(result.Message, "Failed to suspend customer.");
                return;
            }

            var durationLabel = CustomerAccountStatusHelper.GetDurationLabel(suspendDurationDays);
            var updated = await ResolveUpdatedCustomerAsync(customer, result.Data, suspended: true);
            ApplyCustomerUpdate(updated);
            showSuspendForm = false;
            suspendReason = string.Empty;
            await RefreshNotesAfterAccountActionAsync(customer.Id);
            await ReloadAfterAccountActionAsync(customer.Id, updated, suspended: true);
            pendingToast = $"{updated.FullName ?? "Customer"} suspended for {durationLabel}.";
        }
        finally
        {
            isAccountActionBusy = false;
            StateHasChanged();
        }

        await FlushPendingToastAsync();
    }

    private async Task ReactivateActiveCustomerAsync()
    {
        var customer = GetActiveAccountCustomer();
        if (customer == null || isAccountActionBusy)
            return;

        isAccountActionBusy = true;
        accountActionError = string.Empty;
        StateHasChanged();

        try
        {
            var result = await CustomerService.Reactivate(customer.Id);

            if (!result.Success)
            {
                accountActionError = AdminUiErrorHelper.FromApi(result.Message, "Failed to reactivate customer.");
                return;
            }

            var updated = await ResolveUpdatedCustomerAsync(customer, result.Data, suspended: false);
            ApplyCustomerUpdate(updated);
            showSuspendForm = false;
            await RefreshNotesAfterAccountActionAsync(customer.Id);
            await ReloadAfterAccountActionAsync(customer.Id, updated, suspended: false);
            pendingToast = $"{updated.FullName ?? "Customer"} reactivated.";
        }
        finally
        {
            isAccountActionBusy = false;
            StateHasChanged();
        }

        await FlushPendingToastAsync();
    }

    /// <summary>
    /// Refetches the current page so the table shows server state without a reload. The refreshed
    /// row is only trusted when it already reflects the change, otherwise the locally resolved
    /// model stays so the panel never flips back to the previous status.
    /// </summary>
    private async Task ReloadAfterAccountActionAsync(
        Guid customerId, CustomerModel resolved, bool suspended)
    {
        var wasSelectedForDetails = selectedCustomerForDetails?.Id == customerId;

        suppressDetailsAutoSelect = true;
        try
        {
            loadedCacheKey = null;
            await LoadCustomersAsync(force: true);
        }
        finally
        {
            suppressDetailsAutoSelect = false;
        }

        var refreshed = customers.FirstOrDefault(c => c.Id == customerId);
        var authoritative =
            refreshed != null && CustomerAccountStatusHelper.IsSuspended(refreshed) == suspended
                ? refreshed
                : resolved;

        ApplyCustomerUpdate(authoritative);

        // The customer can drop out of a filtered page (e.g. suspending under the Active filter);
        // keep them selected so the result of the action stays visible.
        if (wasSelectedForDetails && selectedCustomerForDetails?.Id != customerId)
            selectedCustomerForDetails = authoritative;
    }

    private async Task FlushPendingToastAsync()
    {
        if (string.IsNullOrEmpty(pendingToast))
            return;

        var message = pendingToast;
        pendingToast = string.Empty;
        await ShowToast(message, success: true);
    }

    /// <summary>
    /// Status endpoints may omit the customer payload or echo back an id that doesn't match the
    /// admin list, so the locally known customer id always wins and payload fields are overlaid.
    /// </summary>
    private async Task<CustomerModel> ResolveUpdatedCustomerAsync(
        CustomerModel current, CustomerModel? payload, bool suspended)
    {
        var resolved = BuildLocalStatusUpdate(current, suspended);

        // A payload whose id doesn't match is not this customer's record, so it must not
        // overwrite the status we just applied.
        var source = payload != null && payload.Id == current.Id ? payload : null;

        if (source == null)
        {
            var refetched = await CustomerService.GetById(current.Id);
            if (refetched.Success && refetched.Data != null && refetched.Data.Id == current.Id)
                source = refetched.Data;
        }

        if (source == null)
            return resolved;

        if (!string.IsNullOrWhiteSpace(source.FullName))
            resolved.FullName = source.FullName;

        if (!string.IsNullOrWhiteSpace(source.Email))
            resolved.Email = source.Email;

        if (source.CreatedAt.HasValue)
            resolved.CreatedAt = source.CreatedAt;

        if (source.LastOrderAt.HasValue)
            resolved.LastOrderAt = source.LastOrderAt;

        if (source.OrderCount > 0)
            resolved.OrderCount = source.OrderCount;

        if (source.TotalSpent > 0)
            resolved.TotalSpent = source.TotalSpent;

        if (!string.IsNullOrWhiteSpace(source.AccountStatus))
        {
            resolved.AccountStatus = source.AccountStatus;
            resolved.SuspendedUntil = source.SuspendedUntil;
            resolved.SuspendedAt = source.SuspendedAt;
            resolved.SuspensionReason = source.SuspensionReason;
        }

        return resolved;
    }

    private CustomerModel BuildLocalStatusUpdate(CustomerModel current, bool suspended)
    {
        var now = DateTime.UtcNow;

        return new CustomerModel
        {
            Id = current.Id,
            FullName = current.FullName,
            Email = current.Email,
            CreatedAt = current.CreatedAt,
            OrderCount = current.OrderCount,
            TotalSpent = current.TotalSpent,
            LastOrderAt = current.LastOrderAt,
            NoteCount = current.NoteCount,
            LatestNotePreview = current.LatestNotePreview,
            LatestNoteAt = current.LatestNoteAt,
            LatestNoteAuthorName = current.LatestNoteAuthorName,
            LatestNoteAuthorRole = current.LatestNoteAuthorRole,
            AccountStatus = suspended ? "suspended" : "active",
            SuspendedAt = suspended ? now : null,
            SuspendedUntil = suspended ? now.AddDays(suspendDurationDays) : null,
            SuspensionReason = suspended && !string.IsNullOrWhiteSpace(suspendReason)
                ? suspendReason.Trim()
                : null
        };
    }

    private void ApplyCustomerUpdate(CustomerModel updated)
    {
        for (var i = 0; i < customers.Count; i++)
        {
            if (customers[i].Id != updated.Id)
                continue;

            var existingNotes = customers[i];
            updated.NoteCount = existingNotes.NoteCount;
            updated.LatestNotePreview = existingNotes.LatestNotePreview;
            updated.LatestNoteAt = existingNotes.LatestNoteAt;
            updated.LatestNoteAuthorName = existingNotes.LatestNoteAuthorName;
            updated.LatestNoteAuthorRole = existingNotes.LatestNoteAuthorRole;
            customers[i] = updated;
            break;
        }

        if (selectedCustomer?.Id == updated.Id)
            selectedCustomer = updated;

        if (selectedCustomerForDetails?.Id == updated.Id)
            selectedCustomerForDetails = updated;

        if (notesPanelCustomer?.Id == updated.Id)
            notesPanelCustomer = updated;
    }

    private async Task RefreshNotesAfterAccountActionAsync(Guid customerId)
    {
        notesLoadedForCustomerId = null;
        await LoadNotesForCustomerAsync(customerId, forceReload: true);

        var latest = activeCustomerNotes.FirstOrDefault();
        if (latest == null)
            return;

        var customer = customers.FirstOrDefault(c => c.Id == customerId);
        if (customer == null)
            return;

        customer.NoteCount = activeCustomerNotes.Count;
        customer.LatestNotePreview = TruncatePreview(latest.Body);
        customer.LatestNoteAt = latest.CreatedAt;
        customer.LatestNoteAuthorName = latest.AuthorName;
        customer.LatestNoteAuthorRole = latest.AuthorRole;
    }

    private void ResetAccountActionState()
    {
        showSuspendForm = false;
        suspendReason = string.Empty;
        accountActionError = string.Empty;
        isAccountActionBusy = false;
    }

    private void SetSuspendDurationDays(int days) => suspendDurationDays = days;

    private void SetSuspendReason(string value) => suspendReason = value;
}
