namespace NyxMonitor.Core.Models;

/// <summary>One immutable reading of every enabled monitoring module.</summary>
public sealed record SystemSnapshot(
    DateTimeOffset Timestamp,
    CpuSnapshot Cpu,
    GpuSnapshot Gpu,
    MemorySnapshot Memory,
    StorageSnapshot Storage,
    bool HardwareSensorsOnline,
    bool IsElevated);
