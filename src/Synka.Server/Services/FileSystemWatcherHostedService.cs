using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synka.Server.Data;
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        FileSystemWatcherHostedServiceLoggers.LogWatchingStarted(logger, null);

        using var scope = scopeFactory.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<SynkaDbContext>();

        // Get configured root folders (shared or user-specific) that are not deleted
        var rootFolders = await dbContext.Folders
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.ParentFolderId == null)
            .ToListAsync(cancellationToken);

        foreach (var folder in rootFolders)
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
