using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.ProductService;
public class ProductService : IProductService
{
    private readonly HttpClient _http;

    public ProductService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<List<ProductModel>>> GetAll()
    {
        var result = await _http.GetFromJsonAsync<Response<List<ProductModel>>>("api/Product/all");
        return result ?? new Response<List<ProductModel>>
        {
            Success = false,
            Message = "Empty response from server"
        };
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
}
