using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Authorization;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
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
        // Skip migrations in test environments where EnsureCreated is used instead
        if (app.Environment.EnvironmentName == "Testing")
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
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
                endpoint.Metadata.Add(new AuthorizeAttribute(AuthorizationPolicies.AdministratorOnly));
                return;
            }

            endpoint.Metadata.Add(new AllowAnonymousAttribute());
        });
    }

    public static void MapServiceManifestEndpoint(this WebApplication app)
    {
        app.MapGet("/api/v{version:apiVersion}/manifest", async (
            IConfigurationStateService configurationStateService,
            CancellationToken cancellationToken) =>
            await configurationStateService.GetServiceManifestAsync(cancellationToken))
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
            IFileUploadService fileUploadService,
            CancellationToken cancellationToken) =>
        {
            if (!httpContext.Request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Request must be multipart/form-data" });
            }

            var form = await httpContext.Request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");

            if (file is null)
            {
                return Results.BadRequest(new { error = "No file provided. Use 'file' field name." });
            }

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is null || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await fileUploadService.UploadFileAsync(userGuid, file, cancellationToken);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UploadFile")
        .DisableAntiforgery();

        // Get file metadata
        filesGroup.MapGet("/{fileId:guid}", async (
            Guid fileId,
            IFileUploadService fileUploadService,
            CancellationToken cancellationToken) =>
        {
            var metadata = await fileUploadService.GetFileMetadataAsync(fileId, cancellationToken);
            return metadata is not null ? Results.Ok(metadata) : Results.NotFound();
        })
        .WithName("GetFileMetadata");

        // List user's files
        filesGroup.MapGet("/", async (
            HttpContext httpContext,
            IFileUploadService fileUploadService,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is null || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var files = await fileUploadService.ListUserFilesAsync(userGuid, cancellationToken);
            return Results.Ok(files);
        })
        .WithName("ListUserFiles");

        // Delete a file
        filesGroup.MapDelete("/{fileId:guid}", async (
            Guid fileId,
            HttpContext httpContext,
            IFileUploadService fileUploadService,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is null || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var deleted = await fileUploadService.DeleteFileAsync(fileId, userGuid, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteFile");
    }
}
