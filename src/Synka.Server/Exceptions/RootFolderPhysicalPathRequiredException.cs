namespace Synka.Server.Exceptions;

/// <summary>
/// Raised when a root folder is created without a corresponding physical path.
/// </summary>
public sealed class RootFolderPhysicalPathRequiredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootFolderPhysicalPathRequiredException"/> class
    /// with a default error message.
    /// </summary>
    public RootFolderPhysicalPathRequiredException()
        : base("Physical path is required for root folders.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RootFolderPhysicalPathRequiredException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RootFolderPhysicalPathRequiredException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RootFolderPhysicalPathRequiredException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RootFolderPhysicalPathRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
