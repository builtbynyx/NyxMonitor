using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;
using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.ViewModels;

public sealed class MemoryViewModel : PageViewModel
{
    private string _installedText = ValueFormatter.NotAvailable;
    private string _totalText = ValueFormatter.NotAvailable;
    private string _usedText = ValueFormatter.NotAvailable;
    private string _availableText = ValueFormatter.NotAvailable;
    private string _usageText = ValueFormatter.NotAvailable;
    private double _usagePercent;
    private string _detailText = ValueFormatter.NotAvailable;
    private string _commitText = ValueFormatter.NotAvailable;
    private string _commitPercentText = ValueFormatter.NotAvailable;
    private double _commitPercent;

    public MemoryViewModel(HistoryStore history, ISettingsService settings)
    {
        UsageHistory = history.MemoryUsage;
        CommitHistory = history.CommitUsage;
        HistoryLabel = $"Last {CpuViewModel.FormatDuration(settings.Current.HistorySeconds)}";
    }

    public override string Title => "Memory";

    public HistorySeries UsageHistory { get; }
    public HistorySeries CommitHistory { get; }
    public string HistoryLabel { get; }

    public string InstalledText { get => _installedText; private set => Set(ref _installedText, value); }
    public string TotalText { get => _totalText; private set => Set(ref _totalText, value); }
    public string UsedText { get => _usedText; private set => Set(ref _usedText, value); }
    public string AvailableText { get => _availableText; private set => Set(ref _availableText, value); }
    public string UsageText { get => _usageText; private set => Set(ref _usageText, value); }
    public double UsagePercent { get => _usagePercent; private set => Set(ref _usagePercent, value); }
    public string DetailText { get => _detailText; private set => Set(ref _detailText, value); }
    public string CommitText { get => _commitText; private set => Set(ref _commitText, value); }
    public string CommitPercentText { get => _commitPercentText; private set => Set(ref _commitPercentText, value); }
    public double CommitPercent { get => _commitPercent; private set => Set(ref _commitPercent, value); }

    public override void Update(SystemSnapshot snapshot)
    {
        MemorySnapshot memory = snapshot.Memory;

        InstalledText = ValueFormatter.Bytes(memory.InstalledBytes);
        TotalText = ValueFormatter.Bytes(memory.TotalBytes);
        UsedText = ValueFormatter.Bytes(memory.UsedBytes);
        AvailableText = ValueFormatter.Bytes(memory.AvailableBytes);
        UsageText = ValueFormatter.Percent(memory.UtilizationPercent);
        UsagePercent = memory.UtilizationPercent ?? 0;
        DetailText = memory.UsedBytes is null ? ValueFormatter.NotAvailable : $"{UsedText} of {TotalText} in use";
        CommitText = ValueFormatter.UsedOfTotal(memory.CommitUsedBytes, memory.CommitLimitBytes);
        CommitPercentText = ValueFormatter.Percent(memory.CommitPercent);
        CommitPercent = memory.CommitPercent ?? 0;
    }
}
