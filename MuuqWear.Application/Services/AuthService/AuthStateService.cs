using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Application.Services.AuthService
{
    public class AuthStateService
    {
        private readonly ProtectedLocalStorage _localStorage;

        public bool IsLoggedIn { get; private set; } = false;
        public string? UserName { get; private set; }
        public string? Role { get; private set; }

        public AuthStateService(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
        }
        public event Action? OnAuthChecked;

        public async Task InitializeAsync()
        {
            var result = await _localStorage.GetAsync<StorageItem<AuthResponseModel>>("authData");

            if (result.Success && result.Value?.Value != null)
            {
                var storageItem = result.Value;

                if (storageItem.Expiry > DateTime.UtcNow)
                {
                    IsLoggedIn = true;
                    UserName = storageItem.Value.UserName!;
                    Role = storageItem.Value.Role ?? "user";

                }
                else
                {
                    await _localStorage.DeleteAsync("authData");
                    IsLoggedIn = false;
                    UserName = string.Empty;
                    Role = string.Empty;
                }
            }
            else
            {
                IsLoggedIn = false;
                UserName = string.Empty;
            }

            OnAuthChecked?.Invoke();
            Console.WriteLine($"OnAuthChecked fired. IsLoggedIn: {IsLoggedIn}, Role: {Role}");
            System.Diagnostics.Debug.WriteLine($"OnAuthChecked fired. IsLoggedIn: {IsLoggedIn}, Role: {Role}");

        }
        public async Task SetPendingEmailAsync(string email)
        {
            await _localStorage.SetAsync("pendingEmail", email);
        }
        public async Task<string?> GetPendingEmailAsync()
        {
            var result = await _localStorage.GetAsync<string>("pendingEmail");
            if (result.Success && !string.IsNullOrEmpty(result.Value))
                return result.Value;
            return null;
        }
        public async Task ClearPendingEmailAsync()
        {
            await _localStorage.DeleteAsync("pendingEmail");
        }
    }
}