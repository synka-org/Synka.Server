using Microsoft.AspNetCore.Authorization;
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
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();
        database.Database.Migrate();
    }

    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth")
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
        app.MapGet("/", async (IConfigurationStateService configurationStateService, CancellationToken cancellationToken) =>
            {
                var requiresConfiguration = await configurationStateService.RequiresConfigurationAsync(cancellationToken);

                return TypedResults.Ok(new ServiceManifest(
                    Service: "Synka.Server",
                    Version: typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev",
                    RequiresConfiguration: requiresConfiguration));
            })
            .WithName("GetServiceManifest")
            .AllowAnonymous();
    }
}
