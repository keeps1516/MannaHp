using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class RecipeIngredientEndpointsTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid MiDrip = Guid.Parse("c0000000-0002-0000-0000-000000000002");
    private static readonly Guid VDrip12 = Guid.Parse("d0000000-0002-0000-0000-000000000002");
    private static readonly Guid IngCoffee = Guid.Parse("b0000000-000d-0000-0000-000000000013");
    private static readonly Guid IngMilk = Guid.Parse("b0000000-000f-0000-0000-000000000015");

    private string BasePath(Guid menuItemId, Guid variantId) =>
        $"/api/menu-items/{menuItemId}/variants/{variantId}/recipe-ingredients";

    public RecipeIngredientEndpointsTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── GET ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsRecipeIngredients()
    {
        var recipes = await _client.GetFromJsonAsync<List<RecipeIngredientDto>>(
            BasePath(MiDrip, VDrip12));

        recipes.Should().NotBeNull();
        recipes!.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAll_NonexistentVariant_Returns404()
    {
        var response = await _client.GetAsync(BasePath(MiDrip, Guid.NewGuid()));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create a fresh variant to add recipe ingredients to
        var variantReq = new CreateVariantRequest("RecipeTest", 3.00m, 99);
        var variantResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiDrip}/variants", variantReq);
        var variant = await variantResp.Content.ReadFromJsonAsync<MenuItemVariantDto>();

        var req = new CreateRecipeIngredientRequest(IngCoffee, 12m);
        var response = await ownerClient.PostAsJsonAsync(
            BasePath(MiDrip, variant!.Id), req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var recipe = await response.Content.ReadFromJsonAsync<RecipeIngredientDto>();
        recipe!.IngredientId.Should().Be(IngCoffee);
        recipe.IngredientName.Should().Be("Coffee (brewed)");
        recipe.Quantity.Should().Be(12m);
    }

    [Fact]
    public async Task Post_NonexistentIngredient_Returns400()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create a fresh variant
        var variantReq = new CreateVariantRequest("RecipeBadIng", 3.00m, 99);
        var variantResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiDrip}/variants", variantReq);
        var variant = await variantResp.Content.ReadFromJsonAsync<MenuItemVariantDto>();

        var req = new CreateRecipeIngredientRequest(Guid.NewGuid(), 5m);
        var response = await ownerClient.PostAsJsonAsync(
            BasePath(MiDrip, variant!.Id), req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_ValidRequest_UpdatesQuantity()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create variant + recipe ingredient
        var variantReq = new CreateVariantRequest("RecipeUpdate", 3.00m, 99);
        var variantResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiDrip}/variants", variantReq);
        var variant = await variantResp.Content.ReadFromJsonAsync<MenuItemVariantDto>();

        var createReq = new CreateRecipeIngredientRequest(IngMilk, 4m);
        var createResp = await ownerClient.PostAsJsonAsync(
            BasePath(MiDrip, variant!.Id), createReq);
        var created = await createResp.Content.ReadFromJsonAsync<RecipeIngredientDto>();

        var updateReq = new UpdateRecipeIngredientRequest(8m);
        var response = await ownerClient.PutAsJsonAsync(
            $"{BasePath(MiDrip, variant.Id)}/{created!.Id}", updateReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<RecipeIngredientDto>();
        updated!.Quantity.Should().Be(8m);
    }

    // ── DELETE ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingRecipeIngredient_HardDeletes()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create variant + recipe ingredient
        var variantReq = new CreateVariantRequest("RecipeDelete", 3.00m, 99);
        var variantResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiDrip}/variants", variantReq);
        var variant = await variantResp.Content.ReadFromJsonAsync<MenuItemVariantDto>();

        var createReq = new CreateRecipeIngredientRequest(IngCoffee, 10m);
        var createResp = await ownerClient.PostAsJsonAsync(
            BasePath(MiDrip, variant!.Id), createReq);
        var created = await createResp.Content.ReadFromJsonAsync<RecipeIngredientDto>();

        var response = await ownerClient.DeleteAsync(
            $"{BasePath(MiDrip, variant.Id)}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify hard-deleted — no longer in list
        var recipes = await _client.GetFromJsonAsync<List<RecipeIngredientDto>>(
            BasePath(MiDrip, variant.Id));
        recipes!.Should().NotContain(r => r.Id == created.Id);
    }
}
