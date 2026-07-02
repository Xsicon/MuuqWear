using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;using MuuqWear.Application.Services.RefundService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Refund;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminOrdersComponent : IDisposable
{
    [Inject] private IRefundService RefundService { get; set; } = default!;
    [Inject] private AdminOrdersTabCoordinator OrdersTabCoordinator { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "search")]
    public string? SearchQuery { get; set; }

    [SupplyParameterFromQuery(Name = "tab")]
    public string? TabQuery { get; set; }

    [SupplyParameterFromQuery(Name = "status")]
    public string? StatusQuery { get; set; }

    private string? _dataLoadedForTab;

    private string searchTerm = string.Empty;

    // ─── STATE ────────────────────────────────────────────────
    private List<OrderModel> orders = new();
    private bool isLoading = true;
    private string activeMainTab = "orders";
    private string activeStatus = "";
    private int currentPage = 1;
    private int pageSize = 20;
    private int totalCount = 0;

    // ─── VIEW PANEL STATE ─────────────────────────────────────
    private bool isViewPanelOpen = false;
    private string viewPanelKind = "order";
    private OrderModel? viewOrder = null;
    private OrderReturnModel? viewReturn = null;
    private RefundModel? viewRefund = null;
    private bool isLoadingDetail = false;

    // ─── PROCESS MODAL STATE ──────────────────────────────────
    private bool isProcessModalOpen = false;
    private OrderModel? processingOrder = null;
    private OrderModel? processOrderDetail = null;
    private bool isLoadingProcessDetail = false;
    private bool isProcessing = false;
    private string processError = string.Empty;

    private bool isRefundProcessModalOpen = false;
    private RefundModel? processingRefund = null;
    private bool isRefundProcessing = false;
    private string refundProcessError = string.Empty;

    // ─── STATUS TABS ──────────────────────────────────────────
    private record StatusTab(string Label, string Value);

    private List<StatusTab> StatusTabs => new()
    {
        new("All",        ""),
        new("Pending",    "pending"),
        new("Processing", "processing"),
        new("Shipped",    "shipped"),
        new("Delivered",  "delivered"),
        new("Cancelled",  "cancelled")
    };

    // ─── BULK UPDATE STATE ────────────────────────────────────────
    private HashSet<Guid> selectedOrderIds = new();
    private bool isBulkModalOpen = false;
    private string bulkTargetStatus = string.Empty;
    private bool isBulkUpdating = false;

    private List<OrderReturnModel> returns = new();
    private bool isLoadingReturns = true;
    private string activeReturnStatus = "";
    private int returnsCurrentPage = 1;
    private int returnsTotalCount = 0;
    private const int returnsPageSize = 20;

    // ─── RETURN STATUS TABS ───────────────────────────────────────
    private List<StatusTab> ReturnStatusTabs => new()
{
new("All",      ""),
new("Pending",  "pending"),
new("Approved", "approved"),
new("Denied",   "denied")
};

    private List<RefundModel> refunds = new();
    private bool isLoadingRefunds = true;
    private string activeRefundStatus = "";
    private int refundsCurrentPage = 1;
    private int refundsTotalCount = 0;
    private const int refundsPageSize = 20;
    private string refundsError = string.Empty;
    private string returnActionError = string.Empty;
    private string? returnSuccessMessage = null;
    private Guid? processingRefundId = null;
    private bool isRefreshing = false;

    private List<StatusTab> RefundStatusTabs => new()
    {
        new("All",        ""),
        new("Pending",    "pending"),
        new("Processing", "processing"),
        new("Completed",  "completed"),
        new("Failed",     "failed"),
        new("Cancelled",  "cancelled")
    };

    //  true only when all visible orders are selected
    private bool AllSelected =>
        orders.Any() && orders.All(o => selectedOrderIds.Contains(o.Id));

    //  single responsibility — toggles one order selection
    private void ToggleOrderSelection(Guid orderId)
    {
        if (selectedOrderIds.Contains(orderId))
            selectedOrderIds.Remove(orderId);
        else
            selectedOrderIds.Add(orderId);
        StateHasChanged();
    }

    //  select or deselect all visible orders
    private void ToggleSelectAll()
    {
        if (AllSelected)
            selectedOrderIds.Clear();
        else
            foreach (var order in orders)
                selectedOrderIds.Add(order.Id);
        StateHasChanged();
    }

    private void OpenBulkModal()
    {
        bulkTargetStatus = string.Empty;
        processError = string.Empty;
        isBulkModalOpen = true;
    }

    private void CloseBulkModal()
    {
        isBulkModalOpen = false;
        bulkTargetStatus = string.Empty;
        processError = string.Empty;
    }

    private async Task ConfirmBulkUpdate()
    {
        if (string.IsNullOrEmpty(bulkTargetStatus))
        {
            processError = "Please select a status";
            return;
        }

        isBulkUpdating = true;
        processError = string.Empty;
        StateHasChanged();

        var result = await OrderService.BulkUpdateOrderStatus(
            selectedOrderIds.ToList(), bulkTargetStatus);

        if (result.Success)
        {
            foreach (var order in orders
                .Where(o => selectedOrderIds.Contains(o.Id)))
            {
                order.Status = bulkTargetStatus;
            }

            selectedOrderIds.Clear();
            CloseBulkModal();
            OrdersTabCoordinator.RequestBadgeCountsRefresh();
        }
        else
        {
            processError = result.Message ?? "Bulk update failed";
        }

        isBulkUpdating = false;
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        OrdersTabCoordinator.TabChanged += OnOrdersTabChanged;

        ApplyFiltersFromQuery();
    }

    protected override async Task OnParametersSetAsync()
    {
        ApplyFiltersFromQuery();
        await EnsureTabDataLoadedAsync();
    }

    private void ApplyFiltersFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        var tab = AdminOrdersTabCoordinator.NormalizeTab(
            query.TryGetValue("tab", out var tabVal) && !string.IsNullOrEmpty(tabVal)
                ? tabVal.ToString()
                : TabQuery);

        var fromSearch = query.TryGetValue("search", out var searchVal)
            ? searchVal.ToString()
            : SearchQuery ?? string.Empty;

        if (fromSearch != searchTerm)
        {
            searchTerm = fromSearch;
            if (tab == "orders")
                _dataLoadedForTab = null;
        }

        if (query.TryGetValue("status", out var statusVal) && !string.IsNullOrEmpty(statusVal))
        {
            var status = statusVal.ToString()!;
            switch (tab)
            {
                case "returns":
                    activeReturnStatus = status;
                    break;
                case "refunds":
                    activeRefundStatus = status;
                    break;
                default:
                    activeStatus = status;
                    break;
            }
        }
        else if (!string.IsNullOrEmpty(StatusQuery) && tab == "orders")
        {
            activeStatus = StatusQuery;
        }
    }

    private void OnOrdersTabChanged(string tab)
    {
        var normalized = AdminOrdersTabCoordinator.NormalizeTab(tab);
        if (activeMainTab == normalized && _dataLoadedForTab == normalized)
            return;

        activeMainTab = normalized;
        _dataLoadedForTab = null;
        exportMenuOpen = false;
        exportMessage = string.Empty;
        returnActionError = string.Empty;
        returnSuccessMessage = null;

        _ = InvokeAsync(async () =>
        {
            await EnsureTabDataLoadedAsync(force: true);
            StateHasChanged();
        });
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!NavigationManager.ToBaseRelativePath(NavigationManager.Uri)
                .StartsWith("admin/orders", StringComparison.OrdinalIgnoreCase))
            return;

        _dataLoadedForTab = null;

        await InvokeAsync(async () =>
        {
            ApplyTabFromQuery();
            ApplyFiltersFromQuery();
            await EnsureTabDataLoadedAsync(force: true);
            StateHasChanged();
        });
    }

    private async Task EnsureTabDataLoadedAsync(bool force = false)
    {
        var previousTab = activeMainTab;
        ApplyTabFromQuery();

        if (!force && _dataLoadedForTab == activeMainTab && previousTab == activeMainTab)
            return;

        _dataLoadedForTab = activeMainTab;
        await LoadTabData();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        OrdersTabCoordinator.TabChanged -= OnOrdersTabChanged;
    }

    private void ApplyTabFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        string? tab = null;
        if (query.TryGetValue("tab", out var value) && !string.IsNullOrEmpty(value))
            tab = value.ToString();
        else if (!string.IsNullOrEmpty(TabQuery))
            tab = TabQuery;

        activeMainTab = AdminOrdersTabCoordinator.NormalizeTab(tab);
        TabQuery = tab;
    }

    private async Task LoadTabData()
    {
        if (activeMainTab == "orders")
            await LoadOrders();
        else if (activeMainTab == "returns")
            await LoadReturns();
        else if (activeMainTab == "refunds")
            await LoadRefunds();
    }

    private string PageSubtitle => activeMainTab switch
    {
        "returns" => "Review and approve return requests",
        "refunds" => "Manage refund processing",
        _ => "Manage customer orders"
    };

    private async Task RefreshCurrentTab()
    {
        if (isRefreshing)
            return;

        isRefreshing = true;
        _dataLoadedForTab = null;
        StateHasChanged();

        try
        {
            await LoadTabData();
            _dataLoadedForTab = activeMainTab;
        }
        finally
        {
            isRefreshing = false;
            StateHasChanged();
        }
    }

    // ─── LOAD ─────────────────────────────────────────────────
    private async Task LoadOrders()
    {
        isLoading = true;
        selectedOrderIds.Clear();
        StateHasChanged();

        var result = await OrderService.GetAllOrders(
            string.IsNullOrEmpty(activeStatus) ? null : activeStatus,
            string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
            currentPage,
            pageSize);

        if (result.Success && result.Data != null)
        {
            orders = result.Data.Data;
            totalCount = result.Data.TotalCount;
            currentPage = result.Data.Page;
        }

        isLoading = false;
        StateHasChanged();
    }

    // ─── FILTERS ──────────────────────────────────────────────
    private async Task SetStatus(string status)
    {
        activeStatus = status;
        currentPage = 1;
        UpdateOrdersUrl();
        await LoadOrders();
    }

    private async Task ApplySearch()
    {
        currentPage = 1;
        UpdateOrdersUrl();
        await LoadOrders();
    }

    private async Task ClearSearch()
    {
        searchTerm = string.Empty;
        currentPage = 1;
        UpdateOrdersUrl();
        await LoadOrders();
    }

    private void OnSearchInput(ChangeEventArgs e) =>
        searchTerm = e.Value?.ToString() ?? string.Empty;

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await ApplySearch();
    }

    private void UpdateOrdersUrl()
    {
        var query = new Dictionary<string, string?> { ["tab"] = "orders" };

        if (!string.IsNullOrWhiteSpace(activeStatus))
            query["status"] = activeStatus;

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query["search"] = searchTerm.Trim();

        var uri = QueryHelpers.AddQueryString("/admin/orders", query);
        NavigationManager.NavigateTo(uri, replace: true);
    }

    private async Task HandlePageChanged((int Page, int PageSize) args)
    {
        currentPage = args.Page;
        pageSize = args.PageSize;
        await LoadOrders();
    }

    // ─── VIEW PANEL ───────────────────────────────────────────
    private async Task OpenViewPanel(Guid orderId)
    {
        viewPanelKind = "order";
        isViewPanelOpen = true;
        isLoadingDetail = true;
        viewOrder = orders.FirstOrDefault(o => o.Id == orderId);
        viewReturn = null;
        viewRefund = null;
        StateHasChanged();

        var result = await OrderService.GetOrderDetail(orderId);
        if (result.Success && result.Data != null)
            viewOrder = result.Data;

        isLoadingDetail = false;
        StateHasChanged();
    }

    private void OpenReturnViewPanel(OrderReturnModel item)
    {
        viewPanelKind = "return";
        viewReturn = item;
        viewOrder = null;
        viewRefund = null;
        isViewPanelOpen = true;
        isLoadingDetail = false;
    }

    private void OpenRefundViewPanel(RefundModel item)
    {
        viewPanelKind = "refund";
        viewRefund = item;
        viewOrder = null;
        viewReturn = null;
        isViewPanelOpen = true;
        isLoadingDetail = false;
    }

    private void CloseViewPanel()
    {
        isViewPanelOpen = false;
        viewOrder = null;
        viewReturn = null;
        viewRefund = null;
    }

    // ─── PROCESS MODAL ────────────────────────────────────────
    private async Task OpenProcessModal(OrderModel order)
    {
        processingOrder = order;
        processError = string.Empty;
        processOrderDetail = null;

        if (order.Status?.ToLower() == "pending")
        {
            var result = await OrderService.GetOrderDetail(order.Id);

            if (!result.Success || result.Data == null)
            {
                processError = result.Message
                               ?? "Failed to load order details.";
                StateHasChanged();
                return;
            }

            processOrderDetail = result.Data;
        }

        isProcessModalOpen = true;
        StateHasChanged();
    }

    private void CloseProcessModal()
    {
        isProcessModalOpen = false;
        processingOrder = null;
        processOrderDetail = null;
        processError = string.Empty;
    }

    private async Task ConfirmProcess()
    {
        isProcessing = true;
        processError = string.Empty;
        StateHasChanged();

        //  get next status from helper — no magic strings
        var nextStatus = GetNextAction(processingOrder!.Status)?.NextStatus;

        if (string.IsNullOrEmpty(nextStatus))
        {
            processError = "Invalid status transition";
            isProcessing = false;
            StateHasChanged();
            return;
        }

        var result = await OrderService.UpdateOrderStatus(
            processingOrder!.Id, nextStatus);

        if (result.Success)
        {
            //  update local list immediately
            var order = orders.FirstOrDefault(o => o.Id == processingOrder!.Id);
            if (order != null)
                order.Status = nextStatus;

            CloseProcessModal();
            OrdersTabCoordinator.RequestBadgeCountsRefresh();
        }
        else
        {
            processError = result.Message ?? "Failed to update order";
        }

        isProcessing = false;
        StateHasChanged();
    }

    // ─── HELPERS ──────────────────────────────────────────────
    //  single responsibility — only maps status to CSS class
    private string GetStatusBadgeClass(string? status) =>
        status?.ToLower() switch
        {
            "pending" => "status-badge--pending",
            "processing" => "status-badge--processing",
            "shipped" => "status-badge--shipped",
            "delivered" => "status-badge--delivered",
            "cancelled" => "status-badge--cancelled",
            _ => "status-badge--pending"
        };
    //  single responsibility — defines what action is available per status
    private record OrderAction(
        string Label,
        string NextStatus,
        string ModalTitle,
        string ModalSubtitle);

    private OrderAction? GetNextAction(string? status) =>
        status?.ToLower() switch
        {
            "pending" => new OrderAction(
                Label: "Process",
                NextStatus: "processing",
                ModalTitle: "Process Order",
                ModalSubtitle: "Confirm the shipping address before processing. " +
                               "This will change the status to Processing."),

            "processing" => new OrderAction(
                Label: "Mark Shipped",
                NextStatus: "shipped",
                ModalTitle: "Mark as Shipped",
                ModalSubtitle: "Confirm this order has been shipped. " +
                               "The customer will be notified."),

            "shipped" => new OrderAction(
                Label: "Mark Delivered",
                NextStatus: "delivered",
                ModalTitle: "Mark as Delivered",
                ModalSubtitle: "Confirm this order has been delivered. " +
                               "This action cannot be undone."),

            _ => null  // delivered/cancelled → no action
        };

    private async Task LoadReturns()
    {
        isLoadingReturns = true;
        isLoading = false;
        StateHasChanged();

        var result = await OrderReturnService.GetAllReturns(
            string.IsNullOrEmpty(activeReturnStatus)
                ? null
                : activeReturnStatus,
            returnsCurrentPage,
            returnsPageSize);

        if (result.Success && result.Data != null)
        {
            returns = result.Data.Data;
            returnsTotalCount = result.Data.TotalCount;
            returnsCurrentPage = result.Data.Page;
        }

        isLoadingReturns = false;
        StateHasChanged();
    }

    private async Task SetReturnStatus(string status)
    {
        activeReturnStatus = status;
        returnsCurrentPage = 1;
        await LoadReturns();
    }

    private async Task HandleReturnsPageChanged((int Page, int PageSize) args)
    {
        returnsCurrentPage = args.Page;
        await LoadReturns();
    }

    private async Task ApproveReturn(Guid returnId)
    {
        returnActionError = string.Empty;
        returnSuccessMessage = null;
        StateHasChanged();

        var result = await OrderReturnService.UpdateReturnStatus(
            returnId, "approved");

        if (result.Success)
        {
            var item = returns.FirstOrDefault(r => r.Id == returnId);
            if (item != null)
                item.Status = "approved";

            var refundHint = await FindPendingRefundNumberForReturnAsync(returnId);
            returnSuccessMessage = refundHint is not null
                ? $"Return approved. Pending refund {refundHint} was created — process it in Refunds."
                : "Return approved. A pending refund may have been created — check the Refunds tab.";

            OrdersTabCoordinator.RequestBadgeCountsRefresh();
        }
        else
            returnActionError = result.Message ?? "Failed to approve return";

        StateHasChanged();
    }

    private async Task DenyReturn(Guid returnId)
    {
        returnActionError = string.Empty;
        returnSuccessMessage = null;
        StateHasChanged();

        var result = await OrderReturnService.UpdateReturnStatus(
            returnId, "denied");

        if (result.Success)
        {
            var item = returns.FirstOrDefault(r => r.Id == returnId);
            if (item != null)
                item.Status = "denied";
        }
        else
            returnActionError = result.Message ?? "Failed to deny return";

        StateHasChanged();
    }

    private async Task<string?> FindPendingRefundNumberForReturnAsync(Guid returnId)
    {
        var result = await RefundService.GetAllRefunds("pending", 1, 50);
        if (!result.Success || result.Data?.Data is null)
            return null;

        return result.Data.Data
            .FirstOrDefault(r => r.ReturnId == returnId)
            ?.RefundNumber;
    }

    private void NavigateToRefundsTab()
    {
        returnSuccessMessage = null;
        activeMainTab = "refunds";
        activeRefundStatus = "pending";
        _dataLoadedForTab = null;
        NavigationManager.NavigateTo("/admin/orders?tab=refunds&status=pending");
        OrdersTabCoordinator.NotifyTabChanged("refunds");
    }

    private string GetReturnBadgeClass(string? status) =>
    status?.ToLower() switch
    {
        "pending" => "status-badge--pending",
        "approved" => "status-badge--shipped",
        "denied" => "status-badge--cancelled",
        _ => "status-badge--pending"
    };

    private async Task LoadRefunds()
    {
        isLoadingRefunds = true;
        isLoading = false;
        refundsError = string.Empty;
        StateHasChanged();

        try
        {
            var result = await RefundService.GetAllRefunds(
                string.IsNullOrEmpty(activeRefundStatus)
                    ? null
                    : activeRefundStatus,
                refundsCurrentPage,
                refundsPageSize);

            if (result.Success && result.Data != null)
            {
                refunds = result.Data.Data ?? new();
                refundsTotalCount = result.Data.TotalCount;
                refundsCurrentPage = result.Data.Page;

                if (refundsTotalCount > 0 && refunds.Count == 0)
                    refundsError = "Refunds exist but could not be displayed. Check API response mapping.";
            }
            else
            {
                refunds = new();
                refundsTotalCount = 0;
                refundsError = result.Message ?? "Failed to load refunds";
            }
        }
        catch (Exception ex)
        {
            refunds = new();
            refundsTotalCount = 0;
            refundsError = ex.Message;
        }

        isLoadingRefunds = false;
        StateHasChanged();
    }

    private async Task SetRefundStatus(string status)
    {
        activeRefundStatus = status;
        refundsCurrentPage = 1;
        await LoadRefunds();
    }

    private async Task HandleRefundsPageChanged((int Page, int PageSize) args)
    {
        refundsCurrentPage = args.Page;
        await LoadRefunds();
    }

    private void OpenRefundProcessModal(RefundModel refund)
    {
        processingRefund = refund;
        refundProcessError = string.Empty;
        isRefundProcessModalOpen = true;
    }

    private void CloseRefundProcessModal()
    {
        isRefundProcessModalOpen = false;
        processingRefund = null;
        refundProcessError = string.Empty;
    }

    private async Task ConfirmProcessRefund()
    {
        if (processingRefund is null)
            return;

        isRefundProcessing = true;
        refundProcessError = string.Empty;
        processingRefundId = processingRefund.Id;
        StateHasChanged();

        var refundId = processingRefund.Id;
        var result = await RefundService.ProcessRefund(refundId);

        if (result.Success && result.Data != null)
        {
            var item = refunds.FirstOrDefault(r => r.Id == refundId);
            if (item != null)
            {
                item.Status = result.Data.Status;
                item.ProcessedAt = result.Data.ProcessedAt;
                item.StripeRefundId = result.Data.StripeRefundId;
                item.FailureReason = result.Data.FailureReason;
            }
            else
                await LoadRefunds();

            CloseRefundProcessModal();
            OrdersTabCoordinator.RequestBadgeCountsRefresh();
        }
        else
            refundProcessError = result.Message ?? "Failed to process refund";

        processingRefundId = null;
        isRefundProcessing = false;
        StateHasChanged();
    }

    private string GetRefundBadgeClass(string? status) =>
        status?.ToLower() switch
        {
            "pending" => "status-badge--pending",
            "processing" => "status-badge--processing",
            "completed" => "status-badge--delivered",
            "failed" => "status-badge--cancelled",
            "cancelled" => "status-badge--cancelled",
            _ => "status-badge--pending"
        };

    private static bool CanProcessRefund(string? status)
    {
        var s = status?.ToLowerInvariant();
        return s is "pending" or "processing";
    }

}