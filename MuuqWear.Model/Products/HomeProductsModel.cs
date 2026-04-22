namespace MuuqWear.Model.Products;
public class HomeProductsModel
{
    // 6 new arrival products
    public List<ProductModel> NewArrivals { get; set; } = new();

    // 6 featured products
    public List<ProductModel> Featured { get; set; } = new();

    // 6 best seller products
    public List<ProductModel> BestSellers { get; set; } = new();
}
