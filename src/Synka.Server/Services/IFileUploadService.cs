using Synka.Server.Contracts;

namespace Synka.Server.Services;

public interface IFileUploadService
{
    /// <summary>
    /// Upload a file and store its metadata.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="file"></param>
    /// <param name="cancellationToken"></param>
    Task<FileUploadResponse> UploadFileAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file metadata by ID.
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="cancellationToken"></param>
    Task<FileMetadataResponse?> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List files for a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    Task<IEnumerable<FileMetadataResponse>> ListUserFilesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete file and metadata.
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    Task<bool> DeleteFileAsync(
        Guid fileId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
