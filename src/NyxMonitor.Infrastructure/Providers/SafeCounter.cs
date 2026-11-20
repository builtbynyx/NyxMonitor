using System.ComponentModel;
using System.Diagnostics;

namespace NyxMonitor.Infrastructure.Providers;

/// <summary>A performance counter that degrades to "no value" instead of throwing when it is unavailable.</summary>
internal sealed class SafeCounter : IDisposable
{
    private PerformanceCounter? _counter;

    public SafeCounter(string category, string counter, string instance)
    {
        try
        {
            _counter = new PerformanceCounter(category, counter, instance, readOnly: true);
            _counter.NextValue(); // prime: the first sample is always 0
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or Win32Exception or ArgumentException)
        {
            _counter?.Dispose();
            _counter = null;
        }
    }

    public bool IsAvailable => _counter is not null;

    public double? Next()
    {
        if (_counter is null)
        {
            return null;
        }

        try
        {
            return _counter.NextValue();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _counter?.Dispose();
        _counter = null;
    }
}
