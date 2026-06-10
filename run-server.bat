@echo off
setlocal
echo [run-server.bat] Killing any previous python.exe holding the pipe...
taskkill /F /IM python.exe 2>nul
echo [run-server.bat] Starting the PR3 agent server...
set "PROJECT=C:\Users\chris\my-project"
set "PY=C:\Users\chris\AppData\Local\Python\pythoncore-3.14-64\python.exe"
cd /d "%PROJECT%"
echo [run-server.bat] Using Python: %PY%
echo [run-server.bat] Project: %PROJECT%
echo [run-server.bat] Running: %PY% -m agent.server
echo [run-server.bat] (Press Ctrl+C to stop the server)
%PY% -m agent.server
echo [run-server.bat] Server stopped.
pause
endlocal
