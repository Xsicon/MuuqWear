using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MuuqWear.Application.Services.CartService;
using MuuqWear.Application.Services.CategoryService;
using MuuqWear.Application.Services.JournalService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Services.WishlistService;
using MuuqWear.Client.Catalog;
using MuuqWear.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var site = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddTransient<CatalogCacheHandler>();
builder.Services.AddHttpClient<IProductService, ProductService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<ICategoryService, CategoryService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<IJournalService, JournalService>(client => client.BaseAddress = site)
    .AddHttpMessageHandler<CatalogCacheHandler>();
builder.Services.AddHttpClient<ICartService, CartService>(client => client.BaseAddress = site);
builder.Services.AddHttpClient<IWishlistService, WishlistService>(client => client.BaseAddress = site);
builder.Services.AddScoped<CartStateService>();
builder.Services.AddScoped<WishlistStateService>();
builder.Services.AddScoped<StorefrontHomeCacheService>();

await builder.Build().RunAsync();
