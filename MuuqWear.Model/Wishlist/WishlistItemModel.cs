using System.Text.Json.Serialization;

namespace MuuqWear.Model.Wishlist;

public class WishlistItemModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_image_url")]
    public string? ProductImageUrl { get; set; }

    [JsonPropertyName("product_price")]
    public decimal ProductPrice { get; set; }

    [JsonPropertyName("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
