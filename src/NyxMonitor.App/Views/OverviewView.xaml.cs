using System.Windows;
using System.Windows.Controls;

namespace NyxMonitor.App.Views;

public partial class OverviewView : UserControl
{
    private const double SingleColumnBelow = 900;

    public OverviewView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    // Pure layout concern, so it lives in the view rather than the view-model.
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        CardGrid.Columns = e.NewSize.Width < SingleColumnBelow ? 1 : 2;
}
