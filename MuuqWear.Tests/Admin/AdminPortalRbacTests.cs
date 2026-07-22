using MuuqWear.Model.NotificationModel;
using MuuqWear.Web.Constants;
using MuuqWear.Web.Services;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class AdminPortalRolesTests
{
    [Theory]
    [InlineData(AdminPortalRoles.Admin, AdminPortalSection.Orders, true)]
    [InlineData(AdminPortalRoles.Admin, AdminPortalSection.Customers, true)]
    [InlineData(AdminPortalRoles.OperationsManager, AdminPortalSection.Orders, true)]
    [InlineData(AdminPortalRoles.OperationsManager, AdminPortalSection.Products, false)]
    [InlineData(AdminPortalRoles.SupportTeam, AdminPortalSection.Support, true)]
    [InlineData(AdminPortalRoles.SupportTeam, AdminPortalSection.Orders, false)]
    [InlineData(AdminPortalRoles.Merchandising, AdminPortalSection.Products, true)]
    [InlineData(AdminPortalRoles.Merchandising, AdminPortalSection.Orders, false)]
    [InlineData(AdminPortalRoles.ContentTeam, AdminPortalSection.Content, true)]
    [InlineData(AdminPortalRoles.TechnologySystems, AdminPortalSection.System, true)]
    [InlineData(AdminPortalRoles.TechnologySystems, AdminPortalSection.Content, false)]
    public void CanAccess_matches_section_matrix(string role, string section, bool expected) =>
        Assert.Equal(expected, AdminPortalRoles.CanAccess(role, section));

    [Fact]
    public void All_staff_roles_can_access_overview()
    {
        foreach (var role in StaffRoles())
            Assert.True(AdminPortalRoles.CanAccess(role, AdminPortalSection.Overview));
    }

    [Theory]
    [InlineData(AdminPortalRoles.OperationsManager, 4)]
    [InlineData(AdminPortalRoles.SupportTeam, 2)]
    [InlineData(AdminPortalRoles.Merchandising, 2)]
    [InlineData(AdminPortalRoles.ContentTeam, 2)]
    [InlineData(AdminPortalRoles.TechnologySystems, 2)]
    [InlineData(AdminPortalRoles.Admin, 9)]
    public void GetSections_returns_expected_count(string role, int expectedCount) =>
        Assert.Equal(expectedCount, AdminPortalRoles.GetSections(role).Count);

    [Theory]
    [InlineData(AdminPortalRoles.SupportTeam, "/admin/support")]
    [InlineData(AdminPortalRoles.Merchandising, "/admin/products")]
    [InlineData(AdminPortalRoles.ContentTeam, "/admin/content")]
    [InlineData(AdminPortalRoles.TechnologySystems, "/admin/system")]
    [InlineData(AdminPortalRoles.Admin, "/admin")]
    [InlineData(AdminPortalRoles.OperationsManager, "/admin")]
    public void GetDefaultHome_returns_role_landing(string role, string expectedHome) =>
        Assert.Equal(expectedHome, AdminPortalRoles.GetDefaultHome(role));

    [Theory]
    [InlineData("admin", true)]
    [InlineData("operations_manager", true)]
    [InlineData("user", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAllowed_accepts_staff_roles_only(string? role, bool expected) =>
        Assert.Equal(expected, AdminPortalRoles.IsAllowed(role));

    private static IEnumerable<string> StaffRoles() =>
    [
        AdminPortalRoles.Admin,
        AdminPortalRoles.OperationsManager,
        AdminPortalRoles.SupportTeam,
        AdminPortalRoles.Merchandising,
        AdminPortalRoles.ContentTeam,
        AdminPortalRoles.TechnologySystems
    ];
}

public class AdminPortalRouteAccessTests
{
    [Theory]
    [InlineData(AdminPortalRoles.Merchandising, "/admin/products?view=catalog", true)]
    [InlineData(AdminPortalRoles.Merchandising, "/admin/orders", false)]
    [InlineData(AdminPortalRoles.SupportTeam, "/admin/support?tab=tickets", true)]
    [InlineData(AdminPortalRoles.SupportTeam, "/admin/customers?view=notes", false)]
    [InlineData(AdminPortalRoles.Admin, "/admin/customers", true)]
    [InlineData(AdminPortalRoles.OperationsManager, "/admin/affiliates?tab=pending", true)]
    public void CanAccessPath_enforces_route_matrix(string role, string path, bool expected) =>
        Assert.Equal(expected, AdminPortalRouteAccess.CanAccessPath(role, path));

    [Theory]
    [InlineData("/admin/orders?tab=orders", AdminPortalSection.Orders)]
    [InlineData("/admin/customers?view=notes", AdminPortalSection.Customers)]
    [InlineData("/admin/live-chat", AdminPortalSection.Support)]
    [InlineData("/admin/jobs/abc/applications", AdminPortalSection.Careers)]
    public void GetSectionFromPath_maps_routes(string path, string expectedSection) =>
        Assert.Equal(expectedSection, AdminPortalRouteAccess.GetSectionFromPath(path));
}

public class AdminHeaderNotificationAccessTests
{
    [Theory]
    [InlineData(AdminPortalRoles.Merchandising, NotificationType.Order, false)]
    [InlineData(AdminPortalRoles.OperationsManager, NotificationType.Order, true)]
    [InlineData(AdminPortalRoles.Merchandising, NotificationType.LowStock, true)]
    [InlineData(AdminPortalRoles.SupportTeam, NotificationType.CustomerMessage, true)]
    [InlineData(AdminPortalRoles.OperationsManager, NotificationType.CustomerMessage, false)]
    [InlineData(AdminPortalRoles.Admin, "campaign_blast", true)]
    [InlineData(AdminPortalRoles.Merchandising, "campaign_blast", false)]
    [InlineData(AdminPortalRoles.SupportTeam, "campaign_blast", false)]
    public void CanSeeNotificationType_filters_by_role(string role, string type, bool expected) =>
        Assert.Equal(expected, AdminHeaderNotificationAccess.CanSeeNotificationType(role, type));

    [Fact]
    public void Support_can_see_customer_notes_and_lands_on_support()
    {
        Assert.True(AdminHeaderNotificationAccess.CanSeeCustomerNotes(AdminPortalRoles.SupportTeam));
        Assert.Equal(
            "/admin/support",
            AdminHeaderNotificationAccess.GetCustomerNotesListLink(AdminPortalRoles.SupportTeam));
        Assert.Equal(
            "/admin/support",
            AdminHeaderNotificationAccess.ResolveLink(
                AdminPortalRoles.SupportTeam,
                "/admin/customers?view=notes&customerId=abc"));
    }

    [Fact]
    public void Admin_can_open_customer_notes_list()
    {
        Assert.Equal(
            "/admin/customers?view=notes",
            AdminHeaderNotificationAccess.GetCustomerNotesListLink(AdminPortalRoles.Admin));
    }
}
