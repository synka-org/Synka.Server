using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Synka.Server.Helpers;

public static class FileIdentifierHelper
{
    /// <summary>
    /// Gets a platform-appropriate file identifier.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>A unique file identifier.</returns>
    public static ulong GetFileId(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsFileId(path);
        }

        return GetUnixInode(path);
    }

    /// <summary>
    /// Gets a Windows file identifier (volume serial + file index) as a formatted string.
    /// Returns null if not running on Windows or if an error occurs.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>A formatted file identifier string, or null if unavailable.</returns>
    public static string? TryGetWindowsFileId(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var info = new BY_HANDLE_FILE_INFORMATION();

            if (!GetFileInformationByHandle(handle, out info))
            {
                return null;
            }

            // Format as "VolumeSerial:FileIndexHigh:FileIndexLow"
            return $"{info.dwVolumeSerialNumber:X8}:{info.nFileIndexHigh:X8}:{info.nFileIndexLow:X8}";
        }
#pragma warning disable CA1031 // Catch specific exceptions - intentionally catching all for Try pattern
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Gets a Unix file identifier (device + inode) as a formatted string.
    /// Returns null if not running on Unix or if an error occurs.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>A formatted file identifier string, or null if unavailable.</returns>
    public static string? TryGetUnixFileId(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            var stat = new Stat();
            if (stat64(path, out stat) != 0)
            {
                return null;
            }

            // Format as "Device:Inode"
            return $"{stat.st_dev:X}:{stat.st_ino:X}";
        }
#pragma warning disable CA1031 // Catch specific exceptions - intentionally catching all for Try pattern
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    private static ulong GetWindowsFileId(string path)
    {
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var info = new BY_HANDLE_FILE_INFORMATION();

        if (!GetFileInformationByHandle(handle, out info))
            throw new IOException("Failed to get file information for " + path);

        // Combine the 32-bit high/low values into a 64-bit ID (same as NTFS File ID)
        return ((ulong)info.nFileIndexHigh << 32) | info.nFileIndexLow;
    }

    private static ulong GetUnixInode(string path)
    {
        var stat = new Stat();
        if (stat64(path, out stat) != 0)
            throw new IOException("Failed to stat file " + path);

        return stat.st_ino;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct BY_HANDLE_FILE_INFORMATION
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint dwVolumeSerialNumber;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint nNumberOfLinks;
        public uint nFileIndexHigh;
        public uint nFileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Stat
    {
        public ulong st_dev;
        public ulong st_ino;
        public ulong st_nlink;
        public uint st_mode;
        public uint st_uid;
        public uint st_gid;
        public ulong __pad0;
        public ulong st_rdev;
        public long st_size;
        public long st_blksize;
        public long st_blocks;
        public Timespec st_atime;
        public Timespec st_mtime;
        public Timespec st_ctime;
        private readonly long __glibc_reserved0;
        private readonly long __glibc_reserved1;
        private readonly long __glibc_reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Timespec
    {
        public long tv_sec;
        public long tv_nsec;
    }

    [DllImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - marshaling is explicitly set via MarshalAs
    private static extern int stat64([MarshalAs(UnmanagedType.LPStr)] string path, out Stat buf);
#pragma warning restore CA2101
}
