using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.WishlistService;
using MuuqWear.Model.Address;
using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Profile;

namespace MuuqWear.Web.Components.Pages.ProfileComponent;

public partial class ProfileComponent
{
    private string activeTab = "overview"; // default tab
    private List<AddressModel> addresses = new();
    private string activeAffiliateTab = "dashboard";

    // Chart state
    private PerformanceChartModel? performanceData;
    private bool isLoadingChart = true;
    private bool isLoadingRecentReferral = true;
    private string? chartError = null;

    private List<RecentReferralModel>? recentReferrals;
    private AffiliateStatusModel? affiliateStatus = new AffiliateStatusModel();
    private bool showAffiliatePrompt = false;

    private AffiliateInfoModel? affiliateInfo;
    private bool isLoadingAffiliateInfo = false;
    private string affiliateInfoError = "";
    private bool linkCopied = false;
    private bool showDeleteConfirm = false;
    private bool isDeletingAccount = false;

    private bool isMobileMenuOpen = false;
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
    private ProfileModel userProfile = new();

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

    protected override void OnInitialized()
    {
        WishlistStateService.OnWishlistChanged += OnWishlistChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is null) return;

        var authState = await AuthenticationStateTask;
        AuthStateProvider.SyncFromPrincipal(authState.User);

        var profileTask = ProfileService.GetProfile();
        var ordersTask = OrderService.GetUserOrders();
        var affiliateTask = LoadAffiliateStatus();

        await Task.WhenAll(profileTask, ordersTask, affiliateTask);

        var profileResult = await profileTask;
        if (profileResult.Success && profileResult.Data != null)
        {
            LoggedInUserName = profileResult.Data.FullName ?? "";
            settingsFullName = profileResult.Data.FullName ?? "";
            settingsEmail = profileResult.Data.Email ?? "";
        }

        var ordersResult = await ordersTask;
        if (ordersResult.Success && ordersResult.Data != null)
            userOrders = ordersResult.Data;

        await WishlistStateService.InitializeAsync(
            authState.User.Identity?.IsAuthenticated == true,
            authState.User.FindFirst("UserId")?.Value);
        await ApplyInitialTabFromRouteAsync();
    }

    private void OnWishlistChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        WishlistStateService.OnWishlistChanged -= OnWishlistChanged;
    }

    private async Task ApplyInitialTabFromRouteAsync()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();

        if (path == "/settings")
        {
            await SelectTab("settings");
            return;
        }

        if (path == "/profile" && !string.IsNullOrEmpty(uri.Query))
        {
            var query = QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("tab", out var tabValues))
            {
                var tab = tabValues.ToString().ToLowerInvariant();
                if (IsValidProfileTab(tab))
                    await SelectTab(tab);
            }
        }
    }

    private static bool IsValidProfileTab(string tab) =>
        tab is "overview" or "orders" or "wishlist" or "addresses" or "affiliate" or "settings";

    private async Task SelectAffiliateTab(string tab)
    {
        var previousTab = activeAffiliateTab;
        activeAffiliateTab = tab;

        // Load chart when dashboard tab is opened for the first time
        if (tab == "dashboard")
        {

            // Small delay to ensure tab content is rendered

            await LoadPerformanceChart();
            await LoadRecentReferrals();
        }

        StateHasChanged();
    }



    private async Task LoadAffiliateInfo()
    {
        affiliateInfoError = "";

        try
        {
            var result = await AffiliateService.GetAffiliateInfo();

            if (result.Success && result.Data != null)
            {
                affiliateInfo = result.Data;
            }
            else
            {
                affiliateInfoError = result.Message ?? "Failed to load affiliate information";
            }
        }
        catch (Exception ex)
        {
            affiliateInfoError = $"Error: {ex.Message}";
        }
        finally
        {
        }
    }



    private async Task CopyLinkToClipboard()
    {
        if (affiliateInfo == null) return;

        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", affiliateInfo.AffiliateLink);

            linkCopied = true;
            StateHasChanged();

            // Reset after 3 seconds
            await Task.Delay(3000);
            linkCopied = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy: {ex.Message}");
        }
    }

    private async Task SelectTab(string tab)
    {
        activeTab = tab;
        isMobileMenuOpen = false;
        if (tab == "wishlist")
        {
            await WishlistStateService.RefreshAsync();
        }
        if (tab == "affiliate")
        {
            //await LoadAffiliateStatus();
            if (affiliateStatus?.ApplicationStatus == "approved")
            {
                await LoadAffiliateInfo();

                await LoadPerformanceChart();
                await LoadRecentReferrals();
                //isLoadingOrders = false;
            }
        }
        else if (tab == "address")
        {
            var addressResult = await AddressService.GetAll();
            if (addressResult.Success && addressResult.Data != null)
                addresses = addressResult.Data;
        }
    }
    private string GetActiveTabLabel()
    {
        return activeTab switch
        {
            "overview" => "Overview",
            "orders" => "Orders",
            "wishlist" => "Wishlist",
            "addresses" => "Addresses",
            "affiliate" => "Affiliate Hub",
            "settings" => "Settings",
            _ => "Overview"
        };
    }

    private static string GetTabIconName(string tab) => tab switch
    {
        "overview" => "dashboard",
        "orders" => "package",
        "wishlist" => "heart",
        "addresses" => "mappin",
        "affiliate" => "users",
        "settings" => "settings",
        _ => "dashboard"
    };

    private static string GetIconPaths(string name) => name switch
    {
        "dashboard" => "<rect width=\"7\" height=\"9\" x=\"3\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"5\" x=\"14\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"9\" x=\"14\" y=\"12\" rx=\"1\"/><rect width=\"7\" height=\"5\" x=\"3\" y=\"16\" rx=\"1\"/>",
        "package" => "<path d=\"m7.5 4.27 9 5.15\"/><path d=\"M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z\"/><path d=\"m3.3 7 8.7 5 8.7-5\"/><path d=\"M12 22V12\"/>",
        "heart" => "<path d=\"M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z\"/>",
        "mappin" => "<path d=\"M20 10c0 4.993-5.539 10.193-7.399 11.799a1 1 0 0 1-1.202 0C9.539 20.193 4 14.993 4 10a8 8 0 0 1 16 0\"/><circle cx=\"12\" cy=\"10\" r=\"3\"/>",
        "users" => "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M22 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>",
        "settings" => "<path d=\"M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
        "logout" => "<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4\"/><polyline points=\"16 17 21 12 16 7\"/><line x1=\"21\" x2=\"9\" y1=\"12\" y2=\"12\"/>",
        "star" => "<polygon points=\"12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2\"/>",
        _ => ""
    };
    private async Task HandleSignOut()
    {
        await CartStateService.ClearCartState();
        await WishlistStateService.ClearWishlistState();
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
            await WishlistStateService.ClearWishlistState();
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

    private async Task LoadAffiliateStatus()
    {
        try
        {
            var result = await AffiliateService.GetStatus();
            if (result.Success && result.Data != null)
            {
                affiliateStatus = result.Data;

                // Show prompt if user hasn't applied yet
                showAffiliatePrompt = affiliateStatus.ApplicationStatus == "not_applied";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading affiliate status: {ex.Message}");
        }
    }

    //private string GetUserInitials()
    //{
    //    if (string.IsNullOrEmpty(profile?.FullName)) return "U";

    //    var parts = profile.FullName.Split(' ');
    //    if (parts.Length >= 2)
    //        return $"{parts[0][0]}{parts[1][0]}".ToUpper();

    //    return profile.FullName[0].ToString().ToUpper();
    //}

    private string GetMemberDate()
    {
        // You'll need to add a "created_at" or "joined_at" field
        // For now, use a placeholder
        return "October 2024";
    }

    private int GetCommissionRate()
    {
        return affiliateInfo?.Tier?.ToLower() switch
        {
            "bronze" => 5,
            "silver" => 10,
            "gold" => 15,
            _ => 5
        };
    }

    private int GetDiscountRate()
    {
        return affiliateInfo?.Tier?.ToLower() switch
        {
            "bronze" => 5,
            "silver" => 10,
            "gold" => 15,
            _ => 5
        };
    }

    // Tier progression helpers
    private int GetNextTierTarget()
    {
        return affiliateInfo?.Tier?.ToLower() switch
        {
            "bronze" => 150, // Next is Silver
            "silver" => 500, // Next is Gold
            "gold" => 500,   // Already at max
            _ => 150
        };
    }

    private string GetNextTier()
    {
        return affiliateInfo?.Tier?.ToLower() switch
        {
            "bronze" => "Silver",
            "silver" => "Gold",
            "gold" => "Gold (Max)",
            _ => "Silver"
        };
    }

    private int GetItemsToNextTier()
    {
        int sold = affiliateInfo?.ItemsSold ?? 0;
        int target = GetNextTierTarget();
        return Math.Max(0, target - sold);
    }

    private string GetProgressDashArray()
    {
        int sold = affiliateInfo?.ItemsSold ?? 0;
        int target = GetNextTierTarget();
        double percentage = Math.Min(100, (double)sold / target * 100);

        double circumference = 2 * Math.PI * 42; // radius = 42
        double progress = circumference * percentage / 100;
        double remaining = circumference - progress;

        return $"{progress:F2} {remaining:F2}";
    }

    // Commission batch helpers
    private int GetCurrentBatchProgress()
    {
        int sold = affiliateInfo?.ItemsSold ?? 0;
        return sold % 10;
    }

    private int GetItemsToNextBatch()
    {
        int progress = GetCurrentBatchProgress();
        return 10 - progress;
    }

    private int GetBatchPercentage()
    {
        return GetCurrentBatchProgress() * 10;
    }

    private async Task LoadPerformanceChart()
    {
        //if (isLoadingChart) return; // Prevent multiple simultaneous loads

        chartError = null;
        StateHasChanged();

        try
        {
            // Fetch data
            var result = await AffiliateService.GetPerformanceChart();

            if (result.Success && result.Data != null)
            {
                performanceData = result.Data;

                // Small delay to ensure canvas is in DOM
                await Task.Delay(200);

                // Render
                await RenderChart();
            }
            else
            {
                chartError = result.Message ?? "Failed to load chart data";
                Console.WriteLine($"Chart data failed: {chartError}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chart error: {ex.Message}");
            chartError = "Failed to load chart";
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task LoadRecentReferrals()
    {
        //if (isLoadingChart) return; // Prevent multiple simultaneous loads

        chartError = null;
        StateHasChanged();

        try
        {
            // Fetch data
            var result = await AffiliateService.GetRecentReferrals();

            if (result.Success && result.Data != null)
            {
                recentReferrals = result.Data;

                // Small delay to ensure canvas is in DOM
                await Task.Delay(200);

                // Render
                await RenderChart();
            }
            else
            {
                chartError = result.Message ?? "Failed to load chart data";
                Console.WriteLine($"Chart data failed: {chartError}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chart error: {ex.Message}");
            chartError = "Failed to load chart";
        }
        finally
        {
            StateHasChanged();
        }
    }


    private async Task RenderChart()
    {

        try
        {
            var canvasExists = await JSRuntime.InvokeAsync<bool>(
                "eval",
                "document.getElementById('performanceChart') !== null");

            if (!canvasExists)
            {
                Console.WriteLine(" Canvas not found");
                chartError = "Chart container not ready";
                StateHasChanged();
                return;
            }

            Console.WriteLine(" Canvas found");

            var chartJsLoaded = await JSRuntime.InvokeAsync<bool>(
                "eval",
                "typeof Chart !== 'undefined'");

            if (!chartJsLoaded)
            {
                Console.WriteLine(" Chart.js not loaded");
                chartError = "Chart library not loaded";
                StateHasChanged();
                return;
            }

            Console.WriteLine(" Chart.js loaded");

            await JSRuntime.InvokeVoidAsync(
                "chartHelpers.renderPerformanceChart",
                performanceData);

            Console.WriteLine(" Chart rendered!");
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($" JS Error: {jsEx.Message}");
            chartError = "Chart failed to render";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Render error: {ex.Message}");
            chartError = "Chart rendering failed";
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("chartHelpers.destroyChart");
        }
        catch
        {
            // Ignore errors during dispose
        }
    }


}

