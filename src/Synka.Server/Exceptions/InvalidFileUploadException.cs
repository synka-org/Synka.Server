namespace Synka.Server.Exceptions;

/// <summary>
/// Raised when a file upload request fails validation.
/// </summary>
/// <param name="message">The error message describing the validation failure.</param>
#pragma warning disable CA1032, RCS1194 // Domain-specific constructor used instead of standard exception constructors
public sealed class InvalidFileUploadException(string message) : Exception(message);
#pragma warning restore CA1032, RCS1194
