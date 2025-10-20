namespace Synka.Server.Data.Entities;

/// <summary>
/// Represents file metadata within a folder.
/// </summary>
public class FileMetadataEntity : IHasUpdatedAt
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who uploaded the file.
    /// </summary>
    public Guid UploadedById { get; set; }

    /// <summary>
    /// Navigation property to the uploading user.
    /// </summary>
    public ApplicationUserEntity UploadedBy { get; set; } = null!;

    /// <summary>
    /// The folder containing this file.
    /// Null if file is in user's root/not in a specific folder.
    /// </summary>
    public Guid? FolderId { get; set; }

    /// <summary>
    /// Navigation property to the folder.
    /// </summary>
    public FolderEntity? Folder { get; set; }

    /// <summary>
    /// Original filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type (MIME type).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Storage path relative to folder's PhysicalPath.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of file content for deduplication.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// True if file was deleted from disk but kept in DB for potential restoration.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Last time metadata was updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
