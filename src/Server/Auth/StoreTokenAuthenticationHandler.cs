using System.Security.Claims;
using System.Text.Encodings.Web;
using MannaHp.Server.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MannaHp.Server.Auth;

public class StoreTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public StoreTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceScopeFactory scopeFactory)
        : base(options, logger, encoder)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tokenHeader = Request.Headers["X-Store-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(tokenHeader))
            return AuthenticateResult.NoResult();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();

        var storeToken = await db.StoreTokens
            .FirstOrDefaultAsync(t => t.Token == tokenHeader
                && !t.Revoked
                && t.ExpiresAt > DateTime.UtcNow);

        if (storeToken is null)
            return AuthenticateResult.Fail("Invalid or expired store token");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, $"store-token:{storeToken.Id}"),
            new Claim("StoreToken", "true"),
        };
        var identity = new ClaimsIdentity(claims, "StoreToken");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "StoreToken");

        return AuthenticateResult.Success(ticket);
    }
}
