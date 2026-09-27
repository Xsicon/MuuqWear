namespace MuuqWear.Model.Authentication;

public class ForgotPasswordModel
{
    public string? Email { get; set; }

    /// <summary>
    /// Frontend URL Supabase should redirect to after the user opens the reset link.
    /// </summary>
    public string? RedirectTo { get; set; }
}
