using System.Text.Json.Serialization;

namespace NyxMonitor.Core.Configuration;

public sealed class AppSettings
{
    public const int MinPollingIntervalMs = 500;
    public const int MaxPollingIntervalMs = 10_000;
    public const int MinHistorySeconds = 30;
    public const int MaxHistorySeconds = 3_600;

    public int PollingIntervalMs { get; set; } = 1_000;
    public int HistorySeconds { get; set; } = 120;
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

    public bool EnableCpuMonitoring { get; set; } = true;
    public bool EnableGpuMonitoring { get; set; } = true;
    public bool EnableMemoryMonitoring { get; set; } = true;
    public bool EnableStorageMonitoring { get; set; } = true;

    /// <summary>Number of samples a history graph keeps for the configured duration and interval.</summary>
    [JsonIgnore]
    public int HistoryCapacity => Math.Max(2, HistorySeconds * 1000 / PollingIntervalMs);

    /// <summary>Clamps hand-edited values into supported ranges.</summary>
    public void Normalize()
    {
        PollingIntervalMs = Math.Clamp(PollingIntervalMs, MinPollingIntervalMs, MaxPollingIntervalMs);
        HistorySeconds = Math.Clamp(HistorySeconds, MinHistorySeconds, MaxHistorySeconds);
        if (!Enum.IsDefined(TemperatureUnit))
        {
            TemperatureUnit = TemperatureUnit.Celsius;
        }
    }
}
