using FirebaseAdmin.Auth;

namespace MuuqWear.Web.Services;

public sealed class FirebaseShopperResetAccount : IShopperResetAccount
{
    private readonly IConfiguration _config;

    public FirebaseShopperResetAccount(IConfiguration config) => _config = config;

    public async Task<ShopperResetAccount?> FindAsync(string email, CancellationToken cancellationToken)
    {
        UserRecord record;
        try
        {
            record = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email, cancellationToken);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(record.Email))
            return null;

        var firebaseLink = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(
            record.Email,
            new ActionCodeSettings
            {
                Url = ShopperPasswordReset.ActionUrl,
                HandleCodeInApp = true
            },
            cancellationToken);
        var link = ShopperPasswordReset.ToActionUrl(firebaseLink, _config["Firebase:ApiKey"]);
        return new ShopperResetAccount(record.Email, record.DisplayName ?? record.Email, link);
    }
}
