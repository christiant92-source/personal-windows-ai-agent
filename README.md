# My Project

Monorepo for the agent (Python) + UI (WinUI / Windows App SDK via .NET) system.

## Layout

- `src/agent/` — Python agent (placeholder only in this PR)
- `src/ui/` — WinUI / C# projects (lite WinRT smoke test + PR 2 WinUI 3 shell skeleton present)
- `protos/` — Shared service contracts (gRPC .proto definitions)
- `docs/` — Setup, architecture, and spike results
- `scripts/` — Bootstrap and helper scripts
- `spike/` — One-off measurement harnesses (IPC spike results recorded)

## Status

- **PR 1 complete** (merged to main): Repository bootstrap, detailed prerequisites (`.NET 8 SDK + Windows App SDK workload` with timing/disk warnings), Python 3.12+ pinned, CI skeleton, mandatory IPC spike (named-pipe+JSON recommended over gRPC-style), lite WinRT smoke test.
- **PR 2 complete on branch** `execute-plan/pr-2-winui3-shell-skeleton`: Runnable shell (adapted to .NET MAUI net10.0-windows using the available `maui-windows` workload + pre-installed Windows App Runtimes 1.6/1.7/1.8; pure WinUI3/WindowsAppSDK VS workload component was never registered despite multiple install attempts). 
  - UI: nav buttons + switchable panes (Chat with send stub + log, Suggestions list, Dashboard, Settings with transport note).
  - "Test Transport" button + standalone `src/ui/TransportTest/` exercise the **exact** named-pipe + JSON client stub from `docs/ipc-spike.md` (pipe `my-agent-ipc`, 4-byte length prefix + UTF-8 JSON Ping {type, nonce, ts}).
  - Build: `dotnet build -c Release -f net10.0-windows10.0.19041.0` succeeds (0 errors).
  - Run: `dotnet run -f net10.0-windows10.0.19041.0 -c Release --no-build --project src/ui/WinUI3App/WinUI3App.csproj` launches the window.
  - TransportTest + button both produce the correct "no listener (EXPECTED)" until the PR 3 Python server exists.
  - MSIX support: `Package.appxmanifest` present in Platforms/Windows; builds with `-p:WindowsPackageType=MSIX` (dev uses unpackaged for speed).

See `docs/ipc-spike.md` for transport recommendation (named pipes + JSON preferred for this desktop agent; gRPC fallback documented). The adaptation was performed after explicit scan of available software (per user request) when the documented WindowsAppSDK workload could not be installed.

## Getting Started

See [docs/setup.md](docs/setup.md) for exact winget + VS 2022 install steps (expect 1–4 hours and 15–30+ GB for the *original* pure WinUI3 path). The practical path that works today for the shell uses the `maui-windows` workload (already present on this machine) + any recent .NET SDK.

Python side (after Python 3.12 installed via py launcher):

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e ".[dev]"   # or follow scripts/setup.ps1
```

Run linters locally (after ruff/mypy installed):

```powershell
ruff check .
ruff format .
mypy src
```

Run the PR 2 shell (Windows):

```powershell
cd src\ui\WinUI3App
dotnet build -c Release -f net10.0-windows10.0.19041.0
dotnet run -f net10.0-windows10.0.19041.0 -c Release --no-build
```

Run transport client validator (no server needed):

```powershell
dotnet run --project ..\TransportTest\TransportTest.csproj -c Release
```

## Next

PR 3: Python agent core skeleton + named-pipe server (matching the exact framing + Ping from the PR 2 client) + basic tool framework + Model Router stub. This will make the "Test Transport" button receive real responses. See design doc for full plan.

## License

TBD
