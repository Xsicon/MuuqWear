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
using MuuqWear.Client.Checkout;
using MuuqWear.Client.Offline;
using MuuqWear.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var site = new Uri(builder.HostEnvironment.BaseAddress);
if (site.Host is "localhost" or "127.0.0.1")
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Cutover:FreezeWrites"] = "true"
    });
}
builder.Services.AddScoped<BrowserTokenStore>();
builder.Services.AddScoped<FirebaseSession>();
builder.Services.AddScoped<IFirebaseIdTokenSource>(sp => sp.GetRequiredService<FirebaseSession>());
builder.Services.AddTransient<FirebaseIdTokenHandler>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddScoped<IAuthService, BrowserAuthService>();
builder.Services.AddTransient<CatalogCacheHandler>();
builder.Services.AddHttpClient<IProductService, ProductService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<ICategoryService, CategoryService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<IJournalService, JournalService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
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

var host = builder.Build();
await host.Services.GetRequiredService<BrowserTokenStore>().RestoreAsync();
await host.Services.GetRequiredService<FirebaseSession>().RestoreAsync();
await host.RunAsync();
