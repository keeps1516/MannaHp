using FluentValidation;
using MannaHp.Client.Services;
using MannaHp.Server.Components;
using MannaHp.Server.Data;
using MannaHp.Server.Endpoints;
using MannaHp.Shared.Validators;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("NextClient", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();

// EF Core + PostgreSQL
builder.Services.AddDbContext<MannaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateCategoryRequestValidator>();

// Client services (needed during server-side prerendering)
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();

// HttpClient for server-side rendering (calls back to own API)
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseCors("NextClient");

app.MapStaticAssets();
app.UseAntiforgery();

// Minimal API endpoints
app.MapCategoryEndpoints();
app.MapIngredientEndpoints();
app.MapMenuItemEndpoints();
app.MapVariantEndpoints();
app.MapAvailableIngredientEndpoints();
app.MapRecipeIngredientEndpoints();
app.MapOrderEndpoints();

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MannaHp.Client._Imports).Assembly);

app.Run();

public partial class Program { }
