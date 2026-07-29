namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public sealed record SupportCannedReply(string Label, string Text);

public static class SupportCannedReplies
{
    public static IReadOnlyList<SupportCannedReply> All { get; } =
    [
        new("Greeting", "Hi! Thanks for reaching out to Muuqwear — I'd be happy to help you with this."),
        new("Order status", "I've checked your order and it's currently on its way. You'll receive a tracking update by email shortly."),
        new("Return label", "No problem — I've emailed you a prepaid return label. Once we receive the item, your refund will be processed within 5–7 business days."),
        new("Refund issued", "Good news — your refund has been issued. Please allow 3–5 business days for it to appear on your statement."),
        new("Apology / delay", "I'm really sorry for the inconvenience. I'm prioritising this for you right now and will make it right."),
        new("Closing", "Is there anything else I can help you with? Thanks for being part of the Muuqwear community!")
    ];
}
