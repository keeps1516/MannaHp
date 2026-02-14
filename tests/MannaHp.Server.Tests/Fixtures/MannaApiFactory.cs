using System.Net.Http.Headers;
using System.Net.Http.Json;
using MannaHp.Server.Data;
using MannaHp.Shared.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MannaHp.Server.Tests.Fixtures;

public class MannaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=restaurant_test;Username=app;Password=devpassword123";

    // Cached tokens so we don't re-login on every request
    private string? _ownerToken;
    private string? _staffToken;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MannaDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Add DbContext pointed at the test database
            services.AddDbContext<MannaDbContext>(options =>
                options.UseNpgsql(TestConnectionString,
                    npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        });
    }

    public async ValueTask InitializeAsync()
    {
        // Create the test DB *before* the host starts (Program.cs seeds the owner on startup)
        // Use a standalone DbContext to avoid triggering the host
        var options = new DbContextOptionsBuilder<MannaDbContext>()
            .UseNpgsql(TestConnectionString,
                npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .Options;

        await using (var db = new MannaDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        // Now it's safe to access Services (triggers host startup + Program.cs owner seed)
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Program.cs already seeds owner@manna.local — add a staff account for tests
        var staff = new AppUser
        {
            UserName = "staff@manna.local",
            Email = "staff@manna.local",
            DisplayName = "Test Staff",
            Role = "Staff"
        };
        await userManager.CreateAsync(staff, "MannaStaff123!");
        await userManager.AddToRoleAsync(staff, "Staff");

        // Clear cached tokens
        _ownerToken = null;
        _staffToken = null;
    }

    /// <summary>
    /// Returns an HttpClient with a Bearer token for the Owner role.
    /// </summary>
    public async Task<HttpClient> CreateOwnerClientAsync()
    {
        _ownerToken ??= await LoginAsync("owner@manna.local", "MannaOwner123!");
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _ownerToken);
        return client;
    }

    /// <summary>
    /// Returns an HttpClient with a Bearer token for the Staff role.
    /// </summary>
    public async Task<HttpClient> CreateStaffClientAsync()
    {
        _staffToken ??= await LoginAsync("staff@manna.local", "MannaStaff123!");
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _staffToken);
        return client;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    public new async ValueTask DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
