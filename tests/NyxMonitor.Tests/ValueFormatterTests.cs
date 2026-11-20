using System.Globalization;
using NyxMonitor.Core.Configuration;
using NyxMonitor.Core.Utilities;
using Xunit;

namespace NyxMonitor.Tests;

public class ValueFormatterTests
{
    public ValueFormatterTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [Fact]
    public void MissingValuesRenderAsNA()
    {
        Assert.Equal("N/A", ValueFormatter.Percent(null));
        Assert.Equal("N/A", ValueFormatter.Percent(double.NaN));
        Assert.Equal("N/A", ValueFormatter.Bytes((long?)null));
        Assert.Equal("N/A", ValueFormatter.Clock(null));
        Assert.Equal("N/A", ValueFormatter.Temperature(null, TemperatureUnit.Celsius));
        Assert.Equal("N/A", ValueFormatter.UsedOfTotal(1, null));
    }

    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(8L * 1024 * 1024 * 1024, "8.0 GB")]
    public void BytesAreScaled(long input, string expected) =>
        Assert.Equal(expected, ValueFormatter.Bytes(input));

    [Fact]
    public void ClockSwitchesToGigahertz()
    {
        Assert.Equal("800 MHz", ValueFormatter.Clock(800));
        Assert.Equal("4.50 GHz", ValueFormatter.Clock(4500));
    }

    [Fact]
    public void TemperatureConvertsToFahrenheit()
    {
        Assert.Equal("50 °C", ValueFormatter.Temperature(50, TemperatureUnit.Celsius));
        Assert.Equal("122 °F", ValueFormatter.Temperature(50, TemperatureUnit.Fahrenheit));
    }

    [Fact]
    public void RateAddsPerSecondSuffix() =>
        Assert.Equal("2.0 MB/s", ValueFormatter.BytesPerSecond(2 * 1024 * 1024));
}
