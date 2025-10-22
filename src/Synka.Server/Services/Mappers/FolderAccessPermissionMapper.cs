using System;
using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Services.Mappers;

internal static class FolderAccessPermissionMapper
{
    public static FolderAccessPermissionLevel ToContractPermission(this FolderAccessLevel permission) => permission switch
    {
        FolderAccessLevel.Read => FolderAccessPermissionLevel.Read,
        FolderAccessLevel.Write => FolderAccessPermissionLevel.Write,
        FolderAccessLevel.Admin => FolderAccessPermissionLevel.Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unsupported folder access level")
    };
}
