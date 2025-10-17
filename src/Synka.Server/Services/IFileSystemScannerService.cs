using Synka.Server.Contracts;

namespace Synka.Server.Services;

/// <summary>
/// Service for scanning the file system and synchronizing changes with the database.
/// </summary>
public interface IFileSystemScannerService
{
    /// <summary>
    /// Scans a folder and its subfolders for changes and updates the database.
    /// </summary>
    /// <param name="folderId">The folder ID to scan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of the scan results.</returns>
    Task<FileSystemScanResult> ScanFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans all folders for the current user and updates the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of the scan results.</returns>
    Task<FileSystemScanResult> ScanAllUserFoldersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans all shared root folders and updates the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of the scan results.</returns>
    Task<FileSystemScanResult> ScanSharedFoldersAsync(
        CancellationToken cancellationToken = default);
}
