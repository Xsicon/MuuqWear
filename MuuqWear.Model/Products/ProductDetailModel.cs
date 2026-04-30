namespace MuuqWear.Model.Products;

public class ProductDetailModel
{
    // basic product info
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? Badge { get; set; }

    // main image — used as first display image
    // if no images in Images list
    public string? ImageUrl { get; set; }

    // stock and availability
    public int Stock { get; set; }
    public bool IsActive { get; set; }

    // sizes stored as comma separated string
    // e.g. "S,M,L,XL,XXL"
    // we split this into list in the page 
    public string? Sizes { get; set; }

    // gender — "Men", "Women", "Unisex"
    public string? Gender { get; set; }

    // category info
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    // multiple images for thumbnails
    // ordered by sort_order ascending 
    public List<ProductImageModel> Images { get; set; } = new();

    // helper property — splits Sizes string into list
    // e.g. "S,M,L,XL" → ["S", "M", "L", "XL"]
    // used directly in razor page foreach loop 
    public List<string> SizeList =>
        string.IsNullOrEmpty(Sizes)
            ? new List<string>()
            : Sizes.Split(',')
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s))
                   .ToList();

    // helper property — gets all image URLs in order
    // if no images in Images list → use main ImageUrl
    // ensures product detail always has at least one image 
    public List<string> AllImageUrls
    {
        get
        {
            // if product has multiple images → use them
            if (Images.Any())
                return Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl ?? "")
                    .Where(url => !string.IsNullOrEmpty(url))
                    .ToList();

            if (!string.IsNullOrEmpty(ImageUrl))
                return new List<string> { ImageUrl };

            // no images at all → empty list
            return new List<string>();
        }
    }
}