using NyxMonitor.Core.Configuration;
using NyxMonitor.Core.Models;
using NyxMonitor.Core.Utilities;
using Xunit;

namespace NyxMonitor.Tests;

public class ModelAndSettingsTests
{
    [Fact]
    public void NormalizeClampsOutOfRangeValues()
    {
        var settings = new AppSettings { PollingIntervalMs = 5, HistorySeconds = 999_999 };
        settings.Normalize();
        Assert.Equal(AppSettings.MinPollingIntervalMs, settings.PollingIntervalMs);
        Assert.Equal(AppSettings.MaxHistorySeconds, settings.HistorySeconds);
    }

    [Fact]
    public void HistoryCapacityFollowsDurationAndInterval()
    {
        var settings = new AppSettings { PollingIntervalMs = 1000, HistorySeconds = 120 };
        Assert.Equal(120, settings.HistoryCapacity);
    }

    [Fact]
    public void MemoryPercentagesAreNullWhenUnknown()
    {
        Assert.Null(new MemorySnapshot().UtilizationPercent);
        var m = new MemorySnapshot { TotalBytes = 200, UsedBytes = 50 };
        Assert.Equal(25, m.UtilizationPercent);
    }

    [Fact]
    public void GpuVramPercentNeedsTotal()
    {
        Assert.Null(new GpuSnapshot { VramUsedBytes = 5 }.VramUtilizationPercent);
        Assert.Equal(50, new GpuSnapshot { VramUsedBytes = 5, VramTotalBytes = 10 }.VramUtilizationPercent);
    }

    [Fact]
    public void StoreRecordsEveryMetric()
    {
        var store = new HistoryStore(5);
        var snapshot = new SystemSnapshot(
            DateTimeOffset.Now,
            new CpuSnapshot { UtilizationPercent = 12, TemperatureCelsius = 55 },
            new GpuSnapshot(),
            new MemorySnapshot { TotalBytes = 100, UsedBytes = 40 },
            new StorageSnapshot { TotalReadBytesPerSecond = 1000 },
            true, false);
        store.Record(snapshot);

        Assert.Equal(12, store.CpuUsage.Latest);
        Assert.Equal(55, store.CpuTemperature.Latest);
        Assert.Null(store.GpuUsage.Latest);
        Assert.Equal(40, store.MemoryUsage.Latest);
        Assert.Equal(1000, store.DiskRead.Latest);
    }

    [Fact]
    public void DriveUsedPercentHandlesZeroCapacity()
    {
        Assert.Equal(0, new DriveSnapshot().UsedPercent);
        Assert.Equal(75, new DriveSnapshot { TotalBytes = 100, FreeBytes = 25 }.UsedPercent);
    }
}
