using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Tests.Infrastructure;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Synka.Server.Tests;

internal sealed class ConfigurationEndpointTests
{
    [Test]
    public async Task Configuration_WhenNoUsersExist_CreatesUserAndReturnsOk()
    {
        // Arrange
    await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.Users.RemoveRange(dbContext.Users);
            await dbContext.SaveChangesAsync();
        }

        var request = new ConfigurationRequest(
            Email: $"admin-{Guid.NewGuid():N}@synka.local",
            Password: "Test123!@#");

        // Act
        var response = await client.PostAsJsonAsync("/configure", request);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Configuration_AfterUserExists_ReturnsConflict()
    {
        // Arrange
    await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.Users.RemoveRange(dbContext.Users);
            await dbContext.SaveChangesAsync();
        }

        var firstRequest = new ConfigurationRequest(
            Email: $"admin-{Guid.NewGuid():N}@synka.local",
            Password: "Test123!@#");

        await client.PostAsJsonAsync("/configure", firstRequest);

        var secondRequest = new ConfigurationRequest(
            Email: $"another-{Guid.NewGuid():N}@synka.local",
            Password: "Test456!@#");

        // Act
        var response = await client.PostAsJsonAsync("/configure", secondRequest);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Configuration_WithInvalidPassword_ReturnsBadRequest()
    {
        // Arrange
    await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.Users.RemoveRange(dbContext.Users);
            await dbContext.SaveChangesAsync();
        }

        var request = new ConfigurationRequest(
            Email: $"admin-{Guid.NewGuid():N}@synka.local",
            Password: "weak");

        // Act
        var response = await client.PostAsJsonAsync("/configure", request);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Configuration_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
    await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.Users.RemoveRange(dbContext.Users);
            await dbContext.SaveChangesAsync();
        }

        var request = new ConfigurationRequest(
            Email: "not-an-email",
            Password: "Test123!@#");

        // Act
        var response = await client.PostAsJsonAsync("/configure", request);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ServiceManifest_AfterConfiguration_RequiresConfigurationIsFalse()
    {
        // Arrange
    await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.Users.RemoveRange(dbContext.Users);
            await dbContext.SaveChangesAsync();
        }

        var request = new ConfigurationRequest(
            Email: $"admin-{Guid.NewGuid():N}@synka.local",
            Password: "Test123!@#");

        await client.PostAsJsonAsync("/configure", request);

        // Act
        var manifest = await client.GetFromJsonAsync<ServiceManifestResponse>("/api/v1/manifest");

        // Assert
        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.RequiresConfiguration)
            .IsFalse();
    }
}
