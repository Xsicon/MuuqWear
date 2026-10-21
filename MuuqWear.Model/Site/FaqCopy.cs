namespace MuuqWear.Model.Site;

public static class FaqCopy
{
    public const string Intro = "Quick answers to the questions we hear most. Still stuck? Write to Info@muuqwear.com.";

    public static readonly FaqGroup[] Groups =
    [
        new("ordering", "Ordering",
        [
            new("When does the store open?", "MuuqWear is coming soon. Join the newsletter on the homepage and we will email you the moment it opens.", false),
            new("Do I need an account to order?", "No. Guest checkout is available. An account just makes reordering faster and lets you track orders.", true),
            new("What payment methods do you accept?", "The payment methods shown at checkout. Payments are processed securely; we never see or store your full card number.", true),
            new("Can I change or cancel my order?", "Yes, before it ships: write to Info@muuqwear.com with your order number as soon as possible.", true),
        ]),
        new("shipping", "Shipping",
        [
            new("Where do you ship?", "At launch we ship within Somalia. International shipping will be announced when it is ready.", true),
            new("How long does delivery take?", "Orders ship in 2-5 business days; delivery then takes about 3-7 business days in major cities and 5-12 elsewhere in Somalia. These are estimates.", true),
            new("How do I track my order?", "A tracking link is emailed when your order ships. If it has not arrived in the estimated window, write to Info@muuqwear.com.", true),
        ]),
        new("returns", "Returns and exchanges",
        [
            new("What is the return policy?", "30 days from delivery, unworn with tags attached, for a refund to your original payment method. See the Shipping, Returns and Exchanges Policy for the full terms.", true),
            new("How do I exchange a size?", "Start a return and place a new order for the size you want; it is faster than a two-way shipment.", true),
            new("What if my item arrives damaged or wrong?", "Write to Info@muuqwear.com within 14 days with your order number and photos. We replace or refund at our cost.", true),
        ]),
        new("sizing", "Sizing",
        [
            new("How do MuuqWear sizes run?", "Use the Size Guide: compare the garment measurements (international cm and US inches charts) to a piece you own that fits well. If you are between sizes, the height and weight recommendations point to the right one.", false),
        ]),
        new("account", "Account and support",
        [
            new("How do I contact support?", "Write to Info@muuqwear.com. We reply within 2 business days.", false),
            new("Do you have a phone number?", "No. Email is our support channel, so every request is tracked and answered in writing.", false),
        ]),
    ];
}

public record FaqGroup(string Id, string Title, FaqItem[] Items);

public record FaqItem(string Question, string Answer, bool AtLaunch);
