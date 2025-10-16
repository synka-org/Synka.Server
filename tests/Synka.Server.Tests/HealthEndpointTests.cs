using System.Net;
using Synka.Server.Tests.Infrastructure;

namespace Synka.Server.Tests;

internal sealed class HealthEndpointTests
{
    [Test]
    public async Task Health_RequiresAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Redirect);
    }
}
