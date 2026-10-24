using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Profile;
using MuuqWear.Web.ServerAuth;

namespace MuuqWear.Web.Services;

public sealed class FirestoreProfileService : IProfileService
{
    private readonly AdminFirestore _store;
    private readonly IHttpContextAccessor _http;

    public FirestoreProfileService(AdminFirestore store, IHttpContextAccessor http)
    {
        _store = store;
        _http = http;
    }

    public async Task<Response<ProfileModel>> GetProfile()
    {
        var (uid, email, name) = Caller();
        if (string.IsNullOrWhiteSpace(uid) && string.IsNullOrWhiteSpace(email))
            return Response<ProfileModel>.Fail("Sign in to view your profile.");

        var profile = await _store.GetShopperProfileAsync(uid ?? email!, email, name);
        return Response<ProfileModel>.SuccessResponse(profile);
    }

    public async Task<Response<ProfileModel>> UpdateProfile(UpdateProfileModel request)
    {
        var (uid, email, _) = Caller();
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(email))
            return Response<ProfileModel>.Fail("Sign in to update your profile.");

        var profile = await _store.UpsertShopperProfileAsync(uid, email, request.FullName ?? "");
        return Response<ProfileModel>.SuccessResponse(profile);
    }

    public async Task<Response<bool>> DeleteAccount()
    {
        var (uid, email, _) = Caller();
        if (string.IsNullOrWhiteSpace(uid))
            return Response<bool>.Fail("Sign in to delete your account.");

        await _store.DisableShopperAsync(uid, email);
        return Response<bool>.SuccessResponse(true);
    }

    private (string? Uid, string? Email, string? Name) Caller() =>
        VerifiedAuthSession.Shopper(_http.HttpContext?.User);
}
