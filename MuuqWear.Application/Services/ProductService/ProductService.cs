using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using MuuqWear.Model.Shared;
using System.Net;
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
                Message = HttpResponseReader.FromException(ex)
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
                Message = HttpResponseReader.FromException(ex)
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
                Message = HttpResponseReader.FromException(ex)
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
                Message = HttpResponseReader.FromException(ex)
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
                Message = HttpResponseReader.FromException(ex)
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
                Message = HttpResponseReader.FromException(ex)
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
                Message = HttpResponseReader.FromException(ex)
            };
        }
    }

    public async Task<Response<ProductModel>> UpdateStock(Guid productId, int totalStock)
    {
        try
        {
            if (totalStock < 0)
            {
                return new Response<ProductModel>
                {
                    Success = false,
                    Message = "Quantity cannot be negative."
                };
            }

            var request = await BuildAggregateStockBatchRequestAsync(productId, totalStock);
            if (request == null)
            {
                return new Response<ProductModel>
                {
                    Success = false,
                    Message = "Product has per-size stock; use size stock batch update instead."
                };
            }

            var batchResult = await UpdateSizeStockBatch(productId, request);

            if (!batchResult.Success
                && request.Upserts.Count > 0
                && request.Items.Count == 0)
            {
                var refreshed = await GetSizeStock(productId);
                if (refreshed.Success && refreshed.Data is { Count: > 0 } sizes)
                {
                    var retryRequest = BuildAggregateStockBatchRequestFromSizes(sizes, totalStock);
                    if (retryRequest != null)
                        batchResult = await UpdateSizeStockBatch(productId, retryRequest);
                }
            }

            if (!batchResult.Success || batchResult.Data?.SizeStock == null)
            {
                return new Response<ProductModel>
                {
                    Success = false,
                    Message = batchResult.Message ?? "Failed to update stock"
                };
            }

            var productResult = await GetById(productId);
            if (productResult.Success && productResult.Data != null)
            {
                productResult.Data.SizeStock = batchResult.Data.SizeStock;
                productResult.Data.Stock = batchResult.Data.TotalStock;
                return productResult;
            }

            return new Response<ProductModel>
            {
                Success = true,
                Message = batchResult.Message,
                Data = new ProductModel
                {
                    Id = productId,
                    Stock = batchResult.Data.TotalStock,
                    SizeStock = batchResult.Data.SizeStock
                }
            };
        }
        catch (Exception ex)
        {
            return new Response<ProductModel>
            {
                Success = false,
                Message = HttpResponseReader.FromException(ex)
            };
        }
    }

    private async Task<BatchUpdateSizeStockRequest?> BuildAggregateStockBatchRequestAsync(
        Guid productId, int totalStock)
    {
        var sizeStockResult = await GetSizeStock(productId);
        if (!sizeStockResult.Success || sizeStockResult.Data == null)
        {
            return new BatchUpdateSizeStockRequest
            {
                Upserts =
                {
                    new BatchSizeStockUpsertItem
                    {
                        Size = ProductStockHelper.DefaultAggregateSize,
                        Quantity = totalStock
                    }
                }
            };
        }

        return BuildAggregateStockBatchRequestFromSizes(sizeStockResult.Data, totalStock);
    }

    private static BatchUpdateSizeStockRequest? BuildAggregateStockBatchRequestFromSizes(
        List<SizeStockModel> sizes,
        int totalStock)
    {
        if (sizes.Count == 0)
        {
            return new BatchUpdateSizeStockRequest
            {
                Upserts =
                {
                    new BatchSizeStockUpsertItem
                    {
                        Size = ProductStockHelper.DefaultAggregateSize,
                        Quantity = totalStock
                    }
                }
            };
        }

        if (sizes.Count > 1)
            return null;

        return new BatchUpdateSizeStockRequest
        {
            Items =
            {
                new BatchSizeStockUpdateItem
                {
                    SizeStockId = sizes[0].Id,
                    Quantity = totalStock
                }
            }
        };
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
                Message = HttpResponseReader.FromException(ex)
            };
        }
    }

    // =============================================
    // BATCH UPDATE SIZE STOCK (atomic)
    // =============================================
    public async Task<Response<BatchUpdateSizeStockResult>> UpdateSizeStockBatch(
        Guid productId, BatchUpdateSizeStockRequest request)
    {
        try
        {
            var result = await _http.PatchAsJsonAsync(
                $"api/Product/{productId}/size-stock/batch",
                request);

            if (result.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
                return await UpdateSizeStockSequentialAsync(productId, request);

            if (!result.IsSuccessStatusCode)
            {
                var message = $"Server error: {result.StatusCode}";
                try
                {
                    var error = await result.Content
                        .ReadFromJsonAsync<Response<BatchUpdateSizeStockResult>>();
                    if (!string.IsNullOrWhiteSpace(error?.Message))
                        message = error.Message;
                }
                catch
                {
                    // keep status-based message
                }

                return new Response<BatchUpdateSizeStockResult>
                {
                    Success = false,
                    Message = message
                };
            }

            var response = await result.Content
                .ReadFromJsonAsync<Response<BatchUpdateSizeStockResult>>();

            return response ?? new Response<BatchUpdateSizeStockResult>
            {
                Success = false,
                Message = "Empty response"
            };
        }
        catch (Exception ex)
        {
            return new Response<BatchUpdateSizeStockResult>
            {
                Success = false,
                Message = HttpResponseReader.FromException(ex)
            };
        }
    }

    /// <summary>
    /// Fallback when the batch endpoint is not deployed on the API yet.
    /// Uses the per-size PATCH/POST endpoints that existed before batch support.
    /// </summary>
    private async Task<Response<BatchUpdateSizeStockResult>> UpdateSizeStockSequentialAsync(
        Guid productId,
        BatchUpdateSizeStockRequest request)
    {
        if (request.Items.Count == 0 && request.Upserts.Count == 0)
        {
            return new Response<BatchUpdateSizeStockResult>
            {
                Success = false,
                Message = "No size stock rows to update."
            };
        }

        foreach (var item in request.Items)
        {
            if (item.SizeStockId == Guid.Empty)
            {
                return new Response<BatchUpdateSizeStockResult>
                {
                    Success = false,
                    Message = "Missing size stock id. Close the modal and try again."
                };
            }

            var updateResult = await UpdateSizeStock(item.SizeStockId, item.Quantity);
            if (!updateResult.Success)
            {
                return new Response<BatchUpdateSizeStockResult>
                {
                    Success = false,
                    Message = updateResult.Message ?? "Failed to update stock"
                };
            }
        }

        foreach (var upsert in request.Upserts)
        {
            var addResult = await AddSizeStock(productId, upsert.Size, upsert.Quantity);
            if (!addResult.Success)
            {
                return new Response<BatchUpdateSizeStockResult>
                {
                    Success = false,
                    Message = addResult.Message ?? "Failed to add size stock"
                };
            }
        }

        var refreshed = await GetSizeStock(productId);
        if (!refreshed.Success || refreshed.Data == null)
        {
            return new Response<BatchUpdateSizeStockResult>
            {
                Success = false,
                Message = refreshed.Message ?? "Stock updated but could not refresh sizes."
            };
        }

        return new Response<BatchUpdateSizeStockResult>
        {
            Success = true,
            Data = new BatchUpdateSizeStockResult
            {
                SizeStock = refreshed.Data,
                TotalStock = refreshed.Data.Sum(s => s.Quantity)
            }
        };
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
                Message = HttpResponseReader.FromException(ex)
            };
        }
    }

}
