using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public sealed class ConfigurationStateService(UserManager<ApplicationUserEntity> userManager) : IConfigurationStateService
{
    private readonly UserManager<ApplicationUserEntity> _userManager = userManager;

    public async Task<bool> RequiresConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var hasUsers = await _userManager.Users.AnyAsync(cancellationToken);
        return !hasUsers;
    }
}
