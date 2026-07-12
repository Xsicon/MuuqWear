using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminContentComponent
{
    private readonly List<AdminMediaItem> mediaItems = new();
    private readonly HashSet<Guid> selectedMediaIds = new();
    private bool mediaLoading;

    private sealed class AdminMediaItem
    {
        public Guid Id { get; init; }
        public string Url { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int UsedCount { get; set; }
        public bool IsSessionUpload { get; set; }
    }

    private async Task LoadMediaLibraryAsync()
    {
        mediaLoading = true;
        pageError = string.Empty;
        selectedMediaIds.Clear();

        try
        {
            var sessionUploads = mediaItems
                .Where(x => x.IsSessionUpload)
                .ToList();

            var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var category in new[]
                     {
                         ContentCategory.JournalArticles,
                         ContentCategory.Events,
                         ContentCategory.DesignHistory
                     })
            {
                var result = await ContentService.GetAll(category);
                if (!result.Success || result.Data == null)
                    continue;

                foreach (var item in result.Data)
                    RegisterMediaUsage(usage, item.ImageUrl, item.SecondImageUrl, item.Content);
            }

            mediaItems.Clear();
            foreach (var (url, count) in usage.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
            {
                if (!IsAllowedMediaUrl(url))
                    continue;

                mediaItems.Add(new AdminMediaItem
                {
                    Id = CreateStableMediaId(url),
                    Url = url,
                    Name = GetMediaFileName(url),
                    UsedCount = count,
                    IsSessionUpload = false
                });
            }

            foreach (var upload in sessionUploads)
            {
                if (mediaItems.Any(x => x.Url.Equals(upload.Url, StringComparison.OrdinalIgnoreCase)))
                    continue;

                mediaItems.Insert(0, upload);
            }

            tabCounts["media"] = mediaItems.Count;
        }
        finally
        {
            mediaLoading = false;
        }
    }

    private async Task<int> CountMediaLibraryAsync()
    {
        var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in new[]
                 {
                     ContentCategory.JournalArticles,
                     ContentCategory.Events,
                     ContentCategory.DesignHistory
                 })
        {
            var result = await ContentService.GetAll(category);
            if (!result.Success || result.Data == null)
                continue;

            foreach (var item in result.Data)
                RegisterMediaUsage(usage, item.ImageUrl, item.SecondImageUrl, item.Content);
        }

        return usage.Keys.Count(IsAllowedMediaUrl);
    }

    private static void RegisterMediaUsage(
        Dictionary<string, int> usage,
        params string?[] urlsAndJson)
    {
        foreach (var value in urlsAndJson)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/", StringComparison.Ordinal))
            {
                usage[value] = usage.GetValueOrDefault(value) + 1;
                continue;
            }

            foreach (var url in ExtractUrlsFromText(value))
                usage[url] = usage.GetValueOrDefault(url) + 1;
        }
    }

    private static IEnumerable<string> ExtractUrlsFromText(string text)
    {
        const string prefix = "http";
        var index = 0;
        while ((index = text.IndexOf(prefix, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = index;
            while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '"' && text[end] != '\'')
                end++;

            yield return text[index..end];
            index = end;
        }
    }

    private static bool IsAllowedMediaUrl(string url)
    {
        if (url.StartsWith("/", StringComparison.Ordinal))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private static Guid CreateStableMediaId(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static string GetMediaFileName(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return Path.GetFileName(uri.LocalPath);

        return Path.GetFileName(url);
    }

    private void ToggleMediaSelection(Guid id)
    {
        var item = mediaItems.FirstOrDefault(x => x.Id == id);
        if (item == null)
            return;

        if (!item.IsSessionUpload)
        {
            ShowToast("Images referenced by published content cannot be removed here.");
            return;
        }

        if (!selectedMediaIds.Add(id))
            selectedMediaIds.Remove(id);
    }

    private void ClearMediaSelection() => selectedMediaIds.Clear();

    private void DeleteSelectedMedia()
    {
        if (selectedMediaIds.Count == 0)
            return;

        var removable = mediaItems
            .Where(x => selectedMediaIds.Contains(x.Id) && x.IsSessionUpload && x.UsedCount == 0)
            .ToList();

        if (removable.Count == 0)
        {
            ShowToast("Only unused uploads from this session can be removed.");
            return;
        }

        foreach (var item in removable)
            mediaItems.Remove(item);

        selectedMediaIds.Clear();
        tabCounts["media"] = mediaItems.Count;
        ShowToast($"{removable.Count} unused upload{(removable.Count == 1 ? "" : "s")} removed");
    }

    private async Task HandleMediaUploadAsync(InputFileChangeEventArgs e)
    {
        mediaError = string.Empty;
        var files = e.GetMultipleFiles(12);
        var uploaded = 0;
        var skipped = 0;

        foreach (var file in files)
        {
            if (!ValidateMediaFile(file, out var validationMessage))
            {
                skipped++;
                mediaError = validationMessage;
                continue;
            }

            try
            {
                await using var stream = file.OpenReadStream(5 * 1024 * 1024);
                await using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                var safeExtension = Path.GetExtension(file.Name);
                if (string.IsNullOrWhiteSpace(safeExtension) || safeExtension.Length > 8)
                    safeExtension = ".jpg";

                var result = await ContentService.UploadImage(
                    $"{Guid.NewGuid()}{safeExtension}",
                    ms.ToArray(),
                    file.ContentType);

                if (result.Success && result.Data != null)
                {
                    mediaItems.Insert(0, new AdminMediaItem
                    {
                        Id = Guid.NewGuid(),
                        Url = result.Data,
                        Name = file.Name,
                        UsedCount = 0,
                        IsSessionUpload = true
                    });
                    uploaded++;
                }
                else
                {
                    skipped++;
                    mediaError = result.Message ?? "Upload failed";
                }
            }
            catch (Exception ex)
            {
                skipped++;
                mediaError = $"Upload failed: {ex.Message}";
            }
        }

        tabCounts["media"] = mediaItems.Count;

        if (uploaded > 0)
        {
            ShowToast($"{uploaded} file{(uploaded == 1 ? "" : "s")} uploaded");
            await RefreshTabCountsAsync();
        }

        if (skipped > 0 && uploaded == 0)
            ShowToast(mediaError ?? "Some files could not be uploaded.");
        else if (skipped > 0)
            ShowToast($"{skipped} file{(skipped == 1 ? "" : "s")} skipped. {mediaError}");
    }

    private static bool ValidateMediaFile(IBrowserFile file, out string message)
    {
        if (file.Size > 5 * 1024 * 1024)
        {
            message = "Image must be under 5 MB";
            return false;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            message = "File must be an image";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void OpenMediaUpload()
    {
        if (activeView != "media")
            SwitchTab("media");
    }
}
