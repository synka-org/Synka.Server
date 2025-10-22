namespace Synka.Server.Services;

/// <summary>
/// LoggerMessage delegates for FileService.
/// </summary>
internal static class FileServiceLoggers
{
    public static readonly Action<ILogger, Guid, string, long, Guid, Exception?> LogFileUploaded =
        LoggerMessage.Define<Guid, string, long, Guid>(
            LogLevel.Information,
            new EventId(101, nameof(LogFileUploaded)),
            "File uploaded - ID: {FileId}, Name: {FileName}, Size: {Size}, User: {UserId}");

    public static readonly Action<ILogger, string, Exception?> LogDeleteFileFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(102, nameof(LogDeleteFileFailed)),
            "Failed to delete file {Path} after upload failure");

    public static readonly Action<ILogger, string, Guid, Exception?> LogFileDeleted =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Information,
            new EventId(103, nameof(LogFileDeleted)),
            "Deleted file {Path} for file ID {FileId}");

    public static readonly Action<ILogger, string, Guid, Exception?> LogDeleteFileForIdFailed =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Warning,
            new EventId(104, nameof(LogDeleteFileForIdFailed)),
            "Failed to delete file {Path} for file ID {FileId}");
}
