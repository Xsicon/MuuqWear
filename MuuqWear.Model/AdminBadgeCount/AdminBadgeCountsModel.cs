namespace MuuqWear.Model.AdminBadgeCount;


public class AdminBadgeCountsModel
{
    public int PendingOrders { get; set; }
    public int TotalCustomers { get; set; }          // changed
    public int TotalProducts { get; set; }            // changed
    public AffiliateCountsModel AffiliateCounts { get; set; } = new();
    public int ActiveChats { get; set; }
    public int OpenTickets { get; set; }
}


public class AffiliateCountsModel
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Waitlisted { get; set; }

    public int Total => Pending + Approved + Rejected + Waitlisted;
}
