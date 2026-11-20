using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.ViewModels;

public sealed class CoreUsageViewModel : ViewModelBase
{
    private double _percent;
    private string _text = ValueFormatter.NotAvailable;

    public CoreUsageViewModel(int index) => Name = $"CPU {index}";

    public string Name { get; }

    public double Percent
    {
        get => _percent;
        private set => Set(ref _percent, value);
    }

    public string Text
    {
        get => _text;
        private set => Set(ref _text, value);
    }

    public void Update(double? usage)
    {
        Percent = usage ?? 0;
        Text = ValueFormatter.Percent(usage);
    }
}
