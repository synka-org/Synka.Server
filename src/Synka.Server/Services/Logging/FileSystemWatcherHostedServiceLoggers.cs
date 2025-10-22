namespace Synka.Server.Services.Logging;

/// <summary>
/// Logger delegates for the FileSystemWatcherHostedService.
/// </summary>
internal static partial class FileSystemWatcherHostedServiceLoggers
{
    [LoggerMessage(
    EventId = 301,
        Level = LogLevel.Information,
        Message = "Starting automatic folder watching...")]
    public static partial void LogWatchingStarted(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
    EventId = 302,
        Level = LogLevel.Information,
        Message = "Watching {Count} folders")]
    public static partial void LogWatchingFolderCount(
        ILogger logger,
        int count,
        Exception? exception);

    [LoggerMessage(
    EventId = 303,
        Level = LogLevel.Information,
        Message = "Started watching folder {FolderId}: {PhysicalPath}")]
    public static partial void LogFolderWatchStarted(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 304,
        Level = LogLevel.Warning,
        Message = "Skipping folder {FolderId} - physical path does not exist: {PhysicalPath}")]
    public static partial void LogFolderPathNotFound(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 305,
        Level = LogLevel.Error,
        Message = "Access denied starting watcher for folder {FolderId}: {PhysicalPath}")]
    public static partial void LogWatcherAccessDenied(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 306,
        Level = LogLevel.Error,
        Message = "I/O error starting watcher for folder {FolderId}: {PhysicalPath}")]
    public static partial void LogWatcherIoError(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 307,
        Level = LogLevel.Information,
        Message = "Stopping folder watchers...")]
    public static partial void LogWatchersStopping(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
    EventId = 308,
        Level = LogLevel.Information,
        Message = "All folder watchers stopped")]
    public static partial void LogWatchersStopped(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
    EventId = 309,
        Level = LogLevel.Information,
        Message = "Change detected in folder {FolderId}: {ChangeType} - {Path}")]
    public static partial void LogChangeDetected(
        ILogger logger,
        Guid folderId,
        string changeType,
        string path,
        Exception? exception);

    [LoggerMessage(
    EventId = 310,
        Level = LogLevel.Information,
        Message = "Rename detected in folder {FolderId}: {OldPath} -> {NewPath}")]
    public static partial void LogRenameDetected(
        ILogger logger,
        Guid folderId,
        string oldPath,
        string newPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 311,
        Level = LogLevel.Error,
        Message = "Error in file system watcher for folder {FolderId}: {PhysicalPath}")]
    public static partial void LogWatcherError(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 312,
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
    EventId = 313,
        Level = LogLevel.Error,
        Message = "Failed to trigger scan for folder {FolderId}")]
    public static partial void LogScanFailed(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
    EventId = 314,
        Level = LogLevel.Error,
        Message = "Unauthorized access when scanning folder {FolderId}")]
    public static partial void LogScanUnauthorized(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
    EventId = 315,
        Level = LogLevel.Information,
        Message = "Dynamically started watching newly created folder {FolderId}: {PhysicalPath}")]
    public static partial void LogDynamicWatcherAdded(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 316,
        Level = LogLevel.Information,
        Message = "Stopped watching deleted folder {FolderId}")]
    public static partial void LogDynamicWatcherRemoved(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
    EventId = 317,
        Level = LogLevel.Information,
        Message = "No root paths configured for the file system watcher; service will remain idle.")]
    public static partial void LogNoConfiguredRootPaths(
        ILogger logger,
        Exception? exception);

    [LoggerMessage(
    EventId = 318,
        Level = LogLevel.Warning,
        Message = "Ignoring configured watcher root path: {RootPath}")]
    public static partial void LogConfiguredRootPathInvalid(
        ILogger logger,
        string rootPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 319,
        Level = LogLevel.Warning,
        Message = "Configured watcher root path has no matching root folder: {RootPath}")]
    public static partial void LogConfiguredRootPathNotMapped(
        ILogger logger,
        string rootPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 320,
        Level = LogLevel.Debug,
        Message = "Skipping folder {FolderId} because its physical path is not configured for watching: {PhysicalPath}")]
    public static partial void LogRootFolderNotConfigured(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 321,
        Level = LogLevel.Warning,
        Message = "Skipping folder {FolderId} because its physical path could not be normalized: {PhysicalPath}")]
    public static partial void LogFolderPathNormalizationFailed(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 322,
        Level = LogLevel.Warning,
        Message = "Ignoring user root base path '{BasePath}' because it is invalid")]
    public static partial void LogUserRootBasePathInvalid(
        ILogger logger,
        string basePath,
        Exception? exception);

    [LoggerMessage(
    EventId = 323,
        Level = LogLevel.Warning,
        Message = "Skipping user root folder {FolderId} for user {UserId} at path {PhysicalPath} because UserRootBasePath is not configured")]
    public static partial void LogUserRootBasePathMissing(
        ILogger logger,
        Guid userId,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 324,
        Level = LogLevel.Warning,
        Message = "Skipping user root folder for user {UserId}. Expected path: {ExpectedPath}, actual path: {ActualPath}")]
    public static partial void LogUserRootPathMismatch(
        ILogger logger,
        Guid userId,
        string expectedPath,
        string actualPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 325,
        Level = LogLevel.Error,
        Message = "Failed to build expected user root path for user {UserId} using base path {BasePath}")]
    public static partial void LogUserRootPathBuildFailed(
        ILogger logger,
        Guid userId,
        string? basePath,
        Exception? exception);

    [LoggerMessage(
    EventId = 326,
        Level = LogLevel.Information,
        Message = "Created user root directory for user {UserId}: {PhysicalPath}")]
    public static partial void LogUserRootDirectoryCreated(
        ILogger logger,
        Guid userId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 327,
        Level = LogLevel.Error,
        Message = "Failed to create user root directory for user {UserId}: {PhysicalPath}")]
    public static partial void LogUserRootDirectoryCreationFailed(
        ILogger logger,
        Guid userId,
        string physicalPath,
        Exception? exception);

}
