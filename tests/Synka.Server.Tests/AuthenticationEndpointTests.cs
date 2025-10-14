using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Data.Entities;
using Synka.Server.Tests.Infrastructure;

namespace Synka.Server.Tests;

internal sealed class AuthenticationEndpointTests
{
    [Test]
    public async Task Register_RequiresAuthentication()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = $"user-{Guid.NewGuid():N}@synka.local", password = "Password1!" });

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Register_RequiresAdministratorRole()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = $"user-{Guid.NewGuid():N}@synka.local", password = "Password1!" });

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Register_AllowsAdministratorToCreateUsers()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory(options =>
        {
            options.UserName = "AdminUser";
            options.Claims.AddAdministratorRole();
        });
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@synka.local";
        var request = new { email, password = "Password1!" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserEntity>>();
        var createdUser = await userManager.FindByEmailAsync(email);

        await Assert.That(createdUser).IsNotNull();

        if (createdUser is not null)
        {
            await userManager.DeleteAsync(createdUser);
        }
    }

}
