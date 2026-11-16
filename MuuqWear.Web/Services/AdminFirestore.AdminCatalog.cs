using Google.Cloud.Firestore;
using MuuqWear.Application.Shared;
using MuuqWear.Model.AffiliateApplication;
using MuuqWear.Model.CareerPage;
using MuuqWear.Model.DTO.AdminSystem;
using MuuqWear.Model.Products;
using MuuqWear.Model.Vote;

namespace MuuqWear.Web.Services;

public sealed partial class AdminFirestore
{
    public async Task<List<CategoryModel>> ListCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("collections").GetSnapshotAsync(cancellationToken);
        return snap.Documents
            .Select(doc =>
            {
                Guid.TryParse(doc.Id, out var id);
                return new CategoryModel
                {
                    Id = id == Guid.Empty ? Guid.NewGuid() : id,
                    Name = string.IsNullOrWhiteSpace(Str(doc, "name")) ? doc.Id : Str(doc, "name")
                };
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<CategoryModel> UpsertCollectionAsync(AddCategoryModel request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var name = request.Name?.Trim() ?? "";
        await _db.Collection("collections").Document(id.ToString()).SetAsync(new Dictionary<string, object>
        {
            ["name"] = name,
            ["createdAt"] = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
        return new CategoryModel { Id = id, Name = name };
    }

    public async Task<List<AffiliateTierModel>> ListAffiliateTiersAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("affiliateTiers").GetSnapshotAsync(cancellationToken);
        if (snap.Count == 0)
        {
            foreach (var tier in AffiliateTierCatalog.DefaultTiers)
                await UpsertAffiliateTierAsync(tier, cancellationToken);
            snap = await _db.Collection("affiliateTiers").GetSnapshotAsync(cancellationToken);
        }

        return snap.Documents.Select(ToTier)
            .OrderBy(t => t.SortOrder)
            .ToList();
    }

    public async Task<AffiliateTierModel?> GetAffiliateTierAsync(string slug, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Collection("affiliateTiers").Document(slug.ToLowerInvariant()).GetSnapshotAsync(cancellationToken);
        return doc.Exists ? ToTier(doc) : null;
    }

    public async Task<AffiliateTierModel> UpsertAffiliateTierAsync(
        AffiliateTierModel tier, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(tier.Slug) ? Guid.NewGuid().ToString("N") : tier.Slug.Trim().ToLowerInvariant();
        tier.Slug = slug;
        if (tier.Id == Guid.Empty)
            tier.Id = Guid.NewGuid();
        tier.UpdatedAt = DateTime.UtcNow;
        await _db.Collection("affiliateTiers").Document(slug).SetAsync(ToTierFields(tier), cancellationToken: cancellationToken);
        return tier;
    }

    public async Task<CareerPageSettingsModel> GetCareerPageSettingsAsync(CancellationToken cancellationToken = default)
    {
        var doc = await _db.Collection("siteSettings").Document("careers").GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
            return new CareerPageSettingsModel();
        return new CareerPageSettingsModel
        {
            HeroEyebrow = string.IsNullOrWhiteSpace(Str(doc, "heroEyebrow"))
                ? new CareerPageSettingsModel().HeroEyebrow
                : Str(doc, "heroEyebrow"),
            HeroTitle = string.IsNullOrWhiteSpace(Str(doc, "heroTitle"))
                ? new CareerPageSettingsModel().HeroTitle
                : Str(doc, "heroTitle"),
            CultureDescription = string.IsNullOrWhiteSpace(Str(doc, "cultureDescription"))
                ? new CareerPageSettingsModel().CultureDescription
                : Str(doc, "cultureDescription")
        };
    }

    public async Task SaveCareerPageSettingsAsync(CareerPageSettingsModel settings, CancellationToken cancellationToken = default)
    {
        await _db.Collection("siteSettings").Document("careers").SetAsync(new Dictionary<string, object>
        {
            ["heroEyebrow"] = settings.HeroEyebrow?.Trim() ?? "",
            ["heroTitle"] = settings.HeroTitle?.Trim() ?? "",
            ["cultureDescription"] = settings.CultureDescription?.Trim() ?? "",
            ["updatedAt"] = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
    }

    public async Task<List<CommissionRow>> ListPendingCommissionsAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("commissions").GetSnapshotAsync(cancellationToken);
        return snap.Documents
            .Select(ToCommission)
            .Where(c => string.Equals(c.Status, "pending", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<CommissionRow>> ListAllCommissionsAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("commissions").GetSnapshotAsync(cancellationToken);
        return snap.Documents.Select(ToCommission).ToList();
    }

    public async Task<AffiliatePayoutResultModel> ProcessAffiliatePayoutAsync(
        string affiliateCode,
        IReadOnlyList<CommissionRow> commissions,
        string? paymentMethod,
        string? adminNotes,
        CancellationToken cancellationToken = default)
    {
        var payoutId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var totalCents = commissions.Sum(c => c.AmountCents);
        var batch = _db.StartBatch();
        batch.Set(_db.Collection("affiliatePayouts").Document(payoutId.ToString()), new Dictionary<string, object>
        {
            ["affiliateCode"] = affiliateCode,
            ["affiliateName"] = commissions.FirstOrDefault()?.AmbassadorName ?? affiliateCode,
            ["totalAmountCents"] = totalCents,
            ["referralCount"] = commissions.Count,
            ["paymentMethod"] = paymentMethod ?? "manual",
            ["adminNotes"] = adminNotes ?? "",
            ["status"] = "completed",
            ["processedAt"] = now
        });

        foreach (var commission in commissions)
        {
            batch.Update(_db.Collection("commissions").Document(commission.Id), new Dictionary<string, object>
            {
                ["status"] = "paid",
                ["payoutId"] = payoutId.ToString(),
                ["paidAt"] = now
            });
        }

        await batch.CommitAsync(cancellationToken);
        return new AffiliatePayoutResultModel
        {
            PayoutId = payoutId,
            AffiliateCode = affiliateCode,
            AffiliateName = commissions.FirstOrDefault()?.AmbassadorName ?? affiliateCode,
            TotalAmount = totalCents / 100m,
            ReferralCount = commissions.Count,
            ProcessedAt = now,
            Status = "completed",
            PaymentMethod = paymentMethod ?? "manual"
        };
    }

    public async Task<List<AffiliatePayoutResultModel>> ListAffiliatePayoutsAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _db.Collection("affiliatePayouts").GetSnapshotAsync(cancellationToken);
        return snap.Documents.Select(doc =>
        {
            Guid.TryParse(doc.Id, out var id);
            return new AffiliatePayoutResultModel
            {
                PayoutId = id,
                AffiliateCode = Str(doc, "affiliateCode"),
                AffiliateName = Str(doc, "affiliateName"),
                TotalAmount = Int(doc, "totalAmountCents") / 100m,
                ReferralCount = Int(doc, "referralCount"),
                ProcessedAt = ToDate(doc, "processedAt") ?? DateTime.UtcNow,
                Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "completed" : Str(doc, "status"),
                PaymentMethod = Str(doc, "paymentMethod")
            };
        }).OrderByDescending(p => p.ProcessedAt).ToList();
    }

    public async Task WriteSystemLogAsync(
        string level, string message, string? source = null, CancellationToken cancellationToken = default)
    {
        await _db.Collection("systemLogs").Document(Guid.NewGuid().ToString("N")).SetAsync(new Dictionary<string, object>
        {
            ["level"] = level,
            ["message"] = message,
            ["source"] = source ?? "admin",
            ["timestamp"] = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
    }

    public async Task<List<SystemLogEntryModel>> ListSystemLogsAsync(
        int days, string level, string? search, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Max(1, days));
        var snap = await _db.Collection("systemLogs").GetSnapshotAsync(cancellationToken);
        IEnumerable<SystemLogEntryModel> query = snap.Documents.Select(doc =>
        {
            Guid.TryParse(doc.Id, out var id);
            return new SystemLogEntryModel
            {
                Id = id == Guid.Empty ? Guid.NewGuid() : id,
                Timestamp = ToDate(doc, "timestamp") ?? DateTime.UtcNow,
                Level = string.IsNullOrWhiteSpace(Str(doc, "level")) ? "info" : Str(doc, "level"),
                Message = Str(doc, "message"),
                Source = EmptyToNull(Str(doc, "source"))
            };
        }).Where(l => l.Timestamp >= since);

        if (!string.IsNullOrWhiteSpace(level) && !level.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l =>
                l.Message.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (l.Source?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        return query.OrderByDescending(l => l.Timestamp).ToList();
    }

    public async Task<VoteItemModel> UpsertVoteAsync(VoteItemModel item, CancellationToken cancellationToken = default)
    {
        if (item.Id == Guid.Empty)
            item.Id = Guid.NewGuid();
        item.CreatedAt ??= DateTime.UtcNow;
        await _db.Collection("communityVotes").Document(item.Id.ToString()).SetAsync(new Dictionary<string, object>
        {
            ["styleName"] = item.StyleName ?? "",
            ["subtitle"] = item.Subtitle ?? "",
            ["description"] = item.Description ?? "",
            ["imageUrl"] = item.ImageUrl ?? "",
            ["tag"] = item.Tag ?? "",
            ["voteCount"] = item.VoteCount,
            ["status"] = item.Status ?? "active",
            ["season"] = item.Season ?? "",
            ["colorOptions"] = item.ColorOptions ?? new List<string>(),
            ["createdAt"] = item.CreatedAt.Value
        }, cancellationToken: cancellationToken);
        return item;
    }

    public async Task<bool> DeleteVoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Collection("communityVotes").Document(id.ToString())
            .DeleteAsync(cancellationToken: cancellationToken);
        return true;
    }

    public async Task<VoteItemModel?> UpdateVoteStatusAsync(
        Guid id, string status, string? tag = null, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Collection("communityVotes").Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
            return null;
        var fields = new Dictionary<string, object> { ["status"] = status };
        if (!string.IsNullOrWhiteSpace(tag))
            fields["tag"] = tag;
        await doc.Reference.UpdateAsync(fields, cancellationToken: cancellationToken);
        return ToVoteItem(await doc.Reference.GetSnapshotAsync(cancellationToken));
    }

    public static VoteItemModel ToVoteItem(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new VoteItemModel
        {
            Id = id,
            StyleName = Str(doc, "styleName"),
            Subtitle = Str(doc, "subtitle"),
            Description = Str(doc, "description"),
            ImageUrl = Str(doc, "imageUrl"),
            Tag = Str(doc, "tag"),
            VoteCount = Int(doc, "voteCount"),
            Status = Str(doc, "status"),
            Season = Str(doc, "season"),
            ColorOptions = Strings(doc, "colorOptions"),
            CreatedAt = ToDate(doc, "createdAt")
        };
    }

    private static AffiliateTierModel ToTier(DocumentSnapshot doc)
    {
        Guid.TryParse(Str(doc, "id"), out var id);
        if (id == Guid.Empty)
            Guid.TryParse(doc.Id, out id);
        return new AffiliateTierModel
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id,
            Slug = string.IsNullOrWhiteSpace(Str(doc, "slug")) ? doc.Id : Str(doc, "slug"),
            DisplayName = Str(doc, "displayName"),
            ItemsSoldThreshold = Int(doc, "itemsSoldThreshold"),
            CommissionRatePercent = Decimal(doc, "commissionRatePercent"),
            ReferralDiscountPercent = Decimal(doc, "referralDiscountPercent"),
            QuarterlyBonusPercent = Decimal(doc, "quarterlyBonusPercent"),
            MaxAffiliates = doc.ContainsField("maxAffiliates") ? Int(doc, "maxAffiliates") : null,
            Perks = Strings(doc, "perks"),
            SortOrder = Int(doc, "sortOrder"),
            IsActive = Bool(doc, "isActive", true),
            UpdatedAt = ToDate(doc, "updatedAt") ?? DateTime.UtcNow
        };
    }

    private static Dictionary<string, object> ToTierFields(AffiliateTierModel tier) => new()
    {
        ["id"] = tier.Id.ToString(),
        ["slug"] = tier.Slug,
        ["displayName"] = tier.DisplayName,
        ["itemsSoldThreshold"] = tier.ItemsSoldThreshold,
        ["commissionRatePercent"] = (double)tier.CommissionRatePercent,
        ["referralDiscountPercent"] = (double)tier.ReferralDiscountPercent,
        ["quarterlyBonusPercent"] = (double)tier.QuarterlyBonusPercent,
        ["maxAffiliates"] = tier.MaxAffiliates ?? 0,
        ["perks"] = tier.Perks ?? new List<string>(),
        ["sortOrder"] = tier.SortOrder,
        ["isActive"] = tier.IsActive,
        ["updatedAt"] = tier.UpdatedAt
    };

    private static CommissionRow ToCommission(DocumentSnapshot doc)
    {
        Guid.TryParse(Str(doc, "orderId"), out var orderId);
        return new CommissionRow
        {
            Id = doc.Id,
            AmbassadorId = Str(doc, "ambassadorId"),
            OrderId = orderId,
            AmountCents = doc.ContainsField("amount") ? Convert.ToInt64(doc.GetValue<object>("amount")) : 0,
            OrderTotalCents = doc.ContainsField("orderTotal") ? Convert.ToInt64(doc.GetValue<object>("orderTotal")) : 0,
            Rate = doc.ContainsField("rate") ? Convert.ToDouble(doc.GetValue<object>("rate")) : 0,
            Status = string.IsNullOrWhiteSpace(Str(doc, "status")) ? "pending" : Str(doc, "status"),
            CreatedAt = ToDate(doc, "createdAt") ?? DateTime.UtcNow
        };
    }

    private static decimal Decimal(DocumentSnapshot doc, string field)
    {
        if (!doc.ContainsField(field))
            return 0;
        try { return Convert.ToDecimal(doc.GetValue<object>(field)); }
        catch { return 0; }
    }

    public sealed class CommissionRow
    {
        public string Id { get; set; } = "";
        public string AmbassadorId { get; set; } = "";
        public string AmbassadorName { get; set; } = "";
        public Guid OrderId { get; set; }
        public long AmountCents { get; set; }
        public long OrderTotalCents { get; set; }
        public double Rate { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; }
    }
}
