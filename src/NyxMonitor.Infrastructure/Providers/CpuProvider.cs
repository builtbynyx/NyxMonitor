using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;
using NyxMonitor.Infrastructure.Hardware;

namespace NyxMonitor.Infrastructure.Providers;

/// <summary>
/// Utilization comes from Windows performance counters (works without admin rights).
/// Temperatures and per-core clocks come from LibreHardwareMonitor and may be unavailable.
/// Current clock falls back to "% Processor Performance" x base clock when no clock sensors exist.
/// </summary>
public sealed class CpuProvider : IMetricProvider<CpuSnapshot>
{
    private sealed record StaticInfo(
        string Name, string Manufacturer, int? Physical, int? Logical, double? BaseMhz, int? L3Kb);

    private readonly HardwareSensorHub _hub;
    private readonly IAppLogger _log;
    private readonly List<SafeCounter> _cores = new();
    private StaticInfo _info = new("N/A", "N/A", null, null, null, null);
    private SafeCounter? _total;
    private SafeCounter? _performance;
    private bool _initialized;

    public CpuProvider(HardwareSensorHub hub, IAppLogger log)
    {
        _hub = hub;
        _log = log;
    }

    public string Name => "CPU";
    public CpuSnapshot Unavailable => new();

    public CpuSnapshot Read()
    {
        EnsureInitialized();

        IReadOnlyList<ISensor> sensors = _hub.GetSensors(HardwareType.Cpu);

        double? usage = Clamp(_total?.Next());
        double? performance = _performance?.Next();
        double?[] perCore = _cores.Select(c => Clamp(c.Next())).ToArray();

        var coreClocks = sensors
            .Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
            .Select(SensorSelector.Value)
            .Where(v => v is > 0)
            .Select(v => v!.Value)
            .ToList();

        double? currentClock = coreClocks.Count > 0
            ? coreClocks.Average()
            : performance is { } p && _info.BaseMhz is { } baseMhz ? p / 100.0 * baseMhz : null;

        return new CpuSnapshot
        {
            Name = _info.Name,
            Manufacturer = _info.Manufacturer,
            PhysicalCores = _info.Physical,
            LogicalProcessors = _info.Logical,
            L3CacheKilobytes = _info.L3Kb,
            BaseClockMhz = _info.BaseMhz,
            CurrentClockMhz = currentClock,
            UtilizationPercent = usage,
            TemperatureCelsius = SensorSelector.PickCpuTemperature(sensors),
            CoreUtilization = perCore,
            TemperatureSensors = sensors
                .Where(s => s.SensorType == SensorType.Temperature)
                .Select(s => new SensorReading(s.Name, SensorSelector.Value(s)))
                .ToList(),
            ClockSensors = sensors
                .Where(s => s.SensorType == SensorType.Clock)
                .Select(s => new SensorReading(s.Name, SensorSelector.Value(s)))
                .ToList()
        };
    }

    public void Dispose()
    {
        _total?.Dispose();
        _performance?.Dispose();
        foreach (SafeCounter counter in _cores)
        {
            counter.Dispose();
        }

        _cores.Clear();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _info = QueryStaticInfo();

        _total = new SafeCounter("Processor Information", "% Processor Utility", "_Total");
        if (!_total.IsAvailable)
        {
            _total.Dispose();
            _total = new SafeCounter("Processor", "% Processor Time", "_Total");
        }

        _performance = new SafeCounter("Processor Information", "% Processor Performance", "_Total");

        try
        {
            var instances = new PerformanceCounterCategory("Processor").GetInstanceNames()
                .Where(n => int.TryParse(n, out _))
                .OrderBy(n => int.Parse(n));
            foreach (string instance in instances)
            {
                _cores.Add(new SafeCounter("Processor", "% Processor Time", instance));
            }
        }
        catch (Exception ex)
        {
            _log.Warning("Per-core CPU counters unavailable.", ex);
        }
    }

    private StaticInfo QueryStaticInfo()
    {
        string name = "N/A";
        string manufacturer = "N/A";
        int physical = 0, logical = 0, l3 = 0;
        double baseMhz = 0;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L3CacheSize FROM Win32_Processor");
            bool first = true;
            foreach (ManagementBaseObject processor in searcher.Get())
            {
                using (processor)
                {
                    if (first)
                    {
                        name = Convert.ToString(processor["Name"])?.Trim() ?? name;
                        manufacturer = Convert.ToString(processor["Manufacturer"])?.Trim() ?? manufacturer;
                        baseMhz = Convert.ToDouble(processor["MaxClockSpeed"] ?? 0);
                        first = false;
                    }

                    physical += Convert.ToInt32(processor["NumberOfCores"] ?? 0);
                    logical += Convert.ToInt32(processor["NumberOfLogicalProcessors"] ?? 0);
                    l3 = Math.Max(l3, Convert.ToInt32(processor["L3CacheSize"] ?? 0));
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warning("WMI processor query failed.", ex);
        }

        if (logical == 0)
        {
            logical = Environment.ProcessorCount;
        }

        if (name == "N/A")
        {
            name = _hub.GetHardware(HardwareType.Cpu).FirstOrDefault()?.Name ?? name;
        }

        return new StaticInfo(
            name,
            manufacturer,
            physical > 0 ? physical : null,
            logical > 0 ? logical : null,
            baseMhz > 0 ? baseMhz : null,
            l3 > 0 ? l3 : null);
    }

    private static double? Clamp(double? value) =>
        value is { } v && double.IsFinite(v) ? Math.Clamp(v, 0, 100) : null;
}
