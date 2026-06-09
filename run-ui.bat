@echo off
setlocal
echo [run-ui.bat] Killing any previous WinUI3App.exe ...
taskkill /F /IM WinUI3App.exe 2>nul

set "PROJECT=C:\Users\chris\my-project"
set "UI_DIR=%PROJECT%\src\ui\WinUI3App"

cd /d "%UI_DIR%"
echo [run-ui.bat] Building MAUI shell (Release, net10.0-windows10.0.19041.0)...
dotnet build -c Release -f net10.0-windows10.0.19041.0 --nologo -v q

if errorlevel 1 (
    echo [run-ui.bat] Build failed.
    pause
    goto :eof
)

set "EXE=%UI_DIR%\bin\Release\net10.0-windows10.0.19041.0\win-x64\WinUI3App.exe"
if not exist "%EXE%" (
    echo [run-ui.bat] ERROR: Executable not found at %EXE%
    pause
    goto :eof
)

echo [run-ui.bat] Launching shell...
start "My Agent (PR 2 Shell)" "%EXE%"

echo [run-ui.bat] Shell launched. This window can be closed.
pause
endlocal
