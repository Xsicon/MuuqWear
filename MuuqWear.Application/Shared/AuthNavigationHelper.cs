using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Model.Authentication;
using System.Net.Http.Json;

namespace MuuqWear.Application.Shared;

public static class AuthNavigationHelper
{
    public static async Task PrepareAndNavigateAsync(
        AuthResponseModel data,
        NavigationManager navigationManager,
        CustomAuthenticationStateProvider authStateProvider,
        IJSRuntime js,
        string returnUrl = "/profile",
        bool rememberMe = false)
    {
        var session = CookieAuthHelper.FromAuthResponse(data, returnUrl, rememberMe);

        try
        {
            var ok = await js.InvokeAsync<bool>("mwSignIn", session);
            if (!ok)
                return;

            // Full load so HttpContext carries the auth cookie for API handlers.
            // Cookie is already set via fetch — no /auth/set-cookie redirect needed.
            navigationManager.NavigateTo(returnUrl, forceLoad: true);
        }
        catch (JSException)
        {
            await FallbackForceLoadAsync(session, navigationManager);
        }
    }

    private static async Task FallbackForceLoadAsync(
        CookieAuthSession session,
        NavigationManager navigationManager)
    {
        using var http = new HttpClient();
        var response = await http.PostAsJsonAsync(
            $"{navigationManager.BaseUri}auth/prepare-cookie", session);

        if (!response.IsSuccessStatusCode)
            return;

        var key = (await response.Content.ReadAsStringAsync()).Trim('"');
        navigationManager.NavigateTo($"/auth/set-cookie?key={key}", forceLoad: true);
    }
}
