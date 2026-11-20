using System.Globalization;
using NyxMonitor.Core.Configuration;

namespace NyxMonitor.Core.Utilities;

/// <summary>Turns raw readings into display strings. Missing values always render as "N/A".</summary>
public static class ValueFormatter
{
    public const string NotAvailable = "N/A";

    private static readonly string[] ByteUnits = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Percent(double? value, int decimals = 0) =>
        IsMissing(value) ? NotAvailable : value!.Value.ToString($"F{decimals}", CultureInfo.CurrentCulture) + "%";

    public static string Count(int? value) =>
        value is null ? NotAvailable : value.Value.ToString(CultureInfo.CurrentCulture);

    public static string Bytes(long? bytes, int decimals = 1) =>
        bytes is null ? NotAvailable : Bytes((double?)bytes.Value, decimals);

    public static string Bytes(double? bytes, int decimals = 1)
    {
        if (IsMissing(bytes))
        {
            return NotAvailable;
        }

        double value = Math.Max(0, bytes!.Value);
        int unit = 0;
        while (value >= 1024 && unit < ByteUnits.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        string format = unit == 0 ? "F0" : $"F{decimals}";
        return value.ToString(format, CultureInfo.CurrentCulture) + " " + ByteUnits[unit];
    }

    public static string BytesPerSecond(double? bytes, int decimals = 1) =>
        IsMissing(bytes) ? NotAvailable : Bytes(bytes, decimals) + "/s";

    public static string Clock(double? megahertz)
    {
        if (IsMissing(megahertz) || megahertz <= 0)
        {
            return NotAvailable;
        }

        return megahertz >= 1000
            ? (megahertz!.Value / 1000).ToString("F2", CultureInfo.CurrentCulture) + " GHz"
            : megahertz!.Value.ToString("F0", CultureInfo.CurrentCulture) + " MHz";
    }

    public static string Temperature(double? celsius, TemperatureUnit unit)
    {
        if (IsMissing(celsius))
        {
            return NotAvailable;
        }

        double value = unit == TemperatureUnit.Fahrenheit ? celsius!.Value * 9 / 5 + 32 : celsius!.Value;
        return value.ToString("F0", CultureInfo.CurrentCulture) + (unit == TemperatureUnit.Fahrenheit ? " °F" : " °C");
    }

    public static string Rpm(double? rpm) =>
        IsMissing(rpm) ? NotAvailable : rpm!.Value.ToString("F0", CultureInfo.CurrentCulture) + " RPM";

    /// <summary>"used / total" for byte quantities, or N/A when either side is unknown.</summary>
    public static string UsedOfTotal(long? used, long? total) =>
        used is null || total is null ? NotAvailable : $"{Bytes(used)} / {Bytes(total)}";

    private static bool IsMissing(double? value) => value is null || !double.IsFinite(value.Value);
}
