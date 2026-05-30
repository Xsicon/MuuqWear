using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Model.Cart;
using MuuqWear.Model.PartnerStoreProduct;

namespace MuuqWear.Web.Components.Pages.PartnerStoreComponent;

public partial class PartnerStoreComponent
{
    // State
    private List<PartnerStoreProductModel>? products;
    private AffiliatePurchaseLimitModel? limitStatus;
    private bool isLoading = true;
    private string? errorMessage;
    private int totalCount = 0;
    private int currentPage = 1;
    private int pageSize = 10;

    private Guid? selectedProductId = null;  // Which product's size selector is open
    private string? selectedSize = null;
    private bool isAddingToCart = false;  // Loading state during add
    private (Guid ProductId, string Message)? addToCartError = null;

    protected override async Task OnInitializedAsync()
    {
        await LoadPartnerStore();
    }

    /// <summary>
    /// Load products and limit status
    /// </summary>
    private async Task LoadPartnerStore()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            // Load products with current page
            var productsTask = AffiliateService.GetPartnerStoreProducts(currentPage, pageSize);
            var limitTask = AffiliateService.GetPurchaseLimitStatus();

            await Task.WhenAll(productsTask, limitTask);

            // Handle paginated products result
            var productsResult = await productsTask;
            if (productsResult.Success && productsResult.Data != null)
            {
                products = productsResult.Data.Data;
                totalCount = productsResult.Data.TotalCount;
            }
            else
            {
                errorMessage = productsResult.Message ?? "Failed to load products";
            }

            // Handle limit result (unchanged)
            var limitResult = await limitTask;
            if (limitResult.Success && limitResult.Data != null)
            {
                limitStatus = limitResult.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [PartnerStore] Load error: {ex.Message}");
            errorMessage = "Failed to load partner store";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
    /// <summary>
    /// Add product to cart with affiliate discount
    /// </summary>
    private async Task AddToBag(PartnerStoreProductModel product)
    {
        try
        {
            Console.WriteLine($"🛒 Adding {product.Name} to cart");

            // TODO: Implement cart integration in next phase
            // For now, just show a message

            // Placeholder: This will be replaced with actual cart service call
            Console.WriteLine($"Product: {product.Name}");
            Console.WriteLine($"Price: ${product.DiscountedPrice}");
            Console.WriteLine($"Discount: ${product.DiscountAmount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [PartnerStore] Add to cart error: {ex.Message}");
            errorMessage = "Failed to add item to cart";
        }
    }
    private async Task HandlePageChanged((int Page, int PageSize) pageInfo)
    {
        currentPage = pageInfo.Page;
        pageSize = pageInfo.PageSize;

        await LoadPartnerStore();
    }

    private void OpenSizeSelector(Guid productId)
    {
        selectedProductId = productId;
        selectedSize = null; // Reset selection
        StateHasChanged();
    }

    private void SelectSize(string size)
    {
        selectedSize = size;
        StateHasChanged();
    }
    private void CancelSizeSelection()
    {
        selectedProductId = null;
        selectedSize = null;
        StateHasChanged();
    }

    private (bool IsValid, string? ErrorMessage) ValidateAddToCart(PartnerStoreProductModel product)
    {
        // Validate product exists
        if (product == null)
        {
            return (false, "Product not found");
        }

        // Validate product is in stock
        if (!product.InStock)
        {
            return (false, "Product is out of stock");
        }

        // Validate selected size is in stock
        // Note: selectedSize is guaranteed to exist here because the confirm button
        // only shows when a size is selected (@if (!string.IsNullOrEmpty(selectedSize)))
        var sizeStock = product.SizeStock.FirstOrDefault(s => s.Size == selectedSize);
        if (sizeStock == null || sizeStock.Quantity <= 0)
        {
            return (false, $"Size {selectedSize} is out of stock");
        }

        // All validations passed
        return (true, null);
    }

    private async Task<(bool CanPurchase, string? ErrorMessage)> CheckPurchaseLimit(int quantity)
    {
        try
        {
            Console.WriteLine($" [PartnerStore] Checking limit for {quantity} item(s)");

            // Call backend to validate
            var result = await AffiliateService.CanPurchaseQuantity(quantity);

            if (!result.Success)
            {
                Console.WriteLine($" [PartnerStore] Limit check failed: {result.Message}");
                return (false, result.Message ?? "Unable to validate purchase limit");
            }

            if (!result.Data)
            {
                // Backend returned false (limit exceeded)
                var remaining = limitStatus?.ItemsRemaining ?? 0;
                var message = remaining > 0
                    ? $"You can only purchase {remaining} more item(s) this month"
                    : "You've reached your 20 item monthly limit";

                Console.WriteLine($" [PartnerStore] Limit exceeded: {message}");
                return (false, message);
            }

            Console.WriteLine($"[PartnerStore] Limit check passed");
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PartnerStore] Limit check error: {ex.Message}");
            return (false, "Unable to verify purchase limit. Please try again.");
        }
    }


    private async Task ConfirmAddToBag(PartnerStoreProductModel product)
    {
        // Clear any previous errors
        addToCartError = null;

        try
        {
            // STEP 1: Validate inputs locally
            var validation = ValidateAddToCart(product);
            if (!validation.IsValid)
            {
                Console.WriteLine($" [PartnerStore] Validation failed: {validation.ErrorMessage}");
                addToCartError = (product.Id, validation.ErrorMessage!);
                StateHasChanged();
                return;
            }

            // STEP 2: Set loading state
            isAddingToCart = true;
            StateHasChanged();

            // STEP 3: Check purchase limit
            var limitCheck = await CheckPurchaseLimit(1);
            if (!limitCheck.CanPurchase)
            {
                addToCartError = (product.Id, limitCheck.ErrorMessage!);
                isAddingToCart = false;
                StateHasChanged();
                return;
            }

            // STEP 4: Add to cart with affiliate discount
            Console.WriteLine($"[PartnerStore] Adding to cart: {product.Name}, Size: {selectedSize}");

            var addToCartRequest = new AddCartItemModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.ImageUrl,
                ProductPrice = product.DiscountedPrice,  //  Use discounted price
                Size = selectedSize!,  // Guaranteed to exist (UI enforces)
                Quantity = 1,
                IsAffiliateDiscount = true  //  Mark as affiliate purchase
            };

            var success = await CartStateService.AddItem(addToCartRequest);


            if (!success)
            {
                Console.WriteLine($"[PartnerStore] Cart add failed");
                addToCartError = (product.Id, "Failed to add to cart");
                isAddingToCart = false;
                StateHasChanged();
                return;
            }

            Console.WriteLine($" [PartnerStore] Successfully added to cart");

            selectedProductId = null;
            selectedSize = null;

            // Reload limit status to reflect potential limit usage
            // (Note: Limit only decreases after checkout, not after add to cart)
            // But good to refresh in case user checked out in another tab
            await LoadLimitStatus();

            // TODO: Show success toast/message
            Console.WriteLine($"🎉 [PartnerStore] Item added! Cart updated.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [PartnerStore] Add to cart error: {ex.Message}");
            addToCartError = (product.Id, "An unexpected error occurred. Please try again.");
        }
        finally
        {
            isAddingToCart = false;
            StateHasChanged();
        }
    }

    private async Task LoadLimitStatus()
    {
        try
        {
            var limitResult = await AffiliateService.GetPurchaseLimitStatus();
            if (limitResult.Success && limitResult.Data != null)
            {
                limitStatus = limitResult.Data;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [PartnerStore] Failed to reload limit: {ex.Message}");
            // Don't show error to user - limit refresh is nice-to-have
        }
    }
}

