using Microsoft.Extensions.Logging;

namespace Synka.Server.Services.Logging;

/// <summary>
/// Logger delegates used by <see cref="RootFolderSynchronizationService"/>.
/// </summary>
internal static partial class RootFolderSynchronizationLoggers
{
    [LoggerMessage(
    EventId = 401,
        Level = LogLevel.Information,
        Message = "Created shared root folder {FolderId} at {PhysicalPath}")]
    public static partial void LogSharedRootCreated(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 402,
        Level = LogLevel.Information,
        Message = "Restored shared root folder {FolderId} at {PhysicalPath}")]
    public static partial void LogSharedRootRestored(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 403,
        Level = LogLevel.Warning,
        Message = "Ignoring configured shared root path because it is invalid: {PhysicalPath}")]
    public static partial void LogSharedRootPathInvalid(
        ILogger logger,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 404,
        Level = LogLevel.Warning,
        Message = "Failed to normalize existing shared root folder {FolderId} at {PhysicalPath}")]
    public static partial void LogExistingSharedRootNormalizationFailed(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception exception);

    [LoggerMessage(
    EventId = 405,
        Level = LogLevel.Warning,
        Message = "Ignoring configured user root base path because it is invalid: {BasePath}")]
    public static partial void LogUserRootBasePathInvalid(
        ILogger logger,
        string? basePath,
        Exception exception);

    [LoggerMessage(
    EventId = 406,
        Level = LogLevel.Information,
        Message = "Created user root folder {FolderId} for user {UserId} at {PhysicalPath}")]
    public static partial void LogUserRootCreated(
        ILogger logger,
        Guid folderId,
        Guid userId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 407,
        Level = LogLevel.Information,
        Message = "Restored user root folder {FolderId} for user {UserId} at {PhysicalPath}")]
    public static partial void LogUserRootRestored(
        ILogger logger,
        Guid folderId,
        Guid userId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 408,
        Level = LogLevel.Information,
        Message = "Updated user root folder {FolderId} for user {UserId} from {PreviousPath} to {PhysicalPath}")]
    public static partial void LogUserRootPathUpdated(
        ILogger logger,
        Guid folderId,
        Guid userId,
        string? previousPath,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 409,
        Level = LogLevel.Warning,
        Message = "Failed to normalize existing user root folder {FolderId} for user {UserId} at {PhysicalPath}")]
    public static partial void LogExistingUserRootNormalizationFailed(
        ILogger logger,
        Guid folderId,
        Guid userId,
        string physicalPath,
        Exception exception);

    [LoggerMessage(
    EventId = 410,
        Level = LogLevel.Warning,
        Message = "Failed to build configured user root path for user {UserId} using base path {BasePath}")]
    public static partial void LogUserRootPathBuildFailed(
        ILogger logger,
        Guid userId,
        string basePath,
        Exception exception);
}
