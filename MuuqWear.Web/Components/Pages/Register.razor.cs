using Microsoft.AspNetCore.Components;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Model.Authentication;

namespace MuuqWear.Web.Components.Pages
{
    public partial class Register
    {

        RegisterModel registerModel = new();
        string message = string.Empty;
        string messageCssClass = string.Empty;

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var loginSesson = await IAuthService.IsUserLoggedIn();
                if (loginSesson)
                {
                    NavigationManager.NavigateTo("/");
                }
            }
        }
    }
}