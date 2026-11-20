using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;

namespace NyxMonitor.Infrastructure.Providers;

/// <summary>
/// Capacity comes from <see cref="DriveInfo"/> and is refreshed every few cycles (it can be slow on removable media).
/// Read/write throughput comes from LogicalDisk / PhysicalDisk performance counters every cycle.
/// Drive health (SMART) is not part of v0.1.0.
/// </summary>
public sealed class StorageProvider : IMetricProvider<StorageSnapshot>
{
    private const int CapacityRefreshEveryNReads = 5;

    private sealed class DriveEntry : IDisposable
    {
        public DriveEntry(string name)
        {
            Name = name;
            string instance = name.TrimEnd('\\'); // "C:\" -> "C:"
            Read = new SafeCounter("LogicalDisk", "Disk Read Bytes/sec", instance);
            Write = new SafeCounter("LogicalDisk", "Disk Write Bytes/sec", instance);
        }

        public string Name { get; }
        public SafeCounter Read { get; }
        public SafeCounter Write { get; }
        public string Label { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long Total { get; set; }
        public long Free { get; set; }

        public void Dispose()
        {
            Read.Dispose();
            Write.Dispose();
        }
    }

    private readonly Dictionary<string, DriveEntry> _drives = new(StringComparer.OrdinalIgnoreCase);
    private readonly IAppLogger _log;
    private SafeCounter? _totalRead;
    private SafeCounter? _totalWrite;
    private int _reads;

    public StorageProvider(IAppLogger log)
    {
        _log = log;
    }

    public string Name => "Storage";
    public StorageSnapshot Unavailable => new();

    public StorageSnapshot Read()
    {
        _totalRead ??= new SafeCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        _totalWrite ??= new SafeCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

        if (_reads++ % CapacityRefreshEveryNReads == 0)
        {
            RefreshDrives();
        }

        var drives = _drives.Values
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Select(d => new DriveSnapshot
            {
                Name = d.Name,
                Label = d.Label,
                Format = d.Format,
                DriveType = d.Type,
                TotalBytes = d.Total,
                FreeBytes = d.Free,
                ReadBytesPerSecond = d.Read.Next(),
                WriteBytesPerSecond = d.Write.Next()
            })
            .ToList();

        return new StorageSnapshot
        {
            Drives = drives,
            TotalReadBytesPerSecond = _totalRead.Next(),
            TotalWriteBytesPerSecond = _totalWrite.Next()
        };
    }

    public void Dispose()
    {
        _totalRead?.Dispose();
        _totalWrite?.Dispose();
        foreach (DriveEntry entry in _drives.Values)
        {
            entry.Dispose();
        }

        _drives.Clear();
    }

    private void RefreshDrives()
    {
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        DriveInfo[] all;
        try
        {
            all = DriveInfo.GetDrives();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Warning("Could not enumerate drives.", ex);
            return;
        }

        foreach (DriveInfo drive in all)
        {
            try
            {
                if (drive.DriveType is not (DriveType.Fixed or DriveType.Removable) || !drive.IsReady)
                {
                    continue;
                }

                if (!_drives.TryGetValue(drive.Name, out DriveEntry? entry))
                {
                    entry = new DriveEntry(drive.Name);
                    _drives[drive.Name] = entry;
                }

                entry.Label = drive.VolumeLabel;
                entry.Format = drive.DriveFormat;
                entry.Type = drive.DriveType.ToString();
                entry.Total = drive.TotalSize;
                entry.Free = drive.AvailableFreeSpace;
                present.Add(drive.Name);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Drive disappeared or is not accessible right now; it simply drops out of the list.
            }
        }

        foreach (string gone in _drives.Keys.Where(k => !present.Contains(k)).ToList())
        {
            _drives[gone].Dispose();
            _drives.Remove(gone);
        }
    }
}
