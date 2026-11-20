using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;
using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.ViewModels;

public enum SensorState
{
    Starting,
    Full,
    Limited,
    Offline
}

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IMonitoringService _monitoring;
    private readonly HistoryStore _history;
    private readonly CpuViewModel _cpu;
    private readonly GpuViewModel _gpu;
    private readonly MemoryViewModel _memory;
    private readonly StorageViewModel _storage;
    private readonly Dispatcher _dispatcher;

    private volatile SystemSnapshot? _latest;
    private int _updatePending;
    private bool _disposed;

    private NavigationItem _selectedItem;
    private SensorState _sensorState = SensorState.Starting;
    private string _statusText = "Starting…";
    private string _sensorText = "Sensors: starting…";

    public MainViewModel(
        IMonitoringService monitoring,
        ISettingsService settings,
        HistoryStore history,
        OverviewViewModel overview,
        CpuViewModel cpu,
        GpuViewModel gpu,
        MemoryViewModel memory,
        StorageViewModel storage)
    {
        _monitoring = monitoring;
        _history = history;
        _cpu = cpu;
        _gpu = gpu;
        _memory = memory;
        _storage = storage;
        _dispatcher = Application.Current.Dispatcher;

        NavigationItems =
        [
            new NavigationItem(overview.Title, overview),
            new NavigationItem(cpu.Title, cpu),
            new NavigationItem(gpu.Title, gpu),
            new NavigationItem(memory.Title, memory),
            new NavigationItem(storage.Title, storage)
        ];
        _selectedItem = NavigationItems[0];

        IntervalText = $"{settings.Current.PollingIntervalMs / 1000.0:0.0#} s";
        VersionText = "v" + GetVersion();

        _monitoring.SnapshotUpdated += OnSnapshotUpdated;
    }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }
    public string VersionText { get; }
    public string IntervalText { get; }
    public string CopyrightText => "© 2026 Nyx / BuiltByNyx";

    public NavigationItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value is not null && Set(ref _selectedItem, value))
            {
                CurrentPage = value.Page;
            }
        }
    }

    private PageViewModel? _currentPage;
    public PageViewModel CurrentPage
    {
        get => _currentPage ?? _selectedItem.Page;
        private set => Set(ref _currentPage, value);
    }

    public SensorState SensorState { get => _sensorState; private set => Set(ref _sensorState, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string SensorText { get => _sensorText; private set => Set(ref _sensorText, value); }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _monitoring.SnapshotUpdated -= OnSnapshotUpdated;
    }

    // Runs on the monitoring thread. Keeps only the newest snapshot and queues at most one UI update,
    // so a busy UI thread drops frames instead of building a backlog.
    private void OnSnapshotUpdated(object? sender, SystemSnapshot snapshot)
    {
        _latest = snapshot;
        if (Interlocked.Exchange(ref _updatePending, 1) == 0)
        {
            _dispatcher.InvokeAsync(ApplyLatest, DispatcherPriority.Background);
        }
    }

    private void ApplyLatest()
    {
        Interlocked.Exchange(ref _updatePending, 0);
        SystemSnapshot? snapshot = _latest;
        if (_disposed || snapshot is null)
        {
            return;
        }

        _history.Record(snapshot);
        _cpu.Update(snapshot);
        _gpu.Update(snapshot);
        _memory.Update(snapshot);
        _storage.Update(snapshot);

        StatusText = $"Live · {IntervalText}";
        if (!snapshot.HardwareSensorsOnline)
        {
            SensorState = SensorState.Offline;
            SensorText = "Sensors: unavailable";
        }
        else if (!snapshot.IsElevated)
        {
            SensorState = SensorState.Limited;
            SensorText = "Sensors: limited — run as administrator for CPU temperature";
        }
        else
        {
            SensorState = SensorState.Full;
            SensorText = "Sensors: full access";
        }
    }

    private static string GetVersion()
    {
        string? info = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return info?.Split('+')[0] ?? "0.1.0";
    }
}
