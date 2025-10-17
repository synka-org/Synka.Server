namespace Synka.Server.Services.Logging;

/// <summary>
/// Logger delegates for the FileSystemScannerService.
/// </summary>
internal static partial class FileSystemScannerLoggers
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "File added: {FileId} - {FileName} ({SizeBytes} bytes)")]
    public static partial void LogFileAdded(
        ILogger logger,
        Guid fileId,
        string fileName,
        long sizeBytes,
        Exception? exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "File updated: {FileId} - {FileName} ({SizeBytes} bytes)")]
    public static partial void LogFileUpdated(
        ILogger logger,
        Guid fileId,
        string fileName,
        long sizeBytes,
        Exception? exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "File marked as deleted: {FileId} - {FileName}")]
    public static partial void LogFileMarkedDeleted(
        ILogger logger,
        Guid fileId,
        string fileName,
        Exception? exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Folder added: {FolderId} - {FolderName}")]
    public static partial void LogFolderAdded(
        ILogger logger,
        Guid folderId,
        string folderName,
        Exception? exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Folder marked as deleted: {FolderId} - {FolderName}")]
    public static partial void LogFolderMarkedDeleted(
        ILogger logger,
        Guid folderId,
        string folderName,
        Exception? exception);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Folder not found on disk: {FolderId} - {PhysicalPath}")]
    public static partial void LogFolderNotFound(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Error scanning folder: {FolderId} - {PhysicalPath}")]
    public static partial void LogScanError(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Error adding file: {FileName}")]
    public static partial void LogFileAddError(
        ILogger logger,
        string fileName,
        Exception? exception);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Error checking file: {FileName}")]
    public static partial void LogFileCheckError(
        ILogger logger,
        string fileName,
        Exception? exception);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "Error adding folder: {FolderName}")]
    public static partial void LogFolderAddError(
        ILogger logger,
        string folderName,
        Exception? exception);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Starting scan for folder: {FolderId}")]
    public static partial void LogScanStarted(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Scan completed: {FoldersScanned} folders scanned, {FilesAdded} files added, {FilesUpdated} updated, {FilesDeleted} deleted, {FoldersAdded} folders added, {FoldersDeleted} folders deleted, {ErrorCount} errors")]
    public static partial void LogScanCompleted(
        ILogger logger,
        int foldersScanned,
        int filesAdded,
        int filesUpdated,
        int filesDeleted,
        int foldersAdded,
        int foldersDeleted,
        int errorCount,
        Exception? exception);
}
