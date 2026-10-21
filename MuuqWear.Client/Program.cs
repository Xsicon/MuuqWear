using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MuuqWear.Application.Services.AuthService;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.JournalService;
using MuuqWear.Application.Services.OrderService;
using MuuqWear.Application.Services.PaymentService;
using MuuqWear.Application.Services.ProductService;
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
var apiBase = builder.Configuration["ApiBaseUrl"];
if (site.Host is "localhost" or "127.0.0.1")
    apiBase = "http://localhost:5243/";
if (string.IsNullOrWhiteSpace(apiBase))
    apiBase = "http://localhost:5243/";
var api = new Uri(apiBase);

builder.Services.AddScoped<BrowserTokenStore>();
builder.Services.AddScoped<FirebaseSession>();
builder.Services.AddScoped<IFirebaseIdTokenSource>(sp => sp.GetRequiredService<FirebaseSession>());
builder.Services.AddTransient<FirebaseIdTokenHandler>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient("MuuqWearApiRaw", client => client.BaseAddress = api);
builder.Services.AddScoped<IAuthService, BrowserAuthService>();
builder.Services.AddTransient<CatalogCacheHandler>();
builder.Services.AddHttpClient<IProductService, ProductService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<ICategoryService, CategoryService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<IJournalService, JournalService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<ICartService, CartService>(client => client.BaseAddress = site);
builder.Services.AddHttpClient<IWishlistService, WishlistService>(client => client.BaseAddress = site);
builder.Services.AddHttpClient<IOrderService, OrderService>(client => client.BaseAddress = site);
builder.Services.AddHttpClient<IPaymentService, PaymentService>(client => client.BaseAddress = site);
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
