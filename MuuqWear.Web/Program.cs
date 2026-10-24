using BlazorBootstrap;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Services.AddressService;
using MuuqWear.Application.Services.AdminBadgeService;
using MuuqWear.Application.Services.AdminSystemService;
using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.WishlistService;
using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.ChatService;
using MuuqWear.Application.Services.ArchiveService;
using MuuqWear.Application.Services.ContentService;
using MuuqWear.Application.Services.CustomerService;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Application.Services.JobPostingService;
using MuuqWear.Application.Services.CareerPageSettingsService;
using MuuqWear.Application.Services.JournalService;
using MuuqWear.Application.Services.MuuqsimoService;
using MuuqWear.Application.Services.NotificationService;
using MuuqWear.Application.Services.OrderReturnService;
using MuuqWear.Application.Services.OrderService;
using MuuqWear.Application.Services.RefundService;
using MuuqWear.Application.Services.PaymentService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Services.ProfileService;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Application.Shared;
using MuuqWear.Client.Auth;
using MuuqWear.Client.Checkout;
using MuuqWear.Client.Offline;
using MuuqWear.Model.Site;
using MuuqWear.Web.Authorization;
using MuuqWear.Web.Components;
using MuuqWear.Web.Constants;
using MuuqWear.Web.Helpers;
using MuuqWear.Web.ServerAuth;
using MuuqWear.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.RequireHeaderSymmetry = false;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IAuthService, AuthService>();

if (FirebaseApp.DefaultInstance is null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault(),
        ProjectId = builder.Configuration["Firebase:ProjectId"] ?? "muuqwear-6e2e0"
    });
}

builder.Services.AddSingleton<StaffSessionJwt>();
builder.Services.AddSingleton<IFirebaseIdTokenVerifier, FirebaseIdTokenVerifier>();
builder.Services.AddSingleton<CookieSessionVerifier>();
builder.Services.AddSingleton<AdminFirestore>();
builder.Services.AddSingleton<StaffWelcomeMail>();
builder.Services.AddScoped<IStaffUserService, FirebaseStaffUserService>();
builder.Services.AddScoped<IProductService, FirestoreProductService>();
builder.Services.AddScoped<IOrderService, FirestoreOrderService>();
builder.Services.AddScoped<ICustomerService, FirestoreCustomerService>();
builder.Services.AddScoped<ICategoryService, FirestoreCategoryService>();


builder.Services.AddScoped<IOrderReturnService, FirestoreOrderReturnService>();
builder.Services.AddScoped<IRefundService, FirestoreRefundService>();
builder.Services.AddScoped<IAddressService, FirestoreAddressService>();
builder.Services.AddScoped<IJournalService, FirestoreJournalService>();
builder.Services.AddSingleton<StorefrontFirestore>();
builder.Services.AddScoped<IMuuqsimoService, FirestoreMuuqsimoService>();
builder.Services.AddScoped<IAdminSystemService, FirestoreAdminSystemService>();
builder.Services.AddScoped<IProfileService, FirestoreProfileService>();
builder.Services.AddScoped<IVoteService, FirestoreVoteService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AdminOrdersTabCoordinator>();
builder.Services.AddScoped<AdminProductsTabCoordinator>();
builder.Services.AddScoped<AdminCustomersTabCoordinator>();
builder.Services.AddScoped<AdminContentTabCoordinator>();
builder.Services.AddScoped<AdminAffiliatesTabCoordinator>();
builder.Services.AddScoped<AdminSupportTabCoordinator>();
builder.Services.AddScoped<AdminCareersTabCoordinator>();
builder.Services.AddScoped<AdminSystemTechTabCoordinator>();
builder.Services.AddScoped<AdminSystemTechIntegrationService>();
builder.Services.AddSingleton<ICareerPageSettingsService, CareerPageSettingsService>();
builder.Services.AddScoped<AdminLowStockCacheService>();
builder.Services.AddScoped<AdminBadgeCountsCacheService>();
builder.Services.AddScoped<AdminContentCountsCacheService>();
builder.Services.AddScoped<NavigationLoadingService>();
builder.Services.AddScoped<StorefrontHomeCacheService>();
builder.Services.AddScoped<CartStateService>();
builder.Services.AddScoped<WishlistStateService>();
builder.Services.AddScoped<FirebaseSession>();
builder.Services.AddScoped<BrowserTokenStore>();
builder.Services.AddScoped<IFirebaseIdTokenSource>(sp => sp.GetRequiredService<FirebaseSession>());
builder.Services.AddTransient<FirebaseIdTokenHandler>();
builder.Services.AddHttpClient("MuuqWearFunctions", client =>
{
    var functionsBase = builder.Configuration["FunctionsBaseUrl"];
    if (!string.IsNullOrWhiteSpace(functionsBase))
        client.BaseAddress = new Uri(functionsBase);
});
builder.Services.AddScoped<CheckoutSessionClient>();
builder.Services.AddScoped<OfflineQueue>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticatedHttpHandler>();
builder.Services.AddSingleton<AuthSessionService>();
builder.Services.AddTransient<AffiliateCookieHandler>();

builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<IContentService, FirestoreContentService>();
builder.Services.AddScoped<IArchiveService, FirestoreArchiveService>();
builder.Services.AddScoped<IHelpCenterService, FirestoreHelpCenterService>();
builder.Services.AddScoped<INotificationService, LocalNotificationService>();
builder.Services.AddSingleton<NotificationRealtimeService>();
// Add after other services
builder.Services.AddScoped<IAffiliateService, FirestoreAffiliateService>();
builder.Services.AddScoped<IChatService, FirestoreChatService>();
builder.Services.AddScoped<IPaymentService, LocalPaymentService>();

builder.Services.AddScoped<IAdminBadgeService, FirestoreAdminBadgeService>();

builder.Services.AddScoped<IJobPostingService, FirestoreJobPostingService>();



builder.Services.AddSingleton<HmacAuthTicketFormat>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "__session";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
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
                if (context.Request.Path.StartsWithSegments("/admin"))
                    context.Response.Redirect("/admin/access-denied");
                else
                    context.Response.Redirect("/login");

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .PostConfigure<HmacAuthTicketFormat>((options, format) =>
    {
        options.TicketDataFormat = format;
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
builder.Services.AddAdminPortalAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();
app.Use(async (ctx, next) =>
{
    var host = ctx.Request.Host.Host;
    if (host.Contains("run.app", StringComparison.OrdinalIgnoreCase)
        || host.Contains("web.app", StringComparison.OrdinalIgnoreCase)
        || host.Contains("firebaseapp.com", StringComparison.OrdinalIgnoreCase))
    {
        var canonical = ctx.RequestServices.GetRequiredService<IConfiguration>()["PublicHost"];
        if (string.IsNullOrWhiteSpace(canonical))
            canonical = "muuqwear.com";
        ctx.Request.Host = new HostString(canonical);
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<FirebaseBearerMiddleware>();
app.UseAuthorization();
app.UseMiddleware<TokenRefreshMiddleware>(); // ← add this line
app.UseMiddleware<AffiliateTrackingMiddleware>();
app.UseAntiforgery();
app.Use(async (ctx, next) =>
{
    var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
    if (CutoverFreeze.Enabled(config["Cutover:FreezeWrites"])
        && !CutoverFreeze.Allows(ctx.Request.Method, ctx.Request.Path)
        && (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/auth")))
    {
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await ctx.Response.WriteAsJsonAsync(new { error = PageCopy.WritesPaused });
        return;
    }

    await next();
});

app.MapAuthCookies();

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/login");
});

app.MapGet("/auth/action", ServeAuthAction);
app.MapGet("/__/auth/action", ServeAuthAction);

app.MapGet("/auth/clear", async (HttpContext ctx, bool expired = false) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect(expired ? "/login?expired=true" : "/");
});

app.MapGet("/api/Product/{**rest}", ServeStorefrontCatalog);
app.MapGet("/api/Category/{**rest}", ServeStorefrontCatalog);
app.MapGet("/api/Journal", ServeStorefrontCatalog);
app.MapGet("/api/Journal/{**rest}", ServeStorefrontCatalog);
app.MapShopperAccount();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MuuqWear.Client.ClientAssemblyMarker).Assembly);

app.Run();

static IResult ServeAuthAction(IWebHostEnvironment env) =>
    Results.File(Path.Combine(env.WebRootPath, "auth", "action.html"), "text/html; charset=utf-8");

static async Task ServeStorefrontCatalog(HttpContext ctx, StorefrontFirestore store)
{
    var payload = await store.HandleAsync(ctx.Request, ctx.RequestAborted);
    ctx.Response.StatusCode = StatusCodes.Status200OK;
    await ctx.Response.WriteAsJsonAsync(payload, ctx.RequestAborted);
}
