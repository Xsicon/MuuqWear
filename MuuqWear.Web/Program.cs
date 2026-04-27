using BlazorBootstrap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Shared;
using MuuqWear.Web.Components;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]!;


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
});

builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
});
builder.Services.AddHttpClient<ICategoryService, CategoryService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
});
builder.Services.AddHttpClient<ICartService, CartService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<CartStateService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticatedHttpHandler>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthenticationStateProvider>());
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
                // ✅ Already handles both cases
                if (context.Request.Path.StartsWithSegments("/admin"))
                    context.Response.Redirect("/not-found");
                else
                    context.Response.Redirect("/register");
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.Redirect("/not-found");
                return Task.CompletedTask;
            }
        };
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


app.MapGet("/auth/set-cookie", async (
    HttpContext ctx,
    string key,
    IMemoryCache cache) =>
{
    if (!cache.TryGetValue(key, out CookieAuthSession? session) || session is null)
    {
        ctx.Response.Redirect("/register");
        return;
    }

    cache.Remove(key);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, session.Username!),
        new Claim(ClaimTypes.Email, session.Email!),
        new Claim(ClaimTypes.Role, session.Role!),
        new Claim("AccessToken", session.Token!),
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
}).DisableAntiforgery(); // ✅ add this

app.MapGet("/auth/clear", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();