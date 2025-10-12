using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Helpers;

namespace Synka.Server.Services;

/// <summary>
/// Service for handling file uploads with metadata tracking using platform-specific file identifiers.
/// </summary>
/// <param name="dbContext">Database context.</param>
/// <param name="configuration">Application configuration.</param>
/// <param name="logger">Logger instance.</param>
public sealed class FileUploadService(
    SynkaDbContext dbContext,
    IConfiguration configuration,
    ILogger<FileUploadService> logger) : IFileUploadService
{
    /// <summary>
    /// 100 MB default
    /// </summary>
    private const long MaxFileSizeBytes = 100 * 1024 * 1024;
    private readonly string _uploadDirectory = configuration.GetValue<string>("FileUpload:Directory") ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    private static readonly Action<ILogger, Guid, string, long, Guid, Exception?> LogFileUploaded =
        LoggerMessage.Define<Guid, string, long, Guid>(
            LogLevel.Information,
            new EventId(1, nameof(LogFileUploaded)),
            "File uploaded - ID: {FileId}, Name: {FileName}, Size: {Size}, User: {UserId}");

    private static readonly Action<ILogger, string, Exception?> LogDeleteFileFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, nameof(LogDeleteFileFailed)),
            "Failed to delete file {Path} after upload failure");

    private static readonly Action<ILogger, string, Guid, Exception?> LogFileDeleted =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Information,
            new EventId(3, nameof(LogFileDeleted)),
            "Deleted file {Path} for file ID {FileId}");

    private static readonly Action<ILogger, string, Guid, Exception?> LogDeleteFileForIdFailed =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Warning,
            new EventId(4, nameof(LogDeleteFileForIdFailed)),
            "Failed to delete file {Path} for file ID {FileId}");

    /// <summary>
    /// Upload a file and store its metadata.
    /// </summary>
    /// <param name="userId">User ID of the uploader.</param>
    /// <param name="file">File to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown if file is empty or exceeds maximum allowed size.</exception>
    public async Task<FileUploadResponse> UploadFileAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
        {
            throw new ArgumentException("File is empty", nameof(file));
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSizeBytes} bytes", nameof(file));
        }

        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadDirectory);

        // Generate unique file ID and storage path
        var fileId = Guid.NewGuid();
        var fileExtension = Path.GetExtension(file.FileName);
        var storageFileName = $"{fileId}{fileExtension}";
        var storagePath = Path.Combine(_uploadDirectory, storageFileName);

        string? contentHash = null;
        string? windowsFileId = null;
        string? unixFileId = null;

        try
        {
            // Save file to disk and compute hash
            using var hashAlgorithm = SHA256.Create();
            await using (var fileStream = new FileStream(storagePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await using var sourceStream = file.OpenReadStream();

                // Copy file while computing hash
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
                hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                if (hashAlgorithm.Hash is not null)
                {
                    contentHash = Convert.ToHexString(hashAlgorithm.Hash);
                }
            }

            // Get platform-specific file identifiers
            windowsFileId = FileIdentifierHelper.TryGetWindowsFileId(storagePath);
            unixFileId = FileIdentifierHelper.TryGetUnixFileId(storagePath);

            // Store metadata in database
            var metadata = new FileMetadataEntity
            {
                Id = fileId,
                UserId = userId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                StoragePath = storagePath,
                WindowsFileId = windowsFileId,
                UnixFileId = unixFileId,
                ContentHash = contentHash,
                UploadedAt = DateTimeOffset.UtcNow
            };

            dbContext.FileMetadata.Add(metadata);
            await dbContext.SaveChangesAsync(cancellationToken);

            LogFileUploaded(logger, fileId, file.FileName, file.Length, userId, null);

            return new FileUploadResponse(
                fileId,
                metadata.FileName,
                metadata.ContentType,
                metadata.SizeBytes,
                metadata.ContentHash,
                metadata.UploadedAt);
        }
#pragma warning disable CA1031 // Catch specific exception - cleanup on any failure
        catch
        {
            // Clean up file on failure
            if (File.Exists(storagePath))
            {
                try
                {
                    File.Delete(storagePath);
                }
#pragma warning disable CA1031 // Catch specific exception - logging cleanup failure
                catch (Exception ex)
                {
                    LogDeleteFileFailed(logger, storagePath, ex);
                }
#pragma warning restore CA1031
            }

            throw;
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Get file metadata by ID.
    /// </summary>
    /// <param name="fileId">File ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<FileMetadataResponse?> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await dbContext.FileMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

        if (metadata is null)
        {
            return null;
        }

        return new FileMetadataResponse(
            metadata.Id,
            metadata.FileName,
            metadata.ContentType,
            metadata.SizeBytes,
            metadata.StoragePath,
            metadata.WindowsFileId,
            metadata.UnixFileId,
            metadata.ContentHash,
            metadata.UploadedAt,
            metadata.UpdatedAt);
    }

    /// <summary>
    /// List files for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IEnumerable<FileMetadataResponse>> ListUserFilesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var files = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

        return files.Select(metadata => new FileMetadataResponse(
            metadata.Id,
            metadata.FileName,
            metadata.ContentType,
            metadata.SizeBytes,
            metadata.StoragePath,
            metadata.WindowsFileId,
            metadata.UnixFileId,
            metadata.ContentHash,
            metadata.UploadedAt,
            metadata.UpdatedAt));
    }

    /// <summary>
    /// Delete file and metadata.
    /// </summary>
    /// <param name="fileId">File ID.</param>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<bool> DeleteFileAsync(
        Guid fileId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await dbContext.FileMetadata
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId, cancellationToken);

        if (metadata is null)
        {
            return false;
        }

        // Delete file from disk
        if (File.Exists(metadata.StoragePath))
        {
            try
            {
                File.Delete(metadata.StoragePath);
                LogFileDeleted(logger, metadata.StoragePath, fileId, null);
            }
#pragma warning disable CA1031 // Catch specific exception - logging deletion failure
            catch (Exception ex)
            {
                LogDeleteFileForIdFailed(logger, metadata.StoragePath, fileId, ex);
            }
#pragma warning restore CA1031
        }

        // Delete metadata from database
        dbContext.FileMetadata.Remove(metadata);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
