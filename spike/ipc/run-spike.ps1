<#
.SYNOPSIS
    Convenience wrapper to run the IPC spike measurement harness.
#>

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "Running IPC spike measurement (this may take 30-90 seconds)..." -ForegroundColor Cyan
& py -3.14 measure.py

Write-Host ""
Write-Host "Results also written to spike-results.json (if generated)." -ForegroundColor Yellow
