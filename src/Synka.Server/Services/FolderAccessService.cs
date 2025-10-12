using Microsoft.EntityFrameworkCore;
using Synka.Server.Data;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public sealed class FolderAccessService(SynkaDbContext context) : IFolderAccessService
{
    public async Task<bool> HasAccessAsync(
        Guid userId,
        Guid folderId,
        FolderAccessLevel requiredPermission,
        CancellationToken cancellationToken = default)
    {
        var effectivePermission = await GetEffectivePermissionAsync(userId, folderId, cancellationToken);
        return effectivePermission.HasValue && effectivePermission >= requiredPermission;
    }

    public async Task<FolderAccessLevel?> GetEffectivePermissionAsync(
        Guid userId,
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var folder = await context.Folders
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == folderId && !f.IsDeleted, cancellationToken);

        if (folder is null)
        {
            return null;
        }

        // Owner has admin permission
        if (folder.OwnerId == userId)
        {
            return FolderAccessLevel.Admin;
        }

        // Shared root folders (OwnerId == null) are accessible to all authenticated users with Read permission
        if (folder.IsSharedRoot)
        {
            return FolderAccessLevel.Read;
        }

        // Check direct access grant
        var now = DateTimeOffset.UtcNow;
        var directAccess = await context.FolderAccess
            .AsNoTracking()
            .Where(a =>
                a.FolderId == folderId &&
                a.UserId == userId &&
                (a.ExpiresAt == null || a.ExpiresAt > now))
            .Select(a => a.Permission)
            .FirstOrDefaultAsync(cancellationToken);

        if (directAccess != default)
        {
            return directAccess;
        }

        // Check inherited access from parent folders
        return await GetInheritedPermissionAsync(userId, folder.ParentFolderId, cancellationToken);
    }

    public async Task<bool> CanShareAsync(
        Guid userId,
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var permission = await GetEffectivePermissionAsync(userId, folderId, cancellationToken);
        return permission == FolderAccessLevel.Admin;
    }

    public async Task GrantAccessAsync(
        Guid userId,
        Guid folderId,
        Guid grantedById,
        FolderAccessLevel permission,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        // Verify folder exists
        var folderExists = await context.Folders
            .AnyAsync(f => f.Id == folderId && !f.IsDeleted, cancellationToken);

        if (!folderExists)
        {
            throw new InvalidOperationException($"Folder '{folderId}' not found.");
        }

        // Check if grantor has admin permission
        if (!await CanShareAsync(grantedById, folderId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User '{grantedById}' does not have permission to share folder '{folderId}'.");
        }

        // Check if access already exists
        var existingAccess = await context.FolderAccess
            .FirstOrDefaultAsync(a => a.FolderId == folderId && a.UserId == userId, cancellationToken);

        if (existingAccess is not null)
        {
            throw new InvalidOperationException($"User '{userId}' already has access to folder '{folderId}'.");
        }

        var access = new FolderAccessEntity
        {
            FolderId = folderId,
            UserId = userId,
            GrantedById = grantedById,
            Permission = permission,
            ExpiresAt = expiresAt
        };

        context.FolderAccess.Add(access);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAccessAsync(
        Guid userId,
        Guid folderId,
        FolderAccessLevel permission,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        var access = await context.FolderAccess
            .FirstOrDefaultAsync(a => a.FolderId == folderId && a.UserId == userId, cancellationToken);

        if (access is null)
        {
            throw new InvalidOperationException($"User '{userId}' does not have access to folder '{folderId}'.");
        }

        access.Permission = permission;
        access.ExpiresAt = expiresAt;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAccessAsync(
        Guid userId,
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var access = await context.FolderAccess
            .FirstOrDefaultAsync(a => a.FolderId == folderId && a.UserId == userId, cancellationToken);

        if (access is not null)
        {
            context.FolderAccess.Remove(access);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<FolderAccessEntity>> GetFolderAccessListAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await context.FolderAccess
            .Include(a => a.User)
            .Include(a => a.GrantedBy)
            .Where(a => a.FolderId == folderId &&
                       (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderBy(a => a.User.UserName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleFolderIdsAsync(
        Guid userId,
        FolderAccessLevel? minimumPermission = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Get folders owned by user
        var ownedFolders = await context.Folders
            .Where(f => f.OwnerId == userId && !f.IsDeleted)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        // Get folders shared with user (split query for EF Core translation)
        var sharedAccessGrants = await context.FolderAccess
            .Where(a => a.UserId == userId &&
                       (a.ExpiresAt == null || a.ExpiresAt > now))
            .Select(a => new { a.FolderId, a.Permission })
            .ToListAsync(cancellationToken);

        var sharedFolders = sharedAccessGrants
            .Where(a => minimumPermission == null || a.Permission >= minimumPermission)
            .Select(a => a.FolderId)
            .ToList();

        // Get shared root folders (accessible to all)
        List<Guid> sharedRoots = [];
        if (minimumPermission == null || minimumPermission == FolderAccessLevel.Read)
        {
            sharedRoots = await context.Folders
                .Where(f => f.OwnerId == null && f.ParentFolderId == null && !f.IsDeleted)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);
        }

        return [.. ownedFolders, .. sharedFolders, .. sharedRoots];
    }

    private async Task<FolderAccessLevel?> GetInheritedPermissionAsync(
        Guid userId,
        Guid? parentFolderId,
        CancellationToken cancellationToken)
    {
        if (!parentFolderId.HasValue)
        {
            return null;
        }

        // Recursively check parent folder permission
        return await GetEffectivePermissionAsync(userId, parentFolderId.Value, cancellationToken);
    }
}
