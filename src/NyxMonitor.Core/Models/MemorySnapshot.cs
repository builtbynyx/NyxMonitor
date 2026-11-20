namespace NyxMonitor.Core.Models;

public sealed record MemorySnapshot
{
    /// <summary>Physically installed RAM (may exceed <see cref="TotalBytes"/> because of hardware reservations).</summary>
    public long? InstalledBytes { get; init; }

    /// <summary>RAM usable by Windows.</summary>
    public long? TotalBytes { get; init; }
    public long? UsedBytes { get; init; }
    public long? AvailableBytes { get; init; }
    public long? CommitUsedBytes { get; init; }
    public long? CommitLimitBytes { get; init; }

    public double? UtilizationPercent =>
        UsedBytes is { } used && TotalBytes is > 0 and var total ? used * 100.0 / total : null;

    public double? CommitPercent =>
        CommitUsedBytes is { } used && CommitLimitBytes is > 0 and var limit ? used * 100.0 / limit : null;
}
