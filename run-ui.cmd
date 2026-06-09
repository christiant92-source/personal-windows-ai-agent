@echo off
setlocal
echo [run-ui.cmd] Killing any previous WinUI3App.exe ...
taskkill /F /IM WinUI3App.exe 2>nul

set "PROJECT=C:\Users\chris\my-project"
set "UI_DIR=%PROJECT%\src\ui\WinUI3App"

cd /d "%UI_DIR%"
echo [run-ui.cmd] Project: %PROJECT%
echo [run-ui.cmd] Building Release net10.0-windows10.0.19041.0 ...
dotnet build -c Release -f net10.0-windows10.0.19041.0 --nologo -v q

if errorlevel 1 (
    echo [run-ui.cmd] Build failed. Press any key to close.
    pause >nul
    goto :end
)

set "EXE=%UI_DIR%\bin\Release\net10.0-windows10.0.19041.0\win-x64\WinUI3App.exe"
if not exist "%EXE%" (
    echo [run-ui.cmd] ERROR: Executable not found at:
    echo   %EXE%
    echo Build may have used a different layout or you need to build once first.
    pause >nul
    goto :end
)

echo [run-ui.cmd] Launching shell...
start "My Agent (PR 2 Shell)" "%EXE%"

echo [run-ui.cmd] Shell window should appear (or is starting).
echo Close this console when you no longer need the log.
pause >nul

:end
endlocal
