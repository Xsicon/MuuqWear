namespace MuuqWear.Application.Shared;

public static class AdminMoney
{
    public static string Format(decimal amount) => $"${amount:N2}";
}
