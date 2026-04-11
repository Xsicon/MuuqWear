using System.ComponentModel.DataAnnotations;

namespace MuuqWear.Model.Authentication;
public class VerifyOTPModel
{
    public string? Email { get; set; }
    [Required(ErrorMessage = "Code is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter a valid 6-digit code")]
    public string? Otp { get; set; } = string.Empty;
}
