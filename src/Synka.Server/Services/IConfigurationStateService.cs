namespace Synka.Server.Services;

public interface IConfigurationStateService
{
    Task<bool> RequiresConfigurationAsync(CancellationToken cancellationToken = default);
}
