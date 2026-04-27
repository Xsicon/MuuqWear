using System.Text.Json.Serialization;

namespace MuuqWear.Model.Cart;
public class CartItemModel
{
    // cart item id
    public Guid Id { get; set; }

    // product details 
    [JsonPropertyName("product_id")]
    public Guid ProductId { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_image_url")]
    public string? ProductImageUrl { get; set; }

    [JsonPropertyName("product_price")]
    public decimal ProductPrice { get; set; }

    // cart item details
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }

    // computed 
    public decimal ItemTotal => ProductPrice * Quantity;
}

// used when adding item to cart
public class AddCartItemModel
{
    public Guid ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? ProductName { get; set; }
    public string? ProductImageUrl { get; set; }
    public decimal ProductPrice { get; set; }
}

// used when updating quantity
public class UpdateCartItemModel
{
    public Guid CartItemId { get; set; }
    public int Quantity { get; set; }
}
