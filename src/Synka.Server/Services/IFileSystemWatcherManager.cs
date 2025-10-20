namespace Synka.Server.Services;

/// <summary>
/// Service for managing file system watchers dynamically.
/// </summary>
public interface IFileSystemWatcherManager
{
    /// <summary>
    /// Starts watching a newly created folder.
    /// </summary>
    /// <param name="folderId">The ID of the folder to watch.</param>
    /// <param name="physicalPath">The physical path of the folder.</param>
    /// <param name="ownerId">The owner ID of the folder.</param>
    /// <param name="isRootFolder">True when the folder represents a configured root path.</param>
    void StartWatchingFolder(Guid folderId, string physicalPath, Guid? ownerId, bool isRootFolder);

    /// <summary>
    /// Stops watching a folder.
    /// </summary>
    /// <param name="folderId">The ID of the folder to stop watching.</param>
    void StopWatchingFolder(Guid folderId);
}
