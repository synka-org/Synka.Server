namespace Synka.Server.Contracts;

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
