using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.CustomerService;

public interface ICustomerService
{
    Task<Response<PaginatedResponse<CustomerModel>>> GetAll(
        string? search, int page, int pageSize, string? status = null);

    Task<Response<CustomerModel>> GetById(Guid customerId);

    Task<Response<List<CustomerNoteModel>>> GetNotes(Guid customerId);

    Task<Response<CustomerNoteModel>> AddNote(
        Guid customerId, CreateCustomerNoteModel request);

    Task<Response<CustomerModel>> Suspend(
        Guid customerId, SuspendCustomerModel request);

    Task<Response<CustomerModel>> Reactivate(
        Guid customerId, ReactivateCustomerModel? request = null);
}
