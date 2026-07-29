using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Model.HelpCenter;

namespace MuuqWear.Web.Components.Pages.HelpCenterComponent;

public partial class HelpCenterComponent
{
    [Inject] private IHelpCenterService HelpCenterService { get; set; } = default!;

    private ElementReference topicsTrackRef;
    private ElementReference faqRef;

    private string activeTopic = string.Empty;
    private string expandedFaq = string.Empty;
    private bool isLoadingFaqs = true;

    private record TopicItem(string Key, string Title, string Description, string? FullPageUrl = null);
    private record FaqItem(string Question, string Answer);

    private List<TopicItem> Topics =>
    [
        new("Orders", "Orders", "Track, cancel, or manage your orders."),
        new("Shipping", "Shipping", "Delivery times, tracking, and international options.", "/shipping-return#shipping"),
        new("Returns", "Returns", "Start a return, exchange, or refund.", "/shipping-return#returns"),
        new("Payments", "Payments", "Billing issues, payment methods, and gift cards."),
        new("Account", "Account", "Profile settings, password, and preferences."),
        new("Product Info", "Product Info", "Materials, care instructions, and product details."),
        new("Size Guide", "Size Guide", "Measurements, fit tips, and size charts.", "/size-guide")
    ];

    private Dictionary<string, List<FaqItem>> FaqData { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        FaqData = BuildFallbackFaqData();

        try
        {
            var result = await HelpCenterService.GetPublishedArticles(null, null, 1, 100);
            if (result.Success && result.Data?.Data.Count > 0)
            {
                FaqData = result.Data.Data
                    .GroupBy(a => a.Category)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(a => a.Title)
                            .Select(a => new FaqItem(a.Title, a.Content))
                            .ToList());
            }
        }
        catch
        {
            FaqData = BuildFallbackFaqData();
        }
        finally
        {
            isLoadingFaqs = false;
        }
    }

    private static Dictionary<string, List<FaqItem>> BuildFallbackFaqData() => new()
    {
        ["Orders"] =
        [
            new("How do I track my order?",
                "Once shipped, you'll receive an email with your tracking number. You can also check order status in your account dashboard."),
            new("Can I cancel my order?",
                "Orders can be cancelled within 1 hour of placement. After that, we begin processing immediately."),
            new("How long does processing take?",
                "Most orders are processed and shipped within 24 hours during business days.")
        ],
        ["Payments"] =
        [
            new("What payment methods do you accept?",
                "We accept all major credit cards, PayPal, Apple Pay, and Google Pay."),
            new("Is my payment information secure?",
                "Yes, all payments are processed through Stripe with industry-standard encryption."),
            new("Can I use multiple payment methods?",
                "Currently we support one payment method per order.")
        ],
        ["Account"] =
        [
            new("How do I reset my password?",
                "Click 'Forgot Password' on the login page. You'll receive a reset link via email within a few minutes."),
            new("Can I change my email address?",
                "Yes, you can update your email in your account settings under Profile."),
            new("How do I delete my account?",
                "To delete your account, please contact our support team. We'll process your request within 48 hours.")
        ],
        ["Product Info"] =
        [
            new("What materials do you use?",
                "We use sustainable, high-quality materials. Full fabric composition is listed on each product page."),
            new("How do I care for my MuuqWear items?",
                "Most items are machine washable on cold. Specific care instructions are on the product label and product page."),
            new("Where can I find product specifications?",
                "Each product page lists fabric composition, fit notes, and care instructions.")
        ]
    };

    private string? GetTopicFullPageUrl(string key) =>
        Topics.FirstOrDefault(t => t.Key == key)?.FullPageUrl;

    private async Task SelectTopic(string topic)
    {
        var pageUrl = GetTopicFullPageUrl(topic);
        if (!string.IsNullOrEmpty(pageUrl))
        {
            NavigationManager.NavigateTo(pageUrl);
            return;
        }

        activeTopic = topic;
        expandedFaq = string.Empty;
        StateHasChanged();

        await Task.Delay(50);
        await JS.InvokeVoidAsync("eval",
            "(function(){const el=document.querySelector('.hc-faq');if(el)el.scrollIntoView({behavior:'smooth',block:'start'});})();");
    }

    private void ToggleFaq(string question)
    {
        expandedFaq = expandedFaq == question
            ? string.Empty
            : question;
    }

    private async Task ScrollTopicsLeft() => await ScrollTopics(-1);

    private async Task ScrollTopicsRight() => await ScrollTopics(1);

    private async Task ScrollTopics(int direction)
    {
        await JS.InvokeVoidAsync(
            "eval",
            $@"(function(){{
                const track = document.querySelector('.hc-topics__track');
                const card = track?.querySelector('.hc-topic-card');
                if (!track || !card) return;
                const styles = getComputedStyle(track);
                const gap = parseFloat(styles.columnGap || styles.gap) || 0;
                const step = card.getBoundingClientRect().width + gap;
                const max = Math.max(0, track.scrollWidth - track.clientWidth);
                const next = Math.min(max, Math.max(0, track.scrollLeft + ({direction} * step)));
                track.scrollTo({{ left: next, behavior: 'smooth' }});
            }})();");
    }
}
