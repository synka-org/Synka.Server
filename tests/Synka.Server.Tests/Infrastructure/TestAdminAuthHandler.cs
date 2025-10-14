using System.Security.Claims;
using Synka.Server.Authorization;

namespace Synka.Server.Tests.Infrastructure;

internal static class TestAuthClaimExtensions
{
    public static void AddAdministratorRole(this ICollection<Claim> claims)
    {
        claims.Add(new Claim(ClaimTypes.Role, RoleNames.Administrator));
    }
}
