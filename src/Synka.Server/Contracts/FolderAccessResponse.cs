namespace Synka.Server.Contracts;

public sealed record FolderAccessResponse(
    Guid Id,
    Guid FolderId,
    Guid UserId,
    string UserName,
    Guid GrantedById,
    string GrantedByName,
    FolderAccessPermissionLevel Permission,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiresAt);
