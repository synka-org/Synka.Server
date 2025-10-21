using Synka.Server.Services;

namespace Synka.Server.Tests.Infrastructure;

/// <summary>
/// Mock filesystem service for testing that doesn't perform actual filesystem operations.
/// </summary>
public sealed class MockFileSystemService : IFileSystemService
{
    private readonly HashSet<string> _createdDirectories = [];
    private readonly Dictionary<string, byte[]> _files = [];

    /// <inheritdoc />
    public bool DirectoryExists(string path) => _createdDirectories.Contains(path);

    /// <inheritdoc />
    public void CreateDirectory(string path) => _createdDirectories.Add(path);

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive)
    {
        if (recursive)
        {
            // Remove all directories that start with this path
            _createdDirectories.RemoveWhere(d => d.StartsWith(path, StringComparison.Ordinal));
            // Remove all files in this directory
            foreach (var key in _files.Keys.Where(f => f.StartsWith(path, StringComparison.Ordinal)).ToList())
            {
                _files.Remove(key);
            }
        }
        else
        {
            _createdDirectories.Remove(path);
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path) => _files.ContainsKey(path);

    /// <inheritdoc />
    public Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
    {
        if (mode == FileMode.Create || mode == FileMode.CreateNew)
        {
            var stream = new MemoryStream();
            // When the stream is disposed, save its content
            return new MockFileStream(stream, path, this);
        }

        if (_files.TryGetValue(path, out var data))
        {
            return new MemoryStream(data);
        }

        throw new FileNotFoundException("File not found in mock filesystem", path);
    }

    /// <inheritdoc />
    public void DeleteFile(string path) => _files.Remove(path);

    /// <summary>
    /// Gets all directories that have been created.
    /// </summary>
    public IReadOnlySet<string> CreatedDirectories => _createdDirectories;

    /// <summary>
    /// Gets all files that have been created.
    /// </summary>
    public IReadOnlyDictionary<string, byte[]> Files => _files;

    internal void SaveFile(string path, byte[] data)
    {
        _files[path] = data;
    }

    private sealed class MockFileStream(MemoryStream innerStream, string path, MockFileSystemService parent) : Stream
    {
        private bool _disposed;

        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => innerStream.Length;
        public override long Position
        {
            get => innerStream.Position;
            set => innerStream.Position = value;
        }

        public override void Flush() => innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);
        public override void SetLength(long value) => innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await innerStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => await innerStream.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Save the content before disposing
                parent.SaveFile(path, innerStream.ToArray());
                innerStream.Dispose();
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                // Save the content before disposing
                parent.SaveFile(path, innerStream.ToArray());
                await innerStream.DisposeAsync();
                _disposed = true;
            }
            await base.DisposeAsync();
        }
    }
}
