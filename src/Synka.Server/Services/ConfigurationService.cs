using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public sealed class ConfigurationService(
    IConfigurationStateService configurationStateService,
    UserManager<ApplicationUserEntity> userManager) : IConfigurationService
{
    public async Task<IResult> ConfigureAsync(
        ConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requiresConfiguration = await configurationStateService.RequiresConfigurationAsync(cancellationToken);

        if (!requiresConfiguration)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Already Configured",
                Detail = "The system has already been configured.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var user = new ApplicationUserEntity
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "User Creation Failed",
                Detail = string.Join(", ", result.Errors.Select(e => e.Description)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Results.Ok();
    }
}
