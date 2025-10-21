using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public interface IFolderService
{
    /// <summary>
    /// Internal method for creating folders (root or subfolder) with full control.
    /// Used by configuration/setup code and tests only.
    /// </summary>
    /// <param name="ownerId">The owner of the folder, or null for shared folders.</param>
    /// <param name="name">The name of the folder.</param>
    /// <param name="parentFolderId">The parent folder ID, or null for root folders.</param>
    /// <param name="physicalPath">The physical path on disk. Required for root folders; auto-constructed for subfolders.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created folder entity.</returns>
    Task<FolderEntity> CreateFolderInternalAsync(
        Guid? ownerId,
        string name,
        Guid? parentFolderId,
        string? physicalPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new subfolder under an existing parent folder.
    /// This is the public API method - root folders cannot be created via API.
    /// </summary>
    /// <param name="parentFolderId">The ID of the parent folder.</param>
    /// <param name="name">The name of the new subfolder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created folder response.</returns>
    Task<FolderResponse> CreateFolderAsync(
        Guid parentFolderId,
        string name,
        CancellationToken cancellationToken = default);

    Task<FolderResponse> GetFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderResponse>> GetRootFoldersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderResponse>> GetUserRootFoldersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderResponse>> GetSharedRootFoldersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderResponse>> GetSubfoldersAsync(
        Guid parentFolderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderResponse>> GetAccessibleFoldersAsync(
        Guid? parentFolderId = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task HardDeleteFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task RestoreFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);
}
