namespace NyxMonitor.Core.Models;

public sealed record GpuSnapshot
{
    /// <summary>True when a GPU was found by the hardware sensor library. When false only <see cref="Name"/> may be set.</summary>
    public bool IsAvailable { get; init; }
    public string Name { get; init; } = "N/A";
    public double? UtilizationPercent { get; init; }
    public long? VramUsedBytes { get; init; }
    public long? VramTotalBytes { get; init; }
    public long? SharedMemoryUsedBytes { get; init; }
    public double? TemperatureCelsius { get; init; }
    public double? CoreClockMhz { get; init; }
    public double? MemoryClockMhz { get; init; }
    public double? FanRpm { get; init; }
    public double? FanPercent { get; init; }

    public double? VramUtilizationPercent =>
        VramUsedBytes is { } used && VramTotalBytes is > 0 and var total ? used * 100.0 / total : null;
}
