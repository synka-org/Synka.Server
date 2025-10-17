namespace Synka.Server.Contracts;

public sealed record CreateFolderRequest(
    string Name,
    Guid? ParentFolderId,
    string? PhysicalPath);
