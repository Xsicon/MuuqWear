using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MuuqWear.Model.Products;
public class ProductModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public string? Badge { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsNewArrival { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public string? Description { get; set; }
    public string? Gender { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Sku { get; set; }
    public List<SizeStockModel> SizeStock { get; set; } = new();
    public List<ProductImageModel> Images { get; set; } = new();
    public List<string> ColorOptions { get; set; } = new();


}

public class AddProductModel
{
    [Required(ErrorMessage = "Product name is required")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    public string? Badge { get; set; }
    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int Stock { get; set; }

    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsNewArrival { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public string? Description { get; set; }
    public string? Gender { get; set; }
    public Guid? CategoryId { get; set; }
    public List<ProductImageModel> Images { get; set; } = new();
    public List<string> Sizes { get; set; } = new();
    public List<string> ColorOptions { get; set; } = new();


}

public class UpdateSizeStockModel
{
    public int Quantity { get; set; }
}

public class BatchUpdateSizeStockRequest
{
    public List<BatchSizeStockUpdateItem> Items { get; set; } = new();
    public List<BatchSizeStockUpsertItem> Upserts { get; set; } = new();
}

public class BatchSizeStockUpdateItem
{
    public Guid SizeStockId { get; set; }
    public int Quantity { get; set; }
}

public class BatchSizeStockUpsertItem
{
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class BatchUpdateSizeStockResult
{
    public List<SizeStockModel> SizeStock { get; set; } = new();
    public int TotalStock { get; set; }
}
public class SizeStockModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("sizeStockId")]
    public Guid SizeStockId
    {
        get => Id;
        set
        {
            if (value != Guid.Empty)
                Id = value;
        }
    }

    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}