using MuuqWear.Model.Customer;

namespace MuuqWear.Web.Helpers;

public static class CustomerNoteFormatter
{
    public static string FormatAuthorLabel(CustomerNoteModel note) =>
        FormatAuthorLabel(note.AuthorName, note.AuthorRole);

    public static string FormatAuthorLabel(string? authorName, string? authorRole)
    {
        var name = string.IsNullOrWhiteSpace(authorName)
            ? "Team member"
            : authorName.Trim();

        if (string.IsNullOrWhiteSpace(authorRole))
            return name;

        return $"{name} - {authorRole.Trim()}";
    }

    public static string FormatAuthorBracket(CustomerNoteModel note) =>
        $"[{FormatAuthorLabel(note)}]";
}
