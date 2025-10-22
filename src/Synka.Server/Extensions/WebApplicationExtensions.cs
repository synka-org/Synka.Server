using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Authorization;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Exceptions;
using Synka.Server.Services;

namespace Synka.Server.Extensions;

internal static class WebApplicationExtensions
{
    public static void MapOpenApiDocument(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Expose"))
        {
            app.MapOpenApi("/openapi.json").AllowAnonymous();
        }
    }

    public static void EnsureDatabaseIsMigrated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();

        // Skip migrations for in-memory databases (used in tests)
        // In-memory SQLite connections have "DataSource=:memory:" or "Data Source=:memory:"
        var connectionString = database.Database.GetConnectionString();
        if (connectionString?.Contains(":memory:", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        database.Database.Migrate();
    }
    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/v{version:apiVersion}/auth")
            .WithTags("Authentication");

        var identityEndpoints = authGroup.MapIdentityApi<ApplicationUserEntity>();

        identityEndpoints.Add(endpoint =>
        {
            if (endpoint is not RouteEndpointBuilder routeEndpoint || routeEndpoint.RoutePattern.RawText is not { } pattern)
            {
                return;
            }

            if (pattern.Contains("/manage", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (pattern.EndsWith("/register", StringComparison.OrdinalIgnoreCase))
            {
                endpoint.Metadata.Add(new AuthorizeAttribute(AuthorizationPolicies.AdminOnly));
                return;
            }

            endpoint.Metadata.Add(new AllowAnonymousAttribute());
        });
    }

    public static void MapServiceManifestEndpoint(this WebApplication app)
    {
        app.MapGet("/api/v{version:apiVersion}/manifest", async (
            IConfigurationService configurationService,
            CancellationToken cancellationToken) =>
            await configurationService.GetServiceManifestAsync(cancellationToken))
            .WithName("GetServiceManifest")
            .AllowAnonymous();
    }

    public static void MapConfigurationEndpoint(this WebApplication app)
    {
        app.MapPost("/api/v{version:apiVersion}/configure", async (
            ConfigurationRequest request,
            IConfigurationService configurationService,
            CancellationToken cancellationToken) =>
            await configurationService.ConfigureAsync(request, cancellationToken))
            .WithName("Configuration")
            .WithTags("Configuration")
            .AllowAnonymous();
    }

    public static void MapFileEndpoints(this WebApplication app)
    {
        var filesGroup = app.MapGroup("/api/v{version:apiVersion}/files")
            .WithTags("Files")
            .RequireAuthorization();

        // Upload a file
        filesGroup.MapPost("/", async (
            HttpContext httpContext,
            IFileService fileService,
            CancellationToken cancellationToken) =>
        {
            if (!httpContext.Request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Request must be multipart/form-data" });
            }

            try
            {
                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");

                if (file is null)
                {
                    return Results.BadRequest(new { error = "No file provided. Use 'file' field name." });
                }

                // Required folderId from form data
                if (!form.TryGetValue("folderId", out var folderIdValue) ||
                    !Guid.TryParse(folderIdValue, out var folderId))
                {
                    return Results.BadRequest(new { error = "folderId is required and must be a valid GUID" });
                }

                var response = await fileService.UploadFileAsync(file, folderId, cancellationToken);
                return Results.Ok(response);
            }
            catch (InvalidFileUploadException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidDataException ex)
            {
                // Handle malformed form data
                return Results.BadRequest(new { error = $"Invalid form data: {ex.Message}" });
            }
        })
        .WithName("UploadFile")
        .DisableAntiforgery();

        // Get file metadata
        filesGroup.MapGet("/{fileId:guid}", async (
            Guid fileId,
            IFileService fileService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var metadata = await fileService.GetFileMetadataAsync(fileId, cancellationToken);
                return metadata is not null ? Results.Ok(metadata) : Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("GetFileMetadata");

        // List user's files
        filesGroup.MapGet("/", async (
            IFileService fileService,
            Guid folderId,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var files = await fileService.ListUserFilesAsync(folderId, cancellationToken);
                return Results.Ok(files);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("ListUserFiles");

        // Delete a file
        filesGroup.MapDelete("/{fileId:guid}", async (
            Guid fileId,
            IFileService fileService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await fileService.DeleteFileAsync(fileId, cancellationToken);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("DeleteFile");

        // Scan a specific folder for changes
        filesGroup.MapPost("/scan/{folderId:guid}", async (
            Guid folderId,
            IFileSystemScannerService scannerService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await scannerService.ScanFolderAsync(folderId, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("ScanFolder");

        // Scan all user folders
        filesGroup.MapPost("/scan", async (
            IFileSystemScannerService scannerService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await scannerService.ScanAllUserFoldersAsync(cancellationToken);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("ScanAllUserFolders");

        // Scan shared folders (admin only)
        filesGroup.MapPost("/scan/shared", async (
            IFileSystemScannerService scannerService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await scannerService.ScanSharedFoldersAsync(cancellationToken);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("ScanSharedFolders")
    .RequireAuthorization(AuthorizationPolicies.AdminOnly);
    }

    public static void MapFolderEndpoints(this WebApplication app)
    {
        var foldersGroup = app.MapGroup("/api/v{version:apiVersion}/folders")
            .WithTags("Folders")
            .RequireAuthorization();

        // Get folder by ID
        foldersGroup.MapGet("/{folderId:guid}", async (
            Guid folderId,
            IFolderService folderService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var folder = await folderService.GetFolderAsync(folderId, cancellationToken);
                return Results.Ok(folder);
            }
            catch (FolderNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("GetFolder");

        // Get root folders (both user and shared)
        foldersGroup.MapGet("/roots", async (
            IFolderService folderService,
            CancellationToken cancellationToken) =>
        {
            var folders = await folderService.GetRootFoldersAsync(cancellationToken);
            return Results.Ok(folders);
        })
        .WithName("GetRootFolders");

        // Get subfolders
        foldersGroup.MapGet("/{parentFolderId:guid}/subfolders", async (
            Guid parentFolderId,
            IFolderService folderService,
            CancellationToken cancellationToken) =>
        {
            var subfolders = await folderService.GetSubfoldersAsync(parentFolderId, cancellationToken);
            return Results.Ok(subfolders);
        })
        .WithName("GetSubfolders");

        // Create a new subfolder
        foldersGroup.MapPost("/", async (
            CreateFolderRequest request,
            IFolderService folderService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (!request.ParentFolderId.HasValue)
                {
                    return Results.BadRequest(new { error = "ParentFolderId is required. Root folders cannot be created via API." });
                }

                var response = await folderService.CreateFolderAsync(
                    request.ParentFolderId.Value,
                    request.Name,
                    cancellationToken);

                return Results.Created($"/api/v1/folders/{response.Id}", response);
            }
            catch (FolderNotFoundException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateFolder");
        // Delete a folder (hard delete with disk removal)
        foldersGroup.MapDelete("/{folderId:guid}", async (
            Guid folderId,
            IFolderService folderService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await folderService.HardDeleteFolderAsync(folderId, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .WithName("DeleteFolder");
    }
}
