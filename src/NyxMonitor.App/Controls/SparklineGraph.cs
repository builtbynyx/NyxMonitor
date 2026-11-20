using System.Windows;
using System.Windows.Media;
using NyxMonitor.Core.Utilities;

namespace NyxMonitor.App.Controls;

/// <summary>
/// Lightweight rolling line graph drawn directly with a <see cref="DrawingContext"/>.
/// It redraws only when its <see cref="HistorySeries"/> changes, attaches to the series only while loaded
/// (so recreated views never leak), and breaks the line wherever a sample is missing (NaN).
/// </summary>
public sealed class SparklineGraph : FrameworkElement
{
    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series), typeof(HistorySeries), typeof(SparklineGraph),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSeriesChanged));

    public static readonly DependencyProperty SecondarySeriesProperty = DependencyProperty.Register(
        nameof(SecondarySeries), typeof(HistorySeries), typeof(SparklineGraph),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSeriesChanged));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(SparklineGraph),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0xAA, 0xA0, 0xFF)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SecondaryStrokeProperty = DependencyProperty.Register(
        nameof(SecondaryStroke), typeof(Brush), typeof(SparklineGraph),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x5E, 0xE0, 0xC8)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(SparklineGraph),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Upper bound of the Y axis. NaN (default) auto-scales to the visible data.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(SparklineGraph),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Smallest upper bound used when auto-scaling, so an idle graph does not zoom into noise.</summary>
    public static readonly DependencyProperty AutoMaximumFloorProperty = DependencyProperty.Register(
        nameof(AutoMaximumFloor), typeof(double), typeof(SparklineGraph),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Pen GridPen = CreateGridPen();

    private HistorySeries? _attachedPrimary;
    private HistorySeries? _attachedSecondary;
    private LinearGradientBrush? _fillBrush;
    private Color _fillColor;

    public SparklineGraph()
    {
        SnapsToDevicePixels = true;
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
    }

    public HistorySeries? Series { get => (HistorySeries?)GetValue(SeriesProperty); set => SetValue(SeriesProperty, value); }
    public HistorySeries? SecondarySeries { get => (HistorySeries?)GetValue(SecondarySeriesProperty); set => SetValue(SecondarySeriesProperty, value); }
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public Brush SecondaryStroke { get => (Brush)GetValue(SecondaryStrokeProperty); set => SetValue(SecondaryStrokeProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double AutoMaximumFloor { get => (double)GetValue(AutoMaximumFloorProperty); set => SetValue(AutoMaximumFloorProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        double width = ActualWidth;
        double height = ActualHeight;
        if (width < 4 || height < 4)
        {
            return;
        }

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        for (int i = 1; i < 4; i++)
        {
            double y = Math.Floor(height * i / 4) + 0.5;
            dc.DrawLine(GridPen, new Point(0, y), new Point(width, y));
        }

        ResolveRange(out double min, out double max);
        DrawSeries(dc, Series, Stroke, withFill: true, width, height, min, max);
        DrawSeries(dc, SecondarySeries, SecondaryStroke, withFill: false, width, height, min, max);
    }

    private void DrawSeries(DrawingContext dc, HistorySeries? series, Brush stroke, bool withFill,
        double width, double height, double min, double max)
    {
        if (series is null || series.Count < 2)
        {
            return;
        }

        int count = series.Count;
        double step = width / (series.Capacity - 1);
        double range = max - min;

        double X(int index) => width - (count - 1 - index) * step;
        double Y(double value) => height - 1 - Math.Clamp((value - min) / range, 0, 1) * (height - 2);

        var line = new StreamGeometry();
        var area = withFill ? new StreamGeometry() : null;

        using (StreamGeometryContext lineContext = line.Open())
        using (StreamGeometryContext? areaContext = area?.Open())
        {
            int i = 0;
            while (i < count)
            {
                if (double.IsNaN(series[i]))
                {
                    i++;
                    continue;
                }

                var start = new Point(X(i), Y(series[i]));
                lineContext.BeginFigure(start, isFilled: false, isClosed: false);
                areaContext?.BeginFigure(new Point(start.X, height), isFilled: true, isClosed: true);
                areaContext?.LineTo(start, isStroked: false, isSmoothJoin: false);
                int lastIndex = i;
                i++;

                while (i < count && !double.IsNaN(series[i]))
                {
                    var point = new Point(X(i), Y(series[i]));
                    lineContext.LineTo(point, isStroked: true, isSmoothJoin: true);
                    areaContext?.LineTo(point, isStroked: false, isSmoothJoin: false);
                    lastIndex = i;
                    i++;
                }

                areaContext?.LineTo(new Point(X(lastIndex), height), isStroked: false, isSmoothJoin: false);
            }
        }

        line.Freeze();

        if (area is not null)
        {
            area.Freeze();
            Brush? fill = GetFillBrush(stroke);
            if (fill is not null)
            {
                dc.DrawGeometry(fill, null, area);
            }
        }

        var pen = new Pen(stroke, 1.6) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawGeometry(null, pen, line);
    }

    private void ResolveRange(out double min, out double max)
    {
        min = Minimum;
        max = Maximum;

        if (double.IsNaN(max))
        {
            double peak = Math.Max(Peak(Series), Peak(SecondarySeries));
            max = Math.Max(AutoMaximumFloor, peak * 1.15);
        }

        if (max <= min)
        {
            max = min + 1;
        }
    }

    private static double Peak(HistorySeries? series)
    {
        if (series is null)
        {
            return 0;
        }

        double peak = 0;
        for (int i = 0; i < series.Count; i++)
        {
            double value = series[i];
            if (!double.IsNaN(value) && value > peak)
            {
                peak = value;
            }
        }

        return peak;
    }

    private Brush? GetFillBrush(Brush stroke)
    {
        if (stroke is not SolidColorBrush solid)
        {
            return null;
        }

        if (_fillBrush is null || _fillColor != solid.Color)
        {
            Color c = solid.Color;
            var gradient = new LinearGradientBrush(
                Color.FromArgb(0x44, c.R, c.G, c.B), Color.FromArgb(0x00, c.R, c.G, c.B), 90);
            gradient.Freeze();
            _fillBrush = gradient;
            _fillColor = c;
        }

        return _fillBrush;
    }

    private void Attach()
    {
        Detach();
        _attachedPrimary = Series;
        _attachedSecondary = SecondarySeries;
        if (_attachedPrimary is not null)
        {
            _attachedPrimary.Changed += OnSeriesDataChanged;
        }

        if (_attachedSecondary is not null)
        {
            _attachedSecondary.Changed += OnSeriesDataChanged;
        }

        InvalidateVisual();
    }

    private void Detach()
    {
        if (_attachedPrimary is not null)
        {
            _attachedPrimary.Changed -= OnSeriesDataChanged;
            _attachedPrimary = null;
        }

        if (_attachedSecondary is not null)
        {
            _attachedSecondary.Changed -= OnSeriesDataChanged;
            _attachedSecondary = null;
        }
    }

    private void OnSeriesDataChanged(object? sender, EventArgs e) => InvalidateVisual();

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var graph = (SparklineGraph)d;
        if (graph.IsLoaded)
        {
            graph.Attach();
        }
    }

    private static Pen CreateGridPen()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(0x1E, 0xFF, 0xFF, 0xFF)), 1);
        pen.Freeze();
        return pen;
    }
}
