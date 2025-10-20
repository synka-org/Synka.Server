namespace Synka.Server.Data.Entities;

/// <summary>
/// Represents access granted to a user for a specific folder.
/// User gets access to the folder and all its descendants.
/// </summary>
public class FolderAccessEntity
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The folder being shared.
    /// </summary>
    public Guid FolderId { get; set; }

    /// <summary>
    /// Navigation property to the folder.
    /// </summary>
    public FolderEntity Folder { get; set; } = null!;

    /// <summary>
    /// User being granted access.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public ApplicationUserEntity User { get; set; } = null!;

    /// <summary>
    /// User who granted the access (folder owner or someone with sharing rights).
    /// </summary>
    public Guid GrantedById { get; set; }

    /// <summary>
    /// Navigation property to the granting user.
    /// </summary>
    public ApplicationUserEntity GrantedBy { get; set; } = null!;

    /// <summary>
    /// Permission level granted to the user.
    /// </summary>
    public FolderAccessLevel Permission { get; set; }

    /// <summary>
    /// When access was granted.
    /// </summary>
    public DateTimeOffset GrantedAt { get; set; }

    /// <summary>
    /// Optional expiration date for temporary access.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>
/// Folder access permission levels.
/// </summary>
public enum FolderAccessLevel
{
    /// <summary>
    /// Can view files and folders.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Can upload, modify, and delete files.
    /// </summary>
    Write = 1,

    /// <summary>
    /// Can manage folder and share with others.
    /// </summary>
    Admin = 2
}
