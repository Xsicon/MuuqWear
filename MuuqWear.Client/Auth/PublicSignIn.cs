using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Client.Auth;

public static class PublicSignIn
{
    public static async Task CompleteAsync(
        AuthResponseModel data,
        IJSRuntime js,
        NavigationManager navigation,
        string returnUrl = "/profile",
        bool rememberMe = false)
    {
        try
        {
            var ok = await SyncCookieAsync(data, js, rememberMe, returnUrl);
            if (!ok)
                return;

            navigation.NavigateTo(returnUrl, forceLoad: true);
        }
        catch (JSException)
        {
            navigation.NavigateTo(returnUrl, forceLoad: true);
        }
    }

    public static async Task<bool> SyncCookieAsync(
        AuthResponseModel data,
        IJSRuntime js,
        bool rememberMe = true,
        string returnUrl = "/profile")
    {
        var session = new CookieAuthSession
        {
            Token = data.AccessToken ?? "",
            RefreshToken = data.RefreshToken ?? "",
            Username = data.UserName ?? "",
            Email = data.Email ?? "",
            Role = data.Role ?? "user",
            UserId = data.UserId ?? "",
            ReturnUrl = returnUrl,
            RememberMe = rememberMe
        };

        try
        {
            return await js.InvokeAsync<bool>("mwSignIn", session);
        }
        catch (JSException)
        {
            return false;
        }
    }
}
