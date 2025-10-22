using Microsoft.EntityFrameworkCore;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Exceptions;
using Synka.Server.Extensions;

namespace Synka.Server.Services;

public sealed class FolderService(SynkaDbContext context, TimeProvider timeProvider, ICurrentUserAccessor currentUserAccessor, IFileSystemService fileSystem) : IFolderService
{
    public async Task<FolderResponse> CreateFolderAsync(
        Guid parentFolderId,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Validate parent folder exists
        var parentExists = await context.Folders
            .AnyAsync(f => f.Id == parentFolderId && !f.IsDeleted, cancellationToken);

        if (!parentExists)
        {
            throw new FolderNotFoundException(parentFolderId, "Parent");
        }

        var userId = currentUserAccessor.GetCurrentUserId();

        var folder = await CreateFolderInternalAsync(
            userId,
            name,
            parentFolderId,
            null, // Physical path will be constructed relative to parent
            cancellationToken);

        // Project to response
        return new FolderResponse(
            folder.Id,
            folder.OwnerId,
            folder.ParentFolderId,
            folder.Name,
            folder.PhysicalPath,
            folder.IsSharedRoot,
            folder.IsUserRoot,
            folder.IsDeleted,
            0, // FileCount
            0, // SubfolderCount
            folder.CreatedAt,
            folder.UpdatedAt);
    }

    public async Task<FolderResponse> GetFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var folder = await context.Folders
            .Where(f => f.Id == folderId && !f.IsDeleted)
            .ProjectToResponse()
            .FirstOrDefaultAsync(cancellationToken);

        return folder ?? throw new FolderNotFoundException(folderId);
    }

    public async Task<IReadOnlyList<FolderResponse>> GetRootFoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetCurrentUserId();

        return await context.Folders
            .Where(f => f.ParentFolderId == null && !f.IsDeleted && (f.OwnerId == userId || f.OwnerId == null))
            .OrderBy(f => f.OwnerId == null ? 0 : 1) // Shared roots first
            .ThenBy(f => f.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderResponse>> GetUserRootFoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetCurrentUserId();

        return await context.Folders
            .Where(f => f.OwnerId == userId && f.ParentFolderId == null && !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderResponse>> GetSharedRootFoldersAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Folders
            .Where(f => f.OwnerId == null && f.ParentFolderId == null && !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderResponse>> GetSubfoldersAsync(
        Guid parentFolderId,
        CancellationToken cancellationToken = default)
    {
        return await context.Folders
            .Where(f => f.ParentFolderId == parentFolderId && !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderResponse>> GetAccessibleFoldersAsync(
        Guid? parentFolderId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetCurrentUserId();

        // Get folders owned by the user
        var ownedFolders = await context.Folders
            .Where(f => !f.IsDeleted && f.ParentFolderId == parentFolderId && f.OwnerId == userId)
            .OrderBy(f => f.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);

        // Get shared root folders (accessible to all)
        var sharedRoots = await context.Folders
            .Where(f => !f.IsDeleted && f.ParentFolderId == parentFolderId && f.OwnerId == null)
            .OrderBy(f => f.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);

        // Get folders explicitly shared with user
        var now = timeProvider.GetUtcNow();
        var sharedFolderIds = await context.FolderAccess
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.FolderId, a.ExpiresAt })
            .ToListAsync(cancellationToken);

        var activeSharedFolderIds = sharedFolderIds
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > now)
            .Select(a => a.FolderId)
            .ToList();

        var sharedFolders = await context.Folders
            .Where(f => !f.IsDeleted &&
                       f.ParentFolderId == parentFolderId &&
                       activeSharedFolderIds.Contains(f.Id))
            .OrderBy(f => f.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);

        // Combine and return all accessible folders, ensuring uniqueness by folder ID
        var uniqueFolders = new List<FolderResponse>(ownedFolders.Count + sharedRoots.Count + sharedFolders.Count);
        var seenFolderIds = new HashSet<Guid>();

        static void AddUniqueFolders(IEnumerable<FolderResponse> source, HashSet<Guid> seen, List<FolderResponse> target)
        {
            foreach (var folder in source)
            {
                if (seen.Add(folder.Id))
                {
                    target.Add(folder);
                }
            }
        }

        AddUniqueFolders(ownedFolders, seenFolderIds, uniqueFolders);
        AddUniqueFolders(sharedRoots, seenFolderIds, uniqueFolders);
        AddUniqueFolders(sharedFolders, seenFolderIds, uniqueFolders);

        return uniqueFolders
            .OrderBy(f => f.Name)
            .ToList();
    }

    public async Task SoftDeleteFolderAsync(
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

        // Soft delete allows restoration when content reappears
        await SoftDeleteRecursiveAsync(folder, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task HardDeleteFolderAsync(
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

        // Prevent deletion of root folders via API
        if (folder.ParentFolderId is null)
        {
            throw new InvalidOperationException("Root folders cannot be deleted.");
        }

        // Remove from disk
        if (fileSystem.DirectoryExists(folder.PhysicalPath))
        {
            try
            {
                fileSystem.DeleteDirectory(folder.PhysicalPath, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                throw new InvalidOperationException($"Failed to delete folder from disk: {ex.Message}", ex);
            }
        }

        // Hard delete from database - cascade handled by EF configuration
        context.Folders.Remove(folder);
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

    public async Task<FolderEntity> CreateFolderInternalAsync(
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
                throw new FolderNotFoundException(parentFolderId.Value, "Parent");
            }
        }

        // For root folders, require physical path
        if (!parentFolderId.HasValue && string.IsNullOrWhiteSpace(physicalPath))
        {
            throw new RootFolderPhysicalPathRequiredException();
        }

        // Construct the physical path
        if (parentFolderId.HasValue)
        {
            // For subfolders, construct path relative to parent using sanitized name
            var sanitizedName = SanitizeFileSystemName(name);
            var parent = await context.Folders
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == parentFolderId.Value, cancellationToken);

            physicalPath = Path.Combine(parent!.PhysicalPath, sanitizedName);
        }
        // else: For root folders, use physicalPath as-is

        var folder = new FolderEntity
        {
            OwnerId = ownerId,
            ParentFolderId = parentFolderId,
            Name = name,
            PhysicalPath = physicalPath!,
            CreatedAt = timeProvider.GetUtcNow()
        };

        // Create physical directory on disk
        try
        {
            if (!fileSystem.DirectoryExists(physicalPath!))
            {
                fileSystem.CreateDirectory(physicalPath!);
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to create directory '{physicalPath}': {ex.Message}", ex);
        }

        context.Folders.Add(folder);
        await context.SaveChangesAsync(cancellationToken);

        return folder;
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

    /// <summary>
    /// Sanitizes a folder name for filesystem use by removing or replacing invalid characters.
    /// </summary>
    /// <param name="name">The folder name to sanitize.</param>
    /// <returns>A sanitized folder name safe for filesystem use.</returns>
    private static string SanitizeFileSystemName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(name.Select(c => invalidChars.Contains(c) ? '_' : c));

        // Trim whitespace and dots from ends (invalid on Windows)
        sanitized = sanitized.Trim().TrimEnd('.');

        // Ensure name is not empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "folder";
        }

        return sanitized;
    }
}
