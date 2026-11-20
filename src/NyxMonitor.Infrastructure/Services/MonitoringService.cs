using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;
using NyxMonitor.Infrastructure.Hardware;

namespace NyxMonitor.Infrastructure.Services;

/// <summary>
/// Central polling loop. Runs on a thread-pool thread, reads every enabled provider once per interval
/// and publishes an immutable <see cref="SystemSnapshot"/>. A failing provider never stops the loop.
/// </summary>
public sealed class MonitoringService : IMonitoringService, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly HardwareSensorHub _hub;
    private readonly IMetricProvider<CpuSnapshot> _cpu;
    private readonly IMetricProvider<GpuSnapshot> _gpu;
    private readonly IMetricProvider<MemorySnapshot> _memory;
    private readonly IMetricProvider<StorageSnapshot> _storage;
    private readonly IAppLogger _log;
    private readonly HashSet<string> _failing = new();
    private readonly object _gate = new();

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private long _intervalTicks;

    public MonitoringService(
        ISettingsService settings,
        HardwareSensorHub hub,
        IMetricProvider<CpuSnapshot> cpu,
        IMetricProvider<GpuSnapshot> gpu,
        IMetricProvider<MemorySnapshot> memory,
        IMetricProvider<StorageSnapshot> storage,
        IAppLogger log)
    {
        _settings = settings;
        _hub = hub;
        _cpu = cpu;
        _gpu = gpu;
        _memory = memory;
        _storage = storage;
        _log = log;
        Interval = TimeSpan.FromMilliseconds(settings.Current.PollingIntervalMs);
    }

    public event EventHandler<SystemSnapshot>? SnapshotUpdated;

    public TimeSpan Interval
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _intervalTicks));
        set => Interlocked.Exchange(ref _intervalTicks, Math.Max(TimeSpan.FromMilliseconds(250).Ticks, value.Ticks));
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_loop is not null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            _loop = Task.Run(() => RunAsync(token), token);
        }
    }

    public async Task StopAsync()
    {
        Task? loop;
        CancellationTokenSource? cts;
        lock (_gate)
        {
            loop = _loop;
            cts = _cts;
            _loop = null;
            _cts = null;
        }

        if (loop is null || cts is null)
        {
            return;
        }

        await cts.CancelAsync().ConfigureAwait(false);
        await Task.WhenAny(loop, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
        cts.Dispose();
    }

    public void Dispose() => StopAsync().GetAwaiter().GetResult();

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval);

            // The first cycle opens sensors and primes performance counters; its values are discarded
            // so the UI never shows an artificial 0 % spike.
            Collect();

            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                timer.Period = Interval;
                SystemSnapshot snapshot = Collect();
                try
                {
                    SnapshotUpdated?.Invoke(this, snapshot);
                }
                catch (Exception ex)
                {
                    _log.Warning("A snapshot subscriber threw.", ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _log.Error("Monitoring loop terminated unexpectedly.", ex);
        }
    }

    private SystemSnapshot Collect()
    {
        _hub.Refresh();
        var config = _settings.Current;

        return new SystemSnapshot(
            DateTimeOffset.Now,
            config.EnableCpuMonitoring ? Safe(_cpu) : _cpu.Unavailable,
            config.EnableGpuMonitoring ? Safe(_gpu) : _gpu.Unavailable,
            config.EnableMemoryMonitoring ? Safe(_memory) : _memory.Unavailable,
            config.EnableStorageMonitoring ? Safe(_storage) : _storage.Unavailable,
            _hub.IsOnline,
            _hub.IsElevated);
    }

    private T Safe<T>(IMetricProvider<T> provider)
    {
        try
        {
            T value = provider.Read();
            _failing.Remove(provider.Name);
            return value;
        }
        catch (Exception ex)
        {
            if (_failing.Add(provider.Name)) // log once per outage, not once per second
            {
                _log.Error($"{provider.Name} provider failed.", ex);
            }

            return provider.Unavailable;
        }
    }
}
