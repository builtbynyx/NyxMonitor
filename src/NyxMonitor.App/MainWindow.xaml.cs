using System.Windows;
using NyxMonitor.App.ViewModels;

namespace NyxMonitor.App;

public partial class MainWindow : Window
{
    private const string MaximizeGlyph = "\uE922";
    private const string RestoreGlyph = "\uE923";

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestore(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnStateChanged(object? sender, EventArgs e)
    {
        bool maximized = WindowState == WindowState.Maximized;

        // A chrome-less window extends past the screen edge when maximized; pad by the system frame size.
        RootBorder.Padding = maximized ? SystemParameters.WindowResizeBorderThickness : new Thickness(0);
        MaximizeButton.Content = maximized ? RestoreGlyph : MaximizeGlyph;
        MaximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
    }
}
