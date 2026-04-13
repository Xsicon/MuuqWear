using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;

namespace MuuqWear.Application.Services.ProductService;
public interface IProductService
{
    Task<Response<List<ProductModel>>> GetAll();
    Task<Response<ProductModel>> Add(AddProductModel request);
    Task<Response<string>> UploadImage(Stream fileStream, string fileName, string contentType);
    Task<Response<ProductModel>> Update(Guid id, UpdateProductModel request);
    Task<Response<bool>> Delete(Guid id);


}

