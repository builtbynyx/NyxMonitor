using NyxMonitor.Core.Utilities;
using Xunit;

namespace NyxMonitor.Tests;

public class HistorySeriesTests
{
    [Fact]
    public void KeepsOnlyNewestSamplesOnceFull()
    {
        var series = new HistorySeries(3);
        for (int i = 1; i <= 5; i++)
        {
            series.Add(i);
        }

        Assert.Equal(3, series.Count);
        Assert.Equal(3, series[0]);
        Assert.Equal(5, series[2]);
        Assert.Equal(5, series.Latest);
    }

    [Fact]
    public void MissingValuesBecomeNaNAndLatestIsNull()
    {
        var series = new HistorySeries(4);
        series.Add(10);
        series.Add(null);

        Assert.True(double.IsNaN(series[1]));
        Assert.Null(series.Latest);
    }

    [Fact]
    public void RaisesChangedOnAdd()
    {
        var series = new HistorySeries(2);
        int raised = 0;
        series.Changed += (_, _) => raised++;
        series.Add(1);
        series.Add(2);
        Assert.Equal(2, raised);
    }

    [Fact]
    public void IndexOutOfRangeThrows()
    {
        var series = new HistorySeries(2);
        series.Add(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => series[1]);
    }
}
