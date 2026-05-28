# Development Environment Setup (PR 1)

This document captures the **exact** steps required on a fresh Windows 11 machine (as of 2026-05-28) to reach a working state for this monorepo.

**Critical context from the target machine on 2026-05-28**:
- No .NET SDK installed ("No SDKs were found").
- Only Python 3.14.3 present via the `py` launcher.
- Git 2.54 + winget available.
- 367 GB free on C: (healthy margin).

## 1. Python (Required)

Production code is **pinned to Python 3.12** (see `pyproject.toml`) because many agent libraries (and some transitive deps) have compatibility gaps on 3.14 at the time of writing.

### Recommended: Use the `py` launcher

```powershell
# List what you have
py -0

# Install Python 3.12 (Microsoft Store or web download via the launcher)
py install 3.12

# Verify
py -3.12 --version
```

Alternative: winget or Microsoft Store "Python 3.12".

Create venvs with the specific version:

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
```

## 2. .NET 8 SDK + Windows App SDK Workload (Mandatory for UI)

**WARNING — Realistic expectations on a typical developer machine**:

- **Download + install time**: 1–4 hours wall-clock (highly dependent on network speed and machine I/O).
- **Disk impact**: 15–30+ GB final footprint for the SDK + workloads + caches. Temporary files during extraction/install can briefly consume 50+ GB.
- On the reference machine (2026-05-28) this was a **full day spike** risk item.

### Two paths: Minimal (for the smoke test) vs Recommended (full development)

**Minimal path (sufficient to build and run the lite WinRT smoke test)**

The smoke test in `src/ui/smoke/WinRTConsole/` is deliberately designed so that **only** the standalone .NET 8 SDK + the Windows App SDK workload (plus the matching Windows SDK component) are required. No full Visual Studio Community installation or .sln is needed.

1. Install the .NET 8 SDK only:

   ```powershell
   winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
   ```

2. Install the Windows App SDK workload bits (this can be done via the VS Installer even in a minimal scenario, or via the specific components that the workload pulls in). In practice on a completely clean machine the easiest reliable route is still to let the VS Installer pull the required pieces:

   ```powershell
   winget install Microsoft.VisualStudio.2022.Community --accept-package-agreements --accept-source-agreements
   ```

   Then in the Visual Studio Installer add (at minimum):
   - Windows App SDK component
   - A Windows 10/11 SDK matching the TFM used by the smoke test (see pitfalls below)

   After this the following command succeeds **without** ever opening Visual Studio:

   ```powershell
   cd src\ui\smoke\WinRTConsole
   dotnet build -c Release
   .\bin\Release\net8.0-windows10.0.19041.0\win-x64\WinRTConsole.exe
   ```

   Expected output contains "SUCCESS: WinRT projection loaded without full Visual Studio."

**Recommended path (for comfortable day-to-day WinUI / agent development)**

For real work you will want the full Visual Studio 2022 Community (or higher) with the standard workloads:

- .NET desktop development
- Windows application development (includes Windows App SDK + MSIX tools)
- Latest Windows 11 SDK (10.0.22621 or newer)

Use the same `winget install Microsoft.VisualStudio.2022.Community` + modify steps shown in the "Exact steps" section below. This is what most engineers will actually run.

### Exact steps (full recommended VS 2022 path)

(These are the commands that were used on the reference machine that had zero .NET on 2026-05-28.)

1. .NET 8 SDK (standalone):

   ```powershell
   winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
   ```

2. Visual Studio 2022 Community + required workloads:

   ```powershell
   winget install Microsoft.VisualStudio.2022.Community --accept-package-agreements --accept-source-agreements
   ```

   Modify with the key components (example):

   ```powershell
   $vsInstaller = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vs_installer.exe"
   & $vsInstaller modify --installPath "C:\Program Files\Microsoft Visual Studio\2022\Community" `
       --add Microsoft.VisualStudio.Workload.NetDesktop `
       --add Microsoft.VisualStudio.ComponentGroup.WindowsAppSDK `
       --add Microsoft.VisualStudio.Component.Windows11SDK.22621 `
       --quiet --wait
   ```

3. Verify:

   ```powershell
   dotnet --list-sdks
   # You should now see at least 8.0.x
   ```

   The smoke test (minimal path) will also work after the full install.

## 3. Python Tooling (ruff + mypy)

After Python 3.12 is installed:

```powershell
# One-time (or use scripts/setup.ps1)
py -3.12 -m pip install --user ruff mypy
```

Or run the provided helper:

```powershell
.\scripts\setup.ps1
```

This creates `.venv` (Python 3.12) and installs the dev tools declared in `pyproject.toml`.

Local commands:

```powershell
ruff check .
ruff format .
mypy src
```

## 4. Git & Repository

Already satisfied on the reference machine.

## 5. Full Verification After Setup

- Python 3.12 venv + ruff/mypy pass
- `dotnet --list-sdks` shows 8.0.x
- The WinRTConsole smoke builds and runs
- You can re-run the IPC spike harness if desired: `cd spike\ipc; py -3.12 measure.py` (3.14 also works for this one-off harness that only needs stdlib + protobuf)

## Common Pitfalls Observed on 2026-05-28

- Forgetting the Windows App SDK workload → WinRT projection types missing at build time.
- Using only the standalone .NET SDK without any Windows SDK components → `net8.0-windows10.0.19041.0` targets fail.
- The current smoke test TFM requires the Windows 10 SDK 19041 component (or a matching newer 10.0.19041+ SDK). The Windows 11 SDK (22621) alone may be insufficient for this exact TFM.
- Installing Python only via the Microsoft Store without the `py` launcher → venv creation for specific minor versions becomes painful.

Follow the steps above exactly and the bootstrap experience will match what this PR documents.
