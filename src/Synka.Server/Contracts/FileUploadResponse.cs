namespace Synka.Server.Contracts;

public sealed record FileUploadResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? ContentHash,
    DateTimeOffset UploadedAt);
