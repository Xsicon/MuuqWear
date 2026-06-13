namespace MuuqWear.Model.Authentication;

public class ResetPasswordModel
{
    // token from URL fragment
    public string? AccessToken { get; set; }

    // refresh token from URL fragment
    public string? RefreshToken { get; set; }

    // new password
    public string? NewPassword { get; set; }

    // must match NewPassword
    public string? ConfirmPassword { get; set; }
}