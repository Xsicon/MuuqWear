using Microsoft.AspNetCore.Components;
using MuuqWear.Model.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MuuqWear.Application.Shared;
public static class AuthNavigationHelper
{
    private static readonly HttpClient _http = new();

    public static async Task PrepareAndNavigateAsync(
        AuthResponseModel data,
        NavigationManager navigationManager,
        string returnUrl = "/profile", bool rememberMe = false)

    {
        var session = new CookieAuthSession
        {
            Token = data.AccessToken ?? "",
            Username = data.UserName ?? "",
            Email = data.Email ?? "",
            Role = data.Role ?? "user",
            UserId = data.UserId ?? "",
            ReturnUrl = returnUrl,
            RememberMe = rememberMe
        };


        var response = await _http.PostAsJsonAsync(
            $"{navigationManager.BaseUri}auth/prepare-cookie", session);

        if (!response.IsSuccessStatusCode) return;

        var key = await response.Content.ReadAsStringAsync();

        key = key.Trim('"');


        navigationManager.NavigateTo(
            $"/auth/set-cookie?key={key}", forceLoad: true);
    }
}
