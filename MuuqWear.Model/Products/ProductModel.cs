using System.ComponentModel.DataAnnotations;

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

}
