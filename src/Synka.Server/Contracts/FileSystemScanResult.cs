namespace Synka.Server.Contracts;

/// <summary>
/// Result of a file system scan operation.
/// </summary>
/// <param name="FoldersScanned">Number of folders scanned.</param>
/// <param name="FilesAdded">Number of new files discovered and added to database.</param>
/// <param name="FilesUpdated">Number of files updated in database.</param>
/// <param name="FilesDeleted">Number of files marked as deleted in database.</param>
/// <param name="FoldersAdded">Number of new folders discovered and added to database.</param>
/// <param name="FoldersDeleted">Number of folders marked as deleted in database.</param>
/// <param name="Errors">List of errors encountered during the scan.</param>
public record FileSystemScanResult(
    int FoldersScanned,
    int FilesAdded,
    int FilesUpdated,
    int FilesDeleted,
    int FoldersAdded,
    int FoldersDeleted,
    IReadOnlyList<string> Errors);
