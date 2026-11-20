namespace NyxMonitor.App.ViewModels;

public sealed class SensorItemViewModel : ViewModelBase
{
    private string _valueText = "N/A";

    public SensorItemViewModel(string name) => Name = name;

    public string Name { get; }

    public string ValueText
    {
        get => _valueText;
        set => Set(ref _valueText, value);
    }
}
