# NyxMonitor

NyxMonitor is an open-source Windows system monitoring application built with C# and modern .NET 10. Designed to provide a clear, real-time overview of your PC's hardware and system performance, NyxMonitor brings essential hardware statistics together in one clean, modern interface.

Monitor CPU and GPU utilization, memory consumption, storage activity, hardware temperatures, clock speeds, and more through a unified dashboard with live performance graphs.

## Features

- **System Overview**: View essential hardware statistics and system performance from one centralized dashboard.
- **CPU Monitoring**: Track processor utilization, clock speeds, temperatures, and core/thread information.
- **GPU Monitoring**: Monitor graphics utilization, VRAM consumption, temperatures, and core clock speeds.
- **Memory Monitoring**: View installed memory, current usage, and available RAM.
- **Storage Monitoring**: Track disk read/write activity, drive capacity, and available storage space.
- **Live Performance Graphs**: Visualize hardware utilization and resource activity through continuously updating graphs.
- **Hardware Temperatures**: Keep track of CPU and GPU temperatures in real time.
- **Drive Information**: View individual storage drives alongside their capacity and available space.
- **Live Monitoring**: Automatically refresh hardware statistics at regular intervals.
- **Modern Interface**: Dark-themed WPF interface with purple accents, custom navigation, and dedicated hardware monitoring pages.

## Requirements

- **Windows 10/11** (64-bit)
- **.NET 10 SDK** (only required for building from source)

## Installation

### From GitHub Releases (Recommended)

1. Visit the [NyxMonitor Releases](https://github.com/builtbynyx/NyxMonitor/releases) page.
2. Download the latest `NyxMonitor.exe`.
3. Right-click `NyxMonitor.exe`.
4. Select **Run as administrator**.

**Running NyxMonitor as administrator is recommended for full hardware sensor access and monitoring capabilities.**

### Build from Source

```powershell
# Clone the repository
git clone https://github.com/builtbynyx/NyxMonitor.git
cd NyxMonitor

# Build the project
dotnet build

# Run the application
dotnet run
```

## Usage

### Overview

Launch NyxMonitor to access the system overview dashboard, displaying your CPU, GPU, memory, and storage statistics.

### Hardware Monitoring

Use the navigation panel to access dedicated CPU, GPU, Memory, and Storage pages for more detailed hardware information.

### Live Statistics

NyxMonitor automatically refreshes hardware information, allowing you to monitor system performance as it changes.

### Administrator Permissions

For the best monitoring experience, run NyxMonitor with administrator privileges. Some hardware sensors and system information may require elevated permissions to access.

## Technology

- **C#** — Primary programming language
- **.NET 10** — Application framework
- **WPF** — Windows desktop interface
- **XAML** — Interface layout and styling

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

**NyxMonitor** — Your hardware. Your performance. BuiltByNyx.
