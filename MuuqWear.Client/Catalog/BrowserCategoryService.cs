using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;

namespace MuuqWear.Client.Catalog;

public sealed class BrowserCategoryService : ICategoryService
{
    private readonly BrowserCatalog _catalog;

    public BrowserCategoryService(BrowserCatalog catalog) => _catalog = catalog;

    public async Task<Response<List<CategoryModel>>> GetAll() =>
        Response<List<CategoryModel>>.SuccessResponse(await _catalog.CategoriesAsync());

    public Task<Response<CategoryModel>> Add(AddCategoryModel request) =>
        Task.FromResult(Response<CategoryModel>.Fail("Staff tools stay on the admin host."));
}
