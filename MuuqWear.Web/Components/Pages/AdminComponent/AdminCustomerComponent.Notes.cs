using MuuqWear.Model.Customer;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCustomerComponent
{
    private List<CustomerNoteModel> activeCustomerNotes = new();
    private bool isNotesLoading;
    private bool isNotesSaving;
    private string notesErrorMessage = string.Empty;
    private Guid? notesLoadedForCustomerId;

    private CustomerModel? notesPanelCustomer;
    private bool isNotesPanelOpen;

    private async Task LoadNotesForCustomerAsync(Guid customerId, bool forceReload = false)
    {
        if (!forceReload && notesLoadedForCustomerId == customerId && activeCustomerNotes.Count > 0)
            return;

        isNotesLoading = true;
        notesErrorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var result = await CustomerService.GetNotes(customerId);

            if (!result.Success || result.Data == null)
            {
                activeCustomerNotes = new();
                notesErrorMessage = result.Message ?? "Failed to load notes.";
                return;
            }

            activeCustomerNotes = result.Data
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            notesLoadedForCustomerId = customerId;
        }
        finally
        {
            isNotesLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleAddNoteAsync(string body)
    {
        var customer = notesPanelCustomer ?? selectedCustomerForDetails ?? selectedCustomer;
        if (customer == null || string.IsNullOrWhiteSpace(body))
            return;

        isNotesSaving = true;
        notesErrorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var result = await CustomerService.AddNote(customer.Id, new CreateCustomerNoteModel
            {
                Body = body.Trim()
            });

            if (!result.Success || result.Data == null)
            {
                notesErrorMessage = result.Message ?? "Failed to add note.";
                return;
            }

            activeCustomerNotes.Insert(0, result.Data);
            notesLoadedForCustomerId = customer.Id;
            ApplyNoteSummaryToCustomer(customer.Id, result.Data);
            CustomersTabCoordinator.RequestMessagesRefresh();
            await ShowToast("Note added.", success: true);
        }
        finally
        {
            isNotesSaving = false;
            StateHasChanged();
        }
    }

    private void ApplyNoteSummaryToCustomer(Guid customerId, CustomerNoteModel note)
    {
        var customer = customers.FirstOrDefault(c => c.Id == customerId);
        if (customer == null)
            return;

        customer.NoteCount += 1;
        customer.LatestNotePreview = TruncatePreview(note.Body);
        customer.LatestNoteAt = note.CreatedAt;
        customer.LatestNoteAuthorName = note.AuthorName;
        customer.LatestNoteAuthorRole = note.AuthorRole;
    }

    private async Task OpenNotesPanel(CustomerModel customer)
    {
        notesPanelCustomer = customer;
        isNotesPanelOpen = true;
        activeCustomerNotes = new();
        notesLoadedForCustomerId = null;
        await LoadNotesForCustomerAsync(customer.Id, forceReload: true);
    }

    private void CloseNotesPanel()
    {
        isNotesPanelOpen = false;
        notesPanelCustomer = null;
    }

    private async Task SelectCustomerForDetailsWithNotes(CustomerModel customer)
    {
        SelectCustomerForDetails(customer);
        activeCustomerNotes = new();
        notesLoadedForCustomerId = null;
        await LoadNotesForCustomerAsync(customer.Id, forceReload: true);
    }

    private async Task OpenCustomerDetailWithNotes(CustomerModel customer)
    {
        OpenCustomerDetail(customer);
        activeCustomerNotes = new();
        notesLoadedForCustomerId = null;
        await LoadNotesForCustomerAsync(customer.Id, forceReload: true);
    }

    private void ClearNotesState()
    {
        activeCustomerNotes = new();
        notesLoadedForCustomerId = null;
        notesErrorMessage = string.Empty;
        CloseNotesPanel();
    }

    private static string TruncatePreview(string? value, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength].TrimEnd() + "…";
    }

    private static string FormatNoteTimestamp(DateTime? value) =>
        value?.ToString("MMM dd, yyyy h:mm tt") ?? "—";
}
