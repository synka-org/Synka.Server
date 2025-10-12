using System.Net;
using System.Net.Http.Json;
using Synka.Server.Contracts;
using Synka.Server.Tests.Infrastructure;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Synka.Server.Tests;

internal sealed class ManifestEndpointTests
{
    [Test]
    public async Task ApiV1Manifest_ReturnsManifestPayload()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/manifest");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ServiceManifestResponse>();

        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.Service).IsEqualTo("Synka.Server");
        await Assert.That(payload.RequiresConfiguration).IsTrue();
    }
}
