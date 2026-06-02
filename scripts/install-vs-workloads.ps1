# Helper script to add the exact workloads needed for the WinUI 3 PR 2 shell
# Run this after the base Visual Studio 2022 Community is present via winget.
# This matches the "Exact steps" in docs/setup.md

$ErrorActionPreference = 'Stop'

$vsInstaller = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vs_installer.exe"

if (-not (Test-Path $vsInstaller)) {
    Write-Error "vs_installer.exe not found at expected location: $vsInstaller"
    exit 1
}

Write-Host "=== Starting VS Installer modify for required workloads ===" -ForegroundColor Cyan
Write-Host "This will download and install:"
Write-Host "  - .NET Desktop workload"
Write-Host "  - Windows App SDK component group"
Write-Host "  - Windows 11 SDK 22621 (compatible with our net8.0-windows10.0.19041 TFM)"
Write-Host ""
Write-Host "This step commonly takes 30-120+ minutes depending on your connection and disk speed."
Write-Host "The process runs with --quiet --wait and will not require further interaction."
Write-Host ""

& $vsInstaller modify `
    --installPath "C:\Program Files\Microsoft Visual Studio\2022\Community" `
    --add Microsoft.VisualStudio.Workload.NetDesktop `
    --add Microsoft.VisualStudio.ComponentGroup.WindowsAppSDK `
    --add Microsoft.VisualStudio.Component.Windows11SDK.22621 `
    --quiet `
    --wait `
    --norestart `
    2>&1

$exit = $LASTEXITCODE
if ($exit -eq 0) {
    Write-Host "`n=== Workload modification completed successfully ===" -ForegroundColor Green
} else {
    Write-Warning "vs_installer modify exited with code $exit (this can be normal for some component states; check the installer log if build fails later)."
}

exit $exit
