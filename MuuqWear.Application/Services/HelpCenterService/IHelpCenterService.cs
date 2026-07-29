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

    Task<Response<PaginatedResponse<HelpArticleModel>>> GetPublishedArticles(
        string? category, string? search, int page, int pageSize);
    Task<Response<HelpArticleModel>> GetPublishedArticleById(Guid articleId);

    Task<Response<PaginatedResponse<HelpArticleModel>>> GetAdminArticles(
        string? category, string? status, string? search, int page, int pageSize);
    Task<Response<HelpArticleModel>> GetAdminArticleById(Guid articleId);
    Task<Response<HelpArticleModel>> CreateArticle(SaveHelpArticleModel request);
    Task<Response<HelpArticleModel>> UpdateArticle(
        Guid articleId, SaveHelpArticleModel request);
    Task<Response<HelpArticleModel>> UpdateArticleStatus(
        Guid articleId, string status);
    Task<Response<bool>> DeleteArticle(Guid articleId);
}
