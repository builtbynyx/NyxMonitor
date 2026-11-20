using NyxMonitor.Core.Models;

namespace NyxMonitor.Core.Utilities;

/// <summary>Owns every rolling history shown in the UI so pages share one copy of the data.</summary>
public sealed class HistoryStore
{
    public HistoryStore(int capacity)
    {
        Capacity = capacity;
        CpuUsage = new HistorySeries(capacity);
        CpuTemperature = new HistorySeries(capacity);
        GpuUsage = new HistorySeries(capacity);
        GpuTemperature = new HistorySeries(capacity);
        GpuVramUsage = new HistorySeries(capacity);
        MemoryUsage = new HistorySeries(capacity);
        CommitUsage = new HistorySeries(capacity);
        DiskRead = new HistorySeries(capacity);
        DiskWrite = new HistorySeries(capacity);
    }

    public int Capacity { get; }

    public HistorySeries CpuUsage { get; }
    /// <summary>Always stored in °C; convert only for display.</summary>
    public HistorySeries CpuTemperature { get; }
    public HistorySeries GpuUsage { get; }
    /// <summary>Always stored in °C; convert only for display.</summary>
    public HistorySeries GpuTemperature { get; }
    public HistorySeries GpuVramUsage { get; }
    public HistorySeries MemoryUsage { get; }
    public HistorySeries CommitUsage { get; }
    /// <summary>Bytes per second.</summary>
    public HistorySeries DiskRead { get; }
    /// <summary>Bytes per second.</summary>
    public HistorySeries DiskWrite { get; }

    public void Record(SystemSnapshot snapshot)
    {
        CpuUsage.Add(snapshot.Cpu.UtilizationPercent);
        CpuTemperature.Add(snapshot.Cpu.TemperatureCelsius);
        GpuUsage.Add(snapshot.Gpu.UtilizationPercent);
        GpuTemperature.Add(snapshot.Gpu.TemperatureCelsius);
        GpuVramUsage.Add(snapshot.Gpu.VramUtilizationPercent);
        MemoryUsage.Add(snapshot.Memory.UtilizationPercent);
        CommitUsage.Add(snapshot.Memory.CommitPercent);
        DiskRead.Add(snapshot.Storage.TotalReadBytesPerSecond);
        DiskWrite.Add(snapshot.Storage.TotalWriteBytesPerSecond);
    }
}
