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
- **PR 2 in progress** (branch `execute-plan/pr-2-winui3-shell-skeleton`): WinUI 3 shell skeleton with Fluent command-bar navigation (Chat / Suggestions / Dashboard / Settings panes), named-pipe+JSON transport client stub (per IPC spike rec), MSIX packaging config. Committed skeleton; full build + run requires .NET 8 SDK + Windows App SDK workload (see docs/setup.md). No backend yet.

See `docs/ipc-spike.md` for transport recommendation (named pipes + JSON preferred for this desktop agent; gRPC fallback documented).

## Getting Started

See [docs/setup.md](docs/setup.md) for exact winget + VS 2022 install steps (expect 1–4 hours and 15–30+ GB).

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

## Next

PR 2 delivers the native WinUI 3 desktop shell (chat + suggestions + dashboard) with a stubbed transport client. PR 3 will bring the Python agent core + real transport server + tool framework (applying the IPC spike recommendation).

## License

TBD
