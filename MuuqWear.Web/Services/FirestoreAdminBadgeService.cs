using MuuqWear.Application.Services.AdminBadgeService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.AdminBadgeCount;

namespace MuuqWear.Web.Services;

public sealed class FirestoreAdminBadgeService : IAdminBadgeService
{
    private readonly AdminFirestore _store;

    public FirestoreAdminBadgeService(AdminFirestore store) => _store = store;

    public async Task<Response<AdminBadgeCountsModel>> GetCounts()
    {
        var orders = await _store.ListOrdersAsync("pending", null, 1, 500);
        var customers = await _store.CountCustomersAsync();
        var products = (await _store.ListProductsAsync()).Count;
        var affiliates = await _store.ListAffiliateApplicationsAsync("pending");
        var chats = (await _store.ListChatSessionsAsync())
            .Count(s => !string.Equals(s.Status, "closed", StringComparison.OrdinalIgnoreCase));

        return Response<AdminBadgeCountsModel>.SuccessResponse(new AdminBadgeCountsModel
        {
            PendingOrders = orders.TotalCount,
            TotalCustomers = customers,
            TotalProducts = products,
            ActiveChats = chats,
            OpenTickets = 0,
            AffiliateCounts = new AffiliateCountsModel
            {
                Pending = affiliates.Count
            }
        });
    }
}
