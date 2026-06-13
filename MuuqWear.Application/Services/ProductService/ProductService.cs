using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.ProductService;
public class ProductService : IProductService
{
    private readonly HttpClient _http;

    public ProductService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<PaginatedResponse<ProductModel>>> GetAll(ProductFilterModel filter)
    {
        try
        {
            // build query string from filter object 
            var queryParams = new List<string>();

            queryParams.Add($"page={filter.Page}");
            queryParams.Add($"pageSize={filter.PageSize}");

            if (!string.IsNullOrEmpty(filter.Search))
                queryParams.Add($"search={Uri.EscapeDataString(filter.Search)}");

            if (filter.CategoryId.HasValue)
                queryParams.Add($"categoryId={filter.CategoryId.Value}");

            if (!string.IsNullOrEmpty(filter.Sizes))
                queryParams.Add($"sizes={Uri.EscapeDataString(filter.Sizes)}");

            if (filter.MinPrice.HasValue)
                queryParams.Add($"minPrice={filter.MinPrice.Value}");

            if (filter.MaxPrice.HasValue)
                queryParams.Add($"maxPrice={filter.MaxPrice.Value}");

            if (!string.IsNullOrEmpty(filter.SortBy))
                queryParams.Add($"sortBy={filter.SortBy}");

            if (filter.IncludeTickets)
                queryParams.Add("includeTickets=true");
            var url = $"api/Product/all?{string.Join("&", queryParams)}";

            var result = await _http
                .GetFromJsonAsync<Response<PaginatedResponse<ProductModel>>>(url);

            return result ?? new Response<PaginatedResponse<ProductModel>>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<PaginatedResponse<ProductModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<HomeProductsModel>> GetHomeProducts()
    {
        try
        {
            var result = await _http
                .GetFromJsonAsync<Response<HomeProductsModel>>("api/Product/home");

            return result ?? new Response<HomeProductsModel>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<HomeProductsModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<ProductModel>> Add(AddProductModel request)
    {
        var result = await _http.PostAsJsonAsync("api/Product/add", request);
        if (!result.IsSuccessStatusCode)
        {
            return new Response<ProductModel>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };
        }
        var response = await result.Content.ReadFromJsonAsync<Response<ProductModel>>();
        return response ?? new Response<ProductModel>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    public async Task<Response<string>> UploadImage(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var result = await _http.PostAsync("api/Product/upload-image", content);

            if (!result.IsSuccessStatusCode)
            {
                return new Response<string>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };
            }

            var response = await result.Content.ReadFromJsonAsync<Response<string>>();
            return response ?? new Response<string>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<ProductModel>> Update(Guid id, UpdateProductModel request)
    {
        var result = await _http.PutAsJsonAsync($"api/Product/update/{id}", request);
        if (!result.IsSuccessStatusCode)
        {
            return new Response<ProductModel>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };
        }
        var response = await result.Content.ReadFromJsonAsync<Response<ProductModel>>();
        return response ?? new Response<ProductModel>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    public async Task<Response<bool>> Delete(Guid id)
    {
        var result = await _http.DeleteAsync($"api/Product/delete/{id}");
        if (!result.IsSuccessStatusCode)
        {
            return new Response<bool>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };
        }
        var response = await result.Content.ReadFromJsonAsync<Response<bool>>();
        return response ?? new Response<bool>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    public async Task<Response<ProductModel>> GetById(Guid id)
    {
        try
        {
            // call backend GET api/Product/{id}
            // id inserted directly in URL 
            var result = await _http
                .GetFromJsonAsync<Response<ProductModel>>($"api/Product/{id}");

            return result ?? new Response<ProductModel>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<ProductModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<List<ProductModel>>> GetRelated(Guid id)
    {
        try
        {
            // call backend GET api/Product/{id}/related
            var result = await _http
                .GetFromJsonAsync<Response<List<ProductModel>>>($"api/Product/{id}/related");

            return result ?? new Response<List<ProductModel>>
            {
                Success = false,
                Message = "Empty response from server"
            };
        }
        catch (Exception ex)
        {
            return new Response<List<ProductModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<ProductImageModel>> AddProductImage(AddProductImageModel request)
    {
        var result = await _http.PostAsJsonAsync("api/Product/images/add", request);
        if (!result.IsSuccessStatusCode)
        {
            return new Response<ProductImageModel>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };
        }
        var response = await result.Content.ReadFromJsonAsync<Response<ProductImageModel>>();
        return response ?? new Response<ProductImageModel>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    public async Task<Response<bool>> DeleteProductImage(Guid imageId)
    {
        var result = await _http.DeleteAsync($"api/Product/images/{imageId}");
        if (!result.IsSuccessStatusCode)
        {
            return new Response<bool>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };
        }
        var response = await result.Content.ReadFromJsonAsync<Response<bool>>();
        return response ?? new Response<bool>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    // =============================================
    // GET SIZE STOCK
    // =============================================
    public async Task<Response<List<SizeStockModel>>> GetSizeStock(Guid productId)
    {
        try
        {
            var result = await _http.GetAsync(
                $"api/Product/{productId}/size-stock");

            if (!result.IsSuccessStatusCode)
                return new Response<List<SizeStockModel>>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await result.Content
                .ReadFromJsonAsync<Response<List<SizeStockModel>>>();

            return response ?? new Response<List<SizeStockModel>>
            {
                Success = false,
                Message = "Empty response"
            };
        }
        catch (Exception ex)
        {
            return new Response<List<SizeStockModel>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =============================================
    // UPDATE SIZE STOCK
    // =============================================
    public async Task<Response<SizeStockModel>> UpdateSizeStock(
        Guid sizeStockId, int quantity)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Product/size-stock/{sizeStockId}",
                new UpdateSizeStockModel { Quantity = quantity });

            if (!result.IsSuccessStatusCode)
                return new Response<SizeStockModel>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await result.Content
                .ReadFromJsonAsync<Response<SizeStockModel>>();

            return response ?? new Response<SizeStockModel>
            {
                Success = false,
                Message = "Empty response"
            };
        }
        catch (Exception ex)
        {
            return new Response<SizeStockModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<Response<ProductModel>> UpdateStock(Guid productId, int totalStock)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Product/{productId}/stock",
                new { Stock = totalStock });

            if (!result.IsSuccessStatusCode)
                return new Response<ProductModel>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await result.Content
                .ReadFromJsonAsync<Response<ProductModel>>();

            return response ?? new Response<ProductModel>
            {
                Success = false,
                Message = "Empty response"
            };
        }
        catch (Exception ex)
        {
            return new Response<ProductModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =============================================
    // DELETE SIZE STOCK
    // =============================================
    public async Task<Response<bool>> DeleteSizeStock(Guid sizeStockId)
    {
        try
        {
            var result = await _http.DeleteAsync(
                $"api/Product/size-stock/{sizeStockId}");

            if (!result.IsSuccessStatusCode)
                return new Response<bool>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await result.Content
                .ReadFromJsonAsync<Response<bool>>();

            return response ?? new Response<bool>
            {
                Success = false,
                Message = "Empty response"
            };
        }
        catch (Exception ex)
        {
            return new Response<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // =============================================
    // ADD SIZE STOCK
    // =============================================
    public async Task<Response<SizeStockModel>> AddSizeStock(
        Guid productId, string size, int quantity)
    {
        try
        {
            var result = await _http.PostAsJsonAsync(
                $"api/Product/{productId}/size-stock",
                new { Size = size, Quantity = quantity });

            if (!result.IsSuccessStatusCode)
                return new Response<SizeStockModel>
                {
                    Success = false,
                    Message = $"Server error: {result.StatusCode}"
                };

            var response = await result.Content
                .ReadFromJsonAsync<Response<SizeStockModel>>();

            return response ?? new Response<SizeStockModel>
            {
                Success = false,
                Message = "Empty response"
            };
        }
        catch (Exception ex)
        {
            return new Response<SizeStockModel>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

}
