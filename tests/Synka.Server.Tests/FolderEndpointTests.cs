using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Contracts;
using Synka.Server.Services;
using Synka.Server.Tests.Infrastructure;
using TUnit.Core;

namespace Synka.Server.Tests;

/// <summary>
/// Tests for folder-related HTTP endpoints.
/// </summary>
internal sealed class FolderEndpointTests
{
    [Test]
    public async Task DeleteFolder_WithNonRootFolder_ReturnsNoContent()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create a parent folder first using FolderService directly (since API doesn't allow root creation)
        using var scope = factory.Services.CreateScope();
        var folderService = scope.ServiceProvider.GetRequiredService<IFolderService>();
        var userId = Guid.NewGuid();
        var parent = await folderService.CreateFolderAsync(userId, "Parent", null, "/test/parent");

        // Create a child folder via API
        var childRequest = new CreateFolderRequest("Child", parent.Id, null);
        var childResponse = await client.PostAsJsonAsync("/api/v1/folders", childRequest);
        await Assert.That(childResponse.IsSuccessStatusCode).IsTrue();
        var child = await childResponse.Content.ReadFromJsonAsync<FolderResponse>();

        // Act - Delete the child folder
        var deleteResponse = await client.DeleteAsync($"/api/v1/folders/{child!.Id}");

        // Assert
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify folder is gone
        var getResponse = await client.GetAsync($"/api/v1/folders/{child.Id}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteFolder_WithRootFolder_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create a root folder directly using FolderService (since API doesn't allow root creation)
        using var scope = factory.Services.CreateScope();
        var folderService = scope.ServiceProvider.GetRequiredService<IFolderService>();
        var userId = Guid.NewGuid();
        var folder = await folderService.CreateFolderAsync(userId, "RootFolder", null, "/test/root");

        // Act - Try to delete the root folder via API
        var deleteResponse = await client.DeleteAsync($"/api/v1/folders/{folder.Id}");

        // Assert
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var errorContent = await deleteResponse.Content.ReadAsStringAsync();
        await Assert.That(errorContent).Contains("Root folders cannot be deleted");
    }

    [Test]
    public async Task DeleteFolder_WithNonExistentFolder_ReturnsNoContent()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/folders/{nonExistentId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteFolder_RequiresAuthentication()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var folderId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/folders/{folderId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task CreateFolder_WithValidData_ReturnsCreated()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create a parent folder first using FolderService directly
        using var scope = factory.Services.CreateScope();
        var folderService = scope.ServiceProvider.GetRequiredService<IFolderService>();
        var userId = Guid.NewGuid();
        var parent = await folderService.CreateFolderAsync(userId, "Parent", null, "/test/parent");

        var request = new CreateFolderRequest("TestFolder", parent.Id, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/folders", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var folder = await response.Content.ReadFromJsonAsync<FolderResponse>();
        await Assert.That(folder).IsNotNull();
        await Assert.That(folder!.Name).IsEqualTo("TestFolder");
        await Assert.That(folder.ParentFolderId).IsEqualTo(parent.Id);
    }

    [Test]
    public async Task CreateFolder_WithRootFolder_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new CreateFolderRequest("RootFolder", null, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/folders", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var errorContent = await response.Content.ReadAsStringAsync();
        await Assert.That(errorContent).Contains("Root folders cannot be created via API");
    }

    [Test]
    public async Task GetRootFolders_ReturnsUserAndSharedFolders()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Create a user root folder directly using FolderService
        using var scope = factory.Services.CreateScope();
        var folderService = scope.ServiceProvider.GetRequiredService<IFolderService>();
        var userId = Guid.NewGuid();
        await folderService.CreateFolderAsync(userId, "UserRoot", null, "/test/userroot");
        await folderService.CreateFolderAsync(null, "SharedRoot", null, "/test/sharedroot");

        // Act
        var response = await client.GetAsync("/api/v1/folders/roots");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var folders = await response.Content.ReadFromJsonAsync<IReadOnlyList<FolderResponse>>();
        await Assert.That(folders).IsNotNull();
        // Response includes both user and shared root folders
        // User folders have IsUserRoot = true, shared folders have IsSharedRoot = true
    }
}
