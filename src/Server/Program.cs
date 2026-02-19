using System.Text;
using FluentValidation;
#if !DOCKER
using MannaHp.Client.Services;
using MannaHp.Server.Components;
#endif
using MannaHp.Server.Data;
using MannaHp.Server.Endpoints;
using MannaHp.Server.Hubs;
using MannaHp.Server.Services;
using MannaHp.Shared.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
#if !DOCKER
using MudBlazor.Services;
#endif

var builder = WebApplication.CreateBuilder(args);

// CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("NextClient", policy =>
    {
        var origins = builder.Configuration["CorsOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

#if !DOCKER
// Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();
#endif

// EF Core + PostgreSQL
builder.Services.AddDbContext<MannaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateCategoryRequestValidator>();

// Identity
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<MannaDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Owner", policy => policy.RequireRole("Owner"))
    .AddPolicy("Staff", policy => policy.RequireRole("Owner", "Staff"));

builder.Services.AddSingleton<TokenService>();

// SignalR for real-time order updates
builder.Services.AddSignalR();

#if !DOCKER
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
#endif

var app = builder.Build();

#if !DOCKER
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
#endif

app.UseCors("NextClient");

app.UseAuthentication();
app.UseAuthorization();

#if !DOCKER
app.MapStaticAssets();
app.UseAntiforgery();
#endif

// Seed owner account at startup
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var owner = await userManager.FindByEmailAsync("owner@manna.local");
    if (owner is null)
    {
        owner = new AppUser
        {
            UserName = "owner@manna.local",
            Email = "owner@manna.local",
            EmailConfirmed = true,
            DisplayName = "Owner",
            Role = "Owner"
        };
        await userManager.CreateAsync(owner, "MannaOwner123!");
        await userManager.AddToRoleAsync(owner, "Owner");
    }
}

// Minimal API endpoints
app.MapAuthEndpoints();
app.MapCategoryEndpoints();
app.MapIngredientEndpoints();
app.MapMenuItemEndpoints();
app.MapVariantEndpoints();
app.MapAvailableIngredientEndpoints();
app.MapRecipeIngredientEndpoints();
app.MapOrderEndpoints();

// SignalR hubs
app.MapHub<OrderHub>("/hubs/orders");

#if !DOCKER
// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MannaHp.Client._Imports).Assembly);
#endif

app.Run();

public partial class Program { }
