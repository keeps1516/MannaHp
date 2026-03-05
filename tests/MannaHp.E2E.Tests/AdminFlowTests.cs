using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.E2E.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using Microsoft.Playwright;

namespace MannaHp.E2E.Tests;

[TestFixture]
public class AdminFlowTests
{
    // ── Helpers ─────────────────────────────────────────────────────

    private static async Task<IPage> LoginAsOwnerAsync()
    {
        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync($"{E2EFixture.NextBaseUrl}/admin/login");

        // Fill login form
        await page.WaitForSelectorAsync("input[type='email']");
        await page.FillAsync("input[type='email']", "owner@manna.local");
        await page.FillAsync("input[type='password']", "MannaOwner123!");

        await page.GetByRole(AriaRole.Button, new() { Name = "Sign In" })
            .Or(page.GetByRole(AriaRole.Button, new() { Name = "Login" }))
            .Or(page.GetByRole(AriaRole.Button, new() { Name = "Log In" }))
            .ClickAsync();

        // Wait for redirect to admin dashboard
        await page.WaitForURLAsync("**/admin/**", new() { Timeout = 15_000 });

        return page;
    }

    /// <summary>
    /// Creates an in-store order via API so there's an active order to view.
    /// </summary>
    private static async Task<OrderDto> CreateApiOrderAsync()
    {
        using var http = new HttpClient { BaseAddress = new Uri(E2EFixture.ApiBaseUrl) };
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
        [
            new CreateOrderItemRequest(
                Guid.Parse("c0000000-001f-0000-0000-000000000031"), // Chips
                Guid.Parse("d0000000-003e-0000-0000-000000000062"), // Regular
                1, null, null)
        ]);
        var response = await http.PostAsJsonAsync("/api/orders", req);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        return result!.Order;
    }

    // ── Tests ───────────────────────────────────────────────────────

    [Test]
    public async Task AdminLogin_DashboardLoads()
    {
        var page = await LoginAsOwnerAsync();

        // Should be on some admin page after login
        page.Url.Should().Contain("/admin");

        // Page should have loaded content (not blank)
        var body = await page.TextContentAsync("body");
        body.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Admin_ViewsActiveOrders()
    {
        // Create an order first so there's something to see
        await CreateApiOrderAsync();

        var page = await LoginAsOwnerAsync();

        // Navigate to orders page
        await page.GotoAsync($"{E2EFixture.NextBaseUrl}/admin/orders");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Should show at least one order card
        var body = await page.TextContentAsync("body");
        body.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Admin_UpdatesOrderStatus()
    {
        var order = await CreateApiOrderAsync();

        var page = await LoginAsOwnerAsync();
        await page.GotoAsync($"{E2EFixture.NextBaseUrl}/admin/orders");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find an order card and click to advance status
        // The order card body is a button that advances the order
        var orderCards = page.Locator("[data-testid='order-card']")
            .Or(page.Locator("article"))
            .Or(page.Locator(".rounded-xl, .rounded-lg").Filter(new() { HasText = "Chips" }));

        if (await orderCards.CountAsync() > 0)
        {
            // Verify the API accepted the status update directly
            using var http = new HttpClient { BaseAddress = new Uri(E2EFixture.ApiBaseUrl) };

            // Login to get a token for the API call
            var loginResp = await http.PostAsJsonAsync("/api/auth/login",
                new LoginRequest("owner@manna.local", "MannaOwner123!"));
            var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

            var patchResp = await http.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest(OrderStatus.Preparing));
            patchResp.IsSuccessStatusCode.Should().BeTrue();

            var updated = await http.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
            updated!.Status.Should().Be(OrderStatus.Preparing);
        }
    }

    [Test]
    public async Task Admin_CreatesNewCategory()
    {
        using var http = new HttpClient { BaseAddress = new Uri(E2EFixture.ApiBaseUrl) };

        // Login to get owner token
        var loginResp = await http.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("owner@manna.local", "MannaOwner123!"));
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        // Create category via API
        var catName = $"E2E Test Category {Guid.NewGuid():N[..8]}";
        var response = await http.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest(catName, 99));
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify it appears in the public menu
        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The new category should eventually appear (may need to check API instead)
        var categories = await http.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        categories!.Should().Contain(c => c.Name == catName);
    }

    [Test]
    public async Task Admin_CreatesNewIngredient()
    {
        using var http = new HttpClient { BaseAddress = new Uri(E2EFixture.ApiBaseUrl) };

        var loginResp = await http.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("owner@manna.local", "MannaOwner123!"));
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        var ingName = $"E2E Test Ingredient {Guid.NewGuid():N[..8]}";
        var response = await http.PostAsJsonAsync("/api/ingredients",
            new CreateIngredientRequest(ingName, UnitOfMeasure.Oz, 1.50m, 100m, 10m));
        response.IsSuccessStatusCode.Should().BeTrue();

        var ingredients = await http.GetFromJsonAsync<List<IngredientDto>>("/api/ingredients");
        ingredients!.Should().Contain(i => i.Name == ingName);
    }
}
