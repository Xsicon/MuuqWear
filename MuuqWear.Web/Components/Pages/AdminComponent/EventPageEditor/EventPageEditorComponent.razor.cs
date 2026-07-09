using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MuuqWear.Application.Services.ContentService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Muuqsimo;

namespace MuuqWear.Web.Components.Pages.AdminComponent.EventPageEditor;

public partial class EventPageEditorComponent
{
    [Parameter, EditorRequired]
    public MuuqsimoPageContentModel Model { get; set; } = new();

    [Parameter]
    public EventCallback<string> OnError { get; set; }

    [Inject] private IContentService ContentService { get; set; } = default!;

    private readonly HashSet<string> expandedSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "hero"
    };

    protected override void OnParametersSet()
    {
        EventPageContentSerializer.EnsureInitialized(Model);
    }

    private bool IsExpanded(string key) => expandedSections.Contains(key);

    private void ToggleSection(string key)
    {
        if (!expandedSections.Add(key))
            expandedSections.Remove(key);
    }

    private static string JoinLines(IEnumerable<string> lines) =>
        string.Join('\n', lines);

    private static List<string> SplitLines(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? new()
            : text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private void UpdatePerks(MuuqsimoTicketTierConfigModel tier, ChangeEventArgs e)
    {
        tier.Perks = SplitLines(e.Value?.ToString());
    }

    private async Task ReportError(string message)
    {
        if (OnError.HasDelegate)
            await OnError.InvokeAsync(message);
    }

    private async Task UploadImageAsync(Func<string, Task> setUrl, InputFileChangeEventArgs e)
    {
        var file = e.File;

        if (file.Size > 5 * 1024 * 1024)
        {
            await ReportError("Image must be under 5MB");
            return;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            await ReportError("File must be an image");
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var result = await ContentService.UploadImage(
                $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}",
                ms.ToArray(),
                file.ContentType);

            if (result.Success && result.Data != null)
                await setUrl(result.Data);
            else
                await ReportError(result.Message ?? "Upload failed");
        }
        catch (Exception ex)
        {
            await ReportError("Upload failed: " + ex.Message);
        }
    }

    private Task SetHeroSlideImage(int index, string url)
    {
        Model.HeroSlides[index].ImageUrl = url;
        return Task.CompletedTask;
    }

    private Task SetExperienceCardImage(int index, string url)
    {
        Model.Experience!.Cards[index].ImageUrl = url;
        return Task.CompletedTask;
    }

    private Task SetBlueVeilImage(string url)
    {
        Model.BlueVeilWalk!.ImageUrl = url;
        return Task.CompletedTask;
    }

    private Task SetClosingCtaImage(string url)
    {
        Model.ClosingCta!.ImageUrl = url;
        return Task.CompletedTask;
    }

    private static string ToDateValue(string? iso) =>
        DateTimeOffset.TryParse(iso, out var dto) ? dto.ToString("yyyy-MM-dd") : string.Empty;

    private static string ToTimeValue(string? iso) =>
        DateTimeOffset.TryParse(iso, out var dto) ? dto.ToString("HH:mm") : string.Empty;

    private static string MergeDateTime(string? date, string? time, string? existing, bool utc = false)
    {
        if (string.IsNullOrWhiteSpace(date))
            return existing ?? string.Empty;

        var timePart = string.IsNullOrWhiteSpace(time) ? "00:00" : time;
        if (!DateOnly.TryParse(date, out var day) || !TimeOnly.TryParse(timePart, out var clock))
            return existing ?? string.Empty;

        var offset = TimeSpan.Zero;
        if (!utc && DateTimeOffset.TryParse(existing, out var prior))
            offset = prior.Offset;

        var merged = new DateTimeOffset(day.ToDateTime(clock), offset);
        return utc
            ? merged.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
            : merged.ToString("o");
    }

    private void UpdateStartDate(ChangeEventArgs e, bool isDate) =>
        Model.StartDate = MergeDateTime(
            isDate ? e.Value?.ToString() : ToDateValue(Model.StartDate),
            isDate ? ToTimeValue(Model.StartDate) : e.Value?.ToString(),
            Model.StartDate);

    private void UpdateEndDate(ChangeEventArgs e, bool isDate) =>
        Model.EndDate = MergeDateTime(
            isDate ? e.Value?.ToString() : ToDateValue(Model.EndDate),
            isDate ? ToTimeValue(Model.EndDate) : e.Value?.ToString(),
            Model.EndDate);

    private void UpdateCountdown(ChangeEventArgs e, bool isDate) =>
        Model.CountdownUtc = MergeDateTime(
            isDate ? e.Value?.ToString() : ToDateValue(Model.CountdownUtc),
            isDate ? ToTimeValue(Model.CountdownUtc) : e.Value?.ToString(),
            Model.CountdownUtc,
            utc: true);
}
