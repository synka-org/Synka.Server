using Synka.Server.Data.Entities;

namespace Synka.Server.Contracts;

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
