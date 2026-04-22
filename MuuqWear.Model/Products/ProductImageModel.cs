namespace MuuqWear.Model.Products;
public class ProductImageModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}

public class AddProductImageModel
{
    public Guid ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; } = 0;
}
