param()

$ErrorActionPreference = "Stop"

try {
    chcp 65001 > $null
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    [Console]::InputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
} catch {}

function Write-Title {
    param([string]$Title)
    Write-Host ""
    Write-Host ("=" * 64) -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host ("=" * 64) -ForegroundColor Cyan
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

function Get-Workspaces {
    $root = Find-OpenClawRoot
    foreach ($name in @("CodexWorkspace", "ClaudeWorkspace", "OpenClawManager")) {
        $path = Join-Path $root $name
        if (Test-Path -LiteralPath (Join-Path $path ".git")) {
            [pscustomobject]@{
                Name = $name
                Path = $path
            }
        }
    }
}

function Invoke-InRepo {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][scriptblock]$Command
    )

    Push-Location $Path
    try {
        & $Command
    }
    finally {
        Pop-Location
    }
}

Write-Title "Sync OpenClaw workspaces"
$workspaces = @(Get-Workspaces)
if ($workspaces.Count -eq 0) {
    Write-Host "No OpenClaw workspaces were found." -ForegroundColor Yellow
    exit 0
}

foreach ($workspace in $workspaces) {
    Write-Host ""
    Write-Host "Syncing $($workspace.Name)..." -ForegroundColor White
    Invoke-InRepo $workspace.Path {
        $dirty = git status --porcelain
        if ($dirty) {
            Write-Host "Working tree has local changes; fetch only." -ForegroundColor Yellow
            git fetch --prune
            git status --short --branch
            return
        }

        git fetch --prune
        if ($LASTEXITCODE -ne 0) { throw "git fetch failed" }

        $upstream = git rev-parse --abbrev-ref --symbolic-full-name "@{u}" 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($upstream)) {
            git pull --ff-only
            if ($LASTEXITCODE -ne 0) { throw "git pull --ff-only failed" }
        } else {
            Write-Host "No upstream configured; fetch completed." -ForegroundColor Yellow
        }

        git status --short --branch
    }
}
