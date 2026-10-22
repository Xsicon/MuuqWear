using System.Security.Claims;
using MuuqWear.Application.Services.CustomerService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreCustomerService : ICustomerService
{
    private readonly AdminFirestore _store;
    private readonly IHttpContextAccessor _http;

    public FirestoreCustomerService(AdminFirestore store, IHttpContextAccessor http)
    {
        _store = store;
        _http = http;
    }

    public async Task<Response<PaginatedResponse<CustomerModel>>> GetAll(
        string? search, int page, int pageSize, string? status = null) =>
        Response<PaginatedResponse<CustomerModel>>.SuccessResponse(
            await _store.ListCustomersAsync(search, page, pageSize, status));

    public async Task<Response<CustomerModel>> GetById(Guid customerId)
    {
        var customer = await _store.GetCustomerAsync(customerId);
        return customer is null ? Response<CustomerModel>.Fail("Customer not found") : Response<CustomerModel>.SuccessResponse(customer);
    }

    public async Task<Response<List<CustomerNoteModel>>> GetNotes(Guid customerId) =>
        Response<List<CustomerNoteModel>>.SuccessResponse(await _store.GetNotesAsync(customerId));

    public async Task<Response<CustomerNoteModel>> AddNote(Guid customerId, CreateCustomerNoteModel request)
    {
        var user = _http.HttpContext?.User;
        var note = await _store.AddNoteAsync(
            customerId,
            request.Body,
            user?.FindFirst(ClaimTypes.Name)?.Value ?? user?.FindFirst(ClaimTypes.Email)?.Value ?? "staff",
            user?.FindFirst(ClaimTypes.Role)?.Value);
        return Response<CustomerNoteModel>.SuccessResponse(note);
    }

    public async Task<Response<CustomerModel>> Suspend(Guid customerId, SuspendCustomerModel request)
    {
        var customer = await _store.SuspendAsync(customerId, request.DurationDays, request.Reason);
        return customer is null ? Response<CustomerModel>.Fail("Customer not found") : Response<CustomerModel>.SuccessResponse(customer);
    }

    public async Task<Response<CustomerModel>> Reactivate(Guid customerId, ReactivateCustomerModel? request = null)
    {
        var customer = await _store.ReactivateAsync(customerId);
        return customer is null ? Response<CustomerModel>.Fail("Customer not found") : Response<CustomerModel>.SuccessResponse(customer);
    }
}
