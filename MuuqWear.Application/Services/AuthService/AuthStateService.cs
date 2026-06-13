namespace MuuqWear.Application.Services.AuthService;

public class AuthStateService
{
    private string? _pendingEmail;

    public Task SetPendingEmailAsync(string email)
    {
        _pendingEmail = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetPendingEmailAsync()
    {
        return Task.FromResult(_pendingEmail);
    }

    public Task ClearPendingEmailAsync()
    {
        _pendingEmail = null;
        return Task.CompletedTask;
    }
}