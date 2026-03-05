using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using MannaHp.Server.Data;
using MannaHp.Server.Services;
using Microsoft.Extensions.Configuration;

namespace MannaHp.Server.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly AppUser _testUser;

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsATestKeyThatIsAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiresInMinutes"] = "60",
            })
            .Build();

        _tokenService = new TokenService(config);

        _testUser = new AppUser
        {
            Id = "test-user-id-123",
            Email = "test@manna.local",
            UserName = "test@manna.local",
            DisplayName = "Test User",
            Role = "Owner"
        };
    }

    [Fact]
    public void CreateToken_ReturnsNonEmptyToken()
    {
        var (token, _) = _tokenService.CreateToken(_testUser);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateToken_ContainsCorrectEmailClaim()
    {
        var (token, _) = _tokenService.CreateToken(_testUser);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == "test@manna.local");
    }

    [Fact]
    public void CreateToken_ContainsCorrectRoleClaim()
    {
        var (token, _) = _tokenService.CreateToken(_testUser);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == "Owner");
    }

    [Fact]
    public void CreateToken_ContainsCorrectNameIdentifierClaim()
    {
        var (token, _) = _tokenService.CreateToken(_testUser);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == "test-user-id-123");
    }

    [Fact]
    public void CreateToken_ContainsCorrectNameClaim_UsesDisplayName()
    {
        var (token, _) = _tokenService.CreateToken(_testUser);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Name && c.Value == "Test User");
    }

    [Fact]
    public void CreateToken_FallsBackToEmailWhenNoDisplayName()
    {
        var user = new AppUser
        {
            Id = "no-display-name",
            Email = "nodisplay@manna.local",
            UserName = "nodisplay@manna.local",
            DisplayName = null,
            Role = "Staff"
        };

        var (token, _) = _tokenService.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Name && c.Value == "nodisplay@manna.local");
    }

    [Fact]
    public void CreateToken_ExpirationMatchesConfiguredValue()
    {
        var before = DateTime.UtcNow;
        var (_, expiresAt) = _tokenService.CreateToken(_testUser);
        var after = DateTime.UtcNow;

        // Should expire ~60 minutes from now (configured value)
        expiresAt.Should().BeAfter(before.AddMinutes(59));
        expiresAt.Should().BeBefore(after.AddMinutes(61));
    }
}
