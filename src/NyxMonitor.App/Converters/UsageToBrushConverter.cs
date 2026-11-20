using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NyxMonitor.App.Converters;

/// <summary>Accent below 75 %, amber from 75 %, red from 90 %.</summary>
public sealed class UsageToBrushConverter : IValueConverter
{
    private static readonly Brush Normal = Freeze(Color.FromRgb(0xAA, 0xA0, 0xFF));
    private static readonly Brush Warning = Freeze(Color.FromRgb(0xFF, 0xC5, 0x6B));
    private static readonly Brush Critical = Freeze(Color.FromRgb(0xFF, 0x6B, 0x7A));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double percent = value is double d ? d : 0;
        return percent >= 90 ? Critical : percent >= 75 ? Warning : Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
