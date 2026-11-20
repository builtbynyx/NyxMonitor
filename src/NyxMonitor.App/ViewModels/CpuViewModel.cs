using System.Collections.ObjectModel;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Configuration;
using NyxMonitor.Core.Models;
using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.ViewModels;

public sealed class CpuViewModel : PageViewModel
{
    private readonly ISettingsService _settings;

    private string _name = ValueFormatter.NotAvailable;
    private string _manufacturer = ValueFormatter.NotAvailable;
    private string _usageText = ValueFormatter.NotAvailable;
    private double _usagePercent;
    private string _clockText = ValueFormatter.NotAvailable;
    private string _baseClockText = ValueFormatter.NotAvailable;
    private string _temperatureText = ValueFormatter.NotAvailable;
    private string _coresText = ValueFormatter.NotAvailable;
    private string _threadsText = ValueFormatter.NotAvailable;
    private string _cacheText = ValueFormatter.NotAvailable;
    private bool _showSensorNotice;

    public CpuViewModel(HistoryStore history, ISettingsService settings)
    {
        _settings = settings;
        UsageHistory = history.CpuUsage;
        TemperatureHistory = history.CpuTemperature;
        HistoryLabel = $"Last {FormatDuration(settings.Current.HistorySeconds)}";
    }

    public override string Title => "CPU";

    public HistorySeries UsageHistory { get; }
    public HistorySeries TemperatureHistory { get; }
    public string HistoryLabel { get; }

    public ObservableCollection<CoreUsageViewModel> CoreUsages { get; } = new();
    public ObservableCollection<SensorItemViewModel> TemperatureSensors { get; } = new();
    public ObservableCollection<SensorItemViewModel> ClockSensors { get; } = new();

    public string Name { get => _name; private set => Set(ref _name, value); }
    public string Manufacturer { get => _manufacturer; private set => Set(ref _manufacturer, value); }
    public string UsageText { get => _usageText; private set => Set(ref _usageText, value); }
    public double UsagePercent { get => _usagePercent; private set => Set(ref _usagePercent, value); }
    public string ClockText { get => _clockText; private set => Set(ref _clockText, value); }
    public string BaseClockText { get => _baseClockText; private set => Set(ref _baseClockText, value); }
    public string TemperatureText { get => _temperatureText; private set => Set(ref _temperatureText, value); }
    public string CoresText { get => _coresText; private set => Set(ref _coresText, value); }
    public string ThreadsText { get => _threadsText; private set => Set(ref _threadsText, value); }
    public string CacheText { get => _cacheText; private set => Set(ref _cacheText, value); }
    public bool ShowSensorNotice { get => _showSensorNotice; private set => Set(ref _showSensorNotice, value); }

    public override void Update(SystemSnapshot snapshot)
    {
        CpuSnapshot cpu = snapshot.Cpu;
        TemperatureUnit unit = _settings.Current.TemperatureUnit;

        Name = cpu.Name;
        Manufacturer = cpu.Manufacturer;
        UsageText = ValueFormatter.Percent(cpu.UtilizationPercent);
        UsagePercent = cpu.UtilizationPercent ?? 0;
        ClockText = ValueFormatter.Clock(cpu.CurrentClockMhz);
        BaseClockText = ValueFormatter.Clock(cpu.BaseClockMhz);
        TemperatureText = ValueFormatter.Temperature(cpu.TemperatureCelsius, unit);
        CoresText = ValueFormatter.Count(cpu.PhysicalCores);
        ThreadsText = ValueFormatter.Count(cpu.LogicalProcessors);
        CacheText = cpu.L3CacheKilobytes is { } kb ? ValueFormatter.Bytes((long)kb * 1024, 0) : ValueFormatter.NotAvailable;

        CollectionSync.Sync(CoreUsages, cpu.CoreUtilization,
            (_, _) => true,
            _ => new CoreUsageViewModel(CoreUsages.Count),
            (item, value) => item.Update(value));

        CollectionSync.Sync(TemperatureSensors, cpu.TemperatureSensors,
            (item, s) => item.Name == s.Name,
            s => new SensorItemViewModel(s.Name),
            (item, s) => item.ValueText = ValueFormatter.Temperature(s.Value, unit));

        CollectionSync.Sync(ClockSensors, cpu.ClockSensors,
            (item, s) => item.Name == s.Name,
            s => new SensorItemViewModel(s.Name),
            (item, s) => item.ValueText = ValueFormatter.Clock(s.Value));

        ShowSensorNotice = cpu.TemperatureSensors.Count == 0 || cpu.TemperatureCelsius is null;
    }

    internal static string FormatDuration(int seconds) =>
        seconds % 60 == 0 ? $"{seconds / 60} min" : $"{seconds} s";
}
