using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Web.Components.Pages
{
    public partial class Register
    {

        [CascadingParameter]
        private Task<AuthenticationState>? AuthenticationStateTask { get; set; }
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

        protected override async Task OnInitializedAsync()
        {
            if (AuthenticationStateTask is null) return;

            var authState = await AuthenticationStateTask;

            if (authState.User.Identity?.IsAuthenticated == true)
            {
                NavigationManager.NavigateTo("/profile", replace: true); //  Always /profile
            }
        }

        private void HandlePasswordInput(string value)
        {
            registerModel.Password = value;
            CalculateStrength(value);
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
}