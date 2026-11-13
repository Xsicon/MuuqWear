namespace MuuqWear.Application.Shared;

public static class ShopperDisplayName
{
    public static bool IsPersonName(string? value)
    {
        var name = value?.Trim();
        return !string.IsNullOrWhiteSpace(name) && !name.Contains('@', StringComparison.Ordinal);
    }

    public static string Choose(string? storedName, string? alternateName = null, string? claimName = null)
    {
        if (IsPersonName(storedName))
            return storedName!.Trim();
        if (IsPersonName(alternateName))
            return alternateName!.Trim();
        if (IsPersonName(claimName))
            return claimName!.Trim();
        return "";
    }

    public static string Greeting(string? name)
    {
        if (!IsPersonName(name))
            return "there";
        return name!.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }
}
