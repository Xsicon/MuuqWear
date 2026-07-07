using MuuqWear.Model.Customer;
using MuuqWear.Model.Messages;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.Services;

/// <summary>
/// Builds admin header messages from customer internal note summaries.
/// </summary>
public static class AdminHeaderMessagesBuilder
{
    public const int MaxItems = 15;

    public static List<AdminMessageModel> FromCustomers(
        IEnumerable<CustomerModel> customers,
        IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId)
    {
        return customers
            .Where(HasRecentNote)
            .OrderByDescending(c => c.LatestNoteAt)
            .Take(MaxItems)
            .Select(c => ToMessage(c, readAtByCustomerId))
            .ToList();
    }

    private static bool HasRecentNote(CustomerModel customer) =>
        customer.LatestNoteAt.HasValue
        && !string.IsNullOrWhiteSpace(customer.LatestNotePreview);

    private static AdminMessageModel ToMessage(
        CustomerModel customer,
        IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId)
    {
        var createdAt = customer.LatestNoteAt!.Value;
        var isRead = readAtByCustomerId.TryGetValue(customer.Id, out var readAt)
                     && createdAt <= readAt;

        var name = string.IsNullOrWhiteSpace(customer.FullName)
            ? "Customer"
            : customer.FullName.Trim();

        return new AdminMessageModel
        {
            Id = customer.Id,
            CustomerId = customer.Id,
            CustomerName = name,
            Preview = customer.LatestNotePreview!.Trim(),
            AuthorLabel = CustomerNoteFormatter.FormatAuthorLabel(
                customer.LatestNoteAuthorName,
                customer.LatestNoteAuthorRole),
            CreatedAt = createdAt,
            IsRead = isRead,
            Link = $"/admin/customers?view=notes&customerId={customer.Id}"
        };
    }
}
