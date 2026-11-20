using Microsoft.Extensions.DependencyInjection;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;
using NyxMonitor.Infrastructure.Hardware;
using NyxMonitor.Infrastructure.Providers;
using NyxMonitor.Infrastructure.Services;

namespace NyxMonitor.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers logging, settings, the hardware hub, all metric providers and the monitoring service.</summary>
    public static IServiceCollection AddNyxMonitorInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<FileAppLogger>();
        services.AddSingleton<IAppLogger>(sp => sp.GetRequiredService<FileAppLogger>());
        services.AddSingleton<ISettingsService, JsonSettingsService>();

        services.AddSingleton<HardwareSensorHub>();
        services.AddSingleton<IHardwareStatus>(sp => sp.GetRequiredService<HardwareSensorHub>());

        services.AddSingleton<IMetricProvider<CpuSnapshot>, CpuProvider>();
        services.AddSingleton<IMetricProvider<GpuSnapshot>, GpuProvider>();
        services.AddSingleton<IMetricProvider<MemorySnapshot>, MemoryProvider>();
        services.AddSingleton<IMetricProvider<StorageSnapshot>, StorageProvider>();

        services.AddSingleton<IMonitoringService, MonitoringService>();
        return services;
    }
}
