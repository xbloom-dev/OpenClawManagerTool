param()

$ErrorActionPreference = "Stop"

try {
    chcp 65001 > $null
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    [Console]::InputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
} catch {}

function Test-IsAdministrator {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [System.Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Find-OpenClawRoot {
    $candidates = @(
        (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..") -ErrorAction SilentlyContinue),
        (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..") -ErrorAction SilentlyContinue),
        "E:\OpenClaw"
    ) | Where-Object { $_ } | ForEach-Object { $_.ToString() } | Select-Object -Unique

    foreach ($candidate in $candidates) {
        if (
            (Test-Path -LiteralPath (Join-Path $candidate "CodexWorkspace\.git")) -or
            (Test-Path -LiteralPath (Join-Path $candidate "ClaudeWorkspace\.git")) -or
            (Test-Path -LiteralPath (Join-Path $candidate "OpenClawManager\.git"))
        ) {
            return $candidate
        }
    }

    $fallback = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..") -ErrorAction SilentlyContinue
    if ($fallback) { return $fallback.Path }
    return $PSScriptRoot
}

function Get-GitDirectories {
    $root = Find-OpenClawRoot
    foreach ($name in @("CodexWorkspace", "ClaudeWorkspace", "OpenClawManager")) {
        $gitDir = Join-Path $root (Join-Path $name ".git")
        if (Test-Path -LiteralPath $gitDir) {
            $gitDir
        }
    }
}

if (-not (Test-IsAdministrator)) {
    Write-Host "Git ACL repair must be run as Administrator." -ForegroundColor Red
    exit 1
}

$gitDirs = @(Get-GitDirectories)
if ($gitDirs.Count -eq 0) {
    Write-Host "No OpenClaw .git directories were found." -ForegroundColor Yellow
    exit 0
}

$grantRules = @(
    "${env:USERNAME}:(OI)(CI)F",
    "BUILTIN\Administrators:(OI)(CI)F",
    "NT AUTHORITY\SYSTEM:(OI)(CI)F"
)

foreach ($gitDir in $gitDirs) {
    Write-Host ""
    Write-Host "Repairing $gitDir" -ForegroundColor White
    takeown /F $gitDir /R /D Y | Out-Null
    icacls $gitDir /inheritance:r /T /C /Q | Out-Null
    icacls $gitDir /reset /T /C /Q | Out-Null
    icacls $gitDir /grant:r $grantRules /T /C /Q | Out-Null
    Write-Host "ACL repair attempted." -ForegroundColor Green
}
