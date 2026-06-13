using System.ComponentModel.DataAnnotations;

namespace MuuqWear.Model.Authentication;

public class LoginModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[\W_]).+$",
        ErrorMessage = "Password must contain 1 uppercase and 1 special character")]
    public string? Password { get; set; }
}
