using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Client.Auth;

/// <summary>
/// Clears Firebase + local tokens, then hard-navigates to /auth/clear so the
/// HttpOnly __session cookie is removed. Skipping Firebase sign-out lets the
/// next page load re-mint the cookie via ShopperNav SyncCookieAsync.
/// </summary>
public static class PublicSignOut
{
    public static async Task RunAsync(
        FirebaseSession firebase,
        BrowserTokenStore tokens,
        IJSRuntime js,
        NavigationManager navigation,
        bool expired = false)
    {
        try { await firebase.SignOutAsync(); }
        catch (JSException) { }
        catch (InvalidOperationException) { }

        try { await tokens.SaveAsync(new AuthResponseModel()); }
        catch { /* ignore */ }

        try
        {
            await js.InvokeVoidAsync("eval", "document.documentElement.dataset.mwAuth='out'");
        }
        catch (JSException) { }
        catch (InvalidOperationException) { }

        var dest = expired ? "/auth/clear?expired=true" : "/auth/clear";
        navigation.NavigateTo(dest, forceLoad: true);
    }
}
