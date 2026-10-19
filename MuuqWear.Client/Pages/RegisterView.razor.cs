using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Client.Pages;
public partial class RegisterView
{

    RegisterModel registerModel = new();
    string message = string.Empty;
    string messageCssClass = string.Empty;

    private int StrengthLevel = 0;

    // color based on strength
    private string StrengthColor = "";

    // text label based on strength
    private string StrengthText = "";
    async Task HandleRegister()
    {
        try
        {
            var result = await IAuthService.Register(registerModel);
            message = result.Message;
            if (result.Success)
            {
                messageCssClass = "text-success";
                await AuthStateService.SetPendingEmailAsync(registerModel.Email!);
                NavigationManager.NavigateTo("/verify-2fa");
            }
            else
                messageCssClass = "text-danger";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message.ToString());
        }
    }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    private string PasswordFieldClass =>
        EditContext?.FieldCssClass(FieldIdentifier.Create(() => registerModel.Password)) ?? string.Empty;

    private void OnPasswordInput(ChangeEventArgs e)
    {
        registerModel.Password = e.Value?.ToString() ?? string.Empty;
        CalculateStrength(registerModel.Password);
        EditContext?.NotifyFieldChanged(FieldIdentifier.Create(() => registerModel.Password));
    }

    private void CalculateStrength(string password)
    {
        // reset if empty
        if (string.IsNullOrEmpty(password))
        {
            StrengthLevel = 0;
            StrengthColor = "";
            StrengthText = "";
            return;
        }

        // check each rule
        bool hasMinLength = password.Length >= 8;        // at least 8 chars
        bool hasLetter = password.Any(char.IsLetter);    // has any letter
        bool hasDigit = password.Any(char.IsDigit);      // has any number
        bool hasSpecial = password.Any(c =>              // has special char
            !char.IsLetterOrDigit(c));                   // e.g. @, #, !, $

        // calculate level based on rules passed
        if (password.Length < 6)
        {
            // too short → always weak
            StrengthLevel = 1;
            StrengthColor = "#ef4444"; // red
            StrengthText = "Weak";
        }
        else if (!hasMinLength || (hasLetter && !hasDigit) || (!hasLetter && hasDigit))
        {
            // 6-7 chars OR only letters OR only numbers → fair
            StrengthLevel = 2;
            StrengthColor = "#f97316"; // orange
            StrengthText = "Fair";
        }
        else if (hasMinLength && hasLetter && hasDigit && !hasSpecial)
        {
            // 8+ chars with letters and numbers → good
            StrengthLevel = 3;
            StrengthColor = "#eab308"; // yellow
            StrengthText = "Good";
        }
        else if (hasMinLength && hasLetter && hasDigit && hasSpecial)
        {
            // 8+ chars with letters, numbers and special → strong
            StrengthLevel = 4;
            StrengthColor = "#22c55e"; // green
            StrengthText = "Strong";
        }
    }
}
