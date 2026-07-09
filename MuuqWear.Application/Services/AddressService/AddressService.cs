using MuuqWear.Application.Shared;
using MuuqWear.Model.Address;
using System.Net.Http.Json;

namespace MuuqWear.Application.Services.AddressService;

public class AddressService : IAddressService
{
    private readonly HttpClient _http;

    public AddressService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Response<List<AddressModel>>> GetAll()
    {
        try
        {
            var result = await _http.GetAsync("api/Address");
            return await HttpResponseReader.ReadAsync<List<AddressModel>>(result);
        }
        catch (Exception ex)
        {
            return Response<List<AddressModel>>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<AddressModel>> GetById(Guid id)
    {
        try
        {
            var result = await _http.GetAsync($"api/Address/{id}");
            return await HttpResponseReader.ReadAsync<AddressModel>(result);
        }
        catch (Exception ex)
        {
            return Response<AddressModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<AddressModel>> Create(CreateAddressModel request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync("api/Address", request);
            return await HttpResponseReader.ReadAsync<AddressModel>(result);
        }
        catch (Exception ex)
        {
            return Response<AddressModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<AddressModel>> Update(Guid id, UpdateAddressModel request)
    {
        try
        {
            var result = await _http.PutAsJsonAsync($"api/Address/{id}", request);
            return await HttpResponseReader.ReadAsync<AddressModel>(result);
        }
        catch (Exception ex)
        {
            return Response<AddressModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<bool>> Delete(Guid id)
    {
        try
        {
            var result = await _http.DeleteAsync($"api/Address/{id}");
            return await HttpResponseReader.ReadAsync<bool>(result);
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail(HttpResponseReader.FromException(ex));
        }
    }

    public async Task<Response<AddressModel>> SetDefault(Guid id)
    {
        try
        {
            var result = await _http.PatchAsync(
                $"api/Address/{id}/set-default", null);
            return await HttpResponseReader.ReadAsync<AddressModel>(result);
        }
        catch (Exception ex)
        {
            return Response<AddressModel>.Fail(HttpResponseReader.FromException(ex));
        }
    }
}