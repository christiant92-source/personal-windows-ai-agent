<#
.SYNOPSIS
    Bootstrap Python development environment for the monorepo (PR 1+).

.DESCRIPTION
    Creates a .venv using Python 3.12 (recommended pin), installs ruff + mypy.
    Run this after following docs/setup.md for .NET + Python prerequisites.

.NOTES
    Requires Python 3.12+ registered with the py launcher (see docs/setup.md).
    Target machine on 2026-05-28 only had Python 3.14.3; 3.12 must be added for prod compatibility.
#>

param(
    [string]$PythonVersion = "3.12"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepoRoot = Split-Path -Parent $PSScriptRoot
$VenvPath = Join-Path $RepoRoot ".venv"

Write-Host "=== my-project Python setup (target: Python $PythonVersion) ===" -ForegroundColor Cyan

# Verify py launcher and requested version
$pyList = & py -0 2>&1
if (-not ($pyList -match $PythonVersion)) {
    Write-Warning "Python $PythonVersion not found via py launcher."
    Write-Host "Install it first (see docs/setup.md):" -ForegroundColor Yellow
    Write-Host "  py install $PythonVersion" -ForegroundColor Yellow
    Write-Host "  or winget / Microsoft Store for Python $PythonVersion" -ForegroundColor Yellow
    exit 1
}

Write-Host "Creating venv at $VenvPath ..."
if (Test-Path $VenvPath) {
    Write-Host "  (removing existing venv)"
    Remove-Item $VenvPath -Recurse -Force
}

& py -V:$PythonVersion -m venv $VenvPath

$venvPython = Join-Path $VenvPath "Scripts\python.exe"
$venvPip = Join-Path $VenvPath "Scripts\pip.exe"

Write-Host "Upgrading pip ..."
& $venvPython -m pip install --upgrade pip setuptools wheel

Write-Host "Installing dev tools (ruff, mypy) from pyproject.toml ..."
# Use relative extras syntax from repo root (avoids PowerShell array-index parsing of $RepoRoot[dev])
& $venvPip install -e ".[dev]"

Write-Host ""
Write-Host "=== Setup complete ===" -ForegroundColor Green
Write-Host "Activate with:  .\.venv\Scripts\Activate.ps1"
Write-Host "Lint:           ruff check ."
Write-Host "Format check:   ruff format --check ."
Write-Host "Typecheck:      mypy src"
Write-Host ""
Write-Host "Note: Production Python is pinned to 3.12 in pyproject.toml for library compatibility." -ForegroundColor Yellow
