using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Configuration;
using NyxMonitor.Core.Models;
using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.ViewModels;

public sealed class GpuViewModel : PageViewModel
{
    private readonly ISettingsService _settings;

    private string _name = ValueFormatter.NotAvailable;
    private bool _showUnavailableNotice;
    private string _usageText = ValueFormatter.NotAvailable;
    private double _usagePercent;
    private string _vramText = ValueFormatter.NotAvailable;
    private string _vramPercentText = ValueFormatter.NotAvailable;
    private double _vramPercent;
    private string _sharedText = ValueFormatter.NotAvailable;
    private string _temperatureText = ValueFormatter.NotAvailable;
    private string _coreClockText = ValueFormatter.NotAvailable;
    private string _memoryClockText = ValueFormatter.NotAvailable;
    private string _fanText = ValueFormatter.NotAvailable;

    public GpuViewModel(HistoryStore history, ISettingsService settings)
    {
        _settings = settings;
        UsageHistory = history.GpuUsage;
        VramHistory = history.GpuVramUsage;
        TemperatureHistory = history.GpuTemperature;
        HistoryLabel = $"Last {CpuViewModel.FormatDuration(settings.Current.HistorySeconds)}";
    }

    public override string Title => "GPU";

    public HistorySeries UsageHistory { get; }
    public HistorySeries VramHistory { get; }
    public HistorySeries TemperatureHistory { get; }
    public string HistoryLabel { get; }

    public string Name { get => _name; private set => Set(ref _name, value); }
    public bool ShowUnavailableNotice { get => _showUnavailableNotice; private set => Set(ref _showUnavailableNotice, value); }
    public string UsageText { get => _usageText; private set => Set(ref _usageText, value); }
    public double UsagePercent { get => _usagePercent; private set => Set(ref _usagePercent, value); }
    public string VramText { get => _vramText; private set => Set(ref _vramText, value); }
    public string VramPercentText { get => _vramPercentText; private set => Set(ref _vramPercentText, value); }
    public double VramPercent { get => _vramPercent; private set => Set(ref _vramPercent, value); }
    public string SharedText { get => _sharedText; private set => Set(ref _sharedText, value); }
    public string TemperatureText { get => _temperatureText; private set => Set(ref _temperatureText, value); }
    public string CoreClockText { get => _coreClockText; private set => Set(ref _coreClockText, value); }
    public string MemoryClockText { get => _memoryClockText; private set => Set(ref _memoryClockText, value); }
    public string FanText { get => _fanText; private set => Set(ref _fanText, value); }

    public override void Update(SystemSnapshot snapshot)
    {
        GpuSnapshot gpu = snapshot.Gpu;
        TemperatureUnit unit = _settings.Current.TemperatureUnit;

        Name = gpu.Name;
        ShowUnavailableNotice = !gpu.IsAvailable;
        UsageText = ValueFormatter.Percent(gpu.UtilizationPercent);
        UsagePercent = gpu.UtilizationPercent ?? 0;
        VramText = ValueFormatter.UsedOfTotal(gpu.VramUsedBytes, gpu.VramTotalBytes);
        if (gpu.VramTotalBytes is null && gpu.VramUsedBytes is { } usedOnly)
        {
            VramText = ValueFormatter.Bytes(usedOnly) + " used";
        }

        VramPercentText = ValueFormatter.Percent(gpu.VramUtilizationPercent);
        VramPercent = gpu.VramUtilizationPercent ?? 0;
        SharedText = ValueFormatter.Bytes(gpu.SharedMemoryUsedBytes);
        TemperatureText = ValueFormatter.Temperature(gpu.TemperatureCelsius, unit);
        CoreClockText = ValueFormatter.Clock(gpu.CoreClockMhz);
        MemoryClockText = ValueFormatter.Clock(gpu.MemoryClockMhz);
        FanText = gpu.FanRpm is not null
            ? ValueFormatter.Rpm(gpu.FanRpm)
            : gpu.FanPercent is not null ? ValueFormatter.Percent(gpu.FanPercent) : ValueFormatter.NotAvailable;
    }
}
