using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.CartService;
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
        isLoadingOrders = false;
        await LoadAffiliateStatus();
    }

    private async Task SelectAffiliateTab(string tab)
    {
        var previousTab = activeAffiliateTab;
        activeAffiliateTab = tab;

        // Load chart when dashboard tab is opened for the first time
        if (tab == "dashboard")
        {
            Console.WriteLine("Dashboard tab opened - loading chart...");

            // Small delay to ensure tab content is rendered

            await LoadPerformanceChart();
            await LoadRecentReferrals();
        }

        StateHasChanged();
    }



    private async Task LoadAffiliateInfo()
    {
        isLoadingAffiliateInfo = true;
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
            isLoadingAffiliateInfo = false;
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
        isMobileMenuOpen = false; //  close menu after selection
        if (tab == "affiliate")
        {
            //await LoadAffiliateStatus();
            if (affiliateStatus?.ApplicationStatus == "approved")
            {
                await LoadAffiliateInfo();

                await LoadPerformanceChart();
                await LoadRecentReferrals();
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
            "addresses" => "Addresses",
            "settings" => "Settings",
            _ => "Overview"
        };
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

        Console.WriteLine("Loading chart...");
        isLoadingChart = true;
        chartError = null;
        StateHasChanged();

        try
        {
            // Fetch data
            var result = await AffiliateService.GetPerformanceChart();

            if (result.Success && result.Data != null)
            {
                performanceData = result.Data;
                Console.WriteLine($"Chart data loaded: {result.Data.DailyStats.Count} days");

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
            isLoadingChart = false;
            StateHasChanged();
        }
    }

    private async Task LoadRecentReferrals()
    {
        //if (isLoadingChart) return; // Prevent multiple simultaneous loads

        Console.WriteLine("Loading referrals...");
        isLoadingRecentReferral = true;
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
            isLoadingChart = false;
            StateHasChanged();
        }
    }


    private async Task RenderChart()
    {
        Console.WriteLine("🎨 RenderChart: Starting...");

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

