using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MuuqWear.Application.Services.CategoryService;
public class CategoryService : ICategoryService
{
    private readonly HttpClient _http;

    public CategoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<List<CategoryModel>>> GetAll()
    {
        var result = await _http
            .GetFromJsonAsync<Response<List<CategoryModel>>>("api/Category/all");
        return result ?? new Response<List<CategoryModel>>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }

    public async Task<Response<CategoryModel>> Add(AddCategoryModel request)
    {
        var result = await _http.PostAsJsonAsync("api/Category/add", request);
        if (!result.IsSuccessStatusCode)
        {
            return new Response<CategoryModel>
            {
                Success = false,
                Message = $"Server error: {result.StatusCode}"
            };
        }
        var response = await result.Content.ReadFromJsonAsync<Response<CategoryModel>>();
        return response ?? new Response<CategoryModel>
        {
            Success = false,
            Message = "Empty response from server"
        };
    }
}