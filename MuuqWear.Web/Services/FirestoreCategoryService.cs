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
        var collections = await _store.ListCollectionsAsync();
        if (collections.Count > 0)
            return Response<List<CategoryModel>>.SuccessResponse(collections);

        // Fallback for older catalogs that only tagged products without collections docs.
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

    public async Task<Response<CategoryModel>> Add(AddCategoryModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Response<CategoryModel>.Fail("Category name is required");
        return Response<CategoryModel>.SuccessResponse(await _store.UpsertCollectionAsync(request));
    }
}
