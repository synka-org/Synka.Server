using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public sealed class ConfigurationService(
    UserManager<ApplicationUserEntity> userManager) : IConfigurationService
{
    public async Task<IResult> ConfigureAsync(
        ConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (requiresConfiguration, _) = await GetConfigurationStatusAsync(cancellationToken);

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

    public async Task<IResult> GetServiceManifestAsync(CancellationToken cancellationToken = default)
    {
        var (requiresConfiguration, configurationTasks) = await GetConfigurationStatusAsync(cancellationToken);

        return TypedResults.Ok(new ServiceManifestResponse(
            Service: "Synka.Server",
            Version: typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev",
            RequiresConfiguration: requiresConfiguration,
            ConfigurationTasks: configurationTasks));
    }

    private async Task<(bool RequiresConfiguration, IReadOnlyList<ConfigurationTaskStatus> ConfigurationTasks)> GetConfigurationStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var configurationTasks = new List<ConfigurationTaskStatus>();

        var hasUsers = await userManager.Users.AnyAsync(cancellationToken);

        configurationTasks.Add(new ConfigurationTaskStatus(
            Key: "initial-admin-user",
            Title: "Create initial administrator",
            Description: "Create the first administrator account to finish initial setup.",
            State: hasUsers ? ConfigurationTaskState.Completed : ConfigurationTaskState.Pending));

        var requiresConfiguration = configurationTasks.Any(task => task.State == ConfigurationTaskState.Pending);

        return (requiresConfiguration, configurationTasks);
    }
}
