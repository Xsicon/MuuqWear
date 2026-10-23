using Google.Cloud.Firestore;
using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Model.Chat;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.JobApplication;
using MuuqWear.Model.JobPosting;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Refund;
using MuuqWear.Model.Shared;

namespace MuuqWear.Web.Services;

public sealed partial class AdminFirestore
{
    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.Collection("products").Limit(1).GetSnapshotAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> CountCustomersAsync(CancellationToken cancellationToken = default) =>
        (await ListCustomersAsync(null, 1, 5000, null, cancellationToken)).TotalCount;

    public async Task<PaginatedResponse<OrderReturnModel>> ListReturnsAsync(
        string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("orderReturns").GetSnapshotAsync(cancellationToken);
        IEnumerable<OrderReturnModel> query = snap.Documents.Select(ToReturn);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase));
        return Page(query.OrderByDescending(r => r.CreatedAt).ToList(), page, pageSize);
    }

    public async Task<OrderReturnModel> SubmitReturnAsync(SubmitReturnModel request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var model = new OrderReturnModel
        {
            Id = id,
            ReturnNumber = id.ToString("N")[..8].ToUpperInvariant(),
            OrderNumber = request.OrderNumber,
            Email = request.Email,
            FullName = request.FullName,
            ItemsToReturn = request.ItemsToReturn,
            Reason = request.Reason,
            Comments = request.Comments,
            Status = "pending",
            CreatedAt = now
        };
        await _db.Collection("orderReturns").Document(id.ToString()).SetAsync(ToReturnFields(model), cancellationToken: cancellationToken);
        return model;
    }

    public async Task<OrderReturnModel?> UpdateReturnStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Collection("orderReturns").Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
            return null;
        await doc.Reference.UpdateAsync("status", status, cancellationToken: cancellationToken);
        return ToReturn(await doc.Reference.GetSnapshotAsync(cancellationToken));
    }

    public async Task<PaginatedResponse<RefundModel>> ListRefundsAsync(
        string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("refunds").GetSnapshotAsync(cancellationToken);
        IEnumerable<RefundModel> query = snap.Documents.Select(ToRefund);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase));
        return Page(query.OrderByDescending(r => r.CreatedAt).ToList(), page, pageSize);
    }

    public async Task<RefundModel?> ProcessRefundAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Collection("refunds").Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
            return null;
        await doc.Reference.UpdateAsync(new Dictionary<string, object>
        {
            ["status"] = "completed",
            ["processedAt"] = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
        return ToRefund(await doc.Reference.GetSnapshotAsync(cancellationToken));
    }

    public async Task<List<AffiliateApplicationModel>> ListAffiliateApplicationsAsync(
        string? status, CancellationToken cancellationToken = default)
    {
        var snap = await SnapshotAsync("affiliateApplications", _affiliates, cancellationToken);
        IEnumerable<AffiliateApplicationModel> query = snap.Documents.Select(ToAffiliate);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase));
        return query.OrderByDescending(a => a.SubmittedAt).ToList();
    }

    public async Task<AffiliateApplicationModel> SubmitAffiliateAsync(
        SubmitAffiliateApplicationModel request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var model = new AffiliateApplicationModel
        {
            Id = id,
            FullName = request.FullName ?? "",
            Email = request.Email ?? "",
            ContentNiche = request.ContentNiche,
            PortfolioUrl = request.PortfolioUrl,
            WhyMuuqwear = request.WhyMuuqwear,
            SampleFiles = request.SampleFiles,
            SocialHandles = request.SocialHandles,
            AudienceSize = request.AudienceSize,
            Status = "pending",
            SubmittedAt = DateTime.UtcNow
        };
        await _db.Collection("affiliateApplications").Document(id.ToString())
            .SetAsync(ToAffiliateFields(model), cancellationToken: cancellationToken);
        return model;
    }

    public async Task<AffiliateApplicationModel?> UpdateAffiliateStatusAsync(
        Guid id, string status, string? notes, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Collection("affiliateApplications").Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
            return null;
        await doc.Reference.UpdateAsync(new Dictionary<string, object>
        {
            ["status"] = status,
            ["adminNotes"] = notes ?? "",
            ["reviewedAt"] = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
        return ToAffiliate(await doc.Reference.GetSnapshotAsync(cancellationToken));
    }

    public async Task<List<ChatSessionModel>> ListChatSessionsAsync(CancellationToken cancellationToken = default)
    {
        var snap = await SnapshotAsync("chatSessions", _chats, cancellationToken);
        return snap.Documents.Select(ToChatSession)
            .OrderByDescending(s => s.LastActivity)
            .ToList();
    }

    public async Task<List<ChatMessageModel>> ListChatMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("chatSessions").Document(sessionId.ToString())
            .Collection("messages").GetSnapshotAsync(cancellationToken);
        return snap.Documents.Select(ToChatMessage).OrderBy(m => m.CreatedAt).ToList();
    }

    public async Task<ChatMessageModel> SendChatMessageAsync(
        SendMessageRequest request, string senderType, string senderName, CancellationToken cancellationToken = default)
    {
        var sessionId = request.SessionId is Guid existing && existing != Guid.Empty ? existing : Guid.NewGuid();
        var sessionRef = _db.Collection("chatSessions").Document(sessionId.ToString());
        var session = await sessionRef.GetSnapshotAsync(cancellationToken);
        if (!session.Exists)
        {
            await sessionRef.SetAsync(new Dictionary<string, object>
            {
                ["customerName"] = request.GuestName ?? senderName,
                ["customerEmail"] = request.GuestEmail ?? "",
                ["status"] = "open",
                ["createdAt"] = DateTime.UtcNow,
                ["lastActivity"] = DateTime.UtcNow,
                ["lastMessagePreview"] = request.Message,
                ["lastMessageSender"] = senderType,
                ["messageCount"] = 0
            }, cancellationToken: cancellationToken);
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await sessionRef.Collection("messages").Document(id.ToString()).SetAsync(new Dictionary<string, object>
        {
            ["sessionId"] = sessionId.ToString(),
            ["senderType"] = senderType,
            ["senderName"] = senderName,
            ["message"] = request.Message,
            ["createdAt"] = now,
            ["isRead"] = false
        }, cancellationToken: cancellationToken);
        await sessionRef.UpdateAsync(new Dictionary<string, object>
        {
            ["lastActivity"] = now,
            ["lastMessagePreview"] = request.Message,
            ["lastMessageSender"] = senderType
        }, cancellationToken: cancellationToken);

        return new ChatMessageModel
        {
            Id = id,
            SessionId = sessionId,
            SenderType = senderType,
            SenderName = senderName,
            Message = request.Message,
            CreatedAt = now
        };
    }

    public async Task<ChatSessionModel?> GetChatSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Collection("chatSessions").Document(sessionId.ToString()).GetSnapshotAsync(cancellationToken);
        return doc.Exists ? ToChatSession(doc) : null;
    }

    public async Task CloseChatSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        await _db.Collection("chatSessions").Document(sessionId.ToString())
            .UpdateAsync("status", "closed", cancellationToken: cancellationToken);

    public async Task<List<ContentItemModel>> ListContentAsync(ContentCategory type, CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("content").GetSnapshotAsync(cancellationToken);
        return snap.Documents.Select(ToContent)
            .Where(c => string.Equals(c.Category, type.ToString(), StringComparison.OrdinalIgnoreCase)
                        || (type == ContentCategory.JournalArticles && string.IsNullOrWhiteSpace(c.Category)))
            .Where(c => !AdminDirectoryFilter.IsNoiseArticle(c.Title))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }

    public async Task<ContentItemModel> UpsertContentAsync(
        ContentCategory type, ContentItemModel item, CancellationToken cancellationToken = default)
    {
        if (item.Id == Guid.Empty)
            item.Id = Guid.NewGuid();
        item.Category = type.ToString();
        item.CreatedAt ??= DateTime.UtcNow;
        await _db.Collection("content").Document(item.Id.ToString()).SetAsync(ToContentFields(item), cancellationToken: cancellationToken);
        return item;
    }

    public async Task<int> IncrementContentViewsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doc = _db.Collection("content").Document(id.ToString());
        try
        {
            await doc.UpdateAsync("views", FieldValue.Increment(1), cancellationToken: cancellationToken);
            var snap = await doc.GetSnapshotAsync(cancellationToken);
            return snap.Exists ? Int(snap, "views") : 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<bool> DeleteContentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Collection("content").Document(id.ToString()).DeleteAsync(cancellationToken: cancellationToken);
        return true;
    }

    public async Task<List<JobPostingModel>> ListJobPostingsAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("jobPostings").GetSnapshotAsync(cancellationToken);
        var apps = await _db.Collection("jobApplications").GetSnapshotAsync(cancellationToken);
        var counts = apps.Documents
            .GroupBy(d => Str(d, "jobId"))
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        return snap.Documents.Select(d =>
        {
            var job = ToJob(d);
            job.ApplicationCount = counts.GetValueOrDefault(d.Id);
            return job;
        }).OrderByDescending(j => j.CreatedAt).ToList();
    }

    public async Task<JobPostingModel> UpsertJobPostingAsync(JobPostingModel job, CancellationToken cancellationToken = default)
    {
        if (job.Id == Guid.Empty)
            job.Id = Guid.NewGuid();
        job.CreatedAt ??= DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(job.Slug))
            job.Slug = job.Id.ToString("N")[..8];
        await _db.Collection("jobPostings").Document(job.Id.ToString())
            .SetAsync(ToJobFields(job), cancellationToken: cancellationToken);
        return job;
    }

    public async Task DeleteJobPostingAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Collection("jobPostings").Document(id.ToString()).DeleteAsync(cancellationToken: cancellationToken);

    public async Task<List<JobApplicationModel>> ListJobApplicationsAsync(Guid? jobId, CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("jobApplications").GetSnapshotAsync(cancellationToken);
        IEnumerable<JobApplicationModel> query = snap.Documents.Select(ToJobApplication);
        if (jobId is Guid filter && filter != Guid.Empty)
            query = query.Where(a => a.JobId == filter);
        return query.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<JobApplicationModel> UpsertJobApplicationAsync(JobApplicationModel app, CancellationToken cancellationToken = default)
    {
        if (app.Id == Guid.Empty)
            app.Id = Guid.NewGuid();
        app.CreatedAt ??= DateTime.UtcNow;
        app.UpdatedAt = DateTime.UtcNow;
        await _db.Collection("jobApplications").Document(app.Id.ToString())
            .SetAsync(ToJobApplicationFields(app), cancellationToken: cancellationToken);
        return app;
    }

    public async Task DeleteJobApplicationAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Collection("jobApplications").Document(id.ToString()).DeleteAsync(cancellationToken: cancellationToken);

    private static JobPostingModel ToJob(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new JobPostingModel
        {
            Id = id,
            Slug = Str(doc, "slug"),
            Title = Str(doc, "title"),
            Department = Str(doc, "department"),
            Location = Str(doc, "location"),
            Type = Str(doc, "type"),
            Description = Str(doc, "description"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "open" : Str(doc, "status"),
            CreatedAt = ToDate(doc, "createdAt"),
            OpenedAt = ToDate(doc, "openedAt"),
            ClosedAt = ToDate(doc, "closedAt")
        };
    }

    private static Dictionary<string, object> ToJobFields(JobPostingModel job)
    {
        var fields = new Dictionary<string, object>
        {
            ["slug"] = job.Slug,
            ["title"] = job.Title,
            ["department"] = job.Department,
            ["location"] = job.Location,
            ["type"] = job.Type,
            ["description"] = job.Description ?? "",
            ["status"] = job.Status,
            ["createdAt"] = job.CreatedAt ?? DateTime.UtcNow,
            ["openedAt"] = job.OpenedAt ?? job.CreatedAt ?? DateTime.UtcNow
        };
        if (job.ClosedAt.HasValue)
            fields["closedAt"] = job.ClosedAt.Value;
        return fields;
    }

    private static JobApplicationModel ToJobApplication(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        Guid.TryParse(Str(doc, "jobId"), out var jobId);
        return new JobApplicationModel
        {
            Id = id,
            JobId = jobId,
            Name = Str(doc, "name"),
            Email = Str(doc, "email"),
            PortfolioUrl = Str(doc, "portfolioUrl"),
            ResumeUrl = Str(doc, "resumeUrl"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "new" : Str(doc, "status"),
            Notes = Str(doc, "notes"),
            CreatedAt = ToDate(doc, "createdAt"),
            UpdatedAt = ToDate(doc, "updatedAt")
        };
    }

    private static Dictionary<string, object> ToJobApplicationFields(JobApplicationModel app) => new()
    {
        ["jobId"] = app.JobId.ToString(),
        ["name"] = app.Name,
        ["email"] = app.Email,
        ["portfolioUrl"] = app.PortfolioUrl ?? "",
        ["resumeUrl"] = app.ResumeUrl,
        ["status"] = app.Status,
        ["notes"] = app.Notes ?? "",
        ["createdAt"] = app.CreatedAt ?? DateTime.UtcNow,
        ["updatedAt"] = app.UpdatedAt ?? DateTime.UtcNow
    };

    private static OrderReturnModel ToReturn(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        Guid.TryParse(Str(doc, "orderId"), out var orderId);
        return new OrderReturnModel
        {
            Id = id,
            ReturnNumber = Str(doc, "returnNumber"),
            OrderId = orderId == Guid.Empty ? null : orderId,
            OrderNumber = Str(doc, "orderNumber"),
            Email = Str(doc, "email"),
            FullName = Str(doc, "fullName"),
            ItemsToReturn = Str(doc, "itemsToReturn"),
            Reason = Str(doc, "reason"),
            Comments = Str(doc, "comments"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "pending" : Str(doc, "status"),
            CreatedAt = ToDate(doc, "createdAt")
        };
    }

    private static Dictionary<string, object> ToReturnFields(OrderReturnModel model) => new()
    {
        ["returnNumber"] = model.ReturnNumber,
        ["orderNumber"] = model.OrderNumber,
        ["email"] = model.Email,
        ["fullName"] = model.FullName,
        ["itemsToReturn"] = model.ItemsToReturn,
        ["reason"] = model.Reason,
        ["comments"] = model.Comments ?? "",
        ["status"] = model.Status,
        ["createdAt"] = model.CreatedAt ?? DateTime.UtcNow
    };

    private static RefundModel ToRefund(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        Guid.TryParse(Str(doc, "orderId"), out var orderId);
        return new RefundModel
        {
            Id = id,
            RefundNumber = Str(doc, "refundNumber"),
            OrderId = orderId,
            OrderNumber = Str(doc, "orderNumber"),
            Email = Str(doc, "email"),
            FullName = Str(doc, "fullName"),
            Amount = Cents(doc, "amount"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "pending" : Str(doc, "status"),
            CreatedAt = ToDate(doc, "createdAt"),
            ProcessedAt = ToDate(doc, "processedAt")
        };
    }

    private static AffiliateApplicationModel ToAffiliate(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new AffiliateApplicationModel
        {
            Id = id,
            FullName = Str(doc, "fullName"),
            Email = Str(doc, "email"),
            ContentNiche = Str(doc, "contentNiche"),
            PortfolioUrl = Str(doc, "portfolioUrl"),
            WhyMuuqwear = Str(doc, "whyMuuqwear"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "pending" : Str(doc, "status"),
            SubmittedAt = ToDate(doc, "submittedAt") ?? DateTime.UtcNow,
            AdminNotes = Str(doc, "adminNotes"),
            AudienceSize = int.TryParse(Str(doc, "audienceSize"), out var n) ? n : 0
        };
    }

    private static Dictionary<string, object> ToAffiliateFields(AffiliateApplicationModel model) => new()
    {
        ["fullName"] = model.FullName,
        ["email"] = model.Email,
        ["contentNiche"] = model.ContentNiche,
        ["portfolioUrl"] = model.PortfolioUrl ?? "",
        ["whyMuuqwear"] = model.WhyMuuqwear ?? "",
        ["status"] = model.Status,
        ["submittedAt"] = model.SubmittedAt,
        ["audienceSize"] = model.AudienceSize
    };

    private static ChatSessionModel ToChatSession(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new ChatSessionModel
        {
            Id = id,
            CustomerName = Str(doc, "customerName"),
            CustomerEmail = Str(doc, "customerEmail"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "open" : Str(doc, "status"),
            LastActivity = ToDate(doc, "lastActivity") ?? DateTime.UtcNow,
            LastMessagePreview = Str(doc, "lastMessagePreview"),
            LastMessageSender = Str(doc, "lastMessageSender"),
            CreatedAt = ToDate(doc, "createdAt") ?? DateTime.UtcNow
        };
    }

    private static ChatMessageModel ToChatMessage(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        Guid.TryParse(Str(doc, "sessionId"), out var sessionId);
        return new ChatMessageModel
        {
            Id = id,
            SessionId = sessionId,
            SenderType = Str(doc, "senderType"),
            SenderName = Str(doc, "senderName"),
            Message = Str(doc, "message"),
            CreatedAt = ToDate(doc, "createdAt") ?? DateTime.UtcNow
        };
    }

    private static ContentItemModel ToContent(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        Guid.TryParse(Str(doc, "productId"), out var productId);
        return new ContentItemModel
        {
            Id = id,
            Title = Str(doc, "title"),
            Content = Str(doc, "body"),
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "draft" : Str(doc, "status"),
            Category = Str(doc, "category"),
            ImageUrl = Str(doc, "imageUrl"),
            SecondImageUrl = EmptyToNull(Str(doc, "secondImageUrl")),
            Author = EmptyToNull(Str(doc, "author")),
            Excerpt = EmptyToNull(Str(doc, "excerpt")),
            Slug = EmptyToNull(Str(doc, "slug")),
            Designer = EmptyToNull(Str(doc, "designer")),
            Year = EmptyToNull(Str(doc, "year")),
            Collection = EmptyToNull(Str(doc, "collection")),
            Inspiration = EmptyToNull(Str(doc, "inspiration")),
            TechnicalFabric = EmptyToNull(Str(doc, "technicalFabric")),
            TechnicalTechniques = EmptyToNull(Str(doc, "technicalTechniques")),
            TechnicalProduction = EmptyToNull(Str(doc, "technicalProduction")),
            TechnicalAvailability = EmptyToNull(Str(doc, "technicalAvailability")),
            ProductId = productId == Guid.Empty ? null : productId,
            Views = Int(doc, "views"),
            CreatedAt = ToDate(doc, "createdAt"),
            PublishedAt = ToDate(doc, "publishedAt")
        };
    }

    private static Dictionary<string, object> ToContentFields(ContentItemModel item)
    {
        var fields = new Dictionary<string, object>
        {
            ["title"] = item.Title,
            ["body"] = item.Content ?? "",
            ["status"] = item.Status,
            ["category"] = item.Category ?? "",
            ["imageUrl"] = item.ImageUrl ?? "",
            ["secondImageUrl"] = item.SecondImageUrl ?? "",
            ["author"] = item.Author ?? "",
            ["excerpt"] = item.Excerpt ?? "",
            ["slug"] = item.Slug ?? "",
            ["designer"] = item.Designer ?? "",
            ["year"] = item.Year ?? "",
            ["collection"] = item.Collection ?? "",
            ["inspiration"] = item.Inspiration ?? "",
            ["technicalFabric"] = item.TechnicalFabric ?? "",
            ["technicalTechniques"] = item.TechnicalTechniques ?? "",
            ["technicalProduction"] = item.TechnicalProduction ?? "",
            ["technicalAvailability"] = item.TechnicalAvailability ?? "",
            ["views"] = item.Views,
            ["createdAt"] = item.CreatedAt ?? DateTime.UtcNow
        };
        if (item.ProductId is Guid productId && productId != Guid.Empty)
            fields["productId"] = productId.ToString();
        if (item.PublishedAt.HasValue)
            fields["publishedAt"] = item.PublishedAt.Value;
        return fields;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
