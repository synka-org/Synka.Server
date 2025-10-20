namespace Synka.Server.Options;

public sealed class FileSystemWatcherOptions
{
    private TimeSpan _scanDebounceDelay = TimeSpan.FromSeconds(2);

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
}
