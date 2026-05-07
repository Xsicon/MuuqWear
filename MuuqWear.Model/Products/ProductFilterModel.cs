namespace MuuqWear.Model.Products;
public class ProductFilterModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sizes { get; set; }  // ← add back 

    public string? SortBy { get; set; } = "featured";
}

