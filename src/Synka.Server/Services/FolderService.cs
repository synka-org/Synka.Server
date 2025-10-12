using Microsoft.EntityFrameworkCore;
using Synka.Server.Data;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services;

public sealed class FolderService(SynkaDbContext context) : IFolderService
{
    public async Task<FolderEntity> CreateFolderAsync(
        Guid? ownerId,
        string name,
        Guid? parentFolderId,
        string? physicalPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Validate parent folder exists if specified
        if (parentFolderId.HasValue)
        {
            var parentExists = await context.Folders
                .AnyAsync(f => f.Id == parentFolderId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new ArgumentException($"Parent folder '{parentFolderId}' does not exist.", nameof(parentFolderId));
            }
        }

        // For root folders, require physical path
        if (!parentFolderId.HasValue && string.IsNullOrWhiteSpace(physicalPath))
        {
            throw new ArgumentException("Physical path is required for root folders.", nameof(physicalPath));
        }

        // For subfolders, construct path relative to parent
        if (parentFolderId.HasValue && string.IsNullOrWhiteSpace(physicalPath))
        {
            var parent = await context.Folders
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == parentFolderId.Value, cancellationToken);

            physicalPath = Path.Combine(parent!.PhysicalPath, name);
        }

        var folder = new FolderEntity
        {
            OwnerId = ownerId,
            ParentFolderId = parentFolderId,
            Name = name,
            PhysicalPath = physicalPath!
        };

        context.Folders.Add(folder);
        await context.SaveChangesAsync(cancellationToken);

        return folder;
    }

    public async Task<FolderEntity> GetFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var folder = await context.Folders
            .Include(f => f.ChildFolders)
            .Include(f => f.Files)
            .FirstOrDefaultAsync(f => f.Id == folderId && !f.IsDeleted, cancellationToken);

        return folder ?? throw new InvalidOperationException($"Folder '{folderId}' not found.");
    }

    public async Task<IReadOnlyList<FolderEntity>> GetUserRootFoldersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Folders
            .Where(f => f.OwnerId == userId && f.ParentFolderId == null && !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderEntity>> GetSharedRootFoldersAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Folders
            .Where(f => f.OwnerId == null && f.ParentFolderId == null && !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderEntity>> GetSubfoldersAsync(
        Guid parentFolderId,
        CancellationToken cancellationToken = default)
    {
        return await context.Folders
            .Where(f => f.ParentFolderId == parentFolderId && !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderEntity>> GetAccessibleFoldersAsync(
        Guid userId,
        Guid? parentFolderId = null,
        CancellationToken cancellationToken = default)
    {
        // Get folders owned by the user
        var ownedFolders = await context.Folders
            .Where(f => !f.IsDeleted && f.ParentFolderId == parentFolderId && f.OwnerId == userId)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        // Get shared root folders (accessible to all)
        var sharedRoots = await context.Folders
            .Where(f => !f.IsDeleted && f.ParentFolderId == parentFolderId && f.OwnerId == null)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        // Get folders explicitly shared with user
        var now = DateTimeOffset.UtcNow;
        var sharedFolderIds = await context.FolderAccess
            .Where(a => a.UserId == userId &&
                       (a.ExpiresAt == null || a.ExpiresAt > now))
            .Select(a => a.FolderId)
            .ToListAsync(cancellationToken);

        var sharedFolders = await context.Folders
            .Where(f => !f.IsDeleted &&
                       f.ParentFolderId == parentFolderId &&
                       sharedFolderIds.Contains(f.Id))
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        // Combine and return all accessible folders
        return ownedFolders
            .Concat(sharedRoots)
            .Concat(sharedFolders)
            .Distinct()
            .OrderBy(f => f.Name)
            .ToList();
    }

    public async Task DeleteFolderAsync(
        Guid folderId,
        bool softDelete = true,
        CancellationToken cancellationToken = default)
    {
        var folder = await context.Folders
            .Include(f => f.ChildFolders)
            .Include(f => f.Files)
            .FirstOrDefaultAsync(f => f.Id == folderId, cancellationToken);

        if (folder is null)
        {
            return;
        }

        if (softDelete)
        {
            // Recursively soft-delete subfolders and files
            await SoftDeleteRecursiveAsync(folder, cancellationToken);
        }
        else
        {
            // Hard delete - cascade handled by EF configuration
            context.Folders.Remove(folder);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var folder = await context.Folders
            .Include(f => f.ChildFolders)
            .Include(f => f.Files)
            .FirstOrDefaultAsync(f => f.Id == folderId, cancellationToken);

        if (folder is null)
        {
            return;
        }

        // Recursively restore subfolders and files
        await RestoreRecursiveAsync(folder, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        return await context.Folders
            .AnyAsync(f => f.Id == folderId && !f.IsDeleted, cancellationToken);
    }

    private static async Task SoftDeleteRecursiveAsync(FolderEntity folder, CancellationToken cancellationToken)
    {
        folder.IsDeleted = true;

        // Soft-delete all files in this folder
        foreach (var file in folder.Files)
        {
            file.IsDeleted = true;
        }

        // Recursively soft-delete subfolders
        foreach (var subfolder in folder.ChildFolders)
        {
            await SoftDeleteRecursiveAsync(subfolder, cancellationToken);
        }
    }

    private static async Task RestoreRecursiveAsync(FolderEntity folder, CancellationToken cancellationToken)
    {
        folder.IsDeleted = false;

        // Restore all files in this folder
        foreach (var file in folder.Files)
        {
            file.IsDeleted = false;
        }

        // Recursively restore subfolders
        foreach (var subfolder in folder.ChildFolders)
        {
            await RestoreRecursiveAsync(subfolder, cancellationToken);
        }
    }
}
