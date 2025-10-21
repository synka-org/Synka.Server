using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public interface IFolderService
{
    Task<FolderEntity> CreateFolderAsync(
        Guid? ownerId,
        string name,
        Guid? parentFolderId,
        string? physicalPath,
        CancellationToken cancellationToken = default);

    Task<FolderResponse> CreateSubfolderAsync(
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
