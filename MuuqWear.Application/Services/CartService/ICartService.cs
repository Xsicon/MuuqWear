using MuuqWear.Application.Shared;
using MuuqWear.Model.Cart;

namespace MuuqWear.Application.Services.CartService;

public interface ICartService
{
    Task<Response<CartModel>> GetCart();

    Task<Response<CartModel>> AddItem(AddCartItemModel request);

    Task<Response<CartModel>> UpdateQuantity(UpdateCartItemModel request);

    Task<Response<CartModel>> RemoveItem(Guid cartItemId);

    Task<Response<CartModel>> ClearCart();

    Task<Response<CartModel>> MergeCart(List<AddCartItemModel> guestItems);
}