using NyxMonitor.Core.Models;
using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.ViewModels;

public sealed class DriveViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _detail = string.Empty;
    private double _usedPercent;
    private string _usedPercentText = ValueFormatter.NotAvailable;
    private string _usedText = ValueFormatter.NotAvailable;
    private string _freeText = ValueFormatter.NotAvailable;
    private string _totalText = ValueFormatter.NotAvailable;
    private string _readText = ValueFormatter.NotAvailable;
    private string _writeText = ValueFormatter.NotAvailable;

    public DriveViewModel(string name) => Name = name;

    public string Name { get; }

    public string Title { get => _title; private set => Set(ref _title, value); }
    public string Detail { get => _detail; private set => Set(ref _detail, value); }
    public double UsedPercent { get => _usedPercent; private set => Set(ref _usedPercent, value); }
    public string UsedPercentText { get => _usedPercentText; private set => Set(ref _usedPercentText, value); }
    public string UsedText { get => _usedText; private set => Set(ref _usedText, value); }
    public string FreeText { get => _freeText; private set => Set(ref _freeText, value); }
    public string TotalText { get => _totalText; private set => Set(ref _totalText, value); }
    public string ReadText { get => _readText; private set => Set(ref _readText, value); }
    public string WriteText { get => _writeText; private set => Set(ref _writeText, value); }

    public void Update(DriveSnapshot drive)
    {
        string root = drive.Name.TrimEnd('\\');
        Title = string.IsNullOrWhiteSpace(drive.Label) ? root : $"{root}  {drive.Label}";
        Detail = string.Join(" · ", new[] { drive.Format, drive.DriveType }.Where(s => !string.IsNullOrEmpty(s)));
        UsedPercent = drive.UsedPercent;
        UsedPercentText = ValueFormatter.Percent(drive.UsedPercent);
        UsedText = ValueFormatter.Bytes(drive.UsedBytes);
        FreeText = ValueFormatter.Bytes(drive.FreeBytes);
        TotalText = ValueFormatter.Bytes(drive.TotalBytes);
        ReadText = ValueFormatter.BytesPerSecond(drive.ReadBytesPerSecond);
        WriteText = ValueFormatter.BytesPerSecond(drive.WriteBytesPerSecond);
    }
}
