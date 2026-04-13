using Microsoft.AspNetCore.Components.Forms;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Components.Pages.Admin
{
    public partial class AdminProduct
    {
        //List
        private List<ProductModel> Products = new();
        private AddProductModel newProduct = new();

        //Bool
        private bool isLoading = true;
        private bool isModalOpen = false;
        private bool isSaving = false;
        private bool isUploading = false;
        private bool isEditMode = false;
        private bool isDeleteModalOpen = false;
        private bool isDeleting = false;

        //string
        private string imageSource = "url";
        private string searchQuery = string.Empty;
        private string errorMessage = string.Empty;
        private Guid editingProductId = Guid.Empty;
        private Guid deletingProductId = Guid.Empty;
        private string deletingProductName = string.Empty;

        private IEnumerable<ProductModel> FilteredProducts =>
                    string.IsNullOrEmpty(searchQuery)
                        ? Products
                        : Products.Where(p =>
                            (p.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (p.Category?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false));

        protected override async Task OnInitializedAsync()
        {
            await LoadProducts();
        }

        private async Task LoadProducts()
        {
            isLoading = true;
            var result = await ProductService.GetAll();
            if (result.Success && result.Data != null)
                Products = result.Data;
            isLoading = false;
        }

        private void OpenModal()
        {
            isEditMode = false;
            editingProductId = Guid.Empty;
            newProduct = new AddProductModel();
            imageSource = "url";
            errorMessage = string.Empty;
            isModalOpen = true;
        }

        private void CloseModal()
        {
            isModalOpen = false;
        }

        private async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            isUploading = true;
            errorMessage = string.Empty;

            var file = e.File;
            var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024); // 5MB limit

            var result = await ProductService.UploadImage(stream, file.Name, file.ContentType);

            if (result.Success && result.Data != null)
                newProduct.ImageUrl = result.Data;
            else
                errorMessage = result.Message ?? "Image upload failed";

            isUploading = false;
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
                    Price = newProduct.Price,
                    Badge = newProduct.Badge,
                    ImageUrl = newProduct.ImageUrl,
                    Stock = newProduct.Stock,
                    Category = newProduct.Category,
                    IsActive = newProduct.IsActive,
                    IsNewArrival = newProduct.IsNewArrival,
                    IsFeatured = newProduct.IsFeatured,
                    IsBestSeller = newProduct.IsBestSeller
                };

                var result = await ProductService.Update(editingProductId, updateModel);

                if (result.Success && result.Data != null)
                {
                    // update product in local list
                    var index = Products.FindIndex(p => p.Id == editingProductId);
                    if (index != -1)
                        Products[index] = result.Data;
                    CloseModal();
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
                    CloseModal();
                }
                else
                {
                    errorMessage = result.Message ?? "Failed to add product";
                }
            }

            isSaving = false;
        }
        void OpenEditModal(ProductModel product)
        {
            isEditMode = true;
            editingProductId = product.Id;
            newProduct = new AddProductModel
            {
                Name = product.Name,
                Price = product.Price,
                Badge = product.Badge,
                ImageUrl = product.ImageUrl,
                Stock = product.Stock,
                Category = product.Category,
                IsActive = product.IsActive,
                IsNewArrival = product.IsNewArrival,
                IsFeatured = product.IsFeatured,
                IsBestSeller = product.IsBestSeller
            };
            imageSource = "url";
            errorMessage = string.Empty;
            isModalOpen = true;
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
    }
}