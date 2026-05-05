using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.CustomerService;

public interface ICustomerService
{
    Task<Response<PaginatedResponse<CustomerModel>>> GetAll(
        string? search, int page, int pageSize);
}
