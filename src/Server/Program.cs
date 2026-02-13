using FluentValidation;
using MannaHp.Server.Data;
using MannaHp.Server.Endpoints;
using MannaHp.Shared.Validators;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
        policy.WithOrigins("https://localhost:7063", "http://localhost:5012")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddDbContext<MannaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddValidatorsFromAssemblyContaining<CreateCategoryRequestValidator>();

var app = builder.Build();

app.UseCors("AllowClient");

app.MapCategoryEndpoints();
app.MapIngredientEndpoints();
app.MapMenuItemEndpoints();
app.MapVariantEndpoints();
app.MapAvailableIngredientEndpoints();
app.MapRecipeIngredientEndpoints();
app.MapOrderEndpoints();

app.Run();

public partial class Program { }
