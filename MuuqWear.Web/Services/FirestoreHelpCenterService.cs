using Google.Cloud.Firestore;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed class FirestoreHelpCenterService : IHelpCenterService
{
    private readonly FirestoreDb _db;

    public FirestoreHelpCenterService(IConfiguration config) =>
        _db = FirestoreDb.Create(config["Firebase:ProjectId"] ?? "muuqwear-6e2e0");

    public async Task<Response<SupportTicketModel>> SubmitTicket(SubmitTicketModel request)
    {
        var id = Guid.NewGuid();
        var ticket = new SupportTicketModel
        {
            Id = id,
            TicketNumber = id.ToString("N")[..8].ToUpperInvariant(),
            Name = request.Name,
            Email = request.Email,
            Category = request.Category,
            Subject = request.Subject,
            Message = request.Message,
            Status = "open",
            Priority = "normal",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await Tickets().Document(id.ToString()).SetAsync(TicketFields(ticket));
        return Response<SupportTicketModel>.SuccessResponse(ticket);
    }

    public async Task<Response<PaginatedResponse<SupportTicketModel>>> GetAllTickets(
        string? status, int page, int pageSize)
    {
        var all = (await Tickets().GetSnapshotAsync()).Documents.Select(ToTicket).ToList();
        if (!string.IsNullOrWhiteSpace(status))
            all = all.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        return Response<PaginatedResponse<SupportTicketModel>>.SuccessResponse(Page(all, page, pageSize));
    }

    public async Task<Response<SupportTicketModel>> GetTicketById(Guid ticketId)
    {
        var snap = await Tickets().Document(ticketId.ToString()).GetSnapshotAsync();
        return snap.Exists
            ? Response<SupportTicketModel>.SuccessResponse(await WithReplies(ToTicket(snap)))
            : Response<SupportTicketModel>.Fail("Ticket not found");
    }

    public async Task<Response<SupportTicketModel>> UpdateTicketStatus(Guid ticketId, string status)
    {
        await Tickets().Document(ticketId.ToString()).UpdateAsync(new Dictionary<string, object>
        {
            ["status"] = status,
            ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
        });
        return await GetTicketById(ticketId);
    }

    public async Task<Response<SupportTicketModel>> UpdateTicket(Guid ticketId, UpdateTicketModel request)
    {
        var fields = new Dictionary<string, object> { ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow) };
        if (!string.IsNullOrWhiteSpace(request.Status)) fields["status"] = request.Status;
        if (!string.IsNullOrWhiteSpace(request.Priority)) fields["priority"] = request.Priority;
        if (!string.IsNullOrWhiteSpace(request.Team)) fields["team"] = request.Team;
        if (!string.IsNullOrWhiteSpace(request.AssignedToName)) fields["assignedToName"] = request.AssignedToName;
        await Tickets().Document(ticketId.ToString()).UpdateAsync(fields);
        return await GetTicketById(ticketId);
    }

    public Task<Response<SupportTicketModel>> AssignTicketToMe(Guid ticketId) =>
        UpdateTicket(ticketId, new UpdateTicketModel { AssignedToName = "Me", Status = "in_progress" });

    public async Task<Response<SupportTicketReplyModel>> AddTicketReply(Guid ticketId, string message)
    {
        var reply = new SupportTicketReplyModel
        {
            Id = Guid.NewGuid(),
            SenderType = "agent",
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        await Tickets().Document(ticketId.ToString()).Collection("replies")
            .Document(reply.Id.ToString()).SetAsync(new Dictionary<string, object>
            {
                ["senderType"] = reply.SenderType,
                ["message"] = reply.Message,
                ["createdAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        return Response<SupportTicketReplyModel>.SuccessResponse(reply);
    }

    public async Task<Response<TicketStatsModel>> GetStats()
    {
        var all = (await Tickets().GetSnapshotAsync()).Documents.Select(ToTicket).ToList();
        return Response<TicketStatsModel>.SuccessResponse(new TicketStatsModel
        {
            TotalCount = all.Count,
            OpenCount = all.Count(t => t.Status == "open"),
            InProgressCount = all.Count(t => t.Status is "in_progress" or "in-progress")
        });
    }

    public async Task<Response<PaginatedResponse<HelpArticleModel>>> GetPublishedArticles(
        string? category, string? search, int page, int pageSize)
    {
        var all = (await Articles().GetSnapshotAsync()).Documents.Select(ToArticle)
            .Where(a => string.Equals(a.Status, "Published", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(a.Status, "published", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!string.IsNullOrWhiteSpace(category))
            all = all.Where(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(a => (a.Title + a.Content).Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return Response<PaginatedResponse<HelpArticleModel>>.SuccessResponse(Page(all, page, pageSize));
    }

    public async Task<Response<HelpArticleModel>> GetPublishedArticleById(Guid articleId)
    {
        var snap = await Articles().Document(articleId.ToString()).GetSnapshotAsync();
        return snap.Exists
            ? Response<HelpArticleModel>.SuccessResponse(ToArticle(snap))
            : Response<HelpArticleModel>.Fail("Article not found");
    }

    public async Task<Response<PaginatedResponse<HelpArticleModel>>> GetAdminArticles(
        string? category, string? status, string? search, int page, int pageSize)
    {
        var all = (await Articles().GetSnapshotAsync()).Documents.Select(ToArticle).ToList();
        if (!string.IsNullOrWhiteSpace(category))
            all = all.Where(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(status))
            all = all.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(a => (a.Title + a.Content).Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return Response<PaginatedResponse<HelpArticleModel>>.SuccessResponse(Page(all, page, pageSize));
    }

    public Task<Response<HelpArticleModel>> GetAdminArticleById(Guid articleId) =>
        GetPublishedArticleById(articleId);

    public async Task<Response<HelpArticleModel>> CreateArticle(SaveHelpArticleModel request)
    {
        var id = Guid.NewGuid();
        await Articles().Document(id.ToString()).SetAsync(ArticleFields(request, DateTime.UtcNow));
        return await GetPublishedArticleById(id);
    }

    public async Task<Response<HelpArticleModel>> UpdateArticle(Guid articleId, SaveHelpArticleModel request)
    {
        await Articles().Document(articleId.ToString()).SetAsync(ArticleFields(request, DateTime.UtcNow), SetOptions.MergeAll);
        return await GetPublishedArticleById(articleId);
    }

    public async Task<Response<HelpArticleModel>> UpdateArticleStatus(Guid articleId, string status)
    {
        await Articles().Document(articleId.ToString()).UpdateAsync("status", status);
        return await GetPublishedArticleById(articleId);
    }

    public async Task<Response<bool>> DeleteArticle(Guid articleId)
    {
        await Articles().Document(articleId.ToString()).DeleteAsync();
        return Response<bool>.SuccessResponse(true);
    }

    public Task<Response<string>> UploadImage(string fileName, byte[] bytes, string contentType) =>
        Task.FromResult(Response<string>.Fail("Upload images from the product editor for now."));

    public async Task<Response<HelpArticleCommentModel>> AddArticleComment(Guid articleId, string body)
    {
        var comment = new HelpArticleCommentModel { Body = body, CreatedAt = DateTime.UtcNow };
        await Articles().Document(articleId.ToString()).Collection("comments")
            .Document(comment.Id.ToString()).SetAsync(new Dictionary<string, object>
            {
                ["body"] = body,
                ["createdAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        return Response<HelpArticleCommentModel>.SuccessResponse(comment);
    }

    public Task<Response<HelpArticleEngagementModel>> SetArticleVote(Guid articleId, string vote) =>
        Task.FromResult(Response<HelpArticleEngagementModel>.SuccessResponse(new HelpArticleEngagementModel { MyVote = vote }));

    private CollectionReference Tickets() => _db.Collection("supportTickets");
    private CollectionReference Articles() => _db.Collection("helpArticles");

    private async Task<SupportTicketModel> WithReplies(SupportTicketModel ticket)
    {
        var snap = await Tickets().Document(ticket.Id.ToString()).Collection("replies").GetSnapshotAsync();
        ticket.Replies = snap.Documents.Select(d => new SupportTicketReplyModel
        {
            Id = Guid.TryParse(d.Id, out var id) ? id : Guid.Empty,
            SenderType = d.ContainsField("senderType") ? d.GetValue<string>("senderType") : "agent",
            Message = d.ContainsField("message") ? d.GetValue<string>("message") : "",
            CreatedAt = d.ContainsField("createdAt") ? d.GetValue<Timestamp>("createdAt").ToDateTime() : DateTime.UtcNow
        }).ToList();
        ticket.ReplyCount = ticket.Replies.Count;
        return ticket;
    }

    private static Dictionary<string, object> TicketFields(SupportTicketModel ticket) => new()
    {
        ["ticketNumber"] = ticket.TicketNumber,
        ["name"] = ticket.Name,
        ["email"] = ticket.Email,
        ["category"] = ticket.Category,
        ["subject"] = ticket.Subject,
        ["message"] = ticket.Message,
        ["status"] = ticket.Status,
        ["priority"] = ticket.Priority,
        ["createdAt"] = Timestamp.FromDateTime(ticket.CreatedAt ?? DateTime.UtcNow),
        ["updatedAt"] = Timestamp.FromDateTime(ticket.UpdatedAt ?? DateTime.UtcNow)
    };

    private static Dictionary<string, object> ArticleFields(SaveHelpArticleModel request, DateTime now) => new()
    {
        ["title"] = request.Title,
        ["category"] = request.Category,
        ["status"] = request.Status,
        ["content"] = request.Content,
        ["heroImageUrl"] = request.HeroImageUrl ?? "",
        ["updatedAt"] = Timestamp.FromDateTime(now)
    };

    private static SupportTicketModel ToTicket(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new SupportTicketModel
        {
            Id = id,
            TicketNumber = Str(doc, "ticketNumber"),
            Name = Str(doc, "name"),
            Email = Str(doc, "email"),
            Category = Str(doc, "category"),
            Subject = Str(doc, "subject"),
            Message = Str(doc, "message"),
            Status = Str(doc, "status"),
            Priority = Str(doc, "priority"),
            AssignedToName = Str(doc, "assignedToName"),
            Team = Str(doc, "team")
        };
    }

    private static HelpArticleModel ToArticle(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new HelpArticleModel
        {
            Id = id,
            Title = Str(doc, "title"),
            Category = string.IsNullOrWhiteSpace(Str(doc, "category")) ? "Orders" : Str(doc, "category"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "Draft" : Str(doc, "status"),
            Content = Str(doc, "content"),
            HeroImageUrl = Str(doc, "heroImageUrl")
        };
    }

    private static string Str(DocumentSnapshot doc, string field) =>
        doc.ContainsField(field) ? doc.GetValue<string>(field) ?? "" : "";

    private static PaginatedResponse<T> Page<T>(List<T> all, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        var total = all.Count;
        return new PaginatedResponse<T>
        {
            Data = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
