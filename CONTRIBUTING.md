# Contributing to NyxMonitor

1. Fork and create a branch from `main`.
2. Requirements: Windows 10/11, .NET 10 SDK.
3. `dotnet build NyxMonitor.sln` and `dotnet test tests/NyxMonitor.Tests` must pass.
4. Keep new monitoring modules behind `IMetricProvider<T>`; providers must never touch UI objects and must return `null`/`N/A` instead of inventing values.
5. Report bugs with your CPU/GPU model and the log from `%LocalAppData%\BuiltByNyx\NyxMonitor\logs`.

By contributing you agree your work is released under the MIT license.
