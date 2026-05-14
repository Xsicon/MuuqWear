using Microsoft.AspNetCore.Components;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Orders;

namespace MuuqWear.Web.Components.Pages.Admin
{
    public partial class AdminOrders
    {
        [SupplyParameterFromQuery(Name = "search")]
        public string? SearchQuery { get; set; }
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
        private OrderModel? viewOrder = null;
        private bool isLoadingDetail = false;

        // ─── PROCESS MODAL STATE ──────────────────────────────────
        private bool isProcessModalOpen = false;
        private OrderModel? processingOrder = null;
        private OrderModel? processOrderDetail = null;
        private bool isLoadingProcessDetail = false;
        private bool isProcessing = false;
        private string processError = string.Empty;

        // ─── STATUS TABS ──────────────────────────────────────────
        private record StatusTab(string Label, string Value);

        private List<StatusTab> StatusTabs => new()
    {
        new("All",        ""),
        new("Pending",    "pending"),
        new("Processing", "processing"),
        new("Shipped",    "shipped"),
        new("Delivered",  "delivered")
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
                //  update all selected orders locally
                foreach (var order in orders
                    .Where(o => selectedOrderIds.Contains(o.Id)))
                {
                    order.Status = bulkTargetStatus;
                }

                selectedOrderIds.Clear();
                CloseBulkModal();
            }
            else
            {
                processError = result.Message ?? "Bulk update failed";
            }

            isBulkUpdating = false;
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(SearchQuery))
                searchTerm = SearchQuery;
            await LoadOrders();
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
        private async Task SetMainTab(string tab)
        {
            activeMainTab = tab;

            //  load returns only when tab first opened
            if (tab == "returns" && !returns.Any())
                await LoadReturns();

            StateHasChanged();
        }

        private async Task SetStatus(string status)
        {
            activeStatus = status;
            currentPage = 1;
            await LoadOrders();
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
            isViewPanelOpen = true;
            isLoadingDetail = true;
            viewOrder = orders.FirstOrDefault(o => o.Id == orderId);
            StateHasChanged();

            var result = await OrderService.GetOrderDetail(orderId);
            if (result.Success && result.Data != null)
                viewOrder = result.Data;

            isLoadingDetail = false;
            StateHasChanged();
        }

        private void CloseViewPanel()
        {
            isViewPanelOpen = false;
            viewOrder = null;
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
            var result = await OrderReturnService.UpdateReturnStatus(
                returnId, "approved");

            if (result.Success)
            {
                var item = returns.FirstOrDefault(r => r.Id == returnId);
                if (item != null)
                    item.Status = "approved";
                StateHasChanged();
            }
        }

        private async Task DenyReturn(Guid returnId)
        {
            var result = await OrderReturnService.UpdateReturnStatus(
                returnId, "denied");

            if (result.Success)
            {
                var item = returns.FirstOrDefault(r => r.Id == returnId);
                if (item != null)
                    item.Status = "denied";
                StateHasChanged();
            }
        }

        private string GetReturnBadgeClass(string? status) =>
        status?.ToLower() switch
        {
            "pending" => "status-badge--pending",
            "approved" => "status-badge--shipped",
            "denied" => "status-badge--cancelled",
            _ => "status-badge--pending"
        };

    }
}