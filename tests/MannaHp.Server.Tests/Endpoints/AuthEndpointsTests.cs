using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class AuthEndpointsTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    public AuthEndpointsTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── POST /api/auth/login ──────────────────────────────────────────

    [Fact]
    public async Task Login_ValidOwnerCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("owner@manna.local", "MannaOwner123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrEmpty();
        auth.Email.Should().Be("owner@manna.local");
        auth.Role.Should().Be("Owner");
        auth.DisplayName.Should().Be("Owner");
    }

    [Fact]
    public async Task Login_ValidStaffCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("staff@manna.local", "MannaStaff123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.Role.Should().Be("Staff");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("owner@manna.local", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonexistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@manna.local", "Whatever123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("", "MannaOwner123!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────

    [Fact]
    public async Task Me_WithValidToken_ReturnsUser()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        var response = await ownerClient.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("owner@manna.local");
        user.Role.Should().Be("Owner");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "totally-invalid-jwt-token");

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/auth/register (Owner only) ──────────────────────────

    [Fact]
    public async Task Register_AsOwner_CreatesStaffAccount()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        var response = await ownerClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("newstaff@manna.local", "NewStaff123!", "New Staff"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.Email.Should().Be("newstaff@manna.local");
        auth.Role.Should().Be("Staff");
    }

    [Fact]
    public async Task Register_AsStaff_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var response = await staffClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("another@manna.local", "Another123!", "Another"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_Anonymous_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("anon@manna.local", "AnonPass123!", "Anon"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Authorization: Protected endpoints ─────────────────────────────

    [Fact]
    public async Task ProtectedOwnerEndpoint_Anonymous_Returns401()
    {
        // POST /api/categories requires Owner
        var response = await _client.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest("Test", 99));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedOwnerEndpoint_AsOwner_Succeeds()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        var response = await ownerClient.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest("Auth Test Category", 99));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ProtectedOwnerEndpoint_AsStaff_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var response = await staffClient.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest("Staff Attempt", 99));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProtectedStaffEndpoint_AsStaff_Succeeds()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        // GET /api/orders/active requires Staff (Owner or Staff)
        var response = await staffClient.GetAsync("/api/orders/active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedStaffEndpoint_Anonymous_Returns401()
    {
        var response = await _client.GetAsync("/api/orders/active");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Authorization: Customer-facing endpoints stay anonymous ────────

    [Fact]
    public async Task AnonymousEndpoint_GetCategories_Returns200()
    {
        var response = await _client.GetAsync("/api/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnonymousEndpoint_GetMenuItems_Returns200()
    {
        var response = await _client.GetAsync("/api/menu-items");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnonymousEndpoint_PostOrder_Returns201()
    {
        // Ordering doesn't require auth (in-store flow)
        var bowlId = Guid.Parse("c0000000-0001-0000-0000-000000000001");
        var riceId = Guid.Parse("e0000000-0001-0000-0000-000000000000");

        var req = new CreateOrderRequest(
            MannaHp.Shared.Enums.PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(bowlId, null, 1, null, [riceId])]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
