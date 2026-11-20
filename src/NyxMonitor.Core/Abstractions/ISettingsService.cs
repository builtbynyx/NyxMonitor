using NyxMonitor.Core.Configuration;

namespace NyxMonitor.Core.Abstractions;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
}
