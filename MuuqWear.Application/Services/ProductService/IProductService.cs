using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.ProductService;
public interface IProductService
{
    Task<Response<PaginatedResponse<ProductModel>>> GetAll(ProductFilterModel filter);
    Task<Response<HomeProductsModel>> GetHomeProducts();
    Task<Response<ProductModel>> Add(AddProductModel request);
    Task<Response<string>> UploadImage(Stream fileStream, string fileName, string contentType);
    Task<Response<ProductModel>> Update(Guid id, UpdateProductModel request);
    Task<Response<bool>> Delete(Guid id);
    Task<Response<ProductModel>> GetById(Guid id);
    Task<Response<List<ProductModel>>> GetRelated(Guid id);
    Task<Response<ProductImageModel>> AddProductImage(AddProductImageModel request);
    Task<Response<bool>> DeleteProductImage(Guid imageId);

}

