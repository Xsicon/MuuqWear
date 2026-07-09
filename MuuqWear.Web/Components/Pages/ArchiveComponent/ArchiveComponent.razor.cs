using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.ArchiveService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Model.Archive;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Components.Pages.ArchiveComponent;

public partial class ArchiveComponent : IDisposable
{
    [Inject] private IArchiveService ArchiveService { get; set; } = default!;
    [Inject] private IProductService ProductService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "id")]
    public Guid? IdQuery { get; set; }

    private ContentItemModel? viewingItem;
    private bool isLoading = true;
    private Dictionary<string, List<ContentItemModel>> itemsByCollection = new();
    private Dictionary<string, CarouselHandle> handles = new();
    private Guid? _lastRecordedViewId;
    private Guid? _pendingOpenId;
    private bool shareLinkCopied;
    private bool shareCopyFailed;
    private CancellationTokenSource? _shareCopyResetCts;
    private string? shopMessage;
    private bool isShopping;
    private CancellationTokenSource? _shopMessageResetCts;

    private class CarouselHandle
    {
        public ElementReference TrackRef;
    }

    private List<ContentItemModel> ItemsFor(string collectionKey) =>
        itemsByCollection.TryGetValue(collectionKey, out var items)
            ? items
            : new List<ContentItemModel>();

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        _pendingOpenId = ResolveDeepLinkId();
        await LoadItems();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        _shareCopyResetCts?.Cancel();
        _shareCopyResetCts?.Dispose();
        _shopMessageResetCts?.Cancel();
        _shopMessageResetCts?.Dispose();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!e.Location.Contains("/design-history", StringComparison.OrdinalIgnoreCase))
            return;

        _ = InvokeAsync(HandleLocationChangedAsync);
    }

    private async Task HandleLocationChangedAsync()
    {
        var targetId = ResolveDeepLinkId();
        if (targetId == null)
        {
            if (viewingItem != null)
                viewingItem = null;
            StateHasChanged();
            return;
        }

        if (viewingItem?.Id == targetId)
            return;

        var item = FindItemById(targetId.Value);
        if (item != null)
            await OpenStoryPanelAsync(item, recordView: true);
    }

    private Guid? ResolveDeepLinkId()
    {
        if (IdQuery.HasValue && IdQuery.Value != Guid.Empty)
            return IdQuery.Value;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("id", out var queryValue) &&
            Guid.TryParse(queryValue.ToString(), out var fromQuery))
        {
            return fromQuery;
        }

        if (string.IsNullOrEmpty(uri.Fragment) || uri.Fragment.Length <= 1)
            return null;

        var fragment = uri.Fragment.TrimStart('#');
        return Guid.TryParse(fragment, out var fromHash) ? fromHash : null;
    }

    private ContentItemModel? FindItemById(Guid id) =>
        itemsByCollection.Values
            .SelectMany(x => x)
            .FirstOrDefault(x => x.Id == id);

    private async Task LoadItems()
    {
        isLoading = true;
        StateHasChanged();

        var result = await ArchiveService.GetPublishedDesignHistoryAsync();
        if (result.Success && result.Data != null)
        {
            itemsByCollection = result.Data
                .Where(x => !string.IsNullOrWhiteSpace(x.Collection))
                .GroupBy(x => x.Collection!)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        else
        {
            itemsByCollection = new();
        }

        handles = ArchiveCollections.All
            .Where(def => itemsByCollection.ContainsKey(def.CollectionKey))
            .ToDictionary(def => def.CollectionKey, _ => new CarouselHandle());

        isLoading = false;

        var openId = _pendingOpenId ?? ResolveDeepLinkId();
        _pendingOpenId = null;
        if (openId.HasValue)
        {
            var item = FindItemById(openId.Value);
            if (item != null)
                await OpenStoryPanelAsync(item, recordView: true, updateUrl: false);
        }

        StateHasChanged();
    }

    private async Task OpenStoryPanelAsync(
        ContentItemModel item,
        bool recordView = true,
        bool updateUrl = true)
    {
        viewingItem = item;
        ClearShopMessage();
        shareLinkCopied = false;
        shareCopyFailed = false;
        _shareCopyResetCts?.Cancel();

        if (updateUrl)
        {
            var url = $"/design-history#{item.Id}";
            if (NavigationManager.Uri != NavigationManager.ToAbsoluteUri(url).ToString())
                NavigationManager.NavigateTo(url, replace: true);
        }

        if (recordView && _lastRecordedViewId != item.Id)
        {
            _lastRecordedViewId = item.Id;
            var result = await ArchiveService.RecordDesignHistoryViewAsync(item.Id);
            if (result.Success)
                item.Views = result.Data;
        }

        StateHasChanged();
    }

    private void CloseStoryPanel()
    {
        viewingItem = null;
        _lastRecordedViewId = null;
        shareLinkCopied = false;
        shareCopyFailed = false;
        _shareCopyResetCts?.Cancel();
        ClearShopMessage();
        NavigationManager.NavigateTo("/design-history", replace: true);
    }

    private async Task ShopThisDesignAsync()
    {
        if (viewingItem == null || isShopping)
            return;

        ClearShopMessage();
        shareLinkCopied = false;
        shareCopyFailed = false;
        _shareCopyResetCts?.Cancel();

        if (IsDesignShopUnavailable(viewingItem))
        {
            await ShowShopMessageAsync("This design isn't available in the shop right now.");
            return;
        }

        isShopping = true;
        StateHasChanged();

        try
        {
            var (product, status) = await ResolveLinkedProductAsync(viewingItem);
            if (product != null)
            {
                NavigationManager.NavigateTo($"/productdetail/{product.Id}");
                return;
            }

            var message = status switch
            {
                ProductLinkStatus.NotConfigured =>
                    "This design isn't linked to a product in the shop yet.",
                ProductLinkStatus.Inactive =>
                    "The linked product is no longer available in the shop.",
                _ => "The linked product could not be found in the shop."
            };

            await ShowShopMessageAsync(message);
        }
        finally
        {
            isShopping = false;
            StateHasChanged();
        }
    }

    private async Task ShowShopMessageAsync(string message)
    {
        _shopMessageResetCts?.Cancel();
        _shopMessageResetCts?.Dispose();
        _shopMessageResetCts = new CancellationTokenSource();
        var token = _shopMessageResetCts.Token;

        shopMessage = message;
        StateHasChanged();

        try
        {
            await Task.Delay(4000, token);
            shopMessage = null;
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
            // Panel closed or another action reset the cue.
        }
    }

    private void ClearShopMessage()
    {
        shopMessage = null;
        _shopMessageResetCts?.Cancel();
        _shopMessageResetCts?.Dispose();
        _shopMessageResetCts = null;
    }

    private static bool IsDesignShopUnavailable(ContentItemModel item)
    {
        if (string.IsNullOrWhiteSpace(item.TechnicalAvailability))
            return false;

        var availability = item.TechnicalAvailability.ToLowerInvariant();
        return availability.Contains("sold out", StringComparison.Ordinal)
            || availability.Contains("archival piece", StringComparison.Ordinal)
            || availability.Contains("museum archival", StringComparison.Ordinal)
            || availability.Contains("no longer available", StringComparison.Ordinal);
    }

    private enum ProductLinkStatus
    {
        Found,
        NotConfigured,
        NotFound,
        Inactive
    }

    private async Task<(ProductModel? Product, ProductLinkStatus Status)> ResolveLinkedProductAsync(
        ContentItemModel item)
    {
        if (item.ProductId is not Guid productId || productId == Guid.Empty)
            return (null, ProductLinkStatus.NotConfigured);

        var linked = await ProductService.GetById(productId);
        if (!linked.Success || linked.Data == null)
            return (null, ProductLinkStatus.NotFound);

        if (!linked.Data.IsActive)
            return (null, ProductLinkStatus.Inactive);

        return (linked.Data, ProductLinkStatus.Found);
    }

    private async Task ShareStoryAsync()
    {
        if (viewingItem == null)
            return;

        var url = NavigationManager.ToAbsoluteUri($"/design-history#{viewingItem.Id}").ToString();
        var copied = await JS.InvokeAsync<bool>("mwCopyToClipboard", url);
        if (!copied)
        {
            shareLinkCopied = false;
            shareCopyFailed = true;
            ClearShopMessage();
            StateHasChanged();

            _shareCopyResetCts?.Cancel();
            _shareCopyResetCts?.Dispose();
            _shareCopyResetCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(4000, _shareCopyResetCts.Token);
                shareCopyFailed = false;
                StateHasChanged();
            }
            catch (TaskCanceledException)
            {
            }

            return;
        }

        _shareCopyResetCts?.Cancel();
        _shareCopyResetCts?.Dispose();
        _shareCopyResetCts = new CancellationTokenSource();
        var token = _shareCopyResetCts.Token;

        ClearShopMessage();
        shareCopyFailed = false;
        shareLinkCopied = true;
        StateHasChanged();

        try
        {
            await Task.Delay(3000, token);
            shareLinkCopied = false;
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
            // Another share click or panel close reset the cue.
        }
    }

    private async Task ScrollLeft(ElementReference el) =>
        await JS.InvokeVoidAsync("mwScrollLeft", el, ".ar-card");

    private async Task ScrollRight(ElementReference el) =>
        await JS.InvokeVoidAsync("mwScrollRight", el, ".ar-card");

    private IEnumerable<string> SplitParagraphs(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var paragraphs = text
            .Replace("\r\n", "\n")
            .Split(new[] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in paragraphs)
        {
            var trimmed = p.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                yield return trimmed;
        }
    }

    private static bool HasTechnicalDetails(ContentItemModel item) =>
        !string.IsNullOrWhiteSpace(item.TechnicalFabric)
        || !string.IsNullOrWhiteSpace(item.TechnicalTechniques)
        || !string.IsNullOrWhiteSpace(item.TechnicalProduction)
        || !string.IsNullOrWhiteSpace(item.TechnicalAvailability);
}
