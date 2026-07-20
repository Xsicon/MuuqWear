using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Application.Services.CustomerService;
using MuuqWear.Application.Services.NotificationService;
using MuuqWear.Application.Services.OrderService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.NotificationModel;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;
using MuuqWear.Web.Constants;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.Services;

public sealed class AdminHeaderNotificationFeed
{
    public List<NotificationModel> Notifications { get; init; } = new();
    public int UnreadCount { get; init; }
    public int LowStockCount { get; init; }
    public int PendingOrderCount { get; init; }
    public int PendingAffiliateCount { get; init; }
    public int CustomerMessageCount { get; init; }
    public HashSet<Guid> LowStockProductIds { get; init; } = new();
}

/// <summary>
/// Builds a stable admin notification feed: operational alerts from live data,
/// low-stock alerts from the product catalog, plus non-duplicate API notifications.
/// </summary>
public static class AdminHeaderNotificationFeedBuilder
{
    private static readonly IReadOnlyDictionary<Guid, ProductModel> EmptyCatalog =
        new Dictionary<Guid, ProductModel>();

    public static async Task<AdminHeaderNotificationFeed> BuildAsync(
        INotificationService notificationService,
        AdminLowStockCacheService lowStockCache,
        IOrderService orderService,
        IAffiliateService affiliateService,
        ICustomerService customerService,
        IReadOnlySet<Guid> readNotificationIds,
        IReadOnlyDictionary<Guid, DateTime> readNoteAtByCustomerId,
        IDictionary<Guid, DateTime> lowStockFirstSeenAt,
        string? userRole,
        bool forceRefreshLowStock = false,
        IReadOnlyDictionary<Guid, ProductModel>? productsById = null)
    {
        var canOrders = AdminPortalRoles.CanAccess(userRole, AdminPortalSection.Orders);
        var canAffiliates = AdminPortalRoles.CanAccess(userRole, AdminPortalSection.Affiliates);
        var canCustomerNotes = AdminHeaderNotificationAccess.CanSeeCustomerNotes(userRole);
        var canProducts = AdminPortalRoles.CanAccess(userRole, AdminPortalSection.Products);

        var apiTask = notificationService.GetRecent();

        Task<IReadOnlyList<ProductModel>>? lowStockTask = canProducts
            ? lowStockCache.GetLowStockProductsAsync(forceRefreshLowStock)
            : null;

        Task<Response<PaginatedResponse<MuuqWear.Model.Orders.OrderModel>>>? ordersTask = canOrders
            ? orderService.GetAllOrders("pending", null, 1, AdminHeaderOperationalNotificationsBuilder.MaxPendingOrders)
            : null;

        Task<Response<List<MuuqWear.Model.AffiliateApplication.AffiliateApplicationModel>>>? affiliatesTask = canAffiliates
            ? affiliateService.GetAllApplications("pending")
            : null;

        Task<AdminHeaderFeedLoadResult<MuuqWear.Model.Customer.CustomerModel>>? customersTask = canCustomerNotes
            ? AdminHeaderFeedLoader.LoadCustomersAsync(customerService)
            : null;

        var pendingTasks = new List<Task> { apiTask };
        if (lowStockTask != null)
            pendingTasks.Add(lowStockTask);
        if (ordersTask != null)
            pendingTasks.Add(ordersTask);
        if (affiliatesTask != null)
            pendingTasks.Add(affiliatesTask);
        if (customersTask != null)
            pendingTasks.Add(customersTask);

        await Task.WhenAll(pendingTasks);

        var lowStockProducts = lowStockTask != null
            ? (await lowStockTask).ToList()
            : new List<ProductModel>();

        if (canProducts)
            AdminHeaderNotificationsBuilder.UpdateFirstSeenTimes(lowStockProducts, lowStockFirstSeenAt);

        var pendingOrders = ordersTask?.Result.Success == true && ordersTask.Result.Data?.Data != null
            ? ordersTask.Result.Data.Data
            : new List<MuuqWear.Model.Orders.OrderModel>();

        var pendingAffiliates = affiliatesTask?.Result.Success == true && affiliatesTask.Result.Data != null
            ? affiliatesTask.Result.Data
            : new List<MuuqWear.Model.AffiliateApplication.AffiliateApplicationModel>();

        var customerFeed = customersTask != null
            ? await customersTask
            : new AdminHeaderFeedLoadResult<MuuqWear.Model.Customer.CustomerModel>();

        var catalog = productsById ?? EmptyCatalog;
        var merged = new List<NotificationModel>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var apiResult = await apiTask;
        if (apiResult.Success && apiResult.Data is { Count: > 0 })
        {
            foreach (var apiNotif in apiResult.Data)
            {
                if (AdminNotificationEnricher.IsLowStockNotification(apiNotif))
                    continue;

                if (IsOperationalType(apiNotif.Type))
                    continue;

                if (!AdminHeaderNotificationAccess.CanSeeNotificationType(userRole, apiNotif.Type))
                    continue;

                if (!AdminNotificationEnricher.Enrich(apiNotif, catalog))
                    continue;

                TryAddNotification(merged, seenKeys, apiNotif, readNotificationIds, readNoteAtByCustomerId);
            }
        }

        if (canOrders)
        {
            foreach (var orderNotif in AdminHeaderOperationalNotificationsBuilder.FromPendingOrders(pendingOrders))
                TryAddNotification(merged, seenKeys, orderNotif, readNotificationIds, readNoteAtByCustomerId);
        }

        if (canAffiliates)
        {
            foreach (var affiliateNotif in AdminHeaderOperationalNotificationsBuilder.FromPendingAffiliateApplications(pendingAffiliates))
                TryAddNotification(merged, seenKeys, affiliateNotif, readNotificationIds, readNoteAtByCustomerId);
        }

        if (canCustomerNotes)
        {
            foreach (var messageNotif in AdminHeaderOperationalNotificationsBuilder.FromCustomerNotes(
                         customerFeed.Items, readNoteAtByCustomerId))
                TryAddNotification(merged, seenKeys, messageNotif, readNotificationIds, readNoteAtByCustomerId);
        }

        if (canProducts)
        {
            var lowStockNotifications = AdminHeaderNotificationsBuilder.FromLowStockProducts(
                lowStockProducts,
                readNotificationIds,
                lowStockFirstSeenAt);

            foreach (var clientNotif in lowStockNotifications)
                TryAddNotification(merged, seenKeys, clientNotif, readNotificationIds, readNoteAtByCustomerId);
        }

        var notifications = merged
            .OrderBy(n => GetTypeSortOrder(n.Type))
            .ThenByDescending(n => n.CreatedAt)
            .ToList();

        var customerMessages = notifications
            .Where(n => n.Type == NotificationType.CustomerMessage)
            .ToList();

        return new AdminHeaderNotificationFeed
        {
            Notifications = notifications,
            UnreadCount = notifications.Count,
            LowStockCount = canProducts ? lowStockProducts.Count : 0,
            PendingOrderCount = canOrders
                ? ordersTask?.Result.Data?.TotalCount ?? pendingOrders.Count
                : 0,
            PendingAffiliateCount = canAffiliates ? pendingAffiliates.Count : 0,
            CustomerMessageCount = canCustomerNotes ? customerMessages.Count : 0,
            LowStockProductIds = canProducts
                ? lowStockProducts.Select(p => p.Id).ToHashSet()
                : new HashSet<Guid>()
        };
    }

    public static bool IsNotificationRead(
        NotificationModel notif,
        IReadOnlySet<Guid> readNotificationIds,
        IReadOnlyDictionary<Guid, DateTime>? readNoteAtByCustomerId = null)
    {
        if (notif.Type == NotificationType.CustomerMessage
            && notif.CustomerId is Guid customerId
            && readNoteAtByCustomerId != null)
        {
            if (!readNoteAtByCustomerId.TryGetValue(customerId, out var readAt))
                return false;

            return AdminDateTimeHelper.ToUtc(notif.CreatedAt)
                   <= AdminDateTimeHelper.ToUtc(readAt);
        }

        return (notif.Id != Guid.Empty && readNotificationIds.Contains(notif.Id))
               || (notif.SizeStockId is Guid sizeStockId && readNotificationIds.Contains(sizeStockId))
               || (notif.ProductId is Guid productId && readNotificationIds.Contains(productId));
    }

    private static bool IsOperationalType(string type) =>
        type.Equals(NotificationType.Order, StringComparison.OrdinalIgnoreCase)
        || type.Equals(NotificationType.Affiliate, StringComparison.OrdinalIgnoreCase)
        || type.Equals(NotificationType.CustomerMessage, StringComparison.OrdinalIgnoreCase);

    private static int GetTypeSortOrder(string type) => type switch
    {
        NotificationType.Order => 0,
        NotificationType.Affiliate => 1,
        NotificationType.CustomerMessage => 2,
        NotificationType.LowStock => 3,
        NotificationType.Stock => 3,
        _ => 4
    };

    private static void TryAddNotification(
        List<NotificationModel> merged,
        HashSet<string> seenKeys,
        NotificationModel notif,
        IReadOnlySet<Guid> readNotificationIds,
        IReadOnlyDictionary<Guid, DateTime> readNoteAtByCustomerId)
    {
        if (!seenKeys.Add(GetDedupeKey(notif)))
            return;

        if (IsNotificationRead(notif, readNotificationIds, readNoteAtByCustomerId))
            return;

        notif.IsRead = false;
        merged.Add(notif);
    }

    private static string GetDedupeKey(NotificationModel notif)
    {
        if (notif.Type == NotificationType.Order && notif.Id != Guid.Empty)
            return $"order:{notif.Id}";

        if (notif.Type == NotificationType.Affiliate && notif.Id != Guid.Empty)
            return $"affiliate:{notif.Id}";

        if (notif.Type == NotificationType.CustomerMessage && notif.CustomerId is Guid customerId)
            return $"customer-note:{customerId}";

        if (AdminNotificationEnricher.IsLowStockNotification(notif))
        {
            if (notif.ProductId is Guid productId && notif.SizeStockId is Guid sizeStockId)
                return $"lowstock:{productId}:{sizeStockId}";

            if (notif.ProductId is Guid productIdOnly)
                return $"lowstock:{productIdOnly}";

            return $"lowstock:{notif.Id}";
        }

        if (notif.Id != Guid.Empty)
            return $"api:{notif.Id}";

        return $"api:{notif.Type}:{notif.Message}:{notif.CreatedAt:O}";
    }
}
