using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Synka.Server.Tests.Infrastructure;

public sealed class TestAuthHandler(
    IOptionsMonitor<TestAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<TestAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = Options.Claims.Count > 0
            ? [.. Options.Claims]
            : new List<Claim>();

        if (!claims.Any(claim => claim.Type == ClaimTypes.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, Options.UserName ?? "TestUser"));
        }

        // Ensure NameIdentifier claim exists for user ID resolution in file endpoints
        if (!claims.Any(claim => claim.Type == ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string? UserName { get; set; }

    public Collection<Claim> Claims { get; } = [];
}
