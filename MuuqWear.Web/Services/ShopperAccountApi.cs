using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Services.AddressService;
using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Application.Services.OrderService;
using MuuqWear.Model.Address;
using MuuqWear.Model.Profile;

namespace MuuqWear.Web.Services;

public static class ShopperAccountApi
{
    public static void MapShopperAccount(this WebApplication app)
    {
        var profile = app.MapGroup("/api/Profile").RequireAuthorization().DisableAntiforgery();
        profile.MapGet("", async (IProfileService svc) => Results.Json(await svc.GetProfile()));
        profile.MapPut("", async (UpdateProfileModel body, IProfileService svc) =>
            Results.Json(await svc.UpdateProfile(body)));
        profile.MapDelete("/delete-account", async (IProfileService svc) =>
            Results.Json(await svc.DeleteAccount()));

        var address = app.MapGroup("/api/Address").RequireAuthorization().DisableAntiforgery();
        address.MapGet("", async (IAddressService svc) => Results.Json(await svc.GetAll()));
        address.MapGet("/{id:guid}", async (Guid id, IAddressService svc) =>
            Results.Json(await svc.GetById(id)));
        address.MapPost("", async (CreateAddressModel body, IAddressService svc) =>
            Results.Json(await svc.Create(body)));
        address.MapPut("/{id:guid}", async (Guid id, UpdateAddressModel body, IAddressService svc) =>
            Results.Json(await svc.Update(id, body)));
        address.MapDelete("/{id:guid}", async (Guid id, IAddressService svc) =>
            Results.Json(await svc.Delete(id)));
        address.MapMethods("/{id:guid}/set-default", [HttpMethods.Patch, HttpMethods.Post],
            async (Guid id, IAddressService svc) => Results.Json(await svc.SetDefault(id)));

        app.MapGet("/api/Order/my-orders", async (IOrderService svc) =>
            Results.Json(await svc.GetUserOrders()))
            .RequireAuthorization()
            .DisableAntiforgery();

        var affiliate = app.MapGroup("/api/Affiliate").RequireAuthorization().DisableAntiforgery();
        affiliate.MapGet("/status", async (IAffiliateService svc) => Results.Json(await svc.GetStatus()));
        affiliate.MapGet("/info", async (IAffiliateService svc) => Results.Json(await svc.GetAffiliateInfo()));
        affiliate.MapGet("/performance-chart", async (IAffiliateService svc) =>
            Results.Json(await svc.GetPerformanceChart()));
        affiliate.MapGet("/recent-referrals", async (IAffiliateService svc) =>
            Results.Json(await svc.GetRecentReferrals()));
    }
}
