namespace NyxMonitor.Core.Abstractions;

/// <summary>
/// A monitoring module. Providers are read from a single background thread by the monitoring service
/// and must never touch UI objects.
/// </summary>
public interface IMetricProvider<out T> : IDisposable
{
    string Name { get; }

    /// <summary>The value reported when the module is disabled or throws.</summary>
    T Unavailable { get; }

    T Read();
}
