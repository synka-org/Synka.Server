namespace Synka.Server.Exceptions;

/// <summary>
/// Raised when a requested folder cannot be found.
/// </summary>
/// <param name="folderId">The ID of the folder that was not found.</param>
/// <param name="context">Optional context describing where the folder was expected.</param>
#pragma warning disable CA1032, RCS1194 // Domain-specific constructor used instead of standard exception constructors
public sealed class FolderNotFoundException(Guid folderId, string? context = null)
    : Exception(CreateMessage(folderId, context))
{
    public Guid FolderId { get; } = folderId;

    public string? Context { get; } = context;

    private static string CreateMessage(Guid folderId, string? context)
    {
        if (!string.IsNullOrWhiteSpace(context))
        {
            return $"{context} folder '{folderId}' was not found.";
        }

        return $"Folder '{folderId}' was not found.";
    }
}
#pragma warning restore CA1032, RCS1194
