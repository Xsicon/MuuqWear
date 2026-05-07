using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Model.Address;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Profile;

namespace MuuqWear.Web.Components.Pages
{
    public partial class Profile
    {
        private string activeTab = "overview"; // default tab
        private List<AddressModel> addresses = new();

        private bool showDeleteConfirm = false;
        private bool isDeletingAccount = false;
        private string deleteError = string.Empty;
        //  form state
        private bool isAddressFormOpen = false;
        private bool isEditMode = false;
        private AddressModel? editingAddress = null;
        private string addressFormError = string.Empty;
        private bool isSavingAddress = false;
        private CreateAddressModel addressForm = new();
        [CascadingParameter]
        private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

        private List<OrderModel> userOrders = new();
        private bool isLoadingOrders = true;

        public string LoggedInUserName = "";

        // notification settings
        private bool emailNotifications = true;
        private bool smsNotifications = false;
        private bool promotionalEmails = true;

        // settings form
        private string settingsFullName = string.Empty;
        private string settingsEmail = string.Empty;
        private string settingsError = string.Empty;
        private string settingsSuccess = string.Empty;
        private bool isSavingSettings = false;

        protected override async Task OnInitializedAsync()
        {
            if (AuthenticationStateTask is null) return;
            var authState = await AuthenticationStateTask;
            var isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
            var userId = authState.User.FindFirst("UserId")?.Value;
            var profileResult = await ProfileService.GetProfile();
            if (profileResult.Success && profileResult.Data != null)
            {
                LoggedInUserName = profileResult.Data.FullName ?? "";
                settingsFullName = profileResult.Data.FullName ?? "";
                settingsEmail = profileResult.Data.Email ?? "";
            }
            AuthStateProvider.SyncFromPrincipal(authState.User);
            await CartStateService.InitializeAsync(isAuthenticated, userId);

            // fetch orders
            var ordersResult = await OrderService.GetUserOrders();
            if (ordersResult.Success && ordersResult.Data != null)
                userOrders = ordersResult.Data;
            System.Diagnostics.Debug.WriteLine($"Orders success: {ordersResult.Success}");
            System.Diagnostics.Debug.WriteLine($"Orders count: {ordersResult.Data?.Count}");
            System.Diagnostics.Debug.WriteLine($"Orders success: {ordersResult.Message}");
            isLoadingOrders = false;
            var addressResult = await AddressService.GetAll();
            if (addressResult.Success && addressResult.Data != null)
                addresses = addressResult.Data;

        }

        private async Task HandleSignOut()
        {
            await CartStateService.ClearCartState();
            NavigationManager.NavigateTo("/auth/clear", forceLoad: true);
        }


        private async Task HandleSetDefault(Guid addressId)
        {
            var result = await AddressService.SetDefault(addressId);
            if (result.Success)
            {
                // update local state — no need to reload from API
                foreach (var a in addresses)
                    a.IsDefault = a.Id == addressId;

                StateHasChanged();
            }
        }

        private void OpenAddForm()
        {
            isEditMode = false;
            editingAddress = null;
            addressFormError = string.Empty;
            addressForm = new CreateAddressModel(); // ← reset in one line
            isAddressFormOpen = true;
        }
        private async Task HandleSaveAddress()
        {
            // validate
            if (string.IsNullOrWhiteSpace(addressForm.Label))
            { addressFormError = "Label is required"; return; }

            if (string.IsNullOrWhiteSpace(addressForm.FullName))
            { addressFormError = "Full name is required"; return; }

            if (string.IsNullOrWhiteSpace(addressForm.Street1))
            { addressFormError = "Street address is required"; return; }

            if (string.IsNullOrWhiteSpace(addressForm.City))
            { addressFormError = "City is required"; return; }

            if (string.IsNullOrWhiteSpace(addressForm.PostalCode))
            { addressFormError = "Postal code is required"; return; }

            isSavingAddress = true;
            addressFormError = string.Empty;
            StateHasChanged();

            if (isEditMode && editingAddress != null)
            {
                //  cast directly — same fields
                var request = new UpdateAddressModel
                {
                    Label = addressForm.Label,
                    FullName = addressForm.FullName,
                    Street1 = addressForm.Street1,
                    Street2 = string.IsNullOrEmpty(addressForm.Street2)
                                    ? null : addressForm.Street2,
                    City = addressForm.City,
                    State = string.IsNullOrEmpty(addressForm.State)
                                    ? null : addressForm.State,
                    PostalCode = addressForm.PostalCode,
                    Country = addressForm.Country,
                    IsDefault = addressForm.IsDefault
                };

                var result = await AddressService.Update(editingAddress.Id, request);

                if (result.Success && result.Data != null)
                {
                    var index = addresses.FindIndex(a => a.Id == editingAddress.Id);
                    if (index >= 0)
                    {
                        addresses[index] = result.Data;
                        if (addressForm.IsDefault)
                            foreach (var a in addresses.Where(a => a.Id != editingAddress.Id))
                                a.IsDefault = false;
                    }
                    isAddressFormOpen = false;
                }
                else
                {
                    addressFormError = result.Message ?? "Failed to update address";
                }
            }
            else
            {
                var result = await AddressService.Create(addressForm); // ← pass directly 

                if (result.Success && result.Data != null)
                {
                    if (addressForm.IsDefault)
                        foreach (var a in addresses)
                            a.IsDefault = false;

                    addresses.Add(result.Data);
                    isAddressFormOpen = false;
                }
                else
                {
                    addressFormError = result.Message ?? "Failed to save address";
                }
            }

            isSavingAddress = false;
            StateHasChanged();
        }

        private void EditAddress(AddressModel address)
        {
            isEditMode = true;
            editingAddress = address;
            addressFormError = string.Empty;

            // ← populate in one block
            addressForm = new CreateAddressModel
            {
                Label = address.Label,
                FullName = address.FullName,
                Street1 = address.Street1,
                Street2 = address.Street2 ?? string.Empty,
                City = address.City,
                State = address.State ?? string.Empty,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault
            };

            isAddressFormOpen = true;
        }
        private void CloseAddressForm()
        {
            isAddressFormOpen = false;
            addressFormError = string.Empty;
        }

        private async Task HandleSaveSettings()
        {
            if (string.IsNullOrWhiteSpace(settingsFullName))
            { settingsError = "Full name is required"; return; }

            if (string.IsNullOrWhiteSpace(settingsEmail))
            { settingsError = "Email is required"; return; }

            isSavingSettings = true;
            settingsError = string.Empty;
            settingsSuccess = string.Empty;
            StateHasChanged();

            var result = await ProfileService.UpdateProfile(new UpdateProfileModel
            {
                FullName = settingsFullName
            });

            if (result.Success && result.Data != null)
            {
                LoggedInUserName = result.Data.FullName ?? "";
                settingsFullName = result.Data.FullName ?? "";
                AuthSession.UpdateDisplay(result.Data.FullName, settingsEmail); // ← add

                settingsSuccess = "Profile updated successfully";
            }
            else
            {
                settingsError = result.Message ?? "Failed to update profile";
            }

            isSavingSettings = false;
            StateHasChanged();
        }

        private async Task HandleDeleteAccount()
        {
            isDeletingAccount = true;
            deleteError = string.Empty;
            StateHasChanged();

            var result = await ProfileService.DeleteAccount();

            if (result.Success)
            {
                //  sign out and redirect to home
                await CartStateService.ClearCartState();
                NavigationManager.NavigateTo("/auth/clear", forceLoad: true);
            }
            else
            {
                deleteError = result.Message ?? "Failed to delete account";
                isDeletingAccount = false;
                showDeleteConfirm = false;
                StateHasChanged();
            }
        }

    }
}