namespace Synka.Server.Data.Entities;

/// <summary>
/// Represents file metadata with platform-specific identifiers for tracking files even when moved.
/// </summary>
public class FileMetadataEntity
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who uploaded the file.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public ApplicationUserEntity User { get; set; } = null!;

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
    /// Current storage path on disk.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of file content for deduplication.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last time metadata was updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
