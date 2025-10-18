namespace Synka.Server.Contracts;

public sealed record UpdateFolderPermissionRequest(
    FolderAccessPermissionLevel Permission,
    DateTimeOffset? ExpiresAt);
