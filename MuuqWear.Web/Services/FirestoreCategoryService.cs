using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Services;

public sealed class FirestoreCategoryService : ICategoryService
{
    private readonly AdminFirestore _store;

    public FirestoreCategoryService(AdminFirestore store) => _store = store;

    public async Task<Response<List<CategoryModel>>> GetAll()
    {
        var products = await _store.ListProductsAsync();
        var fromProducts = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Category))
            .GroupBy(p => p.Category!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CategoryModel
            {
                Id = g.First().CategoryId ?? Guid.Empty,
                Name = g.Key
            })
            .OrderBy(c => c.Name)
            .ToList();
        return Response<List<CategoryModel>>.SuccessResponse(fromProducts);
    }

    public Task<Response<CategoryModel>> Add(AddCategoryModel request) =>
        Task.FromResult(Response<CategoryModel>.SuccessResponse(new CategoryModel
        {
            Id = Guid.NewGuid(),
            Name = request.Name
        }));
}
