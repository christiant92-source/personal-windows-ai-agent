@echo off
setlocal
echo [run-ui.cmd] Killing any previous WinUI3App.exe ...
taskkill /F /IM WinUI3App.exe 2>nul

set "PROJECT=C:\Users\chris\my-project"
set "UI_DIR=%PROJECT%\src\ui\WinUI3App"

cd /d "%UI_DIR%"
echo [run-ui.cmd] Building the MAUI shell (Release, Windows TFM)...
dotnet build -c Release -f net10.0-windows10.0.19041.0 --nologo -v q

if errorlevel 1 (
    echo [run-ui.cmd] Build failed.
    pause
    goto :eof
)

set "EXE=%UI_DIR%\bin\Release\net10.0-windows10.0.19041.0\win-x64\WinUI3App.exe"
if not exist "%EXE%" (
    echo [run-ui.cmd] EXE not found. Expected:
    echo   %EXE%
    pause
    goto :eof
)

echo [run-ui.cmd] Launching %EXE% ...
start "My Agent (PR 2 Shell)" "%EXE%"

echo [run-ui.cmd] Shell launched. This window can be closed.
pause
endlocal
