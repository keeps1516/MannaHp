using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class AvailableIngredientEndpointsTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid MiBowl = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    private static readonly Guid MiLatte = Guid.Parse("c0000000-0009-0000-0000-000000000009");
    private static readonly Guid IngChicken = Guid.Parse("b0000000-0004-0000-0000-000000000004");
    private static readonly Guid IngIce = Guid.Parse("b0000000-0022-0000-0000-000000000034");

    public AvailableIngredientEndpointsTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── GET /api/menu-items/{id}/available-ingredients ─────────────────

    [Fact]
    public async Task GetAll_ReturnsGroupedIngredients()
    {
        var ingredients = await _client.GetFromJsonAsync<List<AvailableIngredientDto>>(
            $"/api/menu-items/{MiBowl}/available-ingredients");

        ingredients.Should().NotBeNull();
        ingredients!.Should().HaveCountGreaterThanOrEqualTo(10);

        var groups = ingredients.Select(i => i.GroupName).Distinct().ToList();
        groups.Should().Contain("Bases");
        groups.Should().Contain("Proteins");
    }

    [Fact]
    public async Task GetAll_NonexistentMenuItem_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/menu-items/{Guid.NewGuid()}/available-ingredients");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/menu-items/{id}/available-ingredients ────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateAvailableIngredientRequest(IngIce, 0.00m, 1m, false, "Extras", 99);

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiBowl}/available-ingredients", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var available = await response.Content.ReadFromJsonAsync<AvailableIngredientDto>();
        available!.IngredientId.Should().Be(IngIce);
        available.CustomerPrice.Should().Be(0.00m);
        available.GroupName.Should().Be("Extras");
        available.Active.Should().BeTrue();
    }

    [Fact]
    public async Task Post_InvalidIngredientId_Returns400()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateAvailableIngredientRequest(Guid.NewGuid(), 1.00m, 1m, false, "Test", 1);

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiBowl}/available-ingredients", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NonexistentMenuItem_Returns404()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateAvailableIngredientRequest(IngChicken, 3.00m, 6m, false, "Proteins", 1);

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{Guid.NewGuid()}/available-ingredients", req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/menu-items/{id}/available-ingredients/{availableId} ───

    [Fact]
    public async Task Put_ValidRequest_UpdatesAvailableIngredient()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create one first
        var createReq = new CreateAvailableIngredientRequest(IngIce, 0.50m, 2m, false, "Extras", 50);
        var createResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiLatte}/available-ingredients", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<AvailableIngredientDto>();

        var updateReq = new UpdateAvailableIngredientRequest(1.00m, 3m, true, "Add-Ons", 51, true);
        var response = await ownerClient.PutAsJsonAsync(
            $"/api/menu-items/{MiLatte}/available-ingredients/{created!.Id}", updateReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<AvailableIngredientDto>();
        updated!.CustomerPrice.Should().Be(1.00m);
        updated.GroupName.Should().Be("Add-Ons");
        updated.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Put_NonexistentId_Returns404()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new UpdateAvailableIngredientRequest(1.00m, 1m, false, "Test", 1, true);

        var response = await ownerClient.PutAsJsonAsync(
            $"/api/menu-items/{MiBowl}/available-ingredients/{Guid.NewGuid()}", req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/menu-items/{id}/available-ingredients/{availableId} ─

    [Fact]
    public async Task Delete_ExistingAvailableIngredient_SoftDeletes()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create one to delete
        var createReq = new CreateAvailableIngredientRequest(IngIce, 0.25m, 1m, false, "Extras", 98);
        var createResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiLatte}/available-ingredients", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<AvailableIngredientDto>();

        var response = await ownerClient.DeleteAsync(
            $"/api/menu-items/{MiLatte}/available-ingredients/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft-deleted
        var all = await _client.GetFromJsonAsync<List<AvailableIngredientDto>>(
            $"/api/menu-items/{MiLatte}/available-ingredients");
        var deleted = all!.FirstOrDefault(a => a.Id == created.Id);
        deleted.Should().NotBeNull();
        deleted!.Active.Should().BeFalse();
    }
}
