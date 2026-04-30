namespace MuuqWear.Model.Cart;
public class CartModel
{
    // all items in cart
    public List<CartItemModel> Items { get; set; } = new();

    // computed totals — match backend
    public decimal Subtotal => Items.Sum(i => i.ItemTotal);
    public decimal Shipping => 0;
    public decimal Tax => Math.Round(Subtotal * 0.10m, 2);
    public decimal Total => Subtotal + Shipping + Tax;

    // total items for badge
    public int TotalItems => Items.Sum(i => i.Quantity);

    // helper — is cart empty 
    public bool IsEmpty => !Items.Any();
}
