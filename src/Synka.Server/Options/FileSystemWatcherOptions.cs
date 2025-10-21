using System.Collections.ObjectModel;

namespace Synka.Server.Options;

public sealed class FileSystemWatcherOptions
{
    private TimeSpan _scanDebounceDelay = TimeSpan.FromSeconds(2);
    private string? _userRootBasePath;

    public TimeSpan ScanDebounceDelay
    {
        get => _scanDebounceDelay;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(ScanDebounceDelay), value, "Scan debounce delay must be positive.");
            }

            _scanDebounceDelay = value;
        }
    }

    public string? UserRootBasePath
    {
        get => _userRootBasePath;
        set => _userRootBasePath = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public Collection<string> SharedRootPaths { get; } = new();
}
