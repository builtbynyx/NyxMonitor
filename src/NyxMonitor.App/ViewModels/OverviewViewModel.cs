namespace NyxMonitor.App.ViewModels;

/// <summary>The dashboard simply composes the detail view-models; they are updated by <see cref="MainViewModel"/>.</summary>
public sealed class OverviewViewModel : PageViewModel
{
    public OverviewViewModel(CpuViewModel cpu, GpuViewModel gpu, MemoryViewModel memory, StorageViewModel storage)
    {
        Cpu = cpu;
        Gpu = gpu;
        Memory = memory;
        Storage = storage;
    }

    public override string Title => "Overview";

    public CpuViewModel Cpu { get; }
    public GpuViewModel Gpu { get; }
    public MemoryViewModel Memory { get; }
    public StorageViewModel Storage { get; }
}
