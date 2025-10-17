using Synka.Server.Data.Entities;

namespace Synka.Server.Contracts;

public sealed record UpdateFolderPermissionRequest(
    FolderAccessLevel Permission,
    DateTimeOffset? ExpiresAt);
