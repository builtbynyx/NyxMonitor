namespace NyxMonitor.Core.Abstractions;

public interface IHardwareStatus
{
    /// <summary>True once the hardware sensor library opened successfully.</summary>
    bool IsOnline { get; }

    /// <summary>True when the process runs with administrator rights (some sensors require this).</summary>
    bool IsElevated { get; }
}
