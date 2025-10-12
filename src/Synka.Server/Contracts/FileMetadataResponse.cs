namespace Synka.Server.Contracts;

public sealed record FileMetadataResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StoragePath,
    string? WindowsFileId,
    string? UnixFileId,
    string? ContentHash,
    DateTimeOffset UploadedAt,
    DateTimeOffset? UpdatedAt);
