namespace Synka.Server.Services;

/// <summary>
/// Abstraction for filesystem operations to enable testing without actual filesystem access.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Determines whether the given path refers to an existing directory on disk.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>true if path refers to an existing directory; false otherwise.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Creates all directories and subdirectories in the specified path.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Deletes the specified directory and all its contents.
    /// </summary>
    /// <param name="path">The directory to delete.</param>
    /// <param name="recursive">true to remove directories, subdirectories, and files in path; otherwise, false.</param>
    void DeleteDirectory(string path, bool recursive);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The file to check.</param>
    /// <returns>true if the file exists; false otherwise.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Opens a FileStream on the specified path with the specified mode and access.
    /// </summary>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">The file mode.</param>
    /// <param name="access">The file access.</param>
    /// <param name="share">The file share.</param>
    /// <returns>A FileStream opened in the specified mode and access, with no sharing.</returns>
    Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share);

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="path">The file to delete.</param>
    void DeleteFile(string path);
}
