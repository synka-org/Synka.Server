namespace Synka.Server.Services.Logging;

/// <summary>
/// Logger delegates for the FileSystemWatcherHostedService.
/// </summary>
internal static partial class FileSystemWatcherHostedServiceLoggers
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting automatic folder watching...")]
    public static partial void LogWatchingStarted(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Watching {Count} folders")]
    public static partial void LogWatchingFolderCount(
        ILogger logger,
        int count,
        Exception? exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Started watching folder {FolderId}: {PhysicalPath}")]
    public static partial void LogFolderWatchStarted(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Skipping folder {FolderId} - physical path does not exist: {PhysicalPath}")]
    public static partial void LogFolderPathNotFound(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Access denied starting watcher for folder {FolderId}: {PhysicalPath}")]
    public static partial void LogWatcherAccessDenied(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "I/O error starting watcher for folder {FolderId}: {PhysicalPath}")]
    public static partial void LogWatcherIoError(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Stopping folder watchers...")]
    public static partial void LogWatchersStopping(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "All folder watchers stopped")]
    public static partial void LogWatchersStopped(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "Change detected in folder {FolderId}: {ChangeType} - {Path}")]
    public static partial void LogChangeDetected(
        ILogger logger,
        Guid folderId,
        string changeType,
        string path,
        Exception? exception);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Rename detected in folder {FolderId}: {OldPath} -> {NewPath}")]
    public static partial void LogRenameDetected(
        ILogger logger,
        Guid folderId,
        string oldPath,
        string newPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Error,
        Message = "Error in file system watcher for folder {FolderId}: {PhysicalPath}")]
    public static partial void LogWatcherError(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Scan completed for folder {FolderId}: {FilesAdded} files added, {FilesUpdated} updated, {FilesDeleted} deleted")]
    public static partial void LogScanCompleted(
        ILogger logger,
        Guid folderId,
        int filesAdded,
        int filesUpdated,
        int filesDeleted,
        Exception? exception);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Error,
        Message = "Failed to trigger scan for folder {FolderId}")]
    public static partial void LogScanFailed(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Error,
        Message = "Unauthorized access when scanning folder {FolderId}")]
    public static partial void LogScanUnauthorized(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Information,
        Message = "Dynamically started watching newly created folder {FolderId}: {PhysicalPath}")]
    public static partial void LogDynamicWatcherAdded(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Information,
        Message = "Stopped watching deleted folder {FolderId}")]
    public static partial void LogDynamicWatcherRemoved(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Information,
        Message = "No root paths configured for the file system watcher; service will remain idle.")]
    public static partial void LogNoConfiguredRootPaths(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Warning,
        Message = "Ignoring configured watcher root path: {RootPath}")]
    public static partial void LogConfiguredRootPathInvalid(
        ILogger logger,
        string rootPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Warning,
        Message = "Configured watcher root path has no matching root folder: {RootPath}")]
    public static partial void LogConfiguredRootPathNotMapped(
        ILogger logger,
        string rootPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Debug,
        Message = "Skipping folder {FolderId} because its physical path is not configured for watching: {PhysicalPath}")]
    public static partial void LogRootFolderNotConfigured(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Warning,
        Message = "Skipping folder {FolderId} because its physical path could not be normalized: {PhysicalPath}")]
    public static partial void LogFolderPathNormalizationFailed(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 22,
        Level = LogLevel.Warning,
        Message = "Ignoring user root base path '{BasePath}' because it is invalid")]
    public static partial void LogUserRootBasePathInvalid(
        ILogger logger,
        string basePath,
        Exception? exception);

    [LoggerMessage(
        EventId = 23,
        Level = LogLevel.Warning,
        Message = "Skipping user root folder {FolderId} for user {UserId} at path {PhysicalPath} because UserRootBasePath is not configured")]
    public static partial void LogUserRootBasePathMissing(
        ILogger logger,
        Guid userId,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 24,
        Level = LogLevel.Warning,
        Message = "Skipping user root folder for user {UserId}. Expected path: {ExpectedPath}, actual path: {ActualPath}")]
    public static partial void LogUserRootPathMismatch(
        ILogger logger,
        Guid userId,
        string expectedPath,
        string actualPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 25,
        Level = LogLevel.Error,
        Message = "Failed to build expected user root path for user {UserId} using base path {BasePath}")]
    public static partial void LogUserRootPathBuildFailed(
        ILogger logger,
        Guid userId,
        string? basePath,
        Exception? exception);

    [LoggerMessage(
        EventId = 26,
        Level = LogLevel.Information,
        Message = "Created user root directory for user {UserId}: {PhysicalPath}")]
    public static partial void LogUserRootDirectoryCreated(
        ILogger logger,
        Guid userId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 27,
        Level = LogLevel.Error,
        Message = "Failed to create user root directory for user {UserId}: {PhysicalPath}")]
    public static partial void LogUserRootDirectoryCreationFailed(
        ILogger logger,
        Guid userId,
        string physicalPath,
        Exception? exception);
}
