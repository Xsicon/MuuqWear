using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminProductComponent : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AdminProductsTabCoordinator ProductsTabCoordinator { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "search")]
    public string? SearchQuery { get; set; }

    [SupplyParameterFromQuery(Name = "view")]
    public string? ViewQuery { get; set; }

    [SupplyParameterFromQuery(Name = "productId")]
    public string? ProductIdQuery { get; set; }

    private string activeView = "catalog";
    private Guid? pendingFocusProductId;
    private Guid? highlightProductId;

    private bool isStockModalOpen = false;
    private ProductModel? stockProduct = null;
    private List<SizeStockModel> editingSizeStock = new();
    private bool isUpdatingStock = false;
    private string stockError = string.Empty;
    private bool stockLoadFailed = false;
    private bool CanSaveStock => editingSizeStock.Count > 0 && !stockLoadFailed && !isUpdatingStock;
    private bool showDeleteConfirm = false;

    // BULK UPDATE STATE
    private HashSet<Guid> selectedProductIds = new();
    private bool isBulkModalOpen = false;
    private string bulkStockMode = "set";
    private int bulkQuantity = 0;
    private bool isBulkUpdating = false;
    private string bulkError = string.Empty;

    private string activeFilter = "All";

    //  computed — low stock count for warning banner
    private int LowStockCount => FilteredProducts
        .Count(ProductStockHelper.IsLowStock);

    //  update FilteredProducts to handle new filters
    private IEnumerable<ProductModel> FilteredProducts => activeFilter switch
    {
        "All" => ApplySearch(Products),
        "LowStock" => ApplySearch(Products.Where(ProductStockHelper.IsLowStock)),
        "OutOfStock" => ApplySearch(Products.Where(ProductStockHelper.IsOutOfStock)),
        _ => ApplySearch(Products.Where(p => p.CategoryId.ToString() == activeFilter))
    };

    private string PageSubtitle => activeView switch
    {
        "stock" => "Review and update per-size inventory across all products",
        "low-stock" => "Products below the restock threshold",
        "restock" => "Out-of-stock items needing restock",
        _ => "Manage product catalog, pricing, and categories"
    };

    private string ViewContextLabel => activeView switch
    {
        "stock" => "Stock Levels view — size breakdown and Update Stock actions",
        "low-stock" => $"Low Stock Alerts — products with fewer than {ProductStockHelper.LowStockThreshold} units",
        "restock" => "Restock Requests — products with zero total stock",
        _ => string.Empty
    };

    private bool ShowCatalogActions => activeView == "catalog";
    private bool ShowCategoryFilters => activeView == "catalog";
    private bool ShowProductMetaDetails => activeView == "catalog";
    private bool StockFocusLayout => activeView is "stock" or "low-stock" or "restock";

    private List<ProductModel> VisibleProducts => FilteredProducts.ToList();

    private bool AllProductsSelected =>
        VisibleProducts.Any() && VisibleProducts.All(p => selectedProductIds.Contains(p.Id));

    private void ToggleProductSelection(Guid productId)
    {
        if (selectedProductIds.Contains(productId))
            selectedProductIds.Remove(productId);
        else
            selectedProductIds.Add(productId);
    }

    private void ToggleSelectAllProducts()
    {
        if (AllProductsSelected)
            selectedProductIds.Clear();
        else
            foreach (var product in VisibleProducts)
                selectedProductIds.Add(product.Id);
    }

    private void OpenBulkModal()
    {
        bulkStockMode = "set";
        bulkQuantity = 0;
        bulkError = string.Empty;
        isBulkModalOpen = true;
    }

    private void CloseBulkModal()
    {
        isBulkModalOpen = false;
        bulkStockMode = "set";
        bulkQuantity = 0;
        bulkError = string.Empty;
    }

    private async Task ConfirmBulkUpdate()
    {
        if (bulkQuantity < 0)
        {
            bulkError = "Quantity cannot be negative.";
            return;
        }

        if (!selectedProductIds.Any())
            return;

        isBulkUpdating = true;
        bulkError = string.Empty;
        StateHasChanged();

        try
        {
            var productIds = selectedProductIds.ToList();
            var totalCount = productIds.Count;
            var successfulIds = new List<Guid>();

            foreach (var productId in productIds)
            {
                var product = Products.FirstOrDefault(p => p.Id == productId);
                if (product == null)
                {
                    var fetched = await ProductService.GetById(productId);
                    if (fetched.Success && fetched.Data != null)
                    {
                        ProductStockHelper.SyncStockFromSizes(fetched.Data);
                        product = fetched.Data;
                    }
                }

                var productLabel = GetProductLabel(product);

                var sizeStockResult = await ProductService.GetSizeStock(productId);
                var sizes = ResolveSizeStockForBulk(productId, sizeStockResult);

                if (sizes.Count > 0)
                {
                    var (success, error) = await TryBatchUpdateSizeStockAsync(
                        productId,
                        sizes,
                        size => bulkStockMode == "set"
                            ? bulkQuantity
                            : size.Quantity + bulkQuantity,
                        product);

                    if (!success)
                    {
                        ReportBulkPartialFailure(
                            successfulIds.Count, totalCount, productLabel, error!, successfulIds);
                        return;
                    }

                    successfulIds.Add(productId);
                    continue;
                }

                if (product == null)
                {
                    ReportBulkPartialFailure(
                        successfulIds.Count,
                        totalCount,
                        "Unknown product",
                        "Selected product is not in the current list. Reload and try again.",
                        successfulIds);
                    return;
                }

                var currentStock = ProductStockHelper.GetEffectiveStock(product);
                var newStock = bulkStockMode == "set"
                    ? bulkQuantity
                    : currentStock + bulkQuantity;
                newStock = Math.Max(0, newStock);

                var aggregateResult = await ProductService.UpdateStock(productId, newStock);
                if (!aggregateResult.Success)
                {
                    ReportBulkPartialFailure(
                        successfulIds.Count,
                        totalCount,
                        productLabel,
                        aggregateResult.Message ?? "Failed to update stock",
                        successfulIds);
                    return;
                }

                if (aggregateResult.Data?.SizeStock is { Count: > 0 } aggregateSizes)
                    ApplySizeStockToProduct(productId, aggregateSizes, product);
                else
                {
                    product.Stock = newStock;
                    if (Products.All(p => p.Id != productId))
                        Products.Add(product);
                }

                successfulIds.Add(productId);
            }

            selectedProductIds.Clear();
            CloseBulkModal();
            ProductsTabCoordinator.RequestBadgeCountsRefresh();
        }
        finally
        {
            isBulkUpdating = false;
            StateHasChanged();
        }
    }

    private void NavigateToLowStockView()
    {
        NavigationManager.NavigateTo("/admin/products?view=low-stock");
        ProductsTabCoordinator.NotifyViewChanged("low-stock");
    }

    private async Task SetFilter(string filter)
    {
        activeFilter = filter;
        _currentPage = 1; // ← reset to page 1 on filter change
        await LoadProducts(_currentPage, _pageSize, searchQuery);
    }

    // LIST STATE
    private List<ProductModel> Products = new();
    private List<CategoryModel> Categories = new();
    private string searchQuery = "";
    private bool isLoading = false;
    private string pageLoadError = string.Empty;

    // FORM STATE
    private bool isFormOpen = false;
    private bool isEditMode = false;
    private Guid editingProductId = Guid.Empty;
    private AddProductModel newProduct = new();
    private string imageSource = "url";
    private bool isSaving = false;
    private bool isUploading = false;
    private string errorMessage = string.Empty;

    // SIZE & GENDER
    private List<string> AvailableSizes = new() { "XS", "S", "M", "XL", "XXL" };
    private List<string> AvailableGenders = new() { "Men", "Women", "Unisex" };
    private HashSet<string> SelectedSizes = new();

    //Colors

    //  ADD
    private List<string> AvailableColors = new()
{
"#000000", // Black
"#FFFFFF", // White
"#808080", // Gray
"#1E2A47", // Navy
"#4A5C7A", // Blue-Gray
"#8B4513", // Brown
"#2F4F4F", // Dark Slate
"#C0C0C0", // Silver
"#000080", // Navy Blue
"#0F52BA", // Sapphire
"#1E90FF", // Dodger Blue
"#C44545"  // Red
};
    private List<string> SelectedColors = new();


    // CATEGORY
    private string newCategoryName = string.Empty;
    private bool isAddingCategory = false;
    private string categoryError = string.Empty;

    // DELETE STATE
    private bool isDeleteModalOpen = false;
    private Guid deletingProductId = Guid.Empty;
    private string deletingProductName = string.Empty;
    private bool isDeleting = false;

    //PAGINATION STATE
    private const int DefaultPageSize = 10;
    private int _currentPage = 1;
    private int _pageSize = DefaultPageSize;
    private int _totalCount = 0;

    //OBJECT
    private CancellationTokenSource? _searchCts;

    private List<ProductImageModel> editingProductImages = new();
    private string newImageUrl = "";
    private bool isAddingImage = false;
    private bool isUploadingAdditional = false;
    private string imageError = "";

    private IEnumerable<ProductModel> ApplySearch(IEnumerable<ProductModel> source)
    {
        if (string.IsNullOrEmpty(searchQuery)) return source;
        return source.Where(p =>
            (p.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (p.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false));
    }
    protected override async Task OnInitializedAsync()
    {
        ProductsTabCoordinator.ViewChanged += OnProductsViewChanged;
        ProductsTabCoordinator.ProductFocusRequested += OnProductFocusRequested;
        NavigationManager.LocationChanged += OnLocationChanged;

        ApplyViewFromQuery();
        ApplyProductFocusFromQuery();

        if (!string.IsNullOrEmpty(SearchQuery))
            searchQuery = SearchQuery;

        isLoading = true;
        await Task.WhenAll(LoadProducts(_currentPage, _pageSize, searchQuery), LoadCategories());
        isLoading = false;
        await TryFocusProductAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        var previousView = activeView;
        var previousFocus = pendingFocusProductId;
        ApplyViewFromQuery();
        ApplyProductFocusFromQuery();

        if (previousView != activeView && !isFormOpen)
        {
            _currentPage = 1;
            await LoadProducts(_currentPage, _pageSize, searchQuery);
        }

        if (pendingFocusProductId.HasValue && pendingFocusProductId != previousFocus)
        {
            if (activeView != "low-stock")
            {
                activeView = "low-stock";
                ApplyFilterForView();
            }

            await LoadProducts(_currentPage, _pageSize, searchQuery);
            await TryFocusProductAsync();
        }
    }

    private void OnProductsViewChanged(string view)
    {
        var normalized = AdminProductsTabCoordinator.NormalizeView(view);
        if (activeView == normalized)
            return;

        activeView = normalized;
        ApplyFilterForView();
        _currentPage = 1;

        _ = InvokeAsync(async () =>
        {
            await LoadProducts(_currentPage, _pageSize, searchQuery);
            StateHasChanged();
        });
    }

    private void OnProductFocusRequested(Guid productId)
    {
        pendingFocusProductId = productId;

        _ = InvokeAsync(async () =>
        {
            activeView = "low-stock";
            ApplyFilterForView();
            _currentPage = 1;
            await LoadProducts(_currentPage, _pageSize, searchQuery);
            await TryFocusProductAsync();
            StateHasChanged();
        });
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!NavigationManager.ToBaseRelativePath(NavigationManager.Uri)
                .StartsWith("admin/products", StringComparison.OrdinalIgnoreCase))
            return;

        _ = InvokeAsync(HandleLocationChangedAsync);
    }

    private async Task HandleLocationChangedAsync()
    {
        try
        {
            ApplyViewFromQuery();
            ApplyProductFocusFromQuery();
            _currentPage = 1;
            await LoadProducts(_currentPage, _pageSize, searchQuery);
            await TryFocusProductAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            StateHasChanged();
        }
    }

    private void ApplyViewFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        var view = query.TryGetValue("view", out var value) && !string.IsNullOrEmpty(value)
            ? value.ToString()
            : ViewQuery;

        activeView = AdminProductsTabCoordinator.NormalizeView(view);
        ApplyFilterForView();
    }

    private void ApplyProductFocusFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("productId", out var value) &&
            Guid.TryParse(value.ToString(), out var productId))
        {
            pendingFocusProductId = productId;
            return;
        }

        if (!string.IsNullOrWhiteSpace(ProductIdQuery) &&
            Guid.TryParse(ProductIdQuery, out var fromParam))
        {
            pendingFocusProductId = fromParam;
        }
    }

    private async Task TryFocusProductAsync()
    {
        if (!pendingFocusProductId.HasValue)
            return;

        var productId = pendingFocusProductId.Value;
        pendingFocusProductId = null;

        var product = Products.FirstOrDefault(p => p.Id == productId);
        if (product == null)
        {
            var result = await ProductService.GetById(productId);
            if (result.Success && result.Data != null)
            {
                ProductStockHelper.SyncStockFromSizes(result.Data);
                product = result.Data;
            }
        }

        if (product == null)
            return;

        if (Products.All(p => p.Id != product.Id))
        {
            Products.Insert(0, product);
            _totalCount = Products.Count;
        }

        if (isStockModalOpen && stockProduct?.Id == productId)
        {
            highlightProductId = productId;
            await ScrollToProductAsync(productId);
            return;
        }

        highlightProductId = productId;
        await OpenStockModal(product);
        await ScrollToProductAsync(productId);
    }

    private async Task ScrollToProductAsync(Guid productId)
    {
        try
        {
            await JS.InvokeVoidAsync(
                "adminScroll.scrollIntoView",
                $"product-{productId}");
        }
        catch
        {
            // scroll helper optional
        }
    }

    private void ApplyFilterForView()
    {
        var wasStockFilter = activeFilter is "LowStock" or "OutOfStock";

        activeFilter = activeView switch
        {
            "low-stock" => "LowStock",
            "restock" => "OutOfStock",
            _ => "All"
        };

        // Leaving low-stock/restock loaded with PageSize = filtered count (e.g. 1).
        // Reset so catalog/stock views paginate normally again.
        if (wasStockFilter && activeFilter == "All")
            _pageSize = DefaultPageSize;
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        ProductsTabCoordinator.ViewChanged -= OnProductsViewChanged;
        ProductsTabCoordinator.ProductFocusRequested -= OnProductFocusRequested;
    }

    private async Task LoadProducts(int page = 1, int pageSize = 10, string? search = null)
    {
        selectedProductIds.Clear();
        pageLoadError = string.Empty;

        Guid? categoryId = null;
        if (activeFilter != "All" &&
            activeFilter != "LowStock" &&
            activeFilter != "OutOfStock" &&
            Guid.TryParse(activeFilter, out var parsedId))
        {
            categoryId = parsedId;
        }

        //  for stock filters → load all products at once
        var isStockFilter = activeFilter == "LowStock" ||
                            activeFilter == "OutOfStock";

        var filterModel = new ProductFilterModel
        {
            Page = page,
            PageSize = isStockFilter ? 1000 : pageSize, // ← load all for stock filters
            Search = string.IsNullOrEmpty(search) ? null : search,
            CategoryId = categoryId,
            IncludeTickets = true
        };

        try
        {
            var result = await ProductService.GetAll(filterModel);

            if (result.Success && result.Data != null)
            {
                var allProducts = result.Data.Data;

                foreach (var product in allProducts)
                    ProductStockHelper.SyncStockFromSizes(product);

                Products = activeFilter switch
                {
                    "LowStock" => allProducts
                        .Where(ProductStockHelper.IsLowStock)
                        .ToList(),
                    "OutOfStock" => allProducts
                        .Where(ProductStockHelper.IsOutOfStock)
                        .ToList(),
                    _ => allProducts
                };

                _totalCount = isStockFilter ? Products.Count : result.Data.TotalCount;
                _currentPage = isStockFilter ? 1 : result.Data.Page;

                if (!isStockFilter)
                    _pageSize = result.Data.PageSize > 0 ? result.Data.PageSize : DefaultPageSize;
            }
            else
            {
                Products = [];
                _totalCount = 0;
                pageLoadError = AdminUiErrorHelper.FromApi(result.Message, "Failed to load products.");
            }
        }
        catch (Exception ex)
        {
            Products = [];
            _totalCount = 0;
            pageLoadError = AdminUiErrorHelper.FromException(ex);
        }

        StateHasChanged();
    }

    private async Task LoadCategories()
    {
        var result = await CategoryService.GetAll();
        if (result.Success && result.Data != null)
            Categories = result.Data;
    }

    private void OpenAddForm()
    {
        showDeleteConfirm = false; // ← add

        isEditMode = false;
        editingProductId = Guid.Empty;
        newProduct = new AddProductModel();
        SelectedSizes = new HashSet<string>();
        imageSource = "url";
        errorMessage = string.Empty;
        isFormOpen = true;
    }

    private async Task OpenEditForm(ProductModel product)
    {
        showDeleteConfirm = false;
        isEditMode = true;
        editingProductId = product.Id;
        newProduct = new AddProductModel
        {
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Badge = product.Badge,
            ImageUrl = product.ImageUrl,
            Stock = product.Stock,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive,
            IsNewArrival = product.IsNewArrival,
            IsFeatured = product.IsFeatured,
            IsBestSeller = product.IsBestSeller,
            Gender = product.Gender,
            //  restore sizes from SizeStock — not from Sizes string
            Sizes = product.SizeStock.Select(s => s.Size).ToList()
        };

        SelectedColors = product.ColorOptions?.ToList() ?? new List<string>();

        //  restore selected sizes from SizeStock
        SelectedSizes = product.SizeStock
            .Select(s => s.Size)
            .ToHashSet();

        await LoadProductImages(product.Id);

        imageSource = "url";
        errorMessage = string.Empty;
        isFormOpen = true;
    }
    private void CloseForm()
    {
        isFormOpen = false;
    }

    private async Task ToggleSize(string size)
    {
        if (isEditMode)
        {
            if (SelectedSizes.Contains(size))
            {
                //  remove size → delete from product_size_stock
                var sizeStock = Products
                    .FirstOrDefault(p => p.Id == editingProductId)?
                    .SizeStock
                    .FirstOrDefault(s => s.Size == size);

                if (sizeStock != null)
                {
                    var result = await ProductService.DeleteSizeStock(sizeStock.Id);
                    if (result.Success)
                    {
                        SelectedSizes.Remove(size);
                        newProduct.Sizes.Remove(size);

                        // update local product
                        var product = Products.FirstOrDefault(p => p.Id == editingProductId);
                        if (product != null)
                            product.SizeStock.RemoveAll(s => s.Size == size);
                    }
                    else
                    {
                        errorMessage = result.Message ?? "Failed to remove size";
                    }
                }
                else
                {
                    // not in DB yet — just remove from selection
                    SelectedSizes.Remove(size);
                    newProduct.Sizes.Remove(size);
                }
            }
            else
            {
                //  add size → insert into product_size_stock with quantity 0
                var result = await ProductService.AddSizeStock(
                    editingProductId, size, 0);

                if (result.Success && result.Data != null)
                {
                    SelectedSizes.Add(size);
                    newProduct.Sizes.Add(size);

                    // update local product
                    var product = Products.FirstOrDefault(p => p.Id == editingProductId);
                    if (product != null)
                        product.SizeStock.Add(new SizeStockModel
                        {
                            Id = result.Data.Id,
                            Size = result.Data.Size,
                            Quantity = result.Data.Quantity
                        });
                }
                else
                {
                    errorMessage = result.Message ?? "Failed to add size";
                }
            }
        }
        else
        {
            //  add mode — just toggle selection, no DB calls
            if (SelectedSizes.Contains(size))
            {
                SelectedSizes.Remove(size);
                newProduct.Sizes.Remove(size);
            }
            else
            {
                SelectedSizes.Add(size);
                newProduct.Sizes.Add(size);
            }
        }

        StateHasChanged();
    }
    private bool ValidateImage(IBrowserFile file)
    {
        if (file.Size > 5 * 1024 * 1024)
        { errorMessage = "Image must be under 5MB"; return false; }

        if (!file.ContentType.StartsWith("image/"))
        { errorMessage = "File must be an image"; return false; }

        return true;
    }
    private async Task HandleImageUpload(InputFileChangeEventArgs e)
    {
        isUploading = true;
        errorMessage = string.Empty;

        var file = e.File;
        if (!ValidateImage(file)) return;

        var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        var result = await ProductService.UploadImage(stream, file.Name, file.ContentType);

        if (result.Success && result.Data != null)
            newProduct.ImageUrl = result.Data;
        else
            errorMessage = result.Message ?? "Image upload failed";

        isUploading = false;
    }

    private async Task AddCategory()
    {
        if (string.IsNullOrEmpty(newCategoryName))
        {
            categoryError = "Category name is required";
            return;
        }

        isAddingCategory = true;
        categoryError = string.Empty;

        var result = await CategoryService.Add(new AddCategoryModel { Name = newCategoryName });

        if (result.Success && result.Data != null)
        {
            Categories.Add(result.Data);
            newCategoryName = string.Empty;
        }
        else
        {
            categoryError = result.Message ?? "Failed to add category";
        }

        isAddingCategory = false;
    }

    private async Task SaveProduct()
    {
        errorMessage = string.Empty;
        if (string.IsNullOrEmpty(newProduct.Name))
        { errorMessage = "Product name is required"; return; }
        if (newProduct.Price <= 0)
        { errorMessage = "Price must be greater than 0"; return; }
        //  stock validation only for add mode
        if (!isEditMode)
        {
            if (newProduct.Stock <= 0)
            { errorMessage = "Stock cannot be negative or zero"; return; }

            //  ADD - check sizes selected if stock > 0
            if (newProduct.Stock > 0 && !SelectedSizes.Any())
            {
                errorMessage = "Please select at least one size to distribute stock";
                return;
            }
        }
        isSaving = true;

        if (isEditMode)
        {
            var updateModel = new UpdateProductModel
            {
                Name = newProduct.Name,
                Description = newProduct.Description,
                Price = newProduct.Price,
                Badge = newProduct.Badge,
                ImageUrl = newProduct.ImageUrl,
                CategoryId = newProduct.CategoryId,
                IsActive = newProduct.IsActive,
                IsNewArrival = newProduct.IsNewArrival,
                IsFeatured = newProduct.IsFeatured,
                IsBestSeller = newProduct.IsBestSeller,
                Gender = newProduct.Gender,
                ColorOptions = SelectedColors //  ADD
            };

            var result = await ProductService.Update(editingProductId, updateModel);
            if (result.Success && result.Data != null)
            {
                var index = Products.FindIndex(p => p.Id == editingProductId);
                if (index != -1)
                    Products[index] = result.Data;
                CloseForm();
            }
            else
            {
                errorMessage = result.Message ?? "Failed to update product";
            }
        }
        else
        {
            //  ADD - map selected colors before saving
            newProduct.ColorOptions = SelectedColors;

            var result = await ProductService.Add(newProduct);
            if (result.Success && result.Data != null)
            {
                Products.Add(result.Data);
                CloseForm();
            }
            else
            {
                errorMessage = result.Message ?? "Failed to add product";
            }
        }

        isSaving = false;
    }
    void OpenDeleteModal(ProductModel product)
    {
        deletingProductId = product.Id;
        deletingProductName = product.Name ?? "this product";
        isDeleteModalOpen = true;
    }

    void CloseDeleteModal()
    {
        isDeleteModalOpen = false;
        deletingProductId = Guid.Empty;
        deletingProductName = string.Empty;
    }

    private async Task ConfirmDelete()
    {
        isDeleting = true;
        var result = await ProductService.Delete(deletingProductId);

        if (result.Success)
        {
            Products.RemoveAll(p => p.Id == deletingProductId);
            CloseDeleteModal();
            CloseForm();
        }
        else
        {
            errorMessage = result.Message ?? "Failed to delete product";
            CloseDeleteModal();
        }

        isDeleting = false;
    }

    // fires when user clicks page number or changes page size
    private async Task HandlePageChanged((int Page, int PageSize) args)
    {
        _currentPage = args.Page;
        _pageSize = args.PageSize;
        await LoadProducts(_currentPage, _pageSize, searchQuery);
    }

    //Search Method

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        // update search query as user types
        searchQuery = e.Value?.ToString() ?? string.Empty;

        // cancel previous debounce timer if exists
        // user is still typing 
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        try
        {
            // wait 500ms before searching
            // if user types again → this gets cancelled 
            await Task.Delay(500, _searchCts.Token);

            // 500ms passed without new keystroke
            // → user stopped typing → search now 
            await SearchProducts();
        }
        catch (TaskCanceledException)
        {
            // user typed again → debounce cancelled 
            // do nothing
        }
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        // only react to Enter key
        if (e.Key == "Enter")
        {
            // cancel debounce timer immediately
            _searchCts?.Cancel();

            // search right away 
            await SearchProducts();
        }
    }

    private async Task SearchProducts()
    {
        // always reset to page 1 when searching
        // because search results are different data
        _currentPage = 1;
        await LoadProducts(_currentPage, _pageSize, searchQuery);
    }

    private async Task LoadProductImages(Guid productId)
    {
        // fetch product with images from API
        var result = await ProductService.GetById(productId);

        if (result.Success && result.Data != null)
        {
            // map ProductImageModel from product images
            editingProductImages = result.Data.Images
                .Select(i => new ProductImageModel
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ImageUrl = i.ImageUrl,
                    SortOrder = i.SortOrder
                }).ToList();
        }
    }

    private async Task AddImageUrl()
    {
        imageError = string.Empty;

        // validate URL not empty
        if (string.IsNullOrEmpty(newImageUrl))
        {
            imageError = "Image URL is required";
            return;
        }

        isAddingImage = true;

        // next sort order = current count + 1
        // e.g. 3 images exist → new image sort_order = 4
        var nextOrder = editingProductImages.Any()
            ? editingProductImages.Max(i => i.SortOrder) + 1
            : 1;

        var result = await ProductService.AddProductImage(new AddProductImageModel
        {
            ProductId = editingProductId,
            ImageUrl = newImageUrl,
            SortOrder = nextOrder
        });

        if (result.Success && result.Data != null)
        {
            // add to local list immediately 
            // no need to reload from API
            editingProductImages.Add(result.Data);
            newImageUrl = string.Empty; // clear input 
        }
        else
        {
            imageError = result.Message ?? "Failed to add image";
        }

        isAddingImage = false;
    }

    private async Task HandleAdditionalImageUpload(InputFileChangeEventArgs e)
    {
        imageError = string.Empty;
        isUploadingAdditional = true;

        var file = e.File;
        var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);

        // upload to storage bucket
        var uploadResult = await ProductService.UploadImage(stream, file.Name, file.ContentType);

        if (uploadResult.Success && uploadResult.Data != null)
        {
            // next sort order
            var nextOrder = editingProductImages.Any()
                ? editingProductImages.Max(i => i.SortOrder) + 1
                : 1;

            // add to product_images table
            var result = await ProductService.AddProductImage(new AddProductImageModel
            {
                ProductId = editingProductId,
                ImageUrl = uploadResult.Data,
                SortOrder = nextOrder
            });

            if (result.Success && result.Data != null)
            {
                editingProductImages.Add(result.Data);
            }
            else
            {
                imageError = result.Message ?? "Failed to save image";
            }
        }
        else
        {
            imageError = uploadResult.Message ?? "Upload failed";
        }

        isUploadingAdditional = false;
    }

    private async Task DeleteImage(Guid imageId)
    {
        imageError = string.Empty;

        var result = await ProductService.DeleteProductImage(imageId);

        if (result.Success)
        {
            // remove from local list immediately 
            editingProductImages.RemoveAll(i => i.Id == imageId);
        }
        else
        {
            imageError = result.Message ?? "Failed to delete image";
        }
    }

    private async Task UpdateImageOrder(ProductImageModel img, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int newOrder))
        {
            img.SortOrder = newOrder;
            // reorder local list 
            editingProductImages = editingProductImages
                .OrderBy(i => i.SortOrder)
                .ToList();
            StateHasChanged();
        }
    }

    private async Task SaveSizeStock()
    {
        if (stockProduct == null || editingSizeStock.Count == 0)
            return;

        isUpdatingStock = true;
        stockError = string.Empty;
        StateHasChanged();

        try
        {
            var editedQuantities = editingSizeStock.ToDictionary(
                s => s.Size,
                s => s.Quantity,
                StringComparer.OrdinalIgnoreCase);

            var freshResult = await ProductService.GetSizeStock(stockProduct.Id);
            if (!freshResult.Success || freshResult.Data is not { Count: > 0 } freshSizes)
            {
                stockError = freshResult.Message
                             ?? "Could not load size stock IDs. Close the modal and try again.";
                return;
            }

            var batchRequest = BuildBatchRequest(
                freshSizes,
                size => editedQuantities.TryGetValue(size.Size, out var qty) ? qty : size.Quantity);

            var result = await ProductService.UpdateSizeStockBatch(stockProduct.Id, batchRequest);
            if (!result.Success || result.Data?.SizeStock == null)
            {
                stockError = result.Message ?? "Failed to update stock";
                return;
            }

            editingSizeStock = result.Data.SizeStock;
            ApplySizeStockToProduct(stockProduct.Id, result.Data.SizeStock);

            CloseStockModal();
            ProductsTabCoordinator.RequestBadgeCountsRefresh();
        }
        finally
        {
            isUpdatingStock = false;
            StateHasChanged();
        }
    }
    //private async Task RecalculateTotalStock(Guid productId)
    //{
    //    var sizeStockResult = await ProductService.GetSizeStock(productId);
    //    if (!sizeStockResult.Success || sizeStockResult.Data == null) return;

    //    // sum all size quantities
    //    var totalStock = sizeStockResult.Data.Sum(s => s.Quantity);

    //    // update total stock column
    //    var updateModel = new UpdateProductModel
    //    {
    //        Stock = totalStock
    //    };

    //    await ProductService.UpdateStock(productId, totalStock);
    //}
    private async Task OpenStockModal(ProductModel item)
    {
        if (isStockModalOpen)
            CloseStockModal();

        stockProduct = item;
        stockError = string.Empty;
        stockLoadFailed = false;
        isUpdatingStock = false;
        editingSizeStock = new();

        var result = await ProductService.GetSizeStock(item.Id);
        if (result.Success && result.Data is { Count: > 0 })
        {
            editingSizeStock = result.Data;
        }
        else
        {
            stockLoadFailed = true;
            stockError = result.Success && result.Data is { Count: 0 }
                ? "This product has no size stock rows yet. Add sizes from the product edit form first."
                : result.Message ?? "Could not load size stock. Please try again.";
        }

        isStockModalOpen = true;
        StateHasChanged();
    }

    private void CloseStockModal()
    {
        isStockModalOpen = false;
        stockProduct = null;
        editingSizeStock = new();
        stockLoadFailed = false;
    }

    private async Task HandleDeleteFromForm()
    {
        isDeleting = true;
        StateHasChanged();

        var result = await ProductService.Delete(editingProductId);

        if (result.Success)
        {
            Products.RemoveAll(p => p.Id == editingProductId);
            showDeleteConfirm = false;
            CloseForm();
        }
        else
        {
            errorMessage = result.Message ?? "Failed to delete product";
        }

        isDeleting = false;
        StateHasChanged();
    }
    private void ToggleColor(string color)
    {
        if (SelectedColors.Contains(color))
            SelectedColors.Remove(color);
        else
            SelectedColors.Add(color);
    }
}