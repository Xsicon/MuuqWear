using MuuqWear.Model.Customer;
using MuuqWear.Model.Messages;
using MuuqWear.Web.Helpers;

namespace MuuqWear.Web.Services;

/// <summary>
/// Builds admin header messages from customer internal notes.
/// </summary>
public static class AdminHeaderMessagesBuilder
{
    public const int MaxItems = 15;
    public const int NoteFetchCustomerLimit = 20;
    public const int MaxNotesPerCustomer = 10;

    public static List<(CustomerModel Customer, CustomerNoteModel Note)> CapNotesPerCustomer(
        IEnumerable<(CustomerModel Customer, CustomerNoteModel Note)> entries) =>
        entries
            .GroupBy(entry => entry.Customer.Id)
            .SelectMany(group => group
                .OrderByDescending(entry => AdminDateTimeHelper.ToUtc(entry.Note.CreatedAt))
                .Take(MaxNotesPerCustomer))
            .ToList();

    public static int CountUnreadNotes(
        IEnumerable<(CustomerModel Customer, CustomerNoteModel Note)> entries,
        IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId) =>
        entries.Count(entry => !IsNoteRead(entry.Note, readAtByCustomerId));

    public static void MarkCustomerNoteRead(
        IDictionary<Guid, DateTime> readAtByCustomerId,
        Guid customerId,
        DateTime noteCreatedAt)
    {
        var readAt = AdminDateTimeHelper.ToUtc(noteCreatedAt);
        if (readAtByCustomerId.TryGetValue(customerId, out var existing)
            && AdminDateTimeHelper.ToUtc(existing) > readAt)
            readAt = AdminDateTimeHelper.ToUtc(existing);

        readAtByCustomerId[customerId] = readAt;
    }

    public static void MarkAllNotesRead(
        IEnumerable<(CustomerModel Customer, CustomerNoteModel Note)> entries,
        IDictionary<Guid, DateTime> readAtByCustomerId)
    {
        foreach (var group in entries.GroupBy(entry => entry.Customer.Id))
        {
            var latestReadAt = group
                .Select(entry => AdminDateTimeHelper.ToUtc(entry.Note.CreatedAt))
                .Max();

            if (readAtByCustomerId.TryGetValue(group.Key, out var existing)
                && AdminDateTimeHelper.ToUtc(existing) > latestReadAt)
                latestReadAt = AdminDateTimeHelper.ToUtc(existing);

            readAtByCustomerId[group.Key] = latestReadAt;
        }
    }

    public static List<AdminMessageModel> FromCustomers(
        IEnumerable<CustomerModel> customers,
        IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId) =>
        FromNotes(
            customers
                .Where(HasRecentNote)
                .Select(customer => (customer, ToLatestNote(customer))),
            readAtByCustomerId);

    public static List<AdminMessageModel> FromNotes(
        IEnumerable<(CustomerModel Customer, CustomerNoteModel Note)> entries,
        IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId)
    {
        return entries
            .OrderByDescending(entry => AdminDateTimeHelper.ToUtc(entry.Note.CreatedAt))
            .Take(MaxItems)
            .Select(entry => ToMessage(entry.Customer, entry.Note, readAtByCustomerId))
            .ToList();
    }

    public static bool IsNoteRead(
        CustomerNoteModel note,
        IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId)
    {
        if (!readAtByCustomerId.TryGetValue(note.CustomerId, out var readAt))
            return false;

        return AdminDateTimeHelper.ToUtc(note.CreatedAt)
               <= AdminDateTimeHelper.ToUtc(readAt);
    }

    private static bool HasRecentNote(CustomerModel customer) =>
        customer.LatestNoteAt.HasValue
        && !string.IsNullOrWhiteSpace(customer.LatestNotePreview);

    private static CustomerNoteModel ToLatestNote(CustomerModel customer) =>
        new()
        {
            Id = customer.Id,
            CustomerId = customer.Id,
            Body = customer.LatestNotePreview!.Trim(),
            AuthorName = customer.LatestNoteAuthorName ?? string.Empty,
            AuthorRole = customer.LatestNoteAuthorRole,
            CreatedAt = customer.LatestNoteAt!.Value
        };

    private static AdminMessageModel ToMessage(
        CustomerModel customer,
        CustomerNoteModel note,
        IReadOnlyDictionary<Guid, DateTime> readAtByCustomerId)
    {
        var createdAt = AdminDateTimeHelper.ToUtc(note.CreatedAt);
        var name = string.IsNullOrWhiteSpace(customer.FullName)
            ? "Customer"
            : customer.FullName.Trim();

        return new AdminMessageModel
        {
            Id = note.Id,
            CustomerId = customer.Id,
            CustomerName = name,
            Preview = TruncatePreview(note.Body),
            AuthorLabel = CustomerNoteFormatter.FormatAuthorLabel(note),
            CreatedAt = createdAt,
            IsRead = IsNoteRead(note, readAtByCustomerId),
            Link = $"/admin/customers?view=notes&customerId={customer.Id}"
        };
    }

    private static string TruncatePreview(string? value, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Internal note";

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength].TrimEnd() + "…";
    }
}
