using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class MenuItemEndpointsTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid CatBowls = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid MiBowl = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    private static readonly Guid MiLatte = Guid.Parse("c0000000-0009-0000-0000-000000000009");

    public MenuItemEndpointsTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── GET /api/menu-items ─────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsSeededMenuItems()
    {
        var items = await _client.GetFromJsonAsync<List<MenuItemDto>>("/api/menu-items");

        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterThanOrEqualTo(36);
    }

    // ── GET /api/menu-items/{id} — Customizable ─────────────────────

    [Fact]
    public async Task GetById_CustomizableItem_ReturnsAvailableIngredients()
    {
        var item = await _client.GetFromJsonAsync<MenuItemDto>($"/api/menu-items/{MiBowl}");

        item.Should().NotBeNull();
        item!.Name.Should().Be("Burrito Bowl");
        item.IsCustomizable.Should().BeTrue();
        item.AvailableIngredients.Should().NotBeNull();
        item.AvailableIngredients!.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetById_CustomizableItem_IngredientsHaveGroups()
    {
        var item = await _client.GetFromJsonAsync<MenuItemDto>($"/api/menu-items/{MiBowl}");

        var groups = item!.AvailableIngredients!.Select(i => i.GroupName).Distinct().ToList();
        groups.Should().Contain("Bases");
        groups.Should().Contain("Proteins");
        groups.Should().Contain("Fresh Toppings");
    }

    // ── GET /api/menu-items/{id} — Fixed ────────────────────────────

    [Fact]
    public async Task GetById_FixedItem_ReturnsAvailableAddOns()
    {
        var item = await _client.GetFromJsonAsync<MenuItemDto>($"/api/menu-items/{MiLatte}");

        item.Should().NotBeNull();
        item!.Name.Should().Be("Latte");
        item.IsCustomizable.Should().BeFalse();
        // Latte has add-ons (espresso shot, alt milk, whipped cream)
        item.AvailableIngredients.Should().NotBeNull();
        item.AvailableIngredients!.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_FixedItem_ReturnsVariants()
    {
        var item = await _client.GetFromJsonAsync<MenuItemDto>($"/api/menu-items/{MiLatte}");

        item!.Variants.Should().HaveCount(2);
        item.Variants.Select(v => v.Name).Should().Contain("12oz").And.Contain("16oz");
    }

    // ── POST /api/menu-items ────────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateMenuItemRequest(CatBowls, "Test Item", "Description", null, false, false, 99);
        var response = await ownerClient.PostAsJsonAsync("/api/menu-items", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var item = await response.Content.ReadFromJsonAsync<MenuItemDto>();
        item!.Name.Should().Be("Test Item");
        item.CategoryId.Should().Be(CatBowls);
    }

    [Fact]
    public async Task Post_NonexistentCategoryId_Returns400()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateMenuItemRequest(Guid.NewGuid(), "Bad Item", null, null, false, false, 0);
        var response = await ownerClient.PostAsJsonAsync("/api/menu-items", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateMenuItemRequest(CatBowls, "", null, null, false, false, 0);
        var response = await ownerClient.PostAsJsonAsync("/api/menu-items", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
