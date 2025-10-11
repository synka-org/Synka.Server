using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public sealed class ConfigurationStateService(UserManager<ApplicationUserEntity> userManager) : IConfigurationStateService
{
    public async Task<bool> RequiresConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var hasUsers = await userManager.Users.AnyAsync(cancellationToken);
        return !hasUsers;
    }

    public async Task<IResult> GetServiceManifestAsync(CancellationToken cancellationToken = default)
    {
        var requiresConfiguration = await RequiresConfigurationAsync(cancellationToken);

        return TypedResults.Ok(new ServiceManifestResponse(
            Service: "Synka.Server",
            Version: typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev",
            RequiresConfiguration: requiresConfiguration));
    }
}
