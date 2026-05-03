namespace MuuqWear.Application.Shared;
public class AuthSessionService
{
    public bool IsSessionExpired { get; private set; } = false;

    public void MarkExpired()
    {
        IsSessionExpired = true;
        System.Diagnostics.Debug.WriteLine($"MarkExpired called — subscribers: {OnSessionExpired?.GetInvocationList().Length ?? 0}");
        OnSessionExpired?.Invoke();  // ← fire immediately ✅
    }
    public void Reset() => IsSessionExpired = false;
    public string? DisplayName { get; private set; }
    public string? DisplayEmail { get; private set; }
    public event Action? OnProfileUpdated;
    public event Action? OnSessionExpired;

    public void NotifySessionExpired()
    {
        OnSessionExpired?.Invoke();

    }
    public void UpdateDisplay(string? name, string? email)
    {
        DisplayName = name;
        DisplayEmail = email;
        OnProfileUpdated?.Invoke();
    }
}