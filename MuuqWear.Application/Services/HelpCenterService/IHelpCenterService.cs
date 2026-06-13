using MuuqWear.Application.Shared;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Model.Shared;

namespace MuuqWear.Application.Services.HelpCenterService;
public interface IHelpCenterService
{
    Task<Response<SupportTicketModel>> SubmitTicket(SubmitTicketModel request);
    Task<Response<PaginatedResponse<SupportTicketModel>>> GetAllTickets(
        string? status, int page, int pageSize);
    Task<Response<SupportTicketModel>> GetTicketById(Guid ticketId);
    Task<Response<SupportTicketModel>> UpdateTicketStatus(
        Guid ticketId, string status);
    Task<Response<TicketStatsModel>> GetStats();
}