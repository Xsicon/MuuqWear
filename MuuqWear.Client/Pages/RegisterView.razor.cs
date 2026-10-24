using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Client.Auth;
using MuuqWear.Model.Authentication;
using MuuqWear.Model.Site;

namespace MuuqWear.Client.Pages;
public partial class RegisterView
{
    private bool writesFrozen;
    private bool showConfirm;
    private bool isBusy;
    private bool wasmReady;
    private EditContext? registerContext;
    private string? confirmEmail;
    private bool pendingRestored;

    protected override void OnInitialized()
    {
        writesFrozen = CutoverFreeze.Enabled(Config["Cutover:FreezeWrites"]);
        registerContext = new EditContext(registerModel);
    }

    private async Task SubmitRegister()
    {
        if (registerContext != null && !registerContext.Validate())
            return;
        await HandleRegister();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !OperatingSystem.IsBrowser())
            return;
        if (!wasmReady)
        {
            wasmReady = true;
            StateHasChanged();
        }
        if (pendingRestored)
            return;
        pendingRestored = true;
        try
        {
            confirmEmail = await JS.InvokeAsync<string?>("sessionStorage.getItem", SignupPending.StorageKey);
        }
        catch (JSException)
        {
            confirmEmail = await AuthStateService.GetPendingEmailAsync();
        }

        if (string.IsNullOrWhiteSpace(confirmEmail))
            return;

        showConfirm = true;
        registerModel.Email = confirmEmail;
        await AuthStateService.SetPendingEmailAsync(confirmEmail);
        StateHasChanged();
    }

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
        if (writesFrozen)
        {
            message = PageCopy.WritesPaused;
            messageCssClass = "text-danger";
            return;
        }

        isBusy = true;
        message = string.Empty;
        try
        {
            var result = await IAuthService.Register(registerModel);
            message = result.Message;
            if (result.Success)
            {
                messageCssClass = "text-success";
                confirmEmail = registerModel.Email;
                showConfirm = true;
                await AuthStateService.SetPendingEmailAsync(registerModel.Email!);
                try
                {
                    await JS.InvokeVoidAsync("sessionStorage.setItem", SignupPending.StorageKey, registerModel.Email);
                }
                catch (JSException) { }
            }
            else
                messageCssClass = "text-danger";
        }
        catch (Exception ex)
        {
            message = "Unable to create the account. Please try again.";
            messageCssClass = "text-danger";
            Console.WriteLine(ex.Message.ToString());
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task ResendConfirmation()
    {
        if (string.IsNullOrWhiteSpace(confirmEmail) || isBusy)
            return;

        isBusy = true;
        message = string.Empty;
        var result = await FirebaseSession.SendEmailVerificationAsync();
        message = result.Ok
            ? "Confirmation email sent. Check your inbox."
            : result.Message ?? "Unable to send the confirmation email.";
        messageCssClass = result.Ok ? "text-success" : "text-danger";
        isBusy = false;
    }

    private async Task BackToForm()
    {
        showConfirm = false;
        message = string.Empty;
        await AuthStateService.ClearPendingEmailAsync();
        try
        {
            await JS.InvokeVoidAsync("sessionStorage.removeItem", SignupPending.StorageKey);
        }
        catch (JSException) { }
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
