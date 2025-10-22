namespace Synka.Server.Exceptions;

/// <summary>
/// Raised when a root folder is created without a corresponding physical path.
/// </summary>
public sealed class RootFolderPhysicalPathRequiredException()
    : Exception("Physical path is required for root folders.");
