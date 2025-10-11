using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Contracts;
using Synka.Server.Data;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Synka.Server.Tests;

internal sealed class ManifestEndpointTests
{
    [Test]
    public async Task Root_ReturnsManifestPayload()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.Users.RemoveRange(dbContext.Users);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/");

        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ServiceManifest>();

        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.Service).IsEqualTo("Synka.Server");
        await Assert.That(payload.RequiresConfiguration).IsTrue();
    }
}
