using BlazorBootstrap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Services.AddressService;
using MuuqWear.Application.Services.AdminUserService;
using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.ChatService;
using MuuqWear.Application.Services.ContentService;
using MuuqWear.Application.Services.CustomerService;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Application.Services.JournalService;
using MuuqWear.Application.Services.NotificationService;
using MuuqWear.Application.Services.OrderReturnService;
using MuuqWear.Application.Services.OrderService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Services.ProfileService;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Application.Shared;
using MuuqWear.Web.Components;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]!;


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
});

builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<ICategoryService, CategoryService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();

builder.Services.AddHttpClient<ICartService, CartService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<IOrderService, OrderService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>()
.AddHttpMessageHandler<AffiliateCookieHandler>();


builder.Services.AddHttpClient<IOrderReturnService, OrderReturnService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<IAddressService, AddressService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
// no AuthenticatedHttpHandler — public endpoint
builder.Services.AddHttpClient<IJournalService, JournalService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<IAdminSettingService, AdminSettingService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<ICustomerService, CustomerService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<IProfileService, ProfileService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<IVoteService, VoteService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<CartStateService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticatedHttpHandler>();
builder.Services.AddSingleton<AuthSessionService>();
builder.Services.AddTransient<AffiliateCookieHandler>();

builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddHttpClient<IContentService, ContentService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<IHelpCenterService, HelpCenterService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<INotificationService, NotificationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddSingleton<NotificationRealtimeService>();
// Add after other services
builder.Services.AddHttpClient<IAffiliateService, AffiliateService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthenticatedHttpHandler>();

builder.Services.AddHttpClient<IChatService,
                               ChatService>(client =>
                               {
                                   client.BaseAddress = new Uri(apiBaseUrl);
                               })
.AddHttpMessageHandler<AuthenticatedHttpHandler>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "muuqwear_auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(5);
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                //  Already handles both cases
                if (context.Request.Path.StartsWithSegments("/admin"))
                    context.Response.Redirect("/not-found");
                else
                    context.Response.Redirect("/");
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.Redirect("/not-found");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddServerSideBlazor(options =>
{
    //  how long to keep disconnected circuit
    options.DisconnectedCircuitRetentionPeriod =
        TimeSpan.FromMinutes(5);

    //  max disconnected circuits in memory
    options.DisconnectedCircuitMaxRetained = 100;

    //  JS interop timeout
    options.JSInteropDefaultCallTimeout =
        TimeSpan.FromSeconds(30);

    //  detailed errors in development only
    options.DetailedErrors =
        builder.Environment.IsDevelopment();
});

builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TokenRefreshMiddleware>(); // ← add this line
app.UseMiddleware<AffiliateTrackingMiddleware>();
app.UseAntiforgery();

//Set Cookie

app.MapPost("/auth/prepare-cookie", (
    CookieAuthSession session,
    IMemoryCache cache) =>
{
    if (string.IsNullOrEmpty(session.Token) ||
        string.IsNullOrEmpty(session.Role))
        return Results.BadRequest("Invalid session data");
    var key = Guid.NewGuid().ToString();
    cache.Set(key, session, TimeSpan.FromSeconds(30));
    return Results.Ok(key);
});

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/login");
});

app.MapGet("/auth/set-cookie", async (
    HttpContext ctx,
    string key,
    IMemoryCache cache, AuthSessionService authSession) =>
{
    if (!cache.TryGetValue(key, out CookieAuthSession? session) || session is null)
    {
        ctx.Response.Redirect("/register");
        return;
    }

    cache.Remove(key);
    authSession.Reset();
    System.Diagnostics.Debug.WriteLine("AuthSession reset on fresh login ");

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, session.Username!),
        new Claim(ClaimTypes.Email, session.Email!),
        new Claim(ClaimTypes.Role, session.Role!),
        new Claim("AccessToken", session.Token!),
        new Claim("RefreshToken", session.RefreshToken!),
        new Claim("UserId", session.UserId!)
    };

    var identity = new ClaimsIdentity(
        claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    var expiry = session.RememberMe ?? false
        ? DateTimeOffset.UtcNow.AddDays(30)
        : DateTimeOffset.UtcNow.AddHours(5);

    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = session.RememberMe ?? false,
            ExpiresUtc = expiry
        });

    ctx.Response.Redirect(session.ReturnUrl!);
}).DisableAntiforgery(); //  add this

app.MapGet("/auth/clear", async (HttpContext ctx, bool expired = false) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    System.Diagnostics.Debug.WriteLine("Call From Program.cs");
    ctx.Response.Redirect(expired ? "/login?expired=true" : "/");
});



app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();