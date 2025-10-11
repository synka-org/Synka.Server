using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

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
