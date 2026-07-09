using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Application.Services.CustomerService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCustomerComponent : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ICustomerService CustomerService { get; set; } = default!;
    [Inject] private AdminCustomersTabCoordinator CustomersTabCoordinator { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "view")]
    public string? ViewQuery { get; set; }

    private List<CustomerModel> customers = new();
    private bool isLoading;
    private string searchQuery = string.Empty;
    private string errorMessage = string.Empty;
    private int currentPage = 1;
    private int pageSize = 10;
    private int totalCount;
    private CancellationTokenSource? _searchCts;
    private string activeView = "list";

    private bool showToast;
    private bool toastSuccess = true;
    private string toastMessage = string.Empty;
    private CancellationTokenSource? _toastCts;

    private CustomerModel? selectedCustomer;
    private CustomerModel? selectedCustomerForDetails;
    private bool isDetailPanelOpen;

    protected override async Task OnInitializedAsync()
    {
        CustomersTabCoordinator.ViewChanged += OnCustomersViewChanged;
        CustomersTabCoordinator.CustomerFocusRequested += OnCustomerFocusRequested;
        NavigationManager.LocationChanged += OnLocationChanged;
        ApplyViewFromQuery();
        ApplyCustomerFocusFromQuery();
        await LoadCustomers();
        await TryFocusCustomerAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        var previousView = activeView;
        var previousFocus = pendingFocusCustomerId;
        ApplyViewFromQuery();
        ApplyCustomerFocusFromQuery();

        if (previousView != activeView)
        {
            currentPage = 1;
            CloseCustomerDetail();
            selectedCustomerForDetails = null;
            ClearNotesState();
            await LoadCustomers();
        }

        if (pendingFocusCustomerId.HasValue && pendingFocusCustomerId != previousFocus)
        {
            if (activeView != "notes")
                activeView = "notes";

            currentPage = 1;
            await LoadCustomers();
            await TryFocusCustomerAsync();
        }
    }

    private async Task LoadCustomers()
    {
        isLoading = true;
        errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var result = await CustomerService.GetAll(searchQuery, currentPage, pageSize);

            if (!result.Success || result.Data == null)
            {
                errorMessage = result.Message ?? "Failed to load customers.";
                customers = new();
                totalCount = 0;
                await ShowToast(errorMessage, success: false);
                return;
            }

            customers = result.Data.Data;
            totalCount = result.Data.TotalCount;
            currentPage = result.Data.Page;
            pageSize = result.Data.PageSize > 0 ? result.Data.PageSize : pageSize;
            await EnsureDetailsSelectionAsync();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void OnCustomersViewChanged(string view)
    {
        var normalized = AdminCustomersTabCoordinator.NormalizeView(view);
        if (activeView == normalized)
            return;

        activeView = normalized;
        currentPage = 1;
        CloseCustomerDetail();
        selectedCustomerForDetails = null;
        ClearNotesState();

        _ = InvokeAsync(async () =>
        {
            await LoadCustomers();
            StateHasChanged();
        });
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!NavigationManager.ToBaseRelativePath(NavigationManager.Uri)
                .StartsWith("admin/customers", StringComparison.OrdinalIgnoreCase))
            return;

        _ = InvokeAsync(HandleLocationChangedAsync);
    }

    private async Task HandleLocationChangedAsync()
    {
        try
        {
            ApplyViewFromQuery();
            ApplyCustomerFocusFromQuery();
            currentPage = 1;
            CloseCustomerDetail();
            selectedCustomerForDetails = null;
            ClearNotesState();
            await LoadCustomers();
            await TryFocusCustomerAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            StateHasChanged();
        }
    }

    private void ApplyViewFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        var view = query.TryGetValue("view", out var value) && !string.IsNullOrEmpty(value)
            ? value.ToString()
            : ViewQuery;

        activeView = AdminCustomersTabCoordinator.NormalizeView(view);
    }

    private async Task EnsureDetailsSelectionAsync()
    {
        if (activeView != "details" || customers.Count == 0)
            return;

        if (selectedCustomerForDetails != null &&
            customers.Any(c => c.Id == selectedCustomerForDetails.Id))
            return;

        await SelectCustomerForDetailsWithNotes(customers[0]);
    }

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        searchQuery = e.Value?.ToString() ?? string.Empty;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(500, _searchCts.Token);
            currentPage = 1;
            selectedCustomerForDetails = null;
            ClearNotesState();
            await LoadCustomers();
        }
        catch (TaskCanceledException) { }
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            _searchCts?.Cancel();
            currentPage = 1;
            selectedCustomerForDetails = null;
            ClearNotesState();
            await LoadCustomers();
        }
    }

    private async Task HandlePageChanged((int Page, int PageSize) args)
    {
        currentPage = args.Page;
        pageSize = args.PageSize;
        selectedCustomerForDetails = null;
        ClearNotesState();
        await LoadCustomers();
    }

    private void OpenCustomerDetail(CustomerModel customer)
    {
        selectedCustomer = customer;
        isDetailPanelOpen = true;
    }

    private void SelectCustomerForDetails(CustomerModel customer)
    {
        selectedCustomerForDetails = customer;
    }

    private void CloseCustomerDetail()
    {
        isDetailPanelOpen = false;
        selectedCustomer = null;
    }

    private void ViewCustomerOrders()
    {
        var customer = selectedCustomer ?? selectedCustomerForDetails;
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
            return;

        var email = Uri.EscapeDataString(customer.Email.Trim());
        NavigationManager.NavigateTo($"/admin/orders?tab=orders&search={email}");
    }

    private string GetPageSubtitle() => activeView switch
    {
        "details" => "View detailed customer profiles and order history",
        "notes" => "Internal team notes on customer records",
        _ => "View and manage customer accounts"
    };

    private string GetSectionTitle() => activeView switch
    {
        "details" => "Customer Details",
        "notes" => "Customer Notes",
        _ => "Customers"
    };

    private static string FormatDate(DateTime? value) =>
        value?.ToString("MMM dd, yyyy") ?? "—";

    private async Task ShowToast(string message, bool success)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();

        toastMessage = message;
        toastSuccess = success;
        showToast = true;
        StateHasChanged();

        try
        {
            await Task.Delay(3000, _toastCts.Token);
            showToast = false;
            StateHasChanged();
        }
        catch (TaskCanceledException) { }
    }

    public void Dispose()
    {
        CustomersTabCoordinator.ViewChanged -= OnCustomersViewChanged;
        CustomersTabCoordinator.CustomerFocusRequested -= OnCustomerFocusRequested;
        NavigationManager.LocationChanged -= OnLocationChanged;
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _toastCts?.Cancel();
        _toastCts?.Dispose();
    }
}
