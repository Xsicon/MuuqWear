using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Services.AddressService;
using MuuqWear.Application.Services.AffiliateService;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.JournalService;
using MuuqWear.Application.Services.OrderService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Services.ProfileService;
using MuuqWear.Application.Services.WishlistService;
using MuuqWear.Client.Auth;
using MuuqWear.Client.Catalog;
using MuuqWear.Client.Layout;
using MuuqWear.Client.Checkout;
using MuuqWear.Client.Offline;
using MuuqWear.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
if (!builder.HostEnvironment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
    builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
}
var site = new Uri(builder.HostEnvironment.BaseAddress);
var localDev = site.Host is "localhost" or "127.0.0.1" or "::1"
    || builder.HostEnvironment.IsDevelopment();
if (localDev)
{
    // Local Web host renders ShopperRoutes from App.razor — never mount #app here.
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Shopper:Standalone"] = "false",
        ["Cutover:FreezeWrites"] = "false"
    });
}
builder.Services.AddScoped<BrowserTokenStore>();
builder.Services.AddScoped<FirebaseSession>();
builder.Services.AddScoped<IFirebaseIdTokenSource>(sp => sp.GetRequiredService<FirebaseSession>());
builder.Services.AddTransient<FirebaseIdTokenHandler>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddScoped<IAuthService, BrowserAuthService>();
builder.Services.AddScoped<BrowserCatalog>();
builder.Services.AddScoped<IProductService, BrowserProductService>();
builder.Services.AddScoped<ICategoryService, BrowserCategoryService>();
builder.Services.AddScoped<IJournalService, BrowserJournalService>();
builder.Services.AddHttpClient<IProfileService, ProfileService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<FirebaseIdTokenHandler>();
builder.Services.AddHttpClient<IAddressService, AddressService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<FirebaseIdTokenHandler>();
builder.Services.AddHttpClient<IOrderService, OrderService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<FirebaseIdTokenHandler>();
builder.Services.AddHttpClient<IAffiliateService, AffiliateService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<FirebaseIdTokenHandler>();
var functionsBase = builder.Configuration["FunctionsBaseUrl"];
if (site.Host is "localhost" or "127.0.0.1")
    functionsBase = "http://127.0.0.1:5001/muuqwear-6e2e0/europe-west2/";
var functionsClient = builder.Services.AddHttpClient("MuuqWearFunctions");
if (!string.IsNullOrWhiteSpace(functionsBase))
    functionsClient.ConfigureHttpClient(client => client.BaseAddress = new Uri(functionsBase));
functionsClient.AddHttpMessageHandler<FirebaseIdTokenHandler>();
builder.Services.AddScoped<CheckoutSessionClient>();
builder.Services.AddScoped<CartStateService>();
builder.Services.AddScoped<WishlistStateService>();
builder.Services.AddScoped<StorefrontHomeCacheService>();
builder.Services.AddScoped<OfflineQueue>();

// Static Hosting / Production Web shell uses #app.
// Local Dev hosts ShopperRoutes from App.razor — never mount here (WASM often reports Production on localhost).
if (!localDev && (builder.Configuration.GetValue("Shopper:Standalone", false)
    || builder.HostEnvironment.IsProduction()))
{
    builder.RootComponents.Add<HeadOutlet>("head::after");
    builder.RootComponents.Add<ShopperRoutes>("#app");
}

var host = builder.Build();
await host.Services.GetRequiredService<BrowserTokenStore>().RestoreAsync();
await host.Services.GetRequiredService<FirebaseSession>().RestoreAsync();
await host.RunAsync();
