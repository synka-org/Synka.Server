using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Options;
using Synka.Server.Services.Logging;

namespace Synka.Server.Services;

/// <summary>
/// Synchronizes configured root folders with the database so watcher services have consistent state.
/// </summary>
/// <param name="dbContext">Database context used for folder queries and persistence.</param>
/// <param name="folderService">Folder service for creating new root entries.</param>
/// <param name="fileSystem">Filesystem abstraction used when ensuring directories exist.</param>
/// <param name="watcherOptions">Watcher configuration supplying expected root paths.</param>
/// <param name="timeProvider">Time provider used for timestamp updates.</param>
/// <param name="logger">Logger used for structured diagnostic messages.</param>
public sealed class RootFolderSynchronizationService(
    SynkaDbContext dbContext,
    IFolderService folderService,
    IFileSystemService fileSystem,
    IOptions<FileSystemWatcherOptions> watcherOptions,
    TimeProvider timeProvider,
    ILogger<RootFolderSynchronizationService> logger) : IRootFolderSynchronizationService
{
    public async Task EnsureRootFoldersAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await EnsureSharedRootFoldersAsync(now, cancellationToken);
        await EnsureUserRootFoldersAsync(now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSharedRootFoldersAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var configuredPaths = NormalizeConfiguredSharedPaths();

        if (configuredPaths.Count == 0)
        {
            return;
        }

        var existingSharedRoots = await dbContext.Folders
            .Where(folder => folder.ParentFolderId == null && folder.OwnerId == null)
            .ToListAsync(cancellationToken);

        var existingByPath = BuildSharedRootLookup(existingSharedRoots, logger);

        foreach (var normalizedPath in configuredPaths)
        {
            if (existingByPath.TryGetValue(normalizedPath, out var folder))
            {
                if (folder.IsDeleted)
                {
                    folder.IsDeleted = false;
                    folder.UpdatedAt = now;
                    RootFolderSynchronizationLoggers.LogSharedRootRestored(logger, folder.Id, normalizedPath, null);
                }

                EnsureDirectoryExists(normalizedPath);
                continue;
            }

            var folderName = BuildFolderName(normalizedPath);
            var created = await folderService.CreateFolderInternalAsync(
                ownerId: null,
                name: folderName,
                parentFolderId: null,
                physicalPath: normalizedPath,
                cancellationToken);

            RootFolderSynchronizationLoggers.LogSharedRootCreated(logger, created.Id, normalizedPath, null);
        }
    }

    private async Task EnsureUserRootFoldersAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var basePath = watcherOptions.Value.UserRootBasePath;

        if (string.IsNullOrWhiteSpace(basePath))
        {
            return;
        }

        string normalizedBasePath;

        try
        {
            normalizedBasePath = NormalizePath(basePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            RootFolderSynchronizationLoggers.LogUserRootBasePathInvalid(logger, basePath, ex);
            return;
        }

        var userIds = await dbContext.Users
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        var existingUserRoots = await dbContext.Folders
            .Where(folder => folder.ParentFolderId == null && folder.OwnerId != null)
            .ToListAsync(cancellationToken);

        var existingByOwner = new Dictionary<Guid, FolderEntity>(existingUserRoots.Count);

        foreach (var folder in existingUserRoots)
        {
            if (folder.OwnerId is null)
            {
                continue;
            }

            if (!existingByOwner.ContainsKey(folder.OwnerId.Value))
            {
                existingByOwner[folder.OwnerId.Value] = folder;
            }
        }

        foreach (var userId in userIds)
        {
            var expectedPath = BuildUserRootPath(normalizedBasePath, userId);

            if (expectedPath is null)
            {
                continue;
            }

            if (existingByOwner.TryGetValue(userId, out var folder))
            {
                string? normalizedExisting = null;

                try
                {
                    normalizedExisting = NormalizePath(folder.PhysicalPath);
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
                {
                    RootFolderSynchronizationLoggers.LogExistingUserRootNormalizationFailed(logger, folder.Id, userId, folder.PhysicalPath, ex);
                }

                if (normalizedExisting is null || !string.Equals(normalizedExisting, expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    folder.PhysicalPath = expectedPath;
                    folder.Name = userId.ToString("D");
                    folder.UpdatedAt = now;
                    RootFolderSynchronizationLoggers.LogUserRootPathUpdated(logger, folder.Id, userId, normalizedExisting, expectedPath, null);
                }

                if (folder.IsDeleted)
                {
                    folder.IsDeleted = false;
                    folder.UpdatedAt = now;
                    RootFolderSynchronizationLoggers.LogUserRootRestored(logger, folder.Id, userId, expectedPath, null);
                }

                EnsureDirectoryExists(expectedPath);
                continue;
            }

            var created = await folderService.CreateFolderInternalAsync(
                ownerId: userId,
                name: userId.ToString("D"),
                parentFolderId: null,
                physicalPath: expectedPath,
                cancellationToken);

            RootFolderSynchronizationLoggers.LogUserRootCreated(logger, created.Id, userId, expectedPath, null);
        }
    }

    private List<string> NormalizeConfiguredSharedPaths()
    {
        var normalized = new List<string>();

        foreach (var configuredPath in watcherOptions.Value.SharedRootPaths)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                RootFolderSynchronizationLoggers.LogSharedRootPathInvalid(logger, "<empty>", null);
                continue;
            }

            try
            {
                normalized.Add(NormalizePath(configuredPath));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                RootFolderSynchronizationLoggers.LogSharedRootPathInvalid(logger, configuredPath, ex);
            }
        }

        return normalized;
    }

    private static Dictionary<string, FolderEntity> BuildSharedRootLookup(IEnumerable<FolderEntity> folders, ILogger logger)
    {
        var lookup = new Dictionary<string, FolderEntity>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders)
        {
            try
            {
                var normalized = NormalizePath(folder.PhysicalPath);
                lookup[normalized] = folder;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                RootFolderSynchronizationLoggers.LogExistingSharedRootNormalizationFailed(
                    logger,
                    folder.Id,
                    folder.PhysicalPath,
                    ex);
            }
        }

        return lookup;
    }

    private static string BuildFolderName(string normalizedPath)
    {
        var name = Path.GetFileName(normalizedPath);

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var root = Path.GetPathRoot(normalizedPath);
        if (!string.IsNullOrWhiteSpace(root))
        {
            var trimmed = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, ':');
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed;
            }
        }

        return "root";
    }

    private string? BuildUserRootPath(string normalizedBasePath, Guid userId)
    {
        try
        {
            var combined = Path.Combine(normalizedBasePath, userId.ToString("D"));
            return NormalizePath(combined);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            RootFolderSynchronizationLoggers.LogUserRootPathBuildFailed(logger, userId, normalizedBasePath, ex);
            return null;
        }
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!fileSystem.DirectoryExists(path))
        {
            fileSystem.CreateDirectory(path);
        }
    }

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(fullPath);
    }
}
