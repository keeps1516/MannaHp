using System.Text;
using FluentValidation;
using MannaHp.Server.Data;
using MannaHp.Server.Endpoints;
using MannaHp.Server.Hubs;
using MannaHp.Server.Services;
using MannaHp.Shared.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Sentry error tracking
builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"] ?? "";
    o.TracesSampleRate = 0.2;
    o.SendDefaultPii = false;
});

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

// Stripe
builder.Services.AddSingleton<StripeService>();

// SignalR for real-time order updates
builder.Services.AddSignalR();

var app = builder.Build();

app.UseSentryTracing();
app.UseCors("NextClient");

app.UseAuthentication();
app.UseAuthorization();

// Apply pending migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
    await db.Database.MigrateAsync();
}

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
app.MapStripeWebhookEndpoints();

// SignalR hubs
app.MapHub<OrderHub>("/hubs/orders");

app.Run();

public partial class Program { }
