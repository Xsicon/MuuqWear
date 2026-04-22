using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Components.Pages.Admin
{
    public partial class AdminProduct
    {

        // LIST STATE
        private List<ProductModel> Products = new();
        private List<CategoryModel> Categories = new();
        private string searchQuery = "";
        private bool isLoading = true;

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
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalCount = 0;

        //OBJECT
        private CancellationTokenSource? _searchCts;

        private List<ProductImageModel> editingProductImages = new();
        private string newImageUrl = "";
        private bool isAddingImage = false;
        private bool isUploadingAdditional = false;
        private string imageError = "";

        private IEnumerable<ProductModel> FilteredProducts =>
            string.IsNullOrEmpty(searchQuery)
                ? Products
                : Products.Where(p =>
                    (p.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false));

        protected override async Task OnInitializedAsync()
        {
            await Task.WhenAll(LoadProducts(_currentPage, _pageSize), LoadCategories());
        }

        private async Task LoadProducts(int page = 1, int pageSize = 10, string? search = null)
        {
            isLoading = true;

            var result = await ProductService.GetAll(page, pageSize,
                string.IsNullOrEmpty(search) ? null : search);

            if (result.Success && result.Data != null)
            {
                Products = result.Data.Data;
                _totalCount = result.Data.TotalCount;
                _currentPage = result.Data.Page;
                _pageSize = result.Data.PageSize;
            }

            isLoading = false;
        }

        private async Task LoadCategories()
        {
            var result = await CategoryService.GetAll();
            if (result.Success && result.Data != null)
                Categories = result.Data;
        }

        private void OpenAddForm()
        {
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
                Sizes = product.Sizes
            };

            // restore selected sizes
            SelectedSizes = string.IsNullOrEmpty(product.Sizes)
                ? new HashSet<string>()
                : product.Sizes.Split(',').ToHashSet();

            await LoadProductImages(product.Id);

            imageSource = "url";
            errorMessage = string.Empty;
            isFormOpen = true;
        }

        private void CloseForm()
        {
            isFormOpen = false;
        }

        private void ToggleSize(string size)
        {
            if (SelectedSizes.Contains(size))
                SelectedSizes.Remove(size);
            else
                SelectedSizes.Add(size);

            newProduct.Sizes = string.Join(",", SelectedSizes);
        }

        private async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            isUploading = true;
            errorMessage = string.Empty;

            var file = e.File;
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
            {
                errorMessage = "Product name is required";
                return;
            }
            if (newProduct.Price <= 0)
            {
                errorMessage = "Price must be greater than 0";
                return;
            }
            if (newProduct.Stock < 0)
            {
                errorMessage = "Stock cannot be negative";
                return;
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
                    Stock = newProduct.Stock,
                    CategoryId = newProduct.CategoryId,
                    IsActive = newProduct.IsActive,
                    IsNewArrival = newProduct.IsNewArrival,
                    IsFeatured = newProduct.IsFeatured,
                    IsBestSeller = newProduct.IsBestSeller,
                    Gender = newProduct.Gender,
                    Sizes = newProduct.Sizes
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
            // user is still typing ✅
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            try
            {
                // wait 500ms before searching
                // if user types again → this gets cancelled ✅
                await Task.Delay(500, _searchCts.Token);

                // 500ms passed without new keystroke
                // → user stopped typing → search now ✅
                await SearchProducts();
            }
            catch (TaskCanceledException)
            {
                // user typed again → debounce cancelled ✅
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

                // search right away ✅
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
                // add to local list immediately ✅
                // no need to reload from API
                editingProductImages.Add(result.Data);
                newImageUrl = string.Empty; // clear input ✅
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
                // remove from local list immediately ✅
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
                // reorder local list ✅
                editingProductImages = editingProductImages
                    .OrderBy(i => i.SortOrder)
                    .ToList();
                StateHasChanged();
            }
        }
    }
}