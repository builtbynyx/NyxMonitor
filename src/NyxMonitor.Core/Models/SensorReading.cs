namespace NyxMonitor.Core.Models;

/// <summary>A single named sensor value. <see cref="Value"/> is null when the sensor has no reading.</summary>
public sealed record SensorReading(string Name, double? Value);
