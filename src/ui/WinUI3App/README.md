# WinUI 3 Shell (PR 2)

Fluent Design desktop shell skeleton for the personal Windows AI agent.

## Current State (PR 2)
- Single-window app with NavigationView-style command bar.
- Four placeholder panes: Chat, Suggestions, Dashboard, Settings.
- Basic named-pipe + JSON transport client stub (matches IPC spike recommendation in `docs/ipc-spike.md`).
- MSIX packaging configuration (packaged app).
- **No real backend.** All agent behavior is stubbed. End-to-end chat + tool execution arrives in PR 3.

## Build Requirements
See root `docs/setup.md` — you **must** have:
- .NET 8 SDK
- Windows App SDK workload (via VS Installer or component add)
- Windows 10/11 SDK matching the TFM (10.0.19041+)

From repo root (after SDK present):

```powershell
cd src\ui\WinUI3App
dotnet build -c Release
# Then run the packaged app or use the generated MSIX for sideloading
```

The lite WinRT smoke test in `../smoke/WinRTConsole` validates the minimal workload path without a full solution.

## Next
PR 3 wires the real Python agent (gRPC or named-pipe server per spike results) and the first tool surface. The C# client here will be updated to the chosen transport interface (easy swap because of the stub abstraction).

Assets/ folder (logos, splash) is intentionally minimal for the skeleton — add real assets before PR 9 packaging polish.
