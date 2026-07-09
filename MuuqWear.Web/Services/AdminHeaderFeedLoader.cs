using MuuqWear.Application.Services.CustomerService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;
using MuuqWear.Model.Products;

namespace MuuqWear.Web.Services;

public sealed class AdminHeaderFeedLoadResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public bool IsTruncated { get; init; }
}

/// <summary>
/// Loads admin header feed data with an explicit cap and truncation metadata.
/// </summary>
public static class AdminHeaderFeedLoader
{
    public const int PageSize = 500;
    public const int MaxPages = 2;
    public const int MaxRecords = PageSize * MaxPages;

    public static async Task<AdminHeaderFeedLoadResult<ProductModel>> LoadProductsAsync(
        IProductService productService)
    {
        var items = new List<ProductModel>();
        var totalCount = 0;
        var truncated = false;

        for (var page = 1; page <= MaxPages; page++)
        {
            var result = await productService.GetAll(new ProductFilterModel
            {
                Page = page,
                PageSize = PageSize,
                IncludeTickets = true
            });

            if (!result.Success || result.Data?.Data == null)
                break;

            totalCount = result.Data.TotalCount;

            foreach (var product in result.Data.Data)
            {
                ProductStockHelper.SyncStockFromSizes(product);
                items.Add(product);
            }

            if (!result.Data.HasNextPage)
                break;

            if (page == MaxPages)
                truncated = true;
        }

        if (totalCount > items.Count)
            truncated = true;

        return new AdminHeaderFeedLoadResult<ProductModel>
        {
            Items = items,
            TotalCount = totalCount,
            IsTruncated = truncated
        };
    }

    public static async Task<AdminHeaderFeedLoadResult<CustomerModel>> LoadCustomersAsync(
        ICustomerService customerService)
    {
        var items = new List<CustomerModel>();
        var totalCount = 0;
        var truncated = false;

        for (var page = 1; page <= MaxPages; page++)
        {
            var result = await customerService.GetAll(null, page, PageSize);

            if (!result.Success || result.Data?.Data == null)
                break;

            totalCount = result.Data.TotalCount;
            items.AddRange(result.Data.Data);

            if (!result.Data.HasNextPage)
                break;

            if (page == MaxPages)
                truncated = true;
        }

        if (totalCount > items.Count)
            truncated = true;

        return new AdminHeaderFeedLoadResult<CustomerModel>
        {
            Items = items,
            TotalCount = totalCount,
            IsTruncated = truncated
        };
    }
}
