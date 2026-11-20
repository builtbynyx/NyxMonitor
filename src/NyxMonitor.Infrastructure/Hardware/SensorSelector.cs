using LibreHardwareMonitor.Hardware;

namespace NyxMonitor.Infrastructure.Hardware;

/// <summary>Helpers for picking the right sensor out of the differently named sets each vendor exposes.</summary>
internal static class SensorSelector
{
    private static readonly string[] CpuTemperaturePriority =
        ["CPU Package", "Core (Tctl/Tdie)", "Tctl", "Tdie", "Core Max", "Core Average", "CPU Total"];

    public static double? Value(ISensor? sensor) =>
        sensor?.Value is { } v && float.IsFinite(v) ? v : null;

    /// <summary>First sensor of the type whose name equals one of <paramref name="names"/>, in priority order.</summary>
    public static ISensor? Find(IEnumerable<ISensor> sensors, SensorType type, params string[] names)
    {
        var ofType = sensors.Where(s => s.SensorType == type).ToList();
        foreach (string name in names)
        {
            ISensor? match = ofType.FirstOrDefault(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) && Value(s) is not null);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public static ISensor? FirstWithValue(IEnumerable<ISensor> sensors, SensorType type) =>
        sensors.FirstOrDefault(s => s.SensorType == type && Value(s) is not null);

    public static double? PickCpuTemperature(IReadOnlyList<ISensor> sensors)
    {
        var temperatures = sensors.Where(s => s.SensorType == SensorType.Temperature && Value(s) is not null).ToList();

        foreach (string wanted in CpuTemperaturePriority)
        {
            ISensor? match = temperatures.FirstOrDefault(s => s.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return Value(match);
            }
        }

        var cores = temperatures.Where(s => s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)).ToList();
        if (cores.Count > 0)
        {
            return cores.Average(s => Value(s)!.Value);
        }

        return temperatures.Count > 0 ? Value(temperatures[0]) : null;
    }
}
