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
using MuuqWear.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var site = new Uri(builder.HostEnvironment.BaseAddress);
var apiBase = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBase))
    apiBase = "http://localhost:5243/";
var api = new Uri(apiBase);

builder.Services.AddScoped<BrowserTokenStore>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient("MuuqWearApiRaw", client => client.BaseAddress = api);
builder.Services.AddHttpClient<IAuthService, BrowserAuthService>(client => client.BaseAddress = api)
    .AddHttpMessageHandler<BearerTokenHandler>();
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
builder.Services.AddScoped<CartStateService>();
builder.Services.AddScoped<WishlistStateService>();
builder.Services.AddScoped<StorefrontHomeCacheService>();

var host = builder.Build();
await host.Services.GetRequiredService<BrowserTokenStore>().RestoreAsync();
await host.RunAsync();
