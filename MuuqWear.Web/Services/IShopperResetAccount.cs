namespace MuuqWear.Web.Services;

public interface IShopperResetAccount
{
    Task<ShopperResetAccount?> FindAsync(string email, CancellationToken cancellationToken);
}

public sealed record ShopperResetAccount(string Email, string Name, string ResetLink);

public interface IShopperResetMail
{
    Task SendShopperPasswordResetAsync(string email, string name, string link, CancellationToken cancellationToken);
}
