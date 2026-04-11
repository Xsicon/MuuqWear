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
                    await sessionStorage.SetAsync("pendingEmail", registerModel.Email!);
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
    }
}