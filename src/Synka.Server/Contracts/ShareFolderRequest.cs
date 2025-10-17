using Synka.Server.Data.Entities;

namespace Synka.Server.Contracts;

public sealed record ShareFolderRequest(
    Guid UserId,
    FolderAccessLevel Permission,
    DateTimeOffset? ExpiresAt);
