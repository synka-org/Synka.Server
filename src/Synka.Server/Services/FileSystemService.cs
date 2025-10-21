namespace Synka.Server.Services;

/// <summary>
/// Production implementation of filesystem operations.
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
    public Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share) =>
        new FileStream(path, mode, access, share);

    /// <inheritdoc />
    public void DeleteFile(string path) => File.Delete(path);
}
