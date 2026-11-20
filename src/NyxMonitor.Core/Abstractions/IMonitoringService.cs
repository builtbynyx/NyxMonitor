using NyxMonitor.Core.Models;

namespace NyxMonitor.Core.Abstractions;

public interface IMonitoringService
{
    /// <summary>Raised on a background thread after each polling cycle. Handlers must marshal to the UI thread.</summary>
    event EventHandler<SystemSnapshot>? SnapshotUpdated;

    TimeSpan Interval { get; set; }
    void Start();
    Task StopAsync();
}
