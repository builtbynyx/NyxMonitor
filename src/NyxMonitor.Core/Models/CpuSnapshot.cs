namespace NyxMonitor.Core.Models;

public sealed record CpuSnapshot
{
    public string Name { get; init; } = "N/A";
    public string Manufacturer { get; init; } = "N/A";
    public int? PhysicalCores { get; init; }
    public int? LogicalProcessors { get; init; }
    public int? L3CacheKilobytes { get; init; }
    public double? BaseClockMhz { get; init; }
    public double? CurrentClockMhz { get; init; }
    public double? UtilizationPercent { get; init; }
    public double? TemperatureCelsius { get; init; }

    /// <summary>Utilization per logical processor, in OS order. Entries are null when unreadable.</summary>
    public IReadOnlyList<double?> CoreUtilization { get; init; } = Array.Empty<double?>();

    public IReadOnlyList<SensorReading> TemperatureSensors { get; init; } = Array.Empty<SensorReading>();
    public IReadOnlyList<SensorReading> ClockSensors { get; init; } = Array.Empty<SensorReading>();
}
