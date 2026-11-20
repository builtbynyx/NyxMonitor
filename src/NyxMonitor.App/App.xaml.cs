using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using NyxMonitor.App.ViewModels;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Utilities;
using NyxMonitor.Infrastructure;

namespace NyxMonitor.App;

public partial class App : Application
{
    private const string SingleInstanceName = "BuiltByNyx.NyxMonitor.SingleInstance";

    private ServiceProvider? _services;
    private Mutex? _instanceMutex;
    private IAppLogger? _log;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(initiallyOwned: true, SingleInstanceName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("NyxMonitor is already running.", "NyxMonitor", MessageBoxButton.OK, MessageBoxImage.Information);
            _instanceMutex.Dispose();
            _instanceMutex = null;
            Shutdown();
            return;
        }

        var services = new ServiceCollection();
        services.AddNyxMonitorInfrastructure();
        services.AddSingleton(sp => new HistoryStore(sp.GetRequiredService<ISettingsService>().Current.HistoryCapacity));

        services.AddSingleton<CpuViewModel>();
        services.AddSingleton<GpuViewModel>();
        services.AddSingleton<MemoryViewModel>();
        services.AddSingleton<StorageViewModel>();
        services.AddSingleton<OverviewViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        _services = services.BuildServiceProvider();
        _log = _services.GetRequiredService<IAppLogger>();
        _log.Info("NyxMonitor starting.");

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            _log?.Error("Unhandled exception.", args.ExceptionObject as Exception);

        MainWindow window = _services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();

        // Subscribers (MainViewModel) exist by now, so start polling only after the window is up.
        _services.GetRequiredService<IMonitoringService>().Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _log?.Info("NyxMonitor exiting.");
        _services?.Dispose(); // stops the monitoring loop, then disposes providers and LibreHardwareMonitor

        if (_instanceMutex is not null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // not owned by this thread; nothing to release
            }

            _instanceMutex.Dispose();
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log?.Error("Unhandled UI exception.", e.Exception);
        e.Handled = true; // a rendering/binding glitch should not kill a monitoring tool; it is in the log
    }
}
