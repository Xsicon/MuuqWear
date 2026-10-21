namespace MuuqWear.Client.Offline;

public static class OfflineAction
{
    public const string Queued = "Will send when you are back online.";
    public const string SignInNeedsConnection = "Sign-in needs a connection.";
    public const string PaymentNeedsConnection = "Payment needs a connection. Your bag is still here.";

    public static bool Queues(string kind) => kind is "contact" or "newsletter" or "chat" or "wishlist" or "account";

    public static bool StaysOnline(string kind) => kind is "sign-in" or "payment";
}
