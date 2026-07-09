using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.MuuqsimoService;
using MuuqWear.Model.Cart;
using MuuqWear.Model.Muuqsimo;

namespace MuuqWear.Web.Components.Pages.MuuqSimoComponent;

public partial class MuuqSimoComponent : IDisposable
{
    private const string DefaultSlug = "muuqsimo-2025";

    [Inject] private IMuuqsimoService MuuqsimoService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private CartStateService CartStateService { get; set; } = default!;

    [Parameter]
    public string? EventSlug { get; set; }

    [SupplyParameterFromQuery(Name = "slug")]
    public string? SlugQuery { get; set; }

    private MuuqsimoPageModel? eventPage;
    private List<MuuqsimoEventSummaryModel> publishedEvents = new();
    private bool isLoading = true;
    private string? loadError;
    private string activeSlug = DefaultSlug;

    private Guid? processingTicketId;
    private string addToCartMessage = string.Empty;
    private string addToCartMessageCss = string.Empty;
    private Guid? lastMessageTicketId;

    private bool _countdownStarted;
    private bool _swiperInitialized;

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        activeSlug = ResolveSlug();
        await LoadPageAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        var slug = ResolveSlug();
        if (slug == activeSlug)
            return;

        activeSlug = slug;
        await LoadPageAsync();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!e.Location.Contains("/muuqsimo", StringComparison.OrdinalIgnoreCase))
            return;

        _ = InvokeAsync(HandleLocationChangedAsync);
    }

    private async Task HandleLocationChangedAsync()
    {
        var slug = ResolveSlug();
        if (slug == activeSlug)
            return;

        activeSlug = slug;
        await LoadPageAsync();
    }

    private string ResolveSlug()
    {
        if (!string.IsNullOrWhiteSpace(EventSlug))
            return EventSlug.Trim();

        if (!string.IsNullOrWhiteSpace(SlugQuery))
            return SlugQuery.Trim();

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("slug", out var value) && !string.IsNullOrWhiteSpace(value))
            return value.ToString().Trim();

        return DefaultSlug;
    }

    private string EventUrl(string slug, string? hash = null)
    {
        var path = $"/muuqsimo?slug={Uri.EscapeDataString(slug)}";
        return string.IsNullOrEmpty(hash) ? path : $"{path}#{hash}";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (eventPage?.Content == null)
            return;

        if (!_countdownStarted && !string.IsNullOrWhiteSpace(eventPage.Content.CountdownUtc))
        {
            await JS.InvokeVoidAsync("mwMuuqsimoCountdown.start", eventPage.Content.CountdownUtc);
            _countdownStarted = true;
        }

        if (!_swiperInitialized && eventPage.Content.HeroSlides.Count > 0)
        {
            await JS.InvokeVoidAsync("mwMuuqsimoHero.init");
            _swiperInitialized = true;
        }
    }

    private async Task LoadPageAsync()
    {
        isLoading = true;
        loadError = null;
        _countdownStarted = false;
        _swiperInitialized = false;
        StateHasChanged();

        var eventsResult = await MuuqsimoService.GetPublishedEventsAsync();
        if (eventsResult.Success && eventsResult.Data != null)
            publishedEvents = eventsResult.Data;

        var result = await MuuqsimoService.GetPageAsync(activeSlug);

        if (!result.Success || result.Data == null)
        {
            loadError = result.Message ?? "Unable to load Muuqsimo event page.";
            eventPage = null;
        }
        else
        {
            eventPage = result.Data;
            if (!string.IsNullOrWhiteSpace(eventPage.Content.Slug))
                activeSlug = eventPage.Content.Slug;
        }

        isLoading = false;
        StateHasChanged();
    }

    private string HeroMetaLine()
    {
        if (eventPage?.Content == null)
            return string.Empty;

        var venue = eventPage.Content.Venue?.Name;
        if (DateTimeOffset.TryParse(eventPage.Content.StartDate, out var start) &&
            DateTimeOffset.TryParse(eventPage.Content.EndDate, out var end))
        {
            var datePart = start.Date == end.Date
                ? start.ToString("MMMM d, yyyy")
                : $"{start:MMMM d}–{end:d, yyyy}";

            return string.IsNullOrWhiteSpace(venue)
                ? datePart
                : $"{datePart} · {venue}";
        }

        return venue ?? string.Empty;
    }

    private string CountdownDateLine()
    {
        if (eventPage?.Content?.StartDate == null)
            return string.Empty;

        if (!DateTimeOffset.TryParse(eventPage.Content.StartDate, out var start))
            return eventPage.Content.StartDate;

        var offsetLabel = start.Offset == TimeSpan.Zero
            ? "UTC"
            : $"GMT{start.ToString("zzz")}";

        return $"{start:MMMM d, yyyy 'at' h:mm tt} {offsetLabel}";
    }

    private async Task HandleTicketPurchase(MuuqsimoTicketTierModel tier)
    {
        processingTicketId = tier.ProductId;
        addToCartMessage = string.Empty;
        lastMessageTicketId = null;
        StateHasChanged();

        var success = await CartStateService.AddItem(new AddCartItemModel
        {
            ProductId = tier.ProductId,
            Size = "One Size",
            Color = activeSlug.Contains("2026", StringComparison.OrdinalIgnoreCase) ? "Midnight" : "Sapphire",
            Quantity = 1,
            ProductName = tier.Name,
            ProductImageUrl = tier.ImageUrl ?? "/images/muuqsimo-tickets.jpg",
            ProductPrice = tier.Price
        });

        addToCartMessage = success
            ? "Added to bag! ✓"
            : "Failed to add. Please try again.";
        addToCartMessageCss = success ? "text-success" : "text-danger";

        lastMessageTicketId = tier.ProductId;
        processingTicketId = null;
        StateHasChanged();

        await Task.Delay(3000);
        if (lastMessageTicketId == tier.ProductId)
        {
            addToCartMessage = string.Empty;
            lastMessageTicketId = null;
            StateHasChanged();
        }
    }

    private RenderFragment RenderIcon(string icon) => __builder =>
    {
        switch (icon)
        {
            case "sparkles":
                __builder.AddMarkupContent(0, """
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                         viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M9.937 15.5A2 2 0 0 0 8.5 14.063l-6.135-1.582a.5.5 0 0 1 0-.962L8.5 9.936A2 2 0 0 0 9.937 8.5l1.582-6.135a.5.5 0 0 1 .963 0L14.063 8.5A2 2 0 0 0 15.5 9.937l6.135 1.581a.5.5 0 0 1 0 .964L15.5 14.063a2 2 0 0 0-1.437 1.437l-1.582 6.135a.5.5 0 0 1-.963 0z"></path>
                        <path d="M20 3v4"></path><path d="M22 5h-4"></path>
                        <path d="M4 17v2"></path><path d="M5 18H3"></path>
                    </svg>
                    """);
                break;
            case "shirt":
                __builder.AddMarkupContent(0, """
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                         viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M20.38 3.46 16 2a4 4 0 0 1-8 0L3.62 3.46a2 2 0 0 0-1.34 2.23l.58 3.47a1 1 0 0 0 .99.84H6v10c0 1.1.9 2 2 2h8a2 2 0 0 0 2-2V10h2.15a1 1 0 0 0 .99-.84l.58-3.47a2 2 0 0 0-1.34-2.23z"></path>
                    </svg>
                    """);
                break;
            case "trophy":
                __builder.AddMarkupContent(0, """
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                         viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6"></path>
                        <path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18"></path>
                        <path d="M4 22h16"></path>
                        <path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22"></path>
                        <path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22"></path>
                        <path d="M18 2H6v7a6 6 0 0 0 12 0V2Z"></path>
                    </svg>
                    """);
                break;
            case "wine":
                __builder.AddMarkupContent(0, """
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                         viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M8 22h8"></path><path d="M7 10h10"></path>
                        <path d="M12 15v7"></path>
                        <path d="M12 15a5 5 0 0 0 5-5c0-2-.5-4-2-8H9c-1.5 4-2 6-2 8a5 5 0 0 0 5 5Z"></path>
                    </svg>
                    """);
                break;
            case "music":
                __builder.AddMarkupContent(0, """
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                         viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M9 18V5l12-2v13"></path>
                        <circle cx="6" cy="18" r="3"></circle>
                        <circle cx="18" cy="16" r="3"></circle>
                    </svg>
                    """);
                break;
            case "play":
                __builder.AddMarkupContent(0, """
                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18"
                         viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <polygon points="6 3 20 12 6 21 6 3"></polygon>
                    </svg>
                    """);
                break;
            default:
                __builder.AddMarkupContent(0, """
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                         viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"></circle>
                    </svg>
                    """);
                break;
        }
    };
}
