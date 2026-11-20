using System.ComponentModel;
using NyxMonitor.Core.Models;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Infrastructure.Native;

namespace NyxMonitor.Infrastructure.Providers;

/// <summary>Reads physical memory and commit charge through GlobalMemoryStatusEx (no WMI, very cheap).</summary>
public sealed class MemoryProvider : IMetricProvider<MemorySnapshot>
{
    private long? _installedBytes;
    private bool _installedQueried;

    public string Name => "Memory";
    public MemorySnapshot Unavailable => new();

    public MemorySnapshot Read()
    {
        var status = new NativeMethods.MemoryStatusEx { Length = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MemoryStatusEx>() };
        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
        {
            throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        }

        long total = (long)status.TotalPhys;
        long available = (long)status.AvailPhys;
        long commitLimit = (long)status.TotalPageFile;
        long commitAvailable = (long)status.AvailPageFile;

        if (!_installedQueried)
        {
            _installedQueried = true;
            if (NativeMethods.GetPhysicallyInstalledSystemMemory(out ulong kilobytes) && kilobytes > 0)
            {
                _installedBytes = (long)kilobytes * 1024;
            }
        }

        return new MemorySnapshot
        {
            InstalledBytes = _installedBytes ?? total,
            TotalBytes = total,
            AvailableBytes = available,
            UsedBytes = total - available,
            CommitLimitBytes = commitLimit,
            CommitUsedBytes = Math.Max(0, commitLimit - commitAvailable)
        };
    }

    public void Dispose()
    {
    }
}
