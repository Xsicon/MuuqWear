namespace MuuqWear.Client.Auth;

public sealed class FirebaseWebConfig
{
    public string? ApiKey { get; init; }
    public string? AuthDomain { get; init; }
    public string? ProjectId { get; init; }
    public string? AppId { get; init; }
    public string? StorageBucket { get; init; }
    public string? MessagingSenderId { get; init; }
    public string? AppCheckSiteKey { get; init; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(AuthDomain)
        && !string.IsNullOrWhiteSpace(ProjectId)
        && !string.IsNullOrWhiteSpace(AppId);
}
