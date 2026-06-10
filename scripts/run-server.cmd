@echo off
setlocal
echo [run-server.cmd] Killing any previous python.exe holding the pipe...
taskkill /F /IM python.exe 2>nul
echo [run-server.cmd] Starting the PR3 agent server...
set "PROJECT=C:\Users\chris\my-project"
set "PY=C:\Users\chris\AppData\Local\Python\pythoncore-3.14-64\python.exe"
cd /d "%PROJECT%"
echo [run-server.cmd] Using Python: %PY%
echo [run-server.cmd] Project: %PROJECT%
echo [run-server.cmd] Running: %PY% -m agent.server
echo [run-server.cmd] (The server will print listening message and stay running. Press Ctrl+C in this window to stop.)
%PY% -m agent.server
echo [run-server.cmd] Server stopped.
pause
endlocal
