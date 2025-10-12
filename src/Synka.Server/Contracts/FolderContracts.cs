using Synka.Server.Data.Entities;

namespace Synka.Server.Contracts;

public sealed record CreateFolderRequest(
    string Name,
    Guid? ParentFolderId,
    string? PhysicalPath);

public sealed record FolderResponse(
    Guid Id,
    Guid? OwnerId,
    Guid? ParentFolderId,
    string Name,
    string PhysicalPath,
    bool IsSharedRoot,
    bool IsUserRoot,
    bool IsDeleted,
    int FileCount,
    int SubfolderCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ShareFolderRequest(
    Guid UserId,
    FolderAccessLevel Permission,
    DateTimeOffset? ExpiresAt);

public sealed record FolderAccessResponse(
    Guid Id,
    Guid FolderId,
    Guid UserId,
    string UserName,
    Guid GrantedById,
    string GrantedByName,
    FolderAccessLevel Permission,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiresAt);

public sealed record UpdateFolderPermissionRequest(
    FolderAccessLevel Permission,
    DateTimeOffset? ExpiresAt);
