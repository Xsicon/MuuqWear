using System.Text.RegularExpressions;

namespace MuuqWear.Application.Shared;

public static class MuuqWearUtils
{
    /// <summary>
    /// Validates email format
    /// e.g. user@example.com 
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Calculates password strength
    /// Returns level 1-4 and color/text
    /// </summary>
    public static PasswordStrength GetPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return new PasswordStrength(0, "", "");

        bool hasMinLength = password.Length >= 8;
        bool hasLetter = password.Any(char.IsLetter);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        if (password.Length < 6)
            return new PasswordStrength(1, "#ef4444", "Weak");

        if (!hasMinLength || (hasLetter && !hasDigit) || (!hasLetter && hasDigit))
            return new PasswordStrength(2, "#f97316", "Fair");

        if (hasMinLength && hasLetter && hasDigit && !hasSpecial)
            return new PasswordStrength(3, "#eab308", "Good");

        if (hasMinLength && hasLetter && hasDigit && hasSpecial)
            return new PasswordStrength(4, "#22c55e", "Strong");

        return new PasswordStrength(0, "", "");
    }

    /// <summary>
    /// Checks if string is null or whitespace
    /// Shorthand for string.IsNullOrWhiteSpace 
    /// </summary>
    public static bool IsEmpty(string? value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Sanitizes search input
    /// Escapes SQL LIKE special characters 
    /// </summary>
    public static string SanitizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return string.Empty;

        return search
            .Trim()
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    /// <summary>
    /// Returns a light or dark icon color for contrast against a hex background.
    /// </summary>
    public static string GetContrastIconColor(string? hexColor)
    {
        if (!TryParseHexColor(hexColor, out var r, out var g, out var b))
            return "#FFFFFF";

        var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.55 ? "#1E2A47" : "#FFFFFF";
    }

    private static bool TryParseHexColor(string? color, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(color))
            return false;

        var hex = color.Trim();
        if (!hex.StartsWith('#'))
            hex = "#" + hex;

        if (hex.Length == 4)
            hex = $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";

        if (hex.Length != 7)
            return false;

        if (!int.TryParse(hex.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out r) ||
            !int.TryParse(hex.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out g) ||
            !int.TryParse(hex.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out b))
            return false;

        return true;
    }
}

/// <summary>
/// Password strength result
/// </summary>
public class PasswordStrength
{
    // 0 = none, 1 = weak, 2 = fair, 3 = good, 4 = strong
    public int Level { get; }

    // hex color for UI display
    public string Color { get; }

    // text label for UI display
    public string Text { get; }

    public PasswordStrength(int level, string color, string text)
    {
        Level = level;
        Color = color;
        Text = text;
    }
}