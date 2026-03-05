using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class VariantEndpointsTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid MiLatte = Guid.Parse("c0000000-0009-0000-0000-000000000009");
    private static readonly Guid MiBowl = Guid.Parse("c0000000-0001-0000-0000-000000000001");

    public VariantEndpointsTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── GET /api/menu-items/{id}/variants ─────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsVariantsOrderedBySortOrder()
    {
        var variants = await _client.GetFromJsonAsync<List<MenuItemVariantDto>>(
            $"/api/menu-items/{MiLatte}/variants");

        variants.Should().NotBeNull();
        variants!.Should().HaveCountGreaterThanOrEqualTo(2);
        // Seed variants should appear in order; filter to active seed variants
        var activeNames = variants.Where(v => v.Active).Select(v => v.Name).ToList();
        activeNames.Should().ContainInOrder("12oz", "16oz");
    }

    [Fact]
    public async Task GetAll_NonexistentMenuItem_Returns404()
    {
        var response = await _client.GetAsync($"/api/menu-items/{Guid.NewGuid()}/variants");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/menu-items/{id}/variants ────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateVariantRequest("24oz", 7.00m, 3);

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiLatte}/variants", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var variant = await response.Content.ReadFromJsonAsync<MenuItemVariantDto>();
        variant!.Name.Should().Be("24oz");
        variant.Price.Should().Be(7.00m);
        variant.SortOrder.Should().Be(3);
        variant.Active.Should().BeTrue();
    }

    [Fact]
    public async Task Post_NonexistentMenuItem_Returns404()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateVariantRequest("Small", 3.00m, 1);

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{Guid.NewGuid()}/variants", req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new CreateVariantRequest("", 3.00m, 1);

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiLatte}/variants", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/menu-items/{id}/variants/{variantId} ─────────────────

    [Fact]
    public async Task Put_ValidRequest_UpdatesVariant()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create a variant to update
        var createReq = new CreateVariantRequest("ToUpdate", 5.00m, 10);
        var createResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiLatte}/variants", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<MenuItemVariantDto>();

        var updateReq = new UpdateVariantRequest("Updated", 6.50m, 11, true);
        var response = await ownerClient.PutAsJsonAsync(
            $"/api/menu-items/{MiLatte}/variants/{created!.Id}", updateReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var variant = await response.Content.ReadFromJsonAsync<MenuItemVariantDto>();
        variant!.Name.Should().Be("Updated");
        variant.Price.Should().Be(6.50m);
        variant.SortOrder.Should().Be(11);
    }

    [Fact]
    public async Task Put_NonexistentVariant_Returns404()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();
        var req = new UpdateVariantRequest("Whatever", 5.00m, 1, true);

        var response = await ownerClient.PutAsJsonAsync(
            $"/api/menu-items/{MiLatte}/variants/{Guid.NewGuid()}", req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/menu-items/{id}/variants/{variantId} ──────────────

    [Fact]
    public async Task Delete_ExistingVariant_SoftDeletes()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create a variant to delete
        var createReq = new CreateVariantRequest("ToDelete", 4.00m, 20);
        var createResp = await ownerClient.PostAsJsonAsync(
            $"/api/menu-items/{MiLatte}/variants", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<MenuItemVariantDto>();

        var response = await ownerClient.DeleteAsync(
            $"/api/menu-items/{MiLatte}/variants/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft-deleted — variant still in list but Active = false
        var variants = await _client.GetFromJsonAsync<List<MenuItemVariantDto>>(
            $"/api/menu-items/{MiLatte}/variants");
        var deleted = variants!.FirstOrDefault(v => v.Id == created.Id);
        deleted.Should().NotBeNull();
        deleted!.Active.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_NonexistentVariant_Returns404()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        var response = await ownerClient.DeleteAsync(
            $"/api/menu-items/{MiLatte}/variants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
