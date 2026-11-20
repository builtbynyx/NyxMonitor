namespace NyxMonitor.Core.Models;

public sealed record DriveSnapshot
{
    /// <summary>Root path such as <c>C:\</c>. Used as the stable identity of the drive.</summary>
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string DriveType { get; init; } = string.Empty;
    public long TotalBytes { get; init; }
    public long FreeBytes { get; init; }
    public double? ReadBytesPerSecond { get; init; }
    public double? WriteBytesPerSecond { get; init; }

    public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);
    public double UsedPercent => TotalBytes > 0 ? UsedBytes * 100.0 / TotalBytes : 0;
}

public sealed record StorageSnapshot
{
    public IReadOnlyList<DriveSnapshot> Drives { get; init; } = Array.Empty<DriveSnapshot>();
    public double? TotalReadBytesPerSecond { get; init; }
    public double? TotalWriteBytesPerSecond { get; init; }
}
