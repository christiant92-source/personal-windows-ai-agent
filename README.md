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
- **PR 2 + PR 3 PRs created** on GitHub (https://github.com/christiant92-source/personal-windows-ai-agent):
  - [PR #2 (Shell)](https://github.com/christiant92-source/personal-windows-ai-agent/pull/2) — OPEN, base `main`. Includes the runnable MAUI shell + (via stacked) the agent core.
  - [PR #3 (Agent)](https://github.com/christiant92-source/personal-windows-ai-agent/pull/3) — stacked on PR2. Python named-pipe server + router stub + basic tools. **Local end-to-end validation complete** (see below).
- Runnable shell (adapted to .NET MAUI net10.0-windows using the available `maui-windows` workload + pre-installed Windows App Runtimes 1.6/1.7/1.8).
  - UI: nav buttons + switchable panes (Chat with send stub + log, Suggestions list, Dashboard, Settings with transport note).
  - "Test Transport" button + standalone `src/ui/TransportTest/` exercise the **exact** named-pipe + JSON client stub from `docs/ipc-spike.md`.
  - **Success**: With the Python server running, the button now returns a real Pong and displays the echoed nonce from the PR 3 agent.
  - Chat pane now sends real user messages over the named pipe to the PR 3 agent (router decides local vs cloud stub) and displays the agent's response.
- See `docs/ipc-spike.md` for transport recommendation.

**Local validation complete (PR2 + PR3)**: The "Test Transport" button in the shell (launched via desktop shortcut or `run-ui.cmd` / `run-ui.bat`) successfully performs a full round-trip over the named pipe to the Python agent server and shows:

`✓ Connected! Server responded (PR 3 Python agent). Nonce: <real-nonce-from-server> echoed.`

Server logs the connect + full frame + "Pong -> nonce=...".

Real chat is also working: messages typed in the Chat pane are sent to the PR 3 agent, routed (currently always "local"), and the response appears in the conversation.

The adaptation was performed after explicit scan of available software (per user request) when the documented WindowsAppSDK workload could not be installed. PR 2/3 work was done on feature branches and PRs created via gh.

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

Run the PR 2 shell (Windows) — **preferred method** (no PowerShell needed):

Double-click `run-ui.cmd` (root) or `scripts\run-ui.cmd` (or the desktop shortcut you created from it).  
The script does the taskkill, build, and launches the executable directly.

Manual alternative (if you prefer the command line):

```powershell
cd src\ui\WinUI3App
dotnet build -c Release -f net10.0-windows10.0.19041.0
dotnet run -f net10.0-windows10.0.19041.0 -c Release --no-build --no-launch-profile
```

Tip: Use `--no-launch-profile` if the default launch settings interfere with the window appearing.

Run transport client validator (no server needed):

```powershell
dotnet run --project ..\TransportTest\TransportTest.csproj -c Release
```

Run the PR 3 Python agent server (requires pywin32 on Windows):

**Note on "All pipe instances are busy" (error 231) or cmd not working:**
- This happens if a previous server didn't clean up the pipe (e.g. Ctrl+C or crash).
- The updated run-server.bat and .cmd now auto-kill previous python.exe first.
- Use the root `run-server.bat` (double-click or `cmd /c "C:\Users\chris\my-project\run-server.bat"`) -- it works from any dir and prints debug.
- Correct import test (copy exactly, note the quoting):
  cmd /c ""C:\Users\chris\AppData\Local\Python\pythoncore-3.14-64\python.exe" -c "import agent.server as s; print('Import OK'); print('PIPE:', s.PIPE_NAME)" "
- If still issues, kill manually: taskkill /F /IM python.exe

The helpers are in scripts/ and root .bat (we created/updated them for you).

**Easiest (use the root .bat to avoid any relative path or scripts\ issues with cmd /c .\\scripts...):**

**Just double-click these files (in order) from File Explorer:**

1. `scripts\install-agent.cmd`  (installs pywin32 + the package using the correct Python)
2. `scripts\run-server.cmd`     (starts the server in a window that stays open)
3. `run-ui.cmd` (or `run-ui.bat`, or the ones in `scripts\`) — launches the MAUI shell (builds + starts the exe).  
   After the first successful run, right-click the chosen launcher → **Send to > Desktop (create shortcut)** so you have a permanent one-click desktop launcher. No PowerShell required.

These .cmd files are in `C:\Users\chris\my-project\` (root) and `scripts\` and handle paths/directories automatically. Double-clicking them runs them via cmd.exe, which has no script execution policy restrictions.

After the server window prints the "listening on \\.\pipe\my-agent-ipc" message, launch the shell via its .cmd (or desktop shortcut) and click "Test Transport".

**If you are already in PowerShell and want to run the .ps1 versions (or hit the "running scripts is disabled" error):**

From your PowerShell prompt (in the project dir):

```powershell
cmd /c .\scripts\install-agent.cmd
```

Then in a **separate** PowerShell or Command Prompt window:

```powershell
cmd /c .\scripts\run-server.cmd
```

Or use bypass for one session only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-agent.ps1
# Then in new window:
powershell -ExecutionPolicy Bypass -File .\scripts\run-server.ps1
```

Keep the server running. It listens on `\\.\pipe\my-agent-ipc` and responds to Pings with real Pong.

Now run the shell in **another terminal** + click "Test Transport" — it should succeed with:
"✓ Connected! Server responded (PR 3 Python agent). Nonce echoed."

See the server console for `[server] client connected` and `[server] Pong -> nonce=...` logs.

## Next

PR 3 work is complete on the stacked branch (real chat over pipe, router integration, visible route decisions, desktop launchers, full transport validation).

Per the plan: Merge the validated Shell PR (#2) on GitHub first. Then create `execute-plan/pr-4-local-models-cloud-router` from main and start the initial small PR4 commit (improve local backend, make routing decisions affect responses more, log route visibly, basic orchestration).

Desktop launchers remain the easiest way to test going forward.

## License

TBD
