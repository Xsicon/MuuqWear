using MuuqWear.Model.HelpCenter;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public static class HelpArticleSeed
{
    public static List<HelpArticleModel> CreateInitialArticles() =>
    [
        new() { Id = 1, Title = "How do I track my order?", Category = "Orders", Status = "Published", Views = 4821, Helpful = 423, LastUpdated = "Mar 10, 2025", Content = "Once shipped, you'll receive an email with your tracking number. You can also check order status in your account dashboard under Order History." },
        new() { Id = 2, Title = "Can I cancel my order?", Category = "Orders", Status = "Published", Views = 3204, Helpful = 287, LastUpdated = "Mar 8, 2025", Content = "Orders can be cancelled within 1 hour of placement. After that, we begin processing immediately and cancellations are no longer possible." },
        new() { Id = 3, Title = "How long does processing take?", Category = "Orders", Status = "Published", Views = 2891, Helpful = 260, LastUpdated = "Feb 28, 2025", Content = "Most orders are processed and shipped within 24 hours during business days (Monday–Friday)." },
        new() { Id = 4, Title = "Do you ship internationally?", Category = "Shipping", Status = "Published", Views = 5102, Helpful = 480, LastUpdated = "Mar 12, 2025", Content = "Yes, we ship to 40+ countries. International shipping rates and delivery times vary by destination. You can see estimated costs at checkout." },
        new() { Id = 5, Title = "Is shipping free?", Category = "Shipping", Status = "Published", Views = 3988, Helpful = 361, LastUpdated = "Mar 5, 2025", Content = "Standard shipping is free on orders over $150 USD within the continental US. Express and international shipping fees apply." },
        new() { Id = 6, Title = "Can I change my shipping address?", Category = "Shipping", Status = "Published", Views = 1890, Helpful = 172, LastUpdated = "Feb 20, 2025", Content = "Address changes can be made within 1 hour of placing your order. Contact us immediately via live chat if your order needs to be redirected." },
        new() { Id = 7, Title = "What is your return policy?", Category = "Returns", Status = "Published", Views = 6240, Helpful = 589, LastUpdated = "Mar 15, 2025", Content = "We accept returns within 30 days of delivery. Items must be unworn with all original tags still attached. Final sale items are not eligible." },
        new() { Id = 8, Title = "How do I start a return?", Category = "Returns", Status = "Published", Views = 4102, Helpful = 390, LastUpdated = "Mar 14, 2025", Content = "Log into your account, go to Order History, select the item you wish to return, and click 'Start Return'. You'll receive a prepaid return label via email." },
        new() { Id = 9, Title = "When will I receive my refund?", Category = "Returns", Status = "Published", Views = 3521, Helpful = 312, LastUpdated = "Mar 1, 2025", Content = "Refunds are processed within 5–7 business days after we receive your return. You'll receive an email confirmation when your refund is issued." },
        new() { Id = 10, Title = "What payment methods do you accept?", Category = "Payments", Status = "Published", Views = 2980, Helpful = 270, LastUpdated = "Feb 25, 2025", Content = "We accept all major credit/debit cards (Visa, Mastercard, Amex), PayPal, Apple Pay, Google Pay, and Muuqwear Gift Cards." },
        new() { Id = 11, Title = "Is my payment information secure?", Category = "Payments", Status = "Published", Views = 1740, Helpful = 161, LastUpdated = "Feb 18, 2025", Content = "Absolutely. All payments are processed through Stripe, which holds PCI DSS Level 1 certification — the highest level of payment security." },
        new() { Id = 12, Title = "Do you offer gift cards?", Category = "Payments", Status = "Published", Views = 2210, Helpful = 198, LastUpdated = "Mar 3, 2025", Content = "Yes! Digital gift cards are available in $25, $50, $100, and $200 denominations. They can be purchased and redeemed at checkout." },
        new() { Id = 13, Title = "How do I reset my password?", Category = "Account", Status = "Published", Views = 3100, Helpful = 290, LastUpdated = "Mar 10, 2025", Content = "Click 'Forgot Password' on the login page. Enter your email address and we'll send a reset link. The link is valid for 24 hours." },
        new() { Id = 14, Title = "Can I update my email address?", Category = "Account", Status = "Published", Views = 1560, Helpful = 144, LastUpdated = "Feb 28, 2025", Content = "Yes, you can change your email address in Account Settings under your profile. A confirmation email will be sent to your new address." },
        new() { Id = 15, Title = "How do I delete my account?", Category = "Account", Status = "Draft", Views = 0, Helpful = 0, LastUpdated = "Mar 20, 2025", Content = "Contact privacy@muuqwear.com with your account email and we'll process your deletion request within 30 days per GDPR requirements." },
        new() { Id = 16, Title = "How do I find my size?", Category = "Product Info", Status = "Published", Views = 4500, Helpful = 415, LastUpdated = "Mar 12, 2025", Content = "Check our Size Guide page for detailed measurements and fit recommendations. Our garments are cut to a European standard — when in doubt, size up." },
        new() { Id = 17, Title = "What materials do you use?", Category = "Product Info", Status = "Published", Views = 3800, Helpful = 352, LastUpdated = "Mar 8, 2025", Content = "We use organic cotton, recycled polyester, merino wool, and our proprietary AeroWeave technical fabric. Each product page lists the exact fabric composition." },
        new() { Id = 18, Title = "How should I care for my garments?", Category = "Product Info", Status = "Published", Views = 2900, Helpful = 265, LastUpdated = "Mar 4, 2025", Content = "Each item includes care label instructions. Generally: machine wash cold (30°C), hang to dry. Do not tumble dry or iron on prints." },
    ];

    public static readonly string[] Categories =
        ["Orders", "Shipping", "Returns", "Payments", "Account", "Product Info"];
}

public record CategoryMeta(string Bg, string Text, string Dot, string IconKey);

public static class HelpCategoryMeta
{
    public static CategoryMeta Get(string category) => category switch
    {
        "Shipping" => new("#D1FAE5", "#065F46", "#22C55E", "truck"),
        "Returns" => new("#FEE2E2", "#991B1B", "#EF4444", "return"),
        "Payments" => new("#EDE9FE", "#5B21B6", "#8B5CF6", "card"),
        "Account" => new("#FEF3C7", "#92400E", "#F59E0B", "settings"),
        "Product Info" => new("#F0FDF4", "#166534", "#4ADE80", "help"),
        _ => new("#DBEAFE", "#1D4ED8", "#3B82F6", "package")
    };
}
