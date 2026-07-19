namespace MuuqWear.Application.Shared;

/// <summary>
/// Syncs Career Management tab switches between the sidebar and AdminCareerManagementComponent.
/// </summary>
public class AdminCareersTabCoordinator
{
    public event Action<string>? TabChanged;

    public void NotifyTabChanged(string tab) => TabChanged?.Invoke(tab);

    public static string NormalizeTab(string? tab) =>
        tab?.ToLowerInvariant() switch
        {
            "applications" => "applications",
            "settings" => "settings",
            _ => "postings"
        };
}
