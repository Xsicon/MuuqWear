using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace MuuqWear.Client.Auth;

public interface IFirebaseIdTokenSource
{
    Task<string?> GetIdTokenAsync(bool forceRefresh = false);
}

public sealed class FirebaseUserSnapshot
{
    public string? Uid { get; set; }
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
}

public sealed class FirebaseAuthResult
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public string? Uid { get; set; }
    public string? Email { get; set; }
    public bool MfaRequired { get; set; }
    public bool EmailVerified { get; set; }
}

public sealed class FirebaseSession : IFirebaseIdTokenSource
{
    private readonly IJSRuntime _js;
    private readonly FirebaseWebConfig? _config;
    private readonly string? _functionsBaseUrl;
    private bool _ready;

    public FirebaseSession(IJSRuntime js, IConfiguration configuration)
    {
        _js = js;
        _config = configuration.GetSection("Firebase").Get<FirebaseWebConfig>();
        _functionsBaseUrl = configuration["FunctionsBaseUrl"];
    }

    public async Task RestoreAsync()
    {
        if (_config is not { IsComplete: true })
            return;

        try
        {
            _ready = await _js.InvokeAsync<bool>("muuqFirebase.init", new
            {
                apiKey = _config.ApiKey,
                authDomain = _config.AuthDomain,
                projectId = _config.ProjectId,
                appId = _config.AppId,
                storageBucket = _config.StorageBucket,
                messagingSenderId = _config.MessagingSenderId,
                appCheckSiteKey = _config.AppCheckSiteKey,
                functionsBaseUrl = _functionsBaseUrl
            });
        }
        catch (JSException)
        {
            _ready = false;
        }
        catch (InvalidOperationException)
        {
            _ready = false;
        }
    }

    public async Task<string?> GetIdTokenAsync(bool forceRefresh = false)
    {
        if (!_ready)
            return null;

        try
        {
            return await _js.InvokeAsync<string?>("muuqFirebase.getIdToken", forceRefresh);
        }
        catch (JSException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public Task<FirebaseAuthResult> SignInAsync(string email, string password) =>
        CallAsync("muuqFirebase.signIn", email, password);

    public Task<FirebaseAuthResult> CompleteTotpSignInAsync(string code) =>
        CallAsync("muuqFirebase.completeTotpSignIn", code);

    public async Task<FirebaseMfaStatus> GetMfaStatusAsync()
    {
        if (!_ready)
            return new FirebaseMfaStatus { Ok = true, SignedIn = false };

        try
        {
            return await _js.InvokeAsync<FirebaseMfaStatus>("muuqFirebase.mfaStatus")
                ?? new FirebaseMfaStatus { Ok = true, SignedIn = false };
        }
        catch (JSException)
        {
            return new FirebaseMfaStatus { Message = "Sign-in needs a connection." };
        }
        catch (InvalidOperationException)
        {
            return new FirebaseMfaStatus { Message = "Sign-in needs a connection." };
        }
    }

    public Task<FirebaseMfaSetup> StartTotpEnrollAsync() =>
        CallMfaAsync("muuqFirebase.startTotpEnroll");

    public Task<FirebaseMfaSetup> FinishTotpEnrollAsync(string code) =>
        CallMfaAsync("muuqFirebase.finishTotpEnroll", code);

    public Task<FirebaseMfaSetup> UnenrollTotpAsync() =>
        CallMfaAsync("muuqFirebase.unenrollTotp");

    public Task<FirebaseAuthResult> SignUpAsync(string email, string password, string? displayName = null) =>
        CallAsync("muuqFirebase.signUp", email, password, displayName ?? "");

    public Task<FirebaseAuthResult> SendEmailVerificationAsync() =>
        CallAsync("muuqFirebase.sendEmailVerification");

    public Task<FirebaseAuthResult> SendPasswordResetAsync(string email) =>
        CallAsync("muuqFirebase.sendPasswordReset", email);

    public async Task<FirebaseUserSnapshot?> CurrentUserAsync()
    {
        if (!_ready)
            await RestoreAsync();
        if (!_ready)
            return null;

        try
        {
            return await _js.InvokeAsync<FirebaseUserSnapshot?>("muuqFirebase.currentUser");
        }
        catch (JSException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        if (!_ready)
            return;

        try
        {
            await _js.InvokeVoidAsync("muuqFirebase.signOut");
        }
        catch (JSException) { }
        catch (InvalidOperationException) { }
    }

    private async Task<FirebaseAuthResult> CallAsync(string method, params object[] args)
    {
        if (!_ready)
            await RestoreAsync();
        if (!_ready)
            return new FirebaseAuthResult { Message = "Sign-in needs a connection." };

        try
        {
            return await _js.InvokeAsync<FirebaseAuthResult>(method, args)
                ?? new FirebaseAuthResult { Message = "Sign-in needs a connection." };
        }
        catch (JSException)
        {
            return new FirebaseAuthResult { Message = "Sign-in needs a connection." };
        }
        catch (InvalidOperationException)
        {
            return new FirebaseAuthResult { Message = "Sign-in needs a connection." };
        }
    }

    private async Task<FirebaseMfaSetup> CallMfaAsync(string method, params object[] args)
    {
        if (!_ready)
            return new FirebaseMfaSetup { Message = "Sign-in needs a connection." };

        try
        {
            return await _js.InvokeAsync<FirebaseMfaSetup>(method, args)
                ?? new FirebaseMfaSetup { Message = "Sign-in needs a connection." };
        }
        catch (JSException)
        {
            return new FirebaseMfaSetup { Message = "Sign-in needs a connection." };
        }
        catch (InvalidOperationException)
        {
            return new FirebaseMfaSetup { Message = "Sign-in needs a connection." };
        }
    }
}
