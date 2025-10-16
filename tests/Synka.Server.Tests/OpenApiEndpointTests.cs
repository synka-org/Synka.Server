using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Synka.Server.Tests.Infrastructure;

namespace Synka.Server.Tests;

internal sealed class OpenApiEndpointTests
{
    [Test]
    public async Task OpenApi_IsAccessible_InDevelopment()
    {
        await using var factory = new DevelopmentWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi.json");

        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OpenApi_ChallengesOutsideDevelopment_WhenAnonymous()
    {
        await using var factory = new ProductionWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi.json");

        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task OpenApi_ReturnsNotFoundOutsideDevelopment_WhenAuthenticated()
    {
        await using var factory = new ProductionAuthenticatedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi.json");

        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task OpenApi_IsAccessibleInProduction_WhenExposed()
    {
        await using var factory = new ProductionOpenApiExposedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi.json");

        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);
    }

    private sealed class ProductionWebApplicationFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder); // Set up Testing environment and in-memory database
            // Note: Keep Testing environment to avoid migration attempts on in-memory DB
            // OpenAPI will be disabled by default since OpenApi:Expose is not set
        }
    }

    private sealed class ProductionAuthenticatedWebApplicationFactory : AuthenticatedSchemeWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            // Note: Keep Testing environment to avoid migration attempts on in-memory DB
        }
    }

    private sealed class ProductionOpenApiExposedWebApplicationFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder); // Set up Testing environment and in-memory database
            // Note: Keep Testing environment to avoid migration attempts on in-memory DB
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["OpenApi:Expose"] = "true",
                };

                configuration.AddInMemoryCollection(settings);
            });
        }
    }

    private sealed class DevelopmentWebApplicationFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder); // Set up in-memory database
            builder.UseEnvironment("Development"); // Override to Development for OpenAPI exposure
        }
    }
}
