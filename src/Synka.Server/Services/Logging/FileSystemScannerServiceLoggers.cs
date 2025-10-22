namespace Synka.Server.Services.Logging;

/// <summary>
/// Logger delegates for the FileSystemScannerService.
/// </summary>
internal static partial class FileSystemScannerLoggers
{
    [LoggerMessage(
    EventId = 201,
        Level = LogLevel.Information,
        Message = "File added: {FileId} - {FileName} ({SizeBytes} bytes)")]
    public static partial void LogFileAdded(
        ILogger logger,
        Guid fileId,
        string fileName,
        long sizeBytes,
        Exception? exception);

    [LoggerMessage(
    EventId = 202,
        Level = LogLevel.Information,
        Message = "File updated: {FileId} - {FileName} ({SizeBytes} bytes)")]
    public static partial void LogFileUpdated(
        ILogger logger,
        Guid fileId,
        string fileName,
        long sizeBytes,
        Exception? exception);

    [LoggerMessage(
    EventId = 203,
        Level = LogLevel.Information,
        Message = "File marked as deleted: {FileId} - {FileName}")]
    public static partial void LogFileMarkedDeleted(
        ILogger logger,
        Guid fileId,
        string fileName,
        Exception? exception);

    [LoggerMessage(
    EventId = 204,
        Level = LogLevel.Information,
        Message = "Folder added: {FolderId} - {FolderName}")]
    public static partial void LogFolderAdded(
        ILogger logger,
        Guid folderId,
        string folderName,
        Exception? exception);

    [LoggerMessage(
    EventId = 205,
        Level = LogLevel.Information,
        Message = "Folder marked as deleted: {FolderId} - {FolderName}")]
    public static partial void LogFolderMarkedDeleted(
        ILogger logger,
        Guid folderId,
        string folderName,
        Exception? exception);

    [LoggerMessage(
    EventId = 206,
        Level = LogLevel.Information,
        Message = "Folder restored from deleted state: {FolderId} - {FolderName}")]
    public static partial void LogFolderRestored(
        ILogger logger,
        Guid folderId,
        string folderName,
        Exception? exception);

    [LoggerMessage(
    EventId = 207,
        Level = LogLevel.Warning,
        Message = "Folder not found on disk: {FolderId} - {PhysicalPath}")]
    public static partial void LogFolderNotFound(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 208,
        Level = LogLevel.Error,
        Message = "Error scanning folder: {FolderId} - {PhysicalPath}")]
    public static partial void LogScanError(
        ILogger logger,
        Guid folderId,
        string physicalPath,
        Exception? exception);

    [LoggerMessage(
    EventId = 209,
        Level = LogLevel.Error,
        Message = "Error adding file: {FileName}")]
    public static partial void LogFileAddError(
        ILogger logger,
        string fileName,
        Exception? exception);

    [LoggerMessage(
    EventId = 210,
        Level = LogLevel.Error,
        Message = "Error checking file: {FileName}")]
    public static partial void LogFileCheckError(
        ILogger logger,
        string fileName,
        Exception? exception);

    [LoggerMessage(
    EventId = 211,
        Level = LogLevel.Error,
        Message = "Error adding folder: {FolderName}")]
    public static partial void LogFolderAddError(
        ILogger logger,
        string folderName,
        Exception? exception);

    [LoggerMessage(
    EventId = 212,
        Level = LogLevel.Information,
        Message = "Starting scan for folder: {FolderId}")]
    public static partial void LogScanStarted(
        ILogger logger,
        Guid folderId,
        Exception? exception);

    [LoggerMessage(
    EventId = 213,
        Level = LogLevel.Information,
        Message = "Scan completed: {FoldersScanned} folders scanned, {FilesAdded} files added, {FilesUpdated} updated, {FilesDeleted} deleted, {FoldersAdded} folders added, {FoldersDeleted} folders deleted, {FoldersRestored} folders restored, {ErrorCount} errors")]
    public static partial void LogScanCompleted(
        ILogger logger,
        int foldersScanned,
        int filesAdded,
        int filesUpdated,
        int filesDeleted,
        int foldersAdded,
        int foldersDeleted,
        int foldersRestored,
        int errorCount,
        Exception? exception);
}
