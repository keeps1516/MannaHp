using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class IngredientEndpointsTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid IngChicken = Guid.Parse("b0000000-0004-0000-0000-000000000004");

    public IngredientEndpointsTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── GET /api/ingredients ────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsSeededIngredients()
    {
        var ingredients = await _client.GetFromJsonAsync<List<IngredientDto>>("/api/ingredients");

        ingredients.Should().NotBeNull();
        ingredients!.Should().HaveCountGreaterThanOrEqualTo(34);
    }

    [Fact]
    public async Task GetAll_OrderedByName()
    {
        var ingredients = await _client.GetFromJsonAsync<List<IngredientDto>>("/api/ingredients");

        ingredients!.Select(i => i.Name).Should().BeInAscendingOrder();
    }

    // ── GET /api/ingredients/{id} ───────────────────────────────────

    [Fact]
    public async Task GetById_ExistingIngredient_ReturnsIngredient()
    {
        var ingredient = await _client.GetFromJsonAsync<IngredientDto>($"/api/ingredients/{IngChicken}");

        ingredient.Should().NotBeNull();
        ingredient!.Name.Should().Be("Chicken");
        ingredient.Unit.Should().Be(UnitOfMeasure.Oz);
        ingredient.CostPerUnit.Should().Be(0.3125m);
    }

    // ── POST /api/ingredients ───────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateIngredientRequest("Test Ingredient", UnitOfMeasure.Lb, 5.00m, 100m, 10m);
        var response = await ownerClient.PostAsJsonAsync("/api/ingredients", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var ingredient = await response.Content.ReadFromJsonAsync<IngredientDto>();
        ingredient!.Name.Should().Be("Test Ingredient");
        ingredient.Unit.Should().Be(UnitOfMeasure.Lb);
    }

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateIngredientRequest("", UnitOfMeasure.Oz, 1m, 10m, 5m);
        var response = await ownerClient.PostAsJsonAsync("/api/ingredients", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
