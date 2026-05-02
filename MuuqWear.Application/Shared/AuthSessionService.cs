namespace MuuqWear.Application.Shared;
public class AuthSessionService
{
    public bool IsSessionExpired { get; private set; } = false;

    public void MarkExpired() => IsSessionExpired = true;
    public void Reset() => IsSessionExpired = false;
    public event Action? OnSessionExpired;

    public void NotifySessionExpired()
    {
        OnSessionExpired?.Invoke();
    }
}