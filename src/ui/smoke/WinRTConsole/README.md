# WinRT Console Smoke Test

**Purpose**: Minimal validation that a pure C# console application can use WinRT projections (e.g. `Windows.Foundation`) when only the .NET 8 SDK + Windows App SDK workload are installed — no full Visual Studio Community, no .sln, no GUI.

This project is intentionally tiny and is expected to be expanded or removed in later PRs once real WinUI code exists.

## Build (after prerequisites from docs/setup.md)

```powershell
cd src\ui\smoke\WinRTConsole
dotnet build -c Release
.\bin\Release\net8.0-windows10.0.19041.0\win-x64\WinRTConsole.exe
```

Expected output contains "SUCCESS: WinRT projection loaded without full Visual Studio."

## Notes
- TFM + RID chosen so the binary path is deterministic.
- Namespace and strings contain "PR 1" / "Smoke" because this is bootstrap scaffolding.
- Lifetime: delete or evolve when real agent/UI service code lands.
