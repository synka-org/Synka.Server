using Synka.Server.Contracts;

namespace Synka.Server.Services;

public interface IFileService
{
    /// <summary>
    /// Upload a file and store its metadata.
    /// </summary>
    /// <param name="file">File to upload.</param>
    /// <param name="folderId">Folder ID where the file should be stored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FileUploadResponse> UploadFileAsync(
        IFormFile file,
        Guid folderId,
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
    /// List files in a folder.
    /// </summary>
    /// <param name="folderId">Folder ID to filter files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<FileMetadataResponse>> ListUserFilesAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete file and metadata.
    /// </summary>
    /// <param name="fileId">File ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);
}
