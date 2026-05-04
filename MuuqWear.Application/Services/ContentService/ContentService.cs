using Microsoft.AspNetCore.Http;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.ContentService;

public class ContentService : IContentService
{
    private readonly HttpClient _http;

    public ContentService(HttpClient http)
    {
        _http = http;
    }

    // =============================================
    // GET ALL
    // =============================================
    public async Task<Response<List<ContentItemModel>>> GetAll(ContentCategory type)
    {
        try
        {
            var result = await _http.GetAsync($"api/Content/{type}");
            return await ReadResponse<List<ContentItemModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<ContentItemModel>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET BY ID
    // =============================================
    public async Task<Response<ContentItemModel>> GetById(ContentCategory type, Guid id)
    {
        try
        {
            var result = await _http.GetAsync($"api/Content/{type}/{id}");
            return await ReadResponse<ContentItemModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ContentItemModel>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // CREATE
    // =============================================
    public async Task<Response<ContentItemModel>> Create(
        ContentCategory type, CreateContentItemModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync($"api/Content/{type}", request);
            return await ReadResponse<ContentItemModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ContentItemModel>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE
    // =============================================
    public async Task<Response<ContentItemModel>> Update(
        ContentCategory type, Guid id, UpdateContentItemModel request)
    {
        try
        {
            var result = await _http.PutAsJsonAsync($"api/Content/{type}/{id}", request);
            return await ReadResponse<ContentItemModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ContentItemModel>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // DELETE
    // =============================================
    public async Task<Response<bool>> Delete(ContentCategory type, Guid id)
    {
        try
        {
            var result = await _http.DeleteAsync($"api/Content/{type}/{id}");
            return await ReadResponse<bool>(result);
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // PUBLISH
    // =============================================
    public async Task<Response<ContentItemModel>> Publish(ContentCategory type, Guid id)
    {
        try
        {
            var result = await _http.PatchAsync(
                $"api/Content/{type}/{id}/publish", null);
            return await ReadResponse<ContentItemModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ContentItemModel>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UNPUBLISH
    // =============================================
    public async Task<Response<ContentItemModel>> Unpublish(ContentCategory type, Guid id)
    {
        try
        {
            var result = await _http.PatchAsync(
                $"api/Content/{type}/{id}/unpublish", null);
            return await ReadResponse<ContentItemModel>(result);
        }
        catch (Exception ex)
        {
            return Response<ContentItemModel>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // HELPER
    // =============================================
    private async Task<Response<T>> ReadResponse<T>(HttpResponseMessage response)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<Response<T>>();
            return result ?? Response<T>.Fail("Empty response");
        }
        catch
        {
            return Response<T>.Fail("Failed to parse response");
        }
    }

    // =============================================
    // UPLOAD IMAGE
    // =============================================
    public async Task<Response<string>> UploadImage(
        string fileName, byte[] bytes, string contentType)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var result = await _http.PostAsync("api/Content/upload-image", content);
            return await ReadResponse<string>(result);
        }
        catch (Exception ex)
        {
            return Response<string>.Fail("Error: " + ex.Message);
        }
    }
}
