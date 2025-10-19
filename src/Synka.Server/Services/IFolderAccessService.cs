using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public interface IFolderAccessService
{
    Task<bool> HasAccessAsync(
        Guid userId,
        Guid folderId,
        FolderAccessLevel requiredPermission,
        CancellationToken cancellationToken = default);

    Task<FolderAccessLevel?> GetEffectivePermissionAsync(
        Guid userId,
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task<bool> CanShareAsync(
        Guid userId,
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task GrantAccessAsync(
        Guid userId,
        Guid folderId,
        Guid grantedById,
        FolderAccessLevel permission,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default);

    Task UpdateAccessAsync(
        Guid userId,
        Guid folderId,
        FolderAccessLevel permission,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default);

    Task RevokeAccessAsync(
        Guid userId,
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderAccessResponse>> GetFolderAccessListAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetAccessibleFolderIdsAsync(
        Guid userId,
        FolderAccessLevel? minimumPermission = null,
        CancellationToken cancellationToken = default);
}
