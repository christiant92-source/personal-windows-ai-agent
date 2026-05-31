# My Project

Monorepo for the agent (Python) + UI (WinUI / Windows App SDK via .NET) system.

## Layout

- `src/agent/` — Python agent (placeholder only in this PR)
- `src/ui/` — WinUI / C# projects (lite WinRT smoke test present)
- `protos/` — Shared service contracts (gRPC .proto definitions)
- `docs/` — Setup, architecture, and spike results
- `scripts/` — Bootstrap and helper scripts
- `spike/` — One-off measurement harnesses (IPC spike results recorded)

## Status (PR 1)

- Repository bootstrap complete.
- Detailed prerequisites documentation (`.NET 8 SDK + Windows App SDK workload`) with honest timing/disk warnings.
- Python 3.12+ pinned (ruff + mypy).
- GitHub Actions CI skeleton for lint/typecheck.
- **Mandatory IPC spike completed**: gRPC-style (protobuf + protoc) vs named-pipe + JSON. Real measurements on this Windows machine (no .NET SDK present on 2026-05-28) recorded in `docs/ipc-spike.md`.
- Lite WinRT console smoke test (builds with .NET SDK + Windows App SDK workload only, no full VS required).

**No functional agent or UI code yet.** This PR unblocks subsequent work.

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

PR 2+ will add real agent service surface using the recommended transport from the spike.

## License

TBD
