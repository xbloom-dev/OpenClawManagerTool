param(
    [ValidateSet("", "sync", "diag", "acl", "build", "test", "check")]
    [string]$Action = "",
    [switch]$NoAdminPrompt
)

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

    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..") -ErrorAction SilentlyContinue).Path
}

function Get-Workspaces {
    $root = Find-OpenClawRoot
    $names = @("CodexWorkspace", "ClaudeWorkspace", "OpenClawManager")
    foreach ($name in $names) {
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

function Invoke-CompanionScript {
    param(
        [Parameter(Mandatory = $true)][string]$ScriptName
    )

    $scriptPath = Join-Path $PSScriptRoot $ScriptName
    if (-not (Test-Path -LiteralPath $scriptPath)) {
        throw "Required script was not found: $scriptPath"
    }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $scriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "$ScriptName failed with exit code $LASTEXITCODE"
    }
}

function Show-WorkspaceStatus {
    Write-Title "OpenClaw workspace status"
    $workspaces = @(Get-Workspaces)
    if ($workspaces.Count -eq 0) {
        Write-Host "No OpenClaw workspaces were found." -ForegroundColor Yellow
        return
    }

    foreach ($workspace in $workspaces) {
        Write-Host ""
        Write-Host $workspace.Name -ForegroundColor White
        Write-Host $workspace.Path -ForegroundColor DarkGray
        Invoke-InRepo $workspace.Path {
            $branch = (git rev-parse --abbrev-ref HEAD).Trim()
            $commit = (git rev-parse --short HEAD).Trim()
            Write-Host "Branch: $branch  Commit: $commit"
            git status --short --branch
        }
    }
}

function Sync-Workspaces {
    Invoke-CompanionScript "Sync-Workspaces.ps1"
}

function Repair-GitAcl {
    Invoke-CompanionScript "Fix-GitACL.ps1"
}

function Build-Project {
    Write-Title "Build OpenClaw Manager"
    $repo = (Get-Workspaces | Where-Object { Test-Path -LiteralPath (Join-Path $_.Path "OpenClawManager.sln") } | Select-Object -First 1)
    if ($null -eq $repo) {
        Write-Host "OpenClawManager.sln was not found." -ForegroundColor Yellow
        return
    }

    Invoke-InRepo $repo.Path {
        dotnet build .\OpenClawManager.sln --configuration Release
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    }
}

function Test-Project {
    Write-Title "Run TokenService tests"
    $repo = (Get-Workspaces | Where-Object { Test-Path -LiteralPath (Join-Path $_.Path "TokenService.Tests\TokenService.Tests.csproj") } | Select-Object -First 1)
    if ($null -eq $repo) {
        Write-Host "TokenService test project was not found." -ForegroundColor Yellow
        return
    }

    Invoke-InRepo $repo.Path {
        dotnet run --project .\TokenService.Tests\TokenService.Tests.csproj --configuration Release
        if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    }
}

function Invoke-Action {
    param([string]$SelectedAction)
    switch ($SelectedAction) {
        "sync" { Sync-Workspaces }
        "diag" { Show-WorkspaceStatus }
        "acl" { Repair-GitAcl }
        "build" { Build-Project }
        "test" { Test-Project }
        "check" {
            Show-WorkspaceStatus
            Build-Project
            Test-Project
        }
        default { Show-Menu }
    }
}

function Show-Menu {
    while ($true) {
        Write-Title "OpenClaw Tools"
        Write-Host "1. Sync workspaces"
        Write-Host "2. Workspace status"
        Write-Host "3. Repair Git ACL"
        Write-Host "4. Build"
        Write-Host "5. TokenService tests"
        Write-Host "6. Full check"
        Write-Host "Q. Quit"
        Write-Host ""
        $choice = Read-Host "Select action"
        switch ($choice.Trim().ToLowerInvariant()) {
            "1" { Sync-Workspaces }
            "2" { Show-WorkspaceStatus }
            "3" { Repair-GitAcl }
            "4" { Build-Project }
            "5" { Test-Project }
            "6" { Show-WorkspaceStatus; Build-Project; Test-Project }
            "q" { return }
            default { Write-Host "Unknown selection." -ForegroundColor Yellow }
        }
        Write-Host ""
        Read-Host "Press Enter to continue"
    }
}

try {
    Invoke-Action $Action
    Write-Host ""
    Write-Host "Done." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "OpenClaw Tools failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
