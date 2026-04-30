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
}
