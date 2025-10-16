using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

/// <summary>
/// Service for handling file operations with metadata tracking.
/// </summary>
/// <param name="dbContext">Database context.</param>
/// <param name="configuration">Application configuration.</param>
/// <param name="httpContextAccessor">HTTP context accessor.</param>
/// <param name="logger">Logger instance.</param>
public sealed class FileService(
    SynkaDbContext dbContext,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    ILogger<FileService> logger) : IFileService
{
    /// <summary>
    /// 100 MB default
    /// </summary>
    private const long MaxFileSizeBytes = 100 * 1024 * 1024;
    private readonly string _uploadDirectory = configuration.GetValue<string>("FileUpload:Directory") ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    /// <summary>
    /// Upload a file and store its metadata.
    /// </summary>
    /// <param name="file">File to upload.</param>
    /// <param name="folderId">Folder ID where the file should be stored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown if file is empty or exceeds maximum allowed size.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if user cannot be identified.</exception>
    public async Task<FileUploadResponse> UploadFileAsync(
        IFormFile file,
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var userId = ExtractUserId();

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
                hashAlgorithm.TransformFinalBlock([], 0, 0);

                if (hashAlgorithm.Hash is not null)
                {
                    contentHash = Convert.ToHexString(hashAlgorithm.Hash);
                }
            }

            // Store metadata in database
            var metadata = new FileMetadataEntity
            {
                Id = fileId,
                UploadedById = userId,
                FolderId = folderId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                StoragePath = storagePath,
                ContentHash = contentHash,
                UploadedAt = DateTimeOffset.UtcNow
            };

            dbContext.FileMetadata.Add(metadata);
            await dbContext.SaveChangesAsync(cancellationToken);

            FileServiceLoggers.LogFileUploaded(logger, fileId, file.FileName, file.Length, userId, null);

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
                catch (Exception ex)
                {
                    FileServiceLoggers.LogDeleteFileFailed(logger, storagePath, ex);
                }
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
    /// <exception cref="UnauthorizedAccessException">Thrown if user cannot be identified.</exception>
    public async Task<FileMetadataResponse?> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var userId = ExtractUserId();

        var metadata = await dbContext.FileMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedById == userId, cancellationToken);

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
            metadata.ContentHash,
            metadata.UploadedAt,
            metadata.UpdatedAt);
    }

    /// <summary>
    /// List files in a folder.
    /// </summary>
    /// <param name="folderId">Folder ID to filter files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user cannot be identified.</exception>
    public async Task<IEnumerable<FileMetadataResponse>> ListUserFilesAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var userId = ExtractUserId();

        var files = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.UploadedById == userId && f.FolderId == folderId)
            .ToListAsync(cancellationToken);

        // Order in memory after materialization (SQLite doesn't support DateTimeOffset in ORDER BY)
        var orderedFiles = files.OrderByDescending(f => f.UploadedAt);

        return orderedFiles.Select(metadata => new FileMetadataResponse(
            metadata.Id,
            metadata.FileName,
            metadata.ContentType,
            metadata.SizeBytes,
            metadata.StoragePath,
            metadata.ContentHash,
            metadata.UploadedAt,
            metadata.UpdatedAt));
    }

    /// <summary>
    /// Delete file and metadata.
    /// </summary>
    /// <param name="fileId">File ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user cannot be identified.</exception>
    public async Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var userId = ExtractUserId();

        var metadata = await dbContext.FileMetadata
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedById == userId, cancellationToken);

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
                FileServiceLoggers.LogFileDeleted(logger, metadata.StoragePath, fileId, null);
            }
#pragma warning disable CA1031 // Catch specific exception - logging deletion failure
            catch (Exception ex)
            {
                FileServiceLoggers.LogDeleteFileForIdFailed(logger, metadata.StoragePath, fileId, ex);
            }
#pragma warning restore CA1031
        }

        // Delete metadata from database
        dbContext.FileMetadata.Remove(metadata);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Guid ExtractUserId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new UnauthorizedAccessException("HTTP context is not available");
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID could not be determined from the current context");
        }
        return userId;
    }
}
