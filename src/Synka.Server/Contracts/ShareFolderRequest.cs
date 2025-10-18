namespace Synka.Server.Contracts;

public sealed record ShareFolderRequest(
    Guid UserId,
    FolderAccessPermissionLevel Permission,
    DateTimeOffset? ExpiresAt);
