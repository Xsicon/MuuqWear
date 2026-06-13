namespace MuuqWear.Application.Shared;
public class AuthSessionService
{
    public bool IsSessionExpired { get; private set; } = false;

    public event Action? OnSessionExpired;
    public event Action? OnSessionReset;

    //  event carries data inline — not stored in singleton
    // Action<string?, string?> = (name, email)
    public event Action<string?, string?>? OnProfileUpdated;

    public void MarkExpired()
    {
        IsSessionExpired = true;
        OnSessionExpired?.Invoke();
    }

    public void Reset()
    {
        IsSessionExpired = false;
        OnSessionReset?.Invoke();
    }

    public void NotifySessionExpired()
    {
        OnSessionExpired?.Invoke();
    }

    //  passes data through event — not stored
    // each circuit receives its own copy 
    public void UpdateDisplay(string? name, string? email)
    {
        OnProfileUpdated?.Invoke(name, email);
    }
}