using MuuqWear.Application.Shared;
using MuuqWear.Model.Address;

namespace MuuqWear.Application.Services.AddressService;
public interface IAddressService
{
    Task<Response<List<AddressModel>>> GetAll();
    Task<Response<AddressModel>> GetById(Guid id);
    Task<Response<AddressModel>> Create(CreateAddressModel request);
    Task<Response<AddressModel>> Update(Guid id, UpdateAddressModel request);
    Task<Response<bool>> Delete(Guid id);
    Task<Response<AddressModel>> SetDefault(Guid id);
}
