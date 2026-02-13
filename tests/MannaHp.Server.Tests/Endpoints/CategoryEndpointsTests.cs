using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class CategoryEndpointsTests
{
    private readonly HttpClient _client;

    // Known seed GUIDs
    private static readonly Guid CatBowls = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");

    public CategoryEndpointsTests(MannaApiFactory factory) => _client = factory.CreateClient();

    // ── GET /api/categories ─────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsSeededCategories()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        categories.Should().NotBeNull();
        categories!.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetAll_ContainsSeededCategoriesInOrder()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        // Verify the seeded categories exist (other tests may have added more)
        var names = categories!.Select(c => c.Name).ToList();
        names.Should().Contain("Bowls");
        names.Should().Contain("Traditional Drinks");
        names.Should().Contain("Seasonal Specials");
        names.Should().Contain("Sides & Drinks");
        names.Should().Contain("Add-Ons");
    }

    // ── GET /api/categories/{id} ────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingCategory_ReturnsCategory()
    {
        var category = await _client.GetFromJsonAsync<CategoryDto>($"/api/categories/{CatBowls}");

        category.Should().NotBeNull();
        category!.Name.Should().Be("Bowls");
        category.SortOrder.Should().Be(1);
        category.Active.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_NonexistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/categories ────────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201WithCategory()
    {
        var req = new CreateCategoryRequest("Test Category Post", 99);
        var response = await _client.PostAsJsonAsync("/api/categories", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        category.Should().NotBeNull();
        category!.Name.Should().Be("Test Category Post");
        category.SortOrder.Should().Be(99);
        category.Active.Should().BeTrue();
    }

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        var req = new CreateCategoryRequest("", 1);
        var response = await _client.PostAsJsonAsync("/api/categories", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NegativeSortOrder_Returns400()
    {
        var req = new CreateCategoryRequest("Bad Sort", -1);
        var response = await _client.PostAsJsonAsync("/api/categories", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/categories/{id} ────────────────────────────────────

    [Fact]
    public async Task Put_ValidRequest_UpdatesCategory()
    {
        // Create a category specifically for this test so we don't mutate seed data
        var createReq = new CreateCategoryRequest("Category To Update", 50);
        var createResp = await _client.PostAsJsonAsync("/api/categories", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<CategoryDto>();

        var req = new UpdateCategoryRequest("Updated Category", 51, true);
        var response = await _client.PutAsJsonAsync($"/api/categories/{created!.Id}", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        category!.Name.Should().Be("Updated Category");
        category.SortOrder.Should().Be(51);
    }

    [Fact]
    public async Task Put_NonexistentId_Returns404()
    {
        var req = new UpdateCategoryRequest("Whatever", 1, true);
        var response = await _client.PutAsJsonAsync($"/api/categories/{Guid.NewGuid()}", req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/categories/{id} ─────────────────────────────────

    [Fact]
    public async Task Delete_ExistingCategory_Returns204_SoftDeletes()
    {
        // Create a category specifically for this test so we don't affect other tests
        var createReq = new CreateCategoryRequest("Category To Delete", 98);
        var createResp = await _client.PostAsJsonAsync("/api/categories", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<CategoryDto>();

        var response = await _client.DeleteAsync($"/api/categories/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft delete — category still exists but Active is false
        var category = await _client.GetFromJsonAsync<CategoryDto>($"/api/categories/{created.Id}");
        category!.Active.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_NonexistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/categories/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
