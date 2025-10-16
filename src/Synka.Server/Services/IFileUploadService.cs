using Synka.Server.Contracts;

namespace Synka.Server.Services;

public interface IFileUploadService
{
    /// <summary>
    /// Upload a file and store its metadata.
    /// </summary>
    /// <param name="userId">User ID of the uploader.</param>
    /// <param name="file">File to upload.</param>
    /// <param name="folderId">Optional folder ID where the file should be stored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FileUploadResponse> UploadFileAsync(
        Guid userId,
        IFormFile file,
        Guid? folderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file metadata by ID.
    /// </summary>
    /// <param name="fileId">File ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FileMetadataResponse?> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List files for a user, optionally filtered by folder.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="folderId">Optional folder ID to filter files. If null, returns all user's files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<FileMetadataResponse>> ListUserFilesAsync(
        Guid userId,
        Guid? folderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete file and metadata.
    /// </summary>
    /// <param name="fileId">File ID.</param>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> DeleteFileAsync(
        Guid fileId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
