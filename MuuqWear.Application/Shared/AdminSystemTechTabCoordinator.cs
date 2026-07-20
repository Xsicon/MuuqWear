namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs System &amp; Technology tab switches between the sidebar and AdminSystemTechnologyComponent.
/// </summary>
public class AdminSystemTechTabCoordinator
{
    public event Action<string>? TabChanged;

    public void NotifyTabChanged(string tab) => TabChanged?.Invoke(tab);

    public static string NormalizeTab(string? tab) =>
        tab?.ToLowerInvariant() switch
        {
            "integrations" => "integrations",
            "sync" => "sync",
            "logs" => "logs",
            "jobs" => "jobs",
            _ => "health"
        };
}
