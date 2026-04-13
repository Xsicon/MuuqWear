using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MuuqWear.Model.Authentication;
using System.Security.Claims;

namespace MuuqWear.Application.Shared;
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _localStorage;

    public CustomAuthStateProvider(ProtectedLocalStorage localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var result = await _localStorage
                .GetAsync<StorageItem<AuthResponseModel>>("authData");

            if (result.Success && result.Value?.Value != null)
            {
                var storageItem = result.Value;

                if (storageItem.Expiry > DateTime.UtcNow)
                {
                    var user = storageItem.Value;

                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.UserName ?? ""),
                        new Claim(ClaimTypes.Email, user.Email ?? "")
                    }, "apiauth");

                    var claimsPrincipal = new ClaimsPrincipal(identity);

                    return new AuthenticationState(claimsPrincipal);
                }
                else
                {
                    await _localStorage.DeleteAsync("authData");
                }
            }
        }
        catch
        {
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
