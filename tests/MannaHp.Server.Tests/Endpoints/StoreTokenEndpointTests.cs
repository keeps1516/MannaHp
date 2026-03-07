using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Data;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class StoreTokenEndpointTests
{
    private readonly MannaApiFactory _factory;

    public StoreTokenEndpointTests(MannaApiFactory factory)
    {
        _factory = factory;
    }

    // ── POST /api/store-tokens ──────────────────────────────────────

    [Fact]
    public async Task GenerateToken_AsStaff_CreatesTokenWithCorrectExpiry()
    {
        var client = await _factory.CreateStaffClientAsync();

        var response = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(3));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var token = await response.Content.ReadFromJsonAsync<StoreTokenResponse>();
        token.Should().NotBeNull();
        token!.Token.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GenerateToken_WithoutDuration_UsesDefaultFromSettings()
    {
        var client = await _factory.CreateStaffClientAsync();

        var response = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var token = await response.Content.ReadFromJsonAsync<StoreTokenResponse>();
        token.Should().NotBeNull();
        // Default is 7 days from AppSettings seed
        token!.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GenerateToken_RevokesExistingActiveToken()
    {
        var client = await _factory.CreateStaffClientAsync();

        // Generate first token
        var response1 = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        response1.EnsureSuccessStatusCode();
        var token1 = await response1.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Generate second token — should revoke first
        var response2 = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        response2.EnsureSuccessStatusCode();

        // Validate first token — should be invalid (revoked)
        var validateResponse = await _factory.CreateClient()
            .GetAsync($"/api/store-tokens/{token1!.Token}/validate");
        var validation = await validateResponse.Content.ReadFromJsonAsync<StoreTokenValidationResponse>();
        validation!.Valid.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateToken_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(7));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateToken_AsOwner_Succeeds()
    {
        var client = await _factory.CreateOwnerClientAsync();

        var response = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(7));

        // Owner has Staff policy access
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── GET /api/store-tokens/current ───────────────────────────────

    [Fact]
    public async Task GetCurrent_ReturnsActiveToken()
    {
        var client = await _factory.CreateStaffClientAsync();

        // Generate a token first
        await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));

        var response = await client.GetAsync("/api/store-tokens/current");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.Content.ReadFromJsonAsync<StoreTokenResponse>();
        token.Should().NotBeNull();
        token!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrent_DoesNotReturnExpiredTokens()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        // Create a token, then expire it directly in the DB
        var response = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(1));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<StoreTokenResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var dbToken = await db.StoreTokens.FindAsync(created!.Id);
        dbToken!.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        // Revoke all other active tokens to isolate this test
        var activeTokens = db.StoreTokens.Where(t => !t.Revoked && t.ExpiresAt > DateTime.UtcNow);
        foreach (var t in activeTokens) t.Revoked = true;
        await db.SaveChangesAsync();

        var getResponse = await staffClient.GetAsync("/api/store-tokens/current");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrent_DoesNotReturnRevokedTokens()
    {
        var client = await _factory.CreateStaffClientAsync();

        var response = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Revoke it
        await client.DeleteAsync($"/api/store-tokens/{created!.Id}");

        // Ensure no other active tokens
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var others = db.StoreTokens.Where(t => !t.Revoked && t.ExpiresAt > DateTime.UtcNow);
        foreach (var t in others) t.Revoked = true;
        await db.SaveChangesAsync();

        var getResponse = await client.GetAsync("/api/store-tokens/current");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/store-tokens/{id} ───────────────────────────────

    [Fact]
    public async Task RevokeToken_SetsRevokedTrue()
    {
        var client = await _factory.CreateStaffClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/store-tokens/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Validate it's no longer valid
        var validateResponse = await _factory.CreateClient()
            .GetAsync($"/api/store-tokens/{created.Token}/validate");
        var validation = await validateResponse.Content.ReadFromJsonAsync<StoreTokenValidationResponse>();
        validation!.Valid.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeToken_NonexistentId_Returns404()
    {
        var client = await _factory.CreateStaffClientAsync();

        var response = await client.DeleteAsync($"/api/store-tokens/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/store-tokens/{token}/validate ──────────────────────

    [Fact]
    public async Task Validate_ActiveToken_ReturnsValid()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var createResponse = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Validate anonymously
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/store-tokens/{created!.Token}/validate");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var validation = await response.Content.ReadFromJsonAsync<StoreTokenValidationResponse>();
        validation!.Valid.Should().BeTrue();
        validation.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Validate_ExpiredToken_ReturnsInvalid()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var createResponse = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(1));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Expire it in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var dbToken = await db.StoreTokens.FindAsync(created!.Id);
        dbToken!.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/store-tokens/{created.Token}/validate");
        var validation = await response.Content.ReadFromJsonAsync<StoreTokenValidationResponse>();
        validation!.Valid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_RevokedToken_ReturnsInvalid()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var createResponse = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Revoke it
        await staffClient.DeleteAsync($"/api/store-tokens/{created!.Id}");

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/store-tokens/{created.Token}/validate");
        var validation = await response.Content.ReadFromJsonAsync<StoreTokenValidationResponse>();
        validation!.Valid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NonexistentToken_ReturnsInvalid()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/store-tokens/nonexistent-garbage-token/validate");
        var validation = await response.Content.ReadFromJsonAsync<StoreTokenValidationResponse>();
        validation!.Valid.Should().BeFalse();
    }
}
