using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Synka.Server.Contracts;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Services.Logging;

namespace Synka.Server.Services;

/// <summary>
/// Service for scanning the file system and synchronizing changes with the database.
/// </summary>
/// <param name="dbContext">Database context.</param>
/// <param name="currentUserAccessor">Accessor for retrieving current user information.</param>
/// <param name="watcherManager">File system watcher manager for dynamic watcher registration.</param>
/// <param name="logger">Logger instance.</param>
/// <param name="timeProvider">Time abstraction for timestamp generation.</param>
public sealed class FileSystemScannerService(
    SynkaDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor,
    IFileSystemWatcherManager watcherManager,
    ILogger<FileSystemScannerService> logger,
    TimeProvider timeProvider) : IFileSystemScannerService
{
    /// <summary>
    /// Scans a folder and its subfolders for changes and updates the database.
    /// </summary>
    /// <param name="folderId">The ID of the folder to scan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the scan result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the folder is not found or access is denied.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated.</exception>
    public async Task<FileSystemScanResult> ScanFolderAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetCurrentUserId();
        var folder = await dbContext.Folders
            .Include(f => f.Files)
            .Include(f => f.ChildFolders)
            .FirstOrDefaultAsync(f => f.Id == folderId && (f.OwnerId == userId || f.OwnerId == null), cancellationToken);

        if (folder is null)
        {
            throw new InvalidOperationException($"Folder with ID {folderId} not found or access denied");
        }

        var result = new ScanResultBuilder();
        await ScanFolderRecursiveAsync(folder, userId, result, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return result.Build();
    }

    /// <summary>
    /// Scans all folders for the current user and updates the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the scan result.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated.</exception>
    public async Task<FileSystemScanResult> ScanAllUserFoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetCurrentUserId();
        var rootFolders = await dbContext.Folders
            .Include(f => f.Files)
            .Include(f => f.ChildFolders)
            .Where(f => f.OwnerId == userId && f.ParentFolderId == null)
            .ToListAsync(cancellationToken);

        var result = new ScanResultBuilder();

        foreach (var folder in rootFolders)
        {
            await ScanFolderRecursiveAsync(folder, userId, result, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return result.Build();
    }

    /// <summary>
    /// Scans all shared root folders and updates the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the scan result.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated.</exception>
    public async Task<FileSystemScanResult> ScanSharedFoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.GetCurrentUserId();
        var sharedRootFolders = await dbContext.Folders
            .Include(f => f.Files)
            .Include(f => f.ChildFolders)
            .Where(f => f.OwnerId == null && f.ParentFolderId == null)
            .ToListAsync(cancellationToken);

        var result = new ScanResultBuilder();

        foreach (var folder in sharedRootFolders)
        {
            await ScanFolderRecursiveAsync(folder, userId, result, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return result.Build();
    }

    private async Task ScanFolderRecursiveAsync(
        FolderEntity folder,
        Guid userId,
        ScanResultBuilder result,
        CancellationToken cancellationToken)
    {
        result.IncrementFoldersScanned();

        if (!Directory.Exists(folder.PhysicalPath))
        {
            result.AddError($"Folder not found on disk: {folder.PhysicalPath}");
            FileSystemScannerLoggers.LogFolderNotFound(logger, folder.Id, folder.PhysicalPath, null);
            return;
        }

        try
        {
            // Scan files in current folder
            await ScanFilesInFolderAsync(folder, userId, result, cancellationToken);

            // Scan subfolders
            await ScanSubfoldersAsync(folder, userId, result, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            result.AddError($"Access denied scanning folder {folder.PhysicalPath}: {ex.Message}");
            FileSystemScannerLoggers.LogScanError(logger, folder.Id, folder.PhysicalPath, ex);
        }
        catch (IOException ex)
        {
            result.AddError($"I/O error scanning folder {folder.PhysicalPath}: {ex.Message}");
            FileSystemScannerLoggers.LogScanError(logger, folder.Id, folder.PhysicalPath, ex);
        }
    }

    private async Task ScanFilesInFolderAsync(
        FolderEntity folder,
        Guid userId,
        ScanResultBuilder result,
        CancellationToken cancellationToken)
    {
        var filesOnDisk = Directory.GetFiles(folder.PhysicalPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet();

        var filesInDb = folder.Files
            .Where(f => !f.IsDeleted)
            .ToDictionary(f => Path.GetFileName(f.StoragePath), f => f);

        // Find new files
        foreach (var fileName in filesOnDisk.Except(filesInDb.Keys))
        {
            try
            {
                var filePath = Path.Combine(folder.PhysicalPath, fileName!);
                await AddNewFileAsync(folder, userId, filePath, fileName!, result, cancellationToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                result.AddError($"Access denied adding file {fileName}: {ex.Message}");
                FileSystemScannerLoggers.LogFileAddError(logger, fileName!, ex);
            }
            catch (IOException ex)
            {
                result.AddError($"I/O error adding file {fileName}: {ex.Message}");
                FileSystemScannerLoggers.LogFileAddError(logger, fileName!, ex);
            }
        }

        // Find deleted files
        foreach (var fileName in filesInDb.Keys.Except(filesOnDisk))
        {
            var file = filesInDb[fileName!];
            file.IsDeleted = true;
            file.UpdatedAt = timeProvider.GetUtcNow();
            result.IncrementFilesDeleted();
            FileSystemScannerLoggers.LogFileMarkedDeleted(logger, file.Id, file.FileName, null);
        }

        // Check for updated files (based on size or modification time)
        foreach (var fileName in filesOnDisk.Intersect(filesInDb.Keys))
        {
            try
            {
                var filePath = Path.Combine(folder.PhysicalPath, fileName!);
                var file = filesInDb[fileName!];
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length != file.SizeBytes)
                {
                    // File size changed, update metadata
                    await UpdateFileMetadataAsync(file, filePath, fileInfo, result, cancellationToken);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                result.AddError($"Access denied checking file {fileName}: {ex.Message}");
                FileSystemScannerLoggers.LogFileCheckError(logger, fileName!, ex);
            }
            catch (IOException ex)
            {
                result.AddError($"I/O error checking file {fileName}: {ex.Message}");
                FileSystemScannerLoggers.LogFileCheckError(logger, fileName!, ex);
            }
        }
    }

    private async Task AddNewFileAsync(
        FolderEntity folder,
        Guid userId,
        string filePath,
        string fileName,
        ScanResultBuilder result,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        var contentHash = await ComputeFileHashAsync(filePath, cancellationToken);

        var uploadedAt = timeProvider.GetUtcNow();
        var fileMetadata = new FileMetadataEntity
        {
            Id = Guid.NewGuid(),
            UploadedById = userId,
            FolderId = folder.Id,
            FileName = fileName,
            ContentType = GetContentType(fileName),
            SizeBytes = fileInfo.Length,
            StoragePath = filePath,
            ContentHash = contentHash,
            UploadedAt = uploadedAt,
            IsDeleted = false
        };

        dbContext.FileMetadata.Add(fileMetadata);
        result.IncrementFilesAdded();

        FileSystemScannerLoggers.LogFileAdded(logger, fileMetadata.Id, fileName, fileInfo.Length, null);
    }

    private async Task UpdateFileMetadataAsync(
        FileMetadataEntity file,
        string filePath,
        FileInfo fileInfo,
        ScanResultBuilder result,
        CancellationToken cancellationToken)
    {
        file.SizeBytes = fileInfo.Length;
        file.ContentHash = await ComputeFileHashAsync(filePath, cancellationToken);
        file.UpdatedAt = timeProvider.GetUtcNow();

        result.IncrementFilesUpdated();

        FileSystemScannerLoggers.LogFileUpdated(logger, file.Id, file.FileName, fileInfo.Length, null);
    }

    private async Task ScanSubfoldersAsync(
        FolderEntity folder,
        Guid userId,
        ScanResultBuilder result,
        CancellationToken cancellationToken)
    {
        var subfoldersOnDisk = Directory.GetDirectories(folder.PhysicalPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet();

        var subfoldersInDb = folder.ChildFolders
            .Where(f => !f.IsDeleted)
            .ToDictionary(f => f.Name, f => f);

        // Find new subfolders
        foreach (var subfolderName in subfoldersOnDisk.Except(subfoldersInDb.Keys))
        {
            try
            {
                var subfolderPath = Path.Combine(folder.PhysicalPath, subfolderName!);
                var newFolder = await AddNewFolderAsync(folder, userId, subfolderPath, subfolderName!, cancellationToken);
                result.IncrementFoldersAdded();

                // Recursively scan new folder
                await ScanFolderRecursiveAsync(newFolder, userId, result, cancellationToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                result.AddError($"Access denied adding folder {subfolderName}: {ex.Message}");
                FileSystemScannerLoggers.LogFolderAddError(logger, subfolderName!, ex);
            }
            catch (IOException ex)
            {
                result.AddError($"I/O error adding folder {subfolderName}: {ex.Message}");
                FileSystemScannerLoggers.LogFolderAddError(logger, subfolderName!, ex);
            }
        }

        // Find deleted subfolders
        foreach (var subfolderName in subfoldersInDb.Keys.Except(subfoldersOnDisk))
        {
            var subfolder = subfoldersInDb[subfolderName!];
            subfolder.IsDeleted = true;
            subfolder.UpdatedAt = timeProvider.GetUtcNow();
            result.IncrementFoldersDeleted();
            FileSystemScannerLoggers.LogFolderMarkedDeleted(logger, subfolder.Id, subfolder.Name, null);

            // Stop watching the deleted folder
            watcherManager.StopWatchingFolder(subfolder.Id);
        }

        // Recursively scan existing subfolders
        foreach (var subfolderName in subfoldersOnDisk.Intersect(subfoldersInDb.Keys))
        {
            var subfolder = subfoldersInDb[subfolderName!];
            await ScanFolderRecursiveAsync(subfolder, userId, result, cancellationToken);
        }
    }

    private async Task<FolderEntity> AddNewFolderAsync(
        FolderEntity parentFolder,
        Guid userId,
        string folderPath,
        string folderName,
        CancellationToken cancellationToken)
    {
        var newFolder = new FolderEntity
        {
            Id = Guid.NewGuid(),
            OwnerId = parentFolder.OwnerId ?? userId,
            ParentFolderId = parentFolder.Id,
            Name = folderName,
            PhysicalPath = folderPath,
            IsDeleted = false,
            CreatedAt = timeProvider.GetUtcNow()
        };

        dbContext.Folders.Add(newFolder);

        // Load navigation properties for recursive scanning
        await dbContext.Entry(newFolder)
            .Collection(f => f.Files)
            .LoadAsync(cancellationToken);
        await dbContext.Entry(newFolder)
            .Collection(f => f.ChildFolders)
            .LoadAsync(cancellationToken);

        FileSystemScannerLoggers.LogFolderAdded(logger, newFolder.Id, folderName, null);

        // Start watching the newly created folder
        watcherManager.StartWatchingFolder(
            newFolder.Id,
            newFolder.PhysicalPath,
            newFolder.OwnerId,
            isRootFolder: false);

        return newFolder;
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var hashAlgorithm = SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        var hash = await hashAlgorithm.ComputeHashAsync(fileStream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToUpperInvariant();
        return extension switch
        {
            ".TXT" => "text/plain",
            ".PDF" => "application/pdf",
            ".JPG" or ".JPEG" => "image/jpeg",
            ".PNG" => "image/png",
            ".GIF" => "image/gif",
            ".ZIP" => "application/zip",
            ".JSON" => "application/json",
            ".XML" => "application/xml",
            _ => "application/octet-stream"
        };
    }

    private sealed class ScanResultBuilder
    {
        private int _foldersScanned;
        private int _filesAdded;
        private int _filesUpdated;
        private int _filesDeleted;
        private int _foldersAdded;
        private int _foldersDeleted;
        private readonly List<string> _errors = [];

        public void IncrementFoldersScanned() => _foldersScanned++;
        public void IncrementFilesAdded() => _filesAdded++;
        public void IncrementFilesUpdated() => _filesUpdated++;
        public void IncrementFilesDeleted() => _filesDeleted++;
        public void IncrementFoldersAdded() => _foldersAdded++;
        public void IncrementFoldersDeleted() => _foldersDeleted++;
        public void AddError(string error) => _errors.Add(error);

        public FileSystemScanResult Build() => new(
            _foldersScanned,
            _filesAdded,
            _filesUpdated,
            _filesDeleted,
            _foldersAdded,
            _foldersDeleted,
            _errors);
    }
}
