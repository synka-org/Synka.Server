using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Options;
using Synka.Server.Services.Logging;

namespace Synka.Server.Services;

/// <summary>
/// Background service that automatically starts watching all folders on application startup.
/// </summary>
/// <param name="scopeFactory">Scope factory used to resolve scoped dependencies.</param>
/// <param name="logger">Logger instance.</param>
/// <param name="watcherOptions">Configuration options for folder watcher behavior.</param>
public sealed class FileSystemWatcherHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<FileSystemWatcherHostedService> logger,
    IOptions<FileSystemWatcherOptions> watcherOptions) : IHostedService, IFileSystemWatcherManager
{
    private readonly ConcurrentDictionary<Guid, FolderWatcherInstance> _watchers = new();
    private readonly TimeSpan _scanDebounceDelay = watcherOptions.Value.ScanDebounceDelay;
    private readonly string? _userRootBasePath = NormalizeOptionalPath(watcherOptions.Value.UserRootBasePath, logger);
    private readonly HashSet<string> _sharedRootPaths = BuildSharedRootPathSet(watcherOptions.Value.SharedRootPaths, logger);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        FileSystemWatcherHostedServiceLoggers.LogWatchingStarted(logger, null);

        if (_sharedRootPaths.Count == 0 && _userRootBasePath is null)
        {
            FileSystemWatcherHostedServiceLoggers.LogNoConfiguredRootPaths(logger, null);
            FileSystemWatcherHostedServiceLoggers.LogWatchingFolderCount(logger, 0, null);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var synchronizationService = scope.ServiceProvider.GetRequiredService<IRootFolderSynchronizationService>();
        await synchronizationService.EnsureRootFoldersAsync(cancellationToken);

        await using var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();

        // Get configured root folders (shared or user-specific) that are not deleted
        var rootFolders = await dbContext.Folders
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.ParentFolderId == null)
            .ToListAsync(cancellationToken);

        var matchingFolders = new List<(FolderEntity Folder, string NormalizedPath)>();

        foreach (var folder in rootFolders)
        {
            try
            {
                var normalizedPath = NormalizePath(folder.PhysicalPath);

                if (folder.OwnerId is null)
                {
                    if (_sharedRootPaths.Contains(normalizedPath))
                    {
                        matchingFolders.Add((folder, normalizedPath));
                    }
                    else
                    {
                        FileSystemWatcherHostedServiceLoggers.LogRootFolderNotConfigured(logger, folder.Id, folder.PhysicalPath, null);
                    }
                }
                else
                {
                    if (_userRootBasePath is null)
                    {
                        FileSystemWatcherHostedServiceLoggers.LogUserRootBasePathMissing(logger, folder.OwnerId.Value, folder.Id, folder.PhysicalPath, null);
                        continue;
                    }

                    var expectedPath = BuildExpectedUserRootPath(folder.OwnerId.Value);

                    if (expectedPath is null)
                    {
                        FileSystemWatcherHostedServiceLoggers.LogUserRootPathMismatch(logger, folder.OwnerId.Value, string.Empty, normalizedPath, null);
                        continue;
                    }

                    if (!string.Equals(normalizedPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        FileSystemWatcherHostedServiceLoggers.LogUserRootPathMismatch(logger, folder.OwnerId.Value, expectedPath, normalizedPath, null);
                        continue;
                    }

                    EnsureUserRootDirectoryExists(folder.OwnerId.Value, folder.PhysicalPath);
                    matchingFolders.Add((folder, normalizedPath));
                }
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                FileSystemWatcherHostedServiceLoggers.LogFolderPathNormalizationFailed(logger, folder.Id, folder.PhysicalPath, ex);
            }
        }

        var matchedSharedPathSet = matchingFolders
            .Where(entry => entry.Folder.OwnerId is null)
            .Select(entry => entry.NormalizedPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var configuredPath in _sharedRootPaths)
        {
            if (!matchedSharedPathSet.Contains(configuredPath))
            {
                FileSystemWatcherHostedServiceLoggers.LogConfiguredRootPathNotMapped(logger, configuredPath, null);
            }
        }

        foreach (var (folder, _) in matchingFolders)
        {
            if (!Directory.Exists(folder.PhysicalPath))
            {
                FileSystemWatcherHostedServiceLoggers.LogFolderPathNotFound(logger, folder.Id, folder.PhysicalPath, null);
                continue;
            }

            try
            {
                var watcher = new FileSystemWatcher(folder.PhysicalPath)
                {
                    NotifyFilter = NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.Size
                                 | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                var instance = new FolderWatcherInstance(
                    folder.Id,
                    folder.PhysicalPath,
                    watcher,
                    folder.OwnerId,
                    _scanDebounceDelay);

                // Subscribe to events with debouncing
                watcher.Changed += (sender, e) => OnFileSystemChanged(instance, e);
                watcher.Created += (sender, e) => OnFileSystemChanged(instance, e);
                watcher.Deleted += (sender, e) => OnFileSystemChanged(instance, e);
                watcher.Renamed += (sender, e) => OnFileSystemRenamed(instance, e);
                watcher.Error += (sender, e) => OnWatcherError(instance, e);

                _watchers[folder.Id] = instance;

                FileSystemWatcherHostedServiceLoggers.LogFolderWatchStarted(logger, folder.Id, folder.PhysicalPath, null);
            }
            catch (UnauthorizedAccessException ex)
            {
                FileSystemWatcherHostedServiceLoggers.LogWatcherAccessDenied(logger, folder.Id, folder.PhysicalPath, ex);
            }
            catch (IOException ex)
            {
                FileSystemWatcherHostedServiceLoggers.LogWatcherIoError(logger, folder.Id, folder.PhysicalPath, ex);
            }
        }

        FileSystemWatcherHostedServiceLoggers.LogWatchingFolderCount(logger, _watchers.Count, null);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        FileSystemWatcherHostedServiceLoggers.LogWatchersStopping(logger, null);

        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }
        _watchers.Clear();

        FileSystemWatcherHostedServiceLoggers.LogWatchersStopped(logger, null);
        return Task.CompletedTask;
    }

    private static HashSet<string> BuildSharedRootPathSet(
        IEnumerable<string>? sharedPaths,
        ILogger<FileSystemWatcherHostedService> logger)
    {
        var normalizedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var configuredPath in sharedPaths ?? Array.Empty<string>())
        {
            var candidate = string.IsNullOrWhiteSpace(configuredPath)
                ? "<empty>"
                : configuredPath;

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                FileSystemWatcherHostedServiceLoggers.LogConfiguredRootPathInvalid(logger, candidate, null);
                continue;
            }

            try
            {
                var normalized = NormalizePath(configuredPath);
                normalizedPaths.Add(normalized);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                FileSystemWatcherHostedServiceLoggers.LogConfiguredRootPathInvalid(logger, candidate, ex);
            }
        }

        return normalizedPaths;
    }

    private static string? NormalizeOptionalPath(
        string? path,
        ILogger<FileSystemWatcherHostedService> logger)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return NormalizePath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            FileSystemWatcherHostedServiceLoggers.LogUserRootBasePathInvalid(logger, path, ex);
            return null;
        }
    }

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(fullPath);
    }

    private string? BuildExpectedUserRootPath(Guid userId)
    {
        if (_userRootBasePath is null)
        {
            return null;
        }

        try
        {
            return NormalizePath(Path.Combine(_userRootBasePath, userId.ToString("D")));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            FileSystemWatcherHostedServiceLoggers.LogUserRootPathBuildFailed(logger, userId, _userRootBasePath, ex);
            return null;
        }
    }

    private void EnsureUserRootDirectoryExists(Guid userId, string physicalPath)
    {
        if (Directory.Exists(physicalPath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(physicalPath);
            FileSystemWatcherHostedServiceLoggers.LogUserRootDirectoryCreated(logger, userId, physicalPath, null);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            FileSystemWatcherHostedServiceLoggers.LogUserRootDirectoryCreationFailed(logger, userId, physicalPath, ex);
        }
    }

    /// <summary>
    /// Starts watching a newly created folder.
    /// </summary>
    /// <param name="folderId">The ID of the folder to watch.</param>
    /// <param name="physicalPath">The physical path of the folder.</param>
    /// <param name="ownerId">The owner ID of the folder.</param>
    /// <param name="isRootFolder">True when the folder represents a configured root path.</param>
    public void StartWatchingFolder(Guid folderId, string physicalPath, Guid? ownerId, bool isRootFolder)
    {
        if (!isRootFolder)
        {
            return;
        }

        string normalizedPath;

        try
        {
            normalizedPath = NormalizePath(physicalPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            FileSystemWatcherHostedServiceLoggers.LogFolderPathNormalizationFailed(logger, folderId, physicalPath, ex);
            return;
        }

        if (ownerId is null)
        {
            if (_sharedRootPaths.Count == 0)
            {
                FileSystemWatcherHostedServiceLoggers.LogRootFolderNotConfigured(logger, folderId, physicalPath, null);
                return;
            }

            if (!_sharedRootPaths.Contains(normalizedPath))
            {
                FileSystemWatcherHostedServiceLoggers.LogRootFolderNotConfigured(logger, folderId, physicalPath, null);
                return;
            }
        }
        else
        {
            if (_userRootBasePath is null)
            {
                FileSystemWatcherHostedServiceLoggers.LogUserRootBasePathMissing(logger, ownerId.Value, folderId, physicalPath, null);
                return;
            }

            var expectedPath = BuildExpectedUserRootPath(ownerId.Value);

            if (expectedPath is null || !string.Equals(normalizedPath, expectedPath, StringComparison.OrdinalIgnoreCase))
            {
                FileSystemWatcherHostedServiceLoggers.LogUserRootPathMismatch(logger, ownerId.Value, expectedPath ?? string.Empty, normalizedPath, null);
                return;
            }

            EnsureUserRootDirectoryExists(ownerId.Value, physicalPath);
        }

        if (_watchers.ContainsKey(folderId))
        {
            return; // Already watching
        }

        if (!Directory.Exists(physicalPath))
        {
            FileSystemWatcherHostedServiceLoggers.LogFolderPathNotFound(logger, folderId, physicalPath, null);
            return;
        }

        try
        {
            var watcher = new FileSystemWatcher(physicalPath)
            {
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.Size
                             | NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            var instance = new FolderWatcherInstance(folderId, physicalPath, watcher, ownerId, _scanDebounceDelay);

            // Subscribe to events with debouncing
            watcher.Changed += (sender, e) => OnFileSystemChanged(instance, e);
            watcher.Created += (sender, e) => OnFileSystemChanged(instance, e);
            watcher.Deleted += (sender, e) => OnFileSystemChanged(instance, e);
            watcher.Renamed += (sender, e) => OnFileSystemRenamed(instance, e);
            watcher.Error += (sender, e) => OnWatcherError(instance, e);

            _watchers[folderId] = instance;
            FileSystemWatcherHostedServiceLoggers.LogDynamicWatcherAdded(logger, folderId, physicalPath, null);
        }
        catch (UnauthorizedAccessException ex)
        {
            FileSystemWatcherHostedServiceLoggers.LogWatcherAccessDenied(logger, folderId, physicalPath, ex);
        }
        catch (IOException ex)
        {
            FileSystemWatcherHostedServiceLoggers.LogWatcherIoError(logger, folderId, physicalPath, ex);
        }
    }

    /// <summary>
    /// Stops watching a folder.
    /// </summary>
    /// <param name="folderId">The ID of the folder to stop watching.</param>
    public void StopWatchingFolder(Guid folderId)
    {
        if (_watchers.TryRemove(folderId, out var watcher))
        {
            watcher.Dispose();
            FileSystemWatcherHostedServiceLoggers.LogDynamicWatcherRemoved(logger, folderId, null);
        }
    }

    private void OnFileSystemChanged(FolderWatcherInstance instance, FileSystemEventArgs e)
    {
        instance.ScheduleScan(() =>
        {
            FileSystemWatcherHostedServiceLoggers.LogChangeDetected(
                logger, instance.FolderId, e.ChangeType.ToString(), e.FullPath, null);
            TriggerScanAsync(instance).GetAwaiter().GetResult();
        });
    }

    private void OnFileSystemRenamed(FolderWatcherInstance instance, RenamedEventArgs e)
    {
        instance.ScheduleScan(() =>
        {
            FileSystemWatcherHostedServiceLoggers.LogRenameDetected(
                logger, instance.FolderId, e.OldFullPath, e.FullPath, null);
            TriggerScanAsync(instance).GetAwaiter().GetResult();
        });
    }

    private void OnWatcherError(FolderWatcherInstance instance, ErrorEventArgs e)
    {
        var exception = e.GetException();

        FileSystemWatcherHostedServiceLoggers.LogWatcherError(
            logger, instance.FolderId, instance.PhysicalPath, exception);
    }

    private async Task TriggerScanAsync(FolderWatcherInstance instance)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scannerService = scope.ServiceProvider.GetRequiredService<IFileSystemScannerService>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            var previousHttpContext = httpContextAccessor.HttpContext;

            try
            {
                // Create a temporary HttpContext with the folder owner's identity for authorization
                // For system-level scans (shared folders), use a system identity
                if (instance.OwnerId.HasValue)
                {
                    httpContextAccessor.HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(
                            new ClaimsIdentity([
                                new Claim(
                                    ClaimTypes.NameIdentifier,
                                    instance.OwnerId.Value.ToString())
                            ]))
                    };
                }

                var result = await scannerService.ScanFolderAsync(instance.FolderId, CancellationToken.None);

                FileSystemWatcherHostedServiceLoggers.LogScanCompleted(
                    logger, instance.FolderId, result.FilesAdded, result.FilesUpdated, result.FilesDeleted, null);
            }
            finally
            {
                httpContextAccessor.HttpContext = previousHttpContext;
            }
        }
        catch (InvalidOperationException ex)
        {
            FileSystemWatcherHostedServiceLoggers.LogScanFailed(logger, instance.FolderId, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            FileSystemWatcherHostedServiceLoggers.LogScanUnauthorized(logger, instance.FolderId, ex);
        }
    }

    private sealed class FolderWatcherInstance : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly Timer _debounceTimer;
        private readonly TimeSpan _debounceDelay;
        private Action? _pendingAction;
        private readonly Lock _lock = new();

        public Guid FolderId { get; }
        public string PhysicalPath { get; }
        public Guid? OwnerId { get; }

        public FolderWatcherInstance(
            Guid folderId,
            string physicalPath,
            FileSystemWatcher watcher,
            Guid? ownerId,
            TimeSpan debounceDelay)
        {
            FolderId = folderId;
            PhysicalPath = physicalPath;
            OwnerId = ownerId;
            _watcher = watcher;
            _debounceDelay = debounceDelay;

            // Debounce timer configured to wait for a period of inactivity before triggering scan
            _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void ScheduleScan(Action action)
        {
            lock (_lock)
            {
                _pendingAction = action;

                // Reset the timer - scan will trigger after the configured debounce interval
                _debounceTimer.Change(_debounceDelay, Timeout.InfiniteTimeSpan);
            }
        }

        private void OnDebounceTimerElapsed(object? state)
        {
            Action? actionToExecute;

            lock (_lock)
            {
                actionToExecute = _pendingAction;
                _pendingAction = null;
            }

            actionToExecute?.Invoke();
        }

        public void Dispose()
        {
            _debounceTimer.Dispose();
            _watcher.Dispose();
        }
    }
}
