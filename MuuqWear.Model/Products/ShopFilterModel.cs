
namespace MuuqWear.Model.Products;
public class ShopFilterModel
{
    // pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;

    // search
    public string? Search { get; set; }

    // filters
    public Guid? CategoryId { get; set; }
    public List<string> SelectedSizes { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // sorting
    public string SortBy { get; set; } = "featured";

    // helper → converts SelectedSizes list to comma string
    // e.g. ["S", "M"] → "S,M"
    // passed to API as query param ✅
    public string? SizesAsString =>
        SelectedSizes.Any()
            ? string.Join(",", SelectedSizes)
            : null;

    // helper → converts to ProductFilterModel for API call
    public ProductFilterModel ToFilterModel() => new ProductFilterModel
    {
        Page = Page,
        PageSize = PageSize,
        Search = Search,
        CategoryId = CategoryId,
        Sizes = SizesAsString,
        MinPrice = MinPrice,
        MaxPrice = MaxPrice,
        SortBy = SortBy
    };
}
