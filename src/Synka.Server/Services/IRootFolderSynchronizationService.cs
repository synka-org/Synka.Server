namespace Synka.Server.Services;

/// <summary>
/// Ensures configured root folders exist in the database and on disk.
/// </summary>
public interface IRootFolderSynchronizationService
{
    /// <summary>
    /// Validates configured root folders and creates any missing records.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task EnsureRootFoldersAsync(CancellationToken cancellationToken = default);
}
