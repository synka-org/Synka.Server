using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Synka.Server.Tests;

internal sealed class HealthEndpointTests
{
    [Test]
    public async Task Health_RequiresAuthentication()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Redirect);
    }
}
