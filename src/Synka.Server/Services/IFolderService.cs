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

    Task<FolderEntity> GetFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderEntity>> GetUserRootFoldersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderEntity>> GetSharedRootFoldersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderEntity>> GetSubfoldersAsync(
        Guid parentFolderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderEntity>> GetAccessibleFoldersAsync(
        Guid userId,
        Guid? parentFolderId = null,
        CancellationToken cancellationToken = default);

    Task DeleteFolderAsync(
        Guid folderId,
        bool softDelete = true,
        CancellationToken cancellationToken = default);

    Task RestoreFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);
}
