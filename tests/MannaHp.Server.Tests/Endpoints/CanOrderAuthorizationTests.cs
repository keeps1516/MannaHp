using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Data;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class CanOrderAuthorizationTests
{
    private readonly MannaApiFactory _factory;

    public CanOrderAuthorizationTests(MannaApiFactory factory)
    {
        _factory = factory;
    }

    private static CreateOrderRequest MakeOrderRequest() =>
        new(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(
                Guid.Parse("c0000000-0001-0000-0000-000000000001"), // seeded bowl
                null, 1, null,
                [Guid.Parse("e0000000-0001-0000-0000-000000000000")])]);  // seeded ingredient

    [Fact]
    public async Task PlaceOrder_AuthenticatedJwtUser_Succeeds()
    {
        var client = await _factory.CreateStaffClientAsync();
        var response = await client.PostAsJsonAsync("/api/orders", MakeOrderRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_ValidStoreToken_Succeeds()
    {
        // First, create a store token
        var staffClient = await _factory.CreateStaffClientAsync();
        var createResponse = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        createResponse.EnsureSuccessStatusCode();
        var storeToken = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Anonymous client with X-Store-Token header
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Store-Token", storeToken!.Token);

        var response = await client.PostAsJsonAsync("/api/orders", MakeOrderRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlaceOrder_ExpiredStoreToken_Returns401Or403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var createResponse = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(1));
        createResponse.EnsureSuccessStatusCode();
        var storeToken = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Expire it in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var dbToken = await db.StoreTokens.FindAsync(storeToken!.Id);
        dbToken!.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Store-Token", storeToken.Token);

        var response = await client.PostAsJsonAsync("/api/orders", MakeOrderRequest());
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlaceOrder_RevokedStoreToken_Returns401Or403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var createResponse = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        createResponse.EnsureSuccessStatusCode();
        var storeToken = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        // Revoke it
        await staffClient.DeleteAsync($"/api/store-tokens/{storeToken!.Id}");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Store-Token", storeToken.Token);

        var response = await client.PostAsJsonAsync("/api/orders", MakeOrderRequest());
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlaceOrder_NoAuthNoToken_Returns401Or403()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", MakeOrderRequest());
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlaceOrder_GarbageStoreToken_Returns401Or403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Store-Token", "totally-invalid-garbage-token");

        var response = await client.PostAsJsonAsync("/api/orders", MakeOrderRequest());
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlaceOrder_WithValidToken_HasInStorePaymentMethod()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var createResponse = await staffClient.PostAsJsonAsync("/api/store-tokens",
            new GenerateStoreTokenRequest(5));
        createResponse.EnsureSuccessStatusCode();
        var storeToken = await createResponse.Content.ReadFromJsonAsync<StoreTokenResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Store-Token", storeToken!.Token);

        var response = await client.PostAsJsonAsync("/api/orders", MakeOrderRequest());
        response.EnsureSuccessStatusCode();

        var orderResponse = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        orderResponse!.Order.PaymentMethod.Should().Be(PaymentMethod.InStore);
    }
}
