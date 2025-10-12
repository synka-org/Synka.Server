namespace Synka.Server.Data.Entities;

/// <summary>
/// Represents a folder in the file tree.
/// Can be shared (no owner) or user-specific (owned by a user).
/// </summary>
public class FolderEntity
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of this folder (null for shared root folders).
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Navigation property to the owner.
    /// </summary>
    public ApplicationUserEntity? Owner { get; set; }

    /// <summary>
    /// Parent folder ID (null for root folders - both shared and user-specific).
    /// </summary>
    public Guid? ParentFolderId { get; set; }

    /// <summary>
    /// Navigation property to parent folder.
    /// </summary>
    public FolderEntity? ParentFolder { get; set; }

    /// <summary>
    /// Child folders.
    /// </summary>
    public ICollection<FolderEntity> ChildFolders { get; } = [];

    /// <summary>
    /// Files in this folder.
    /// </summary>
    public ICollection<FileMetadataEntity> Files { get; } = [];

    /// <summary>
    /// Users who have been granted access to this folder.
    /// </summary>
    public ICollection<FolderAccessEntity> SharedWith { get; } = [];

    /// <summary>
    /// Folder name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Physical path on disk.
    /// </summary>
    public required string PhysicalPath { get; set; }

    /// <summary>
    /// Soft-delete flag. If true, folder is marked as deleted but can be restored.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// True if this is a shared root folder (accessible to all users).
    /// </summary>
    public bool IsSharedRoot => OwnerId is null && ParentFolderId is null;

    /// <summary>
    /// True if this is a user-specific root folder.
    /// </summary>
    public bool IsUserRoot => OwnerId is not null && ParentFolderId is null;
}
