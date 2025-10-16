using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Tests.Infrastructure;

namespace Synka.Server.Tests;

internal sealed class FileEndpointTests
{
    [Test]
    public async Task UploadFile_RequiresAuthentication()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");
        using var folderIdContent = new StringContent(Guid.NewGuid().ToString());
        content.Add(folderIdContent, "folderId");

        // Act
        var response = await client.PostAsync("/api/v1/files", content);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task UploadFile_WithValidFile_ReturnsOk()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test file content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");
        using var folderIdContent = new StringContent(Guid.NewGuid().ToString());
        content.Add(folderIdContent, "folderId");

        // Act
        var response = await client.PostAsync("/api/v1/files", content);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.FileName).IsEqualTo("test.txt");
    }

    [Test]
    public async Task UploadFile_WithoutMultipartFormData_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/v1/files", content);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UploadFile_WithoutFile_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        using var folderIdContent = new StringContent(Guid.NewGuid().ToString());
        content.Add(folderIdContent, "folderId");

        // Act
        var response = await client.PostAsync("/api/v1/files", content);

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetFileMetadata_RequiresAuthentication()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var fileId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/files/{fileId}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task GetFileMetadata_WithNonExistentFile_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        var fileId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/files/{fileId}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetFileMetadata_WithExistingFile_ReturnsMetadata()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Upload a file first
        using var uploadContent = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        uploadContent.Add(fileContent, "file", "metadata-test.txt");
        using var folderIdContent = new StringContent(Guid.NewGuid().ToString());
        uploadContent.Add(folderIdContent, "folderId");

        var uploadResponse = await client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponse>();

        // Act
        var response = await client.GetAsync($"/api/v1/files/{uploadResult!.Id}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);

        var metadata = await response.Content.ReadFromJsonAsync<FileMetadataResponse>();
        await Assert.That(metadata).IsNotNull();
        await Assert.That(metadata!.Id).IsEqualTo(uploadResult.Id);
        await Assert.That(metadata.FileName).IsEqualTo("metadata-test.txt");
    }

    [Test]
    public async Task ListUserFiles_RequiresAuthentication()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var folderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/files?folderId={folderId}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task ListUserFiles_ReturnsEmptyListForNewUser()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Clean up any existing files
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.FileMetadata.RemoveRange(dbContext.FileMetadata);
            await dbContext.SaveChangesAsync();
        }

        var folderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/files?folderId={folderId}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);

        var files = await response.Content.ReadFromJsonAsync<List<FileMetadataResponse>>();
        await Assert.That(files).IsNotNull();
        await Assert.That(files!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ListUserFiles_ReturnsUploadedFiles()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Clean up any existing files
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
            dbContext.FileMetadata.RemoveRange(dbContext.FileMetadata);
            await dbContext.SaveChangesAsync();
        }

        var folderId = Guid.NewGuid();

        // Upload two files to the same folder
        for (int i = 1; i <= 2; i++)
        {
            using var uploadContent = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes($"test content {i}"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            uploadContent.Add(fileContent, "file", $"test{i}.txt");
            using var folderIdContent = new StringContent(folderId.ToString());
            uploadContent.Add(folderIdContent, "folderId");
            await client.PostAsync("/api/v1/files", uploadContent);
        }

        // Act
        var response = await client.GetAsync($"/api/v1/files?folderId={folderId}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.OK);

        var files = await response.Content.ReadFromJsonAsync<List<FileMetadataResponse>>();
        await Assert.That(files).IsNotNull();
        await Assert.That(files!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task DeleteFile_RequiresAuthentication()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var fileId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/files/{fileId}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task DeleteFile_WithNonExistentFile_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        var fileId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/files/{fileId}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteFile_WithExistingFile_ReturnsNoContent()
    {
        // Arrange
        await using var factory = new AuthenticatedSchemeWebApplicationFactory();
        using var client = factory.CreateClient();

        // Upload a file first
        using var uploadContent = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        uploadContent.Add(fileContent, "file", "delete-test.txt");
        using var folderIdContent = new StringContent(Guid.NewGuid().ToString());
        uploadContent.Add(folderIdContent, "folderId");

        var uploadResponse = await client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponse>();

        // Act
        var response = await client.DeleteAsync($"/api/v1/files/{uploadResult!.Id}");

        // Assert
        await Assert.That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NoContent);

        // Verify file is deleted
        var getResponse = await client.GetAsync($"/api/v1/files/{uploadResult.Id}");
        await Assert.That(getResponse.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound);
    }
}
