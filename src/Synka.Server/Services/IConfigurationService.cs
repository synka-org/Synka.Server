using Synka.Server.Contracts;

namespace Synka.Server.Services;

public interface IConfigurationService
{
    Task<IResult> GetServiceManifestAsync(CancellationToken cancellationToken = default);

    Task<IResult> ConfigureAsync(ConfigurationRequest request, CancellationToken cancellationToken = default);
}
