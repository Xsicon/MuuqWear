using MuuqWear.Model.ContentItem;

namespace MuuqWear.Application.Content;

public static class ContentItemStatusHelper
{
    public static string NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status)
            ? "draft"
            : status.Trim().ToLowerInvariant();

    public static bool IsPublishedStatus(string? status) =>
        NormalizeStatus(status) == "published";

    public static bool IsScheduledStatus(string? status) =>
        NormalizeStatus(status) == "scheduled";

    public static bool IsArchivedStatus(string? status) =>
        NormalizeStatus(status) == "archived";

    public static string GetDisplayLabel(ContentItemModel item) =>
        NormalizeStatus(item.Status) switch
        {
            "published" => "Published",
            "scheduled" => "Scheduled",
            "archived" => "Archived",
            _ => "Draft"
        };

    public static string GetStatusBadgeClass(ContentItemModel item) =>
        NormalizeStatus(item.Status) switch
        {
            "published" => "content-status-badge--published",
            "scheduled" => "content-status-badge--scheduled",
            "archived" => "content-status-badge--archived",
            _ => "content-status-badge--draft"
        };

    public static bool IsDraftStatus(string? status) =>
        NormalizeStatus(status) == "draft";

    public static bool CanPreviewOnSite(ContentItemModel item) =>
        IsPublishedStatus(item.Status);

    public static string GetDisplayLabelFromStatus(string? status) =>
        NormalizeStatus(status) switch
        {
            "published" => "Published",
            "scheduled" => "Scheduled",
            "archived" => "Archived",
            _ => "Draft"
        };

    public static int CountByStatus(IEnumerable<ContentItemModel> items, string status) =>
        items.Count(x => NormalizeStatus(x.Status) == status);
}
