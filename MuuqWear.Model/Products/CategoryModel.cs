namespace MuuqWear.Model.Products;
public class CategoryModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class AddCategoryModel
{
    public string? Name { get; set; }
}
