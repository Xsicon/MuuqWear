using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Model.Customer;
using MuuqWear.Model.NotificationModel;
using MuuqWear.Model.Orders;

namespace MuuqWear.Web.Services;

/// <summary>
/// Builds admin header notifications from live operational data
/// (pending orders, affiliate applications, customer notes).
/// </summary>
public static class AdminHeaderOperationalNotificationsBuilder
{
    public const int MaxPendingOrders = 10;
    public const int MaxPendingAffiliates = 10;
    public const int MaxCustomerMessages = 10;

    public static IEnumerable<NotificationModel> FromPendingOrders(IEnumerable<OrderModel> orders) =>
        orders
            .OrderByDescending(o => o.CreatedAt ?? DateTime.MinValue)
            .Take(MaxPendingOrders)
            .Select(order =>
            {
                var label = string.IsNullOrWhiteSpace(order.OrderNumber)
                    ? "New order"
                    : $"Order #{order.OrderNumber}";

                return new NotificationModel
                {
                    Id = order.Id,
                    Type = NotificationType.Order,
                    Message = $"Pending sale: {label}",
                    Link = "/admin/orders?tab=orders&status=pending",
                    CreatedAt = order.CreatedAt ?? DateTime.UtcNow,
                    TimeAgo = $"{order.Total:C} · awaiting fulfillment"
                };
            });

    public static IEnumerable<NotificationModel> FromPendingAffiliateApplications(
        IEnumerable<AffiliateApplicationModel> applications) =>
        applications
            .Where(a => a.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.SubmittedAt)
            .Take(MaxPendingAffiliates)
            .Select(application => new NotificationModel
            {
                Id = application.Id,
                Type = NotificationType.Affiliate,
                Message = $"Affiliate application: {application.FullName}",
                Link = "/admin/affiliates?tab=pending",
                CreatedAt = application.SubmittedAt,
                TimeAgo = string.IsNullOrWhiteSpace(application.ContentNiche)
                    ? application.Email
                    : $"{application.ContentNiche} · {application.Email}"
            });

    public static IEnumerable<NotificationModel> FromCustomerNotes(
        IEnumerable<CustomerModel> customers,
        IReadOnlyDictionary<Guid, DateTime> readNoteAtByCustomerId) =>
        AdminHeaderMessagesBuilder
            .FromCustomers(customers, readNoteAtByCustomerId)
            .Take(MaxCustomerMessages)
            .Select(message => new NotificationModel
            {
                Id = message.Id,
                Type = NotificationType.CustomerMessage,
                CustomerId = message.CustomerId,
                Message = $"Customer note: {message.CustomerName}",
                Link = message.Link,
                CreatedAt = message.CreatedAt,
                TimeAgo = $"{message.AuthorLabel} · {message.Preview}"
            });
}
