using System.Security.Principal;
using LibreHardwareMonitor.Hardware;
using NyxMonitor.Core.Abstractions;

namespace NyxMonitor.Infrastructure.Hardware;

/// <summary>
/// Owns the single LibreHardwareMonitor <see cref="Computer"/>. It is opened lazily on the first
/// <see cref="Refresh"/> (on the monitoring thread, so startup never blocks the UI) and refreshed once per cycle.
/// </summary>
public sealed class HardwareSensorHub : IHardwareStatus, IDisposable
{
    private readonly IAppLogger _log;
    private readonly object _gate = new();
    private Computer? _computer;
    private bool _openAttempted;
    private bool _disposed;
    private volatile bool _isOnline;

    public HardwareSensorHub(IAppLogger log)
    {
        _log = log;
        IsElevated = DetectElevation();
    }

    public bool IsOnline => _isOnline;
    public bool IsElevated { get; }

    /// <summary>Updates every sensor. Safe to call repeatedly; failures leave the hub offline rather than throwing.</summary>
    public void Refresh()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (!_openAttempted)
            {
                _openAttempted = true;
                try
                {
                    _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
                    _computer.Open();
                    _isOnline = true;
                    _log.Info($"LibreHardwareMonitor opened (elevated: {IsElevated}).");
                }
                catch (Exception ex)
                {
                    _log.Error("LibreHardwareMonitor could not be opened; hardware sensors are unavailable.", ex);
                    CloseQuietly();
                    _isOnline = false;
                }
            }

            if (_computer is null)
            {
                return;
            }

            try
            {
                foreach (IHardware hardware in _computer.Hardware)
                {
                    UpdateRecursive(hardware);
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Sensor refresh failed.", ex);
            }
        }
    }

    public IReadOnlyList<IHardware> GetHardware(params HardwareType[] types)
    {
        lock (_gate)
        {
            if (_computer is null || _disposed)
            {
                return Array.Empty<IHardware>();
            }

            return _computer.Hardware.Where(h => Array.IndexOf(types, h.HardwareType) >= 0).ToList();
        }
    }

    public IReadOnlyList<ISensor> GetSensors(HardwareType type) =>
        GetHardware(type).SelectMany(SensorsOf).ToList();

    public static IEnumerable<ISensor> SensorsOf(IHardware hardware)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            yield return sensor;
        }

        foreach (IHardware sub in hardware.SubHardware)
        {
            foreach (ISensor sensor in SensorsOf(sub))
            {
                yield return sensor;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _isOnline = false;
            CloseQuietly();
        }
    }

    private static void UpdateRecursive(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware sub in hardware.SubHardware)
        {
            UpdateRecursive(sub);
        }
    }

    private void CloseQuietly()
    {
        try
        {
            _computer?.Close();
        }
        catch (Exception ex)
        {
            _log.Warning("Error while closing LibreHardwareMonitor.", ex);
        }

        _computer = null;
    }

    private static bool DetectElevation()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
