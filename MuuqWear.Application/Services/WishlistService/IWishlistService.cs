using MuuqWear.Application.Shared;
using MuuqWear.Model.Wishlist;

namespace MuuqWear.Application.Services.WishlistService;

public interface IWishlistService
{
    Task<Response<List<WishlistItemModel>>> GetWishlist();
    Task<Response<List<WishlistItemModel>>> AddItem(Guid productId);
    Task<Response<List<WishlistItemModel>>> RemoveItem(Guid productId);
    Task<Response<List<WishlistItemModel>>> Merge(List<Guid> productIds);
}
