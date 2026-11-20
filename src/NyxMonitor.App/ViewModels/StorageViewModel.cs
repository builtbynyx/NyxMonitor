using System.Collections.ObjectModel;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Models;
using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.ViewModels;

public sealed class StorageViewModel : PageViewModel
{
    private string _readText = ValueFormatter.NotAvailable;
    private string _writeText = ValueFormatter.NotAvailable;
    private string _summaryText = ValueFormatter.NotAvailable;

    public StorageViewModel(HistoryStore history, ISettingsService settings)
    {
        ReadHistory = history.DiskRead;
        WriteHistory = history.DiskWrite;
        HistoryLabel = $"Last {CpuViewModel.FormatDuration(settings.Current.HistorySeconds)}";
    }

    public override string Title => "Storage";

    public HistorySeries ReadHistory { get; }
    public HistorySeries WriteHistory { get; }
    public string HistoryLabel { get; }

    public ObservableCollection<DriveViewModel> Drives { get; } = new();

    public string ReadText { get => _readText; private set => Set(ref _readText, value); }
    public string WriteText { get => _writeText; private set => Set(ref _writeText, value); }
    public string SummaryText { get => _summaryText; private set => Set(ref _summaryText, value); }

    public override void Update(SystemSnapshot snapshot)
    {
        StorageSnapshot storage = snapshot.Storage;

        ReadText = ValueFormatter.BytesPerSecond(storage.TotalReadBytesPerSecond);
        WriteText = ValueFormatter.BytesPerSecond(storage.TotalWriteBytesPerSecond);

        CollectionSync.Sync(Drives, storage.Drives,
            (item, d) => item.Name == d.Name,
            d => new DriveViewModel(d.Name),
            (item, d) => item.Update(d));

        long total = storage.Drives.Sum(d => d.TotalBytes);
        long free = storage.Drives.Sum(d => d.FreeBytes);
        SummaryText = storage.Drives.Count == 0
            ? ValueFormatter.NotAvailable
            : $"{storage.Drives.Count} drive{(storage.Drives.Count == 1 ? "" : "s")} · {ValueFormatter.Bytes(free)} free of {ValueFormatter.Bytes(total)}";
    }
}
