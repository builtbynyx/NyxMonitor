using System.Management;
using LibreHardwareMonitor.Hardware;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;
using NyxMonitor.Infrastructure.Hardware;

namespace NyxMonitor.Infrastructure.Providers;

/// <summary>
/// Reads the primary GPU through LibreHardwareMonitor. Discrete GPUs (NVIDIA/AMD) are preferred over
/// integrated Intel graphics; among several discrete GPUs the one with the most dedicated memory wins.
/// Sensor names differ per vendor, so each metric tries several names. Missing sensors become null.
/// </summary>
public sealed class GpuProvider : IMetricProvider<GpuSnapshot>
{
    private const double BytesPerMegabyte = 1024.0 * 1024.0;

    private readonly HardwareSensorHub _hub;
    private readonly IAppLogger _log;
    private string? _primaryId;
    private string? _wmiName;
    private bool _wmiQueried;

    public GpuProvider(HardwareSensorHub hub, IAppLogger log)
    {
        _hub = hub;
        _log = log;
    }

    public string Name => "GPU";
    public GpuSnapshot Unavailable => new();

    public GpuSnapshot Read()
    {
        IHardware? gpu = ResolvePrimary();
        if (gpu is null)
        {
            return new GpuSnapshot { IsAvailable = false, Name = GetWmiName() ?? "N/A" };
        }

        var sensors = HardwareSensorHub.SensorsOf(gpu).ToList();

        ISensor? load = SensorSelector.Find(sensors, SensorType.Load, "GPU Core", "D3D 3D");
        ISensor? used = SensorSelector.Find(sensors, SensorType.SmallData, "GPU Memory Used", "D3D Dedicated Memory Used");
        ISensor? total = SensorSelector.Find(sensors, SensorType.SmallData, "GPU Memory Total", "D3D Dedicated Memory Total");
        ISensor? shared = SensorSelector.Find(sensors, SensorType.SmallData, "D3D Shared Memory Used", "GPU Shared Memory Used");
        ISensor? temperature = SensorSelector.Find(sensors, SensorType.Temperature, "GPU Core", "GPU Hot Spot")
                               ?? SensorSelector.FirstWithValue(sensors, SensorType.Temperature);
        ISensor? coreClock = SensorSelector.Find(sensors, SensorType.Clock, "GPU Core");
        ISensor? memoryClock = SensorSelector.Find(sensors, SensorType.Clock, "GPU Memory");
        ISensor? fanRpm = SensorSelector.FirstWithValue(sensors, SensorType.Fan);
        ISensor? fanControl = sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Control &&
            s.Name.Contains("Fan", StringComparison.OrdinalIgnoreCase) &&
            SensorSelector.Value(s) is not null);

        return new GpuSnapshot
        {
            IsAvailable = true,
            Name = gpu.Name,
            UtilizationPercent = SensorSelector.Value(load),
            VramUsedBytes = ToBytes(SensorSelector.Value(used)),
            VramTotalBytes = ToBytes(SensorSelector.Value(total)),
            SharedMemoryUsedBytes = ToBytes(SensorSelector.Value(shared)),
            TemperatureCelsius = SensorSelector.Value(temperature),
            CoreClockMhz = SensorSelector.Value(coreClock),
            MemoryClockMhz = SensorSelector.Value(memoryClock),
            FanRpm = SensorSelector.Value(fanRpm),
            FanPercent = SensorSelector.Value(fanControl)
        };
    }

    public void Dispose()
    {
    }

    private IHardware? ResolvePrimary()
    {
        IReadOnlyList<IHardware> candidates =
            _hub.GetHardware(HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel);
        if (candidates.Count == 0)
        {
            return null;
        }

        if (_primaryId is not null)
        {
            IHardware? cached = candidates.FirstOrDefault(h => h.Identifier.ToString() == _primaryId);
            if (cached is not null)
            {
                return cached;
            }
        }

        IHardware best = candidates
            .OrderByDescending(h => h.HardwareType != HardwareType.GpuIntel)
            .ThenByDescending(h => SensorSelector.Value(
                SensorSelector.Find(HardwareSensorHub.SensorsOf(h), SensorType.SmallData, "GPU Memory Total", "D3D Dedicated Memory Total")) ?? 0)
            .First();

        _primaryId = best.Identifier.ToString();
        return best;
    }

    private string? GetWmiName()
    {
        if (_wmiQueried)
        {
            return _wmiName;
        }

        _wmiQueried = true;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (ManagementBaseObject controller in searcher.Get())
            {
                using (controller)
                {
                    _wmiName = Convert.ToString(controller["Name"])?.Trim();
                    if (!string.IsNullOrEmpty(_wmiName))
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warning("WMI video controller query failed.", ex);
        }

        return _wmiName;
    }

    private static long? ToBytes(double? megabytes) =>
        megabytes is { } mb ? (long)(mb * BytesPerMegabyte) : null;
}
