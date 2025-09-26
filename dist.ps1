param(
    [string]$OutputRoot,
    [string]$DistRoot
)

# Resolve script root compatibly with Windows PowerShell 5.x and PowerShell 7+
$scriptRoot = if ($PSScriptRoot) { 
    $PSScriptRoot 
} elseif ($PSCommandPath) { 
    Split-Path -Parent $PSCommandPath 
} else { 
    Split-Path -Parent $MyInvocation.MyCommand.Path 
}

if (-not $OutputRoot -or [string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $scriptRoot 'Output'
}

if (-not $DistRoot -or [string]::IsNullOrWhiteSpace($DistRoot)) {
    $DistRoot = Join-Path $scriptRoot 'dist'
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-LatestRunFolder([string]$root) {
    if (-not (Test-Path -Path $root -PathType Container)) {
        throw "Output root not found: $root"
    }

    $dirs = Get-ChildItem -Path $root -Directory -ErrorAction Stop
    if (-not $dirs -or $dirs.Count -eq 0) { return $null }

    $pattern = '^[0-9]{8}-[0-9]{6}Z$'
    $matching = $dirs | Where-Object { $_.Name -match $pattern } | Sort-Object Name -Descending
    if ($matching -and $matching.Count -gt 0) { return $matching[0] }

    return ($dirs | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
}

try {
    $runDir = Get-LatestRunFolder -root $OutputRoot
    if (-not $runDir) {
        Write-Error "No run folders found in $OutputRoot"
        exit 1
    }

    Write-Host "Selected run: $($runDir.Name)" -ForegroundColor Cyan

    if (-not (Test-Path -Path $DistRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $DistRoot | Out-Null
    } else {
        $gitkeep = Join-Path $DistRoot '.gitkeep'
        $hasGitkeep = Test-Path $gitkeep
        Get-ChildItem -Path $DistRoot -Force | Where-Object { $_.Name -ne '.gitkeep' } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        if ($hasGitkeep -and -not (Test-Path $gitkeep)) {
            New-Item -ItemType File -Path $gitkeep | Out-Null
        }
    }

    $copied = 0
    $locales = Get-ChildItem -Path $runDir.FullName -Directory
    if (-not $locales -or $locales.Count -eq 0) {
        Write-Warning "No locale folders found in $($runDir.FullName)"
    }

    foreach ($loc in $locales) {
        $sourceHtml = Join-Path $loc.FullName 'index.html'
        if (Test-Path $sourceHtml) {
            $targetDir = Join-Path $DistRoot $loc.Name
            if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir | Out-Null }
            Copy-Item -Path $sourceHtml -Destination (Join-Path $targetDir 'index.html') -Force
            Write-Host "Copied $($loc.Name)/index.html" -ForegroundColor Green
            $copied++
        } else {
            Write-Warning "Missing index.html for locale '$($loc.Name)'"
        }
    }

    if ($copied -gt 0) {
        Write-Host "Done. Copied $copied locale(s) to $DistRoot" -ForegroundColor Cyan
        exit 0
    } else {
        Write-Error "Nothing copied."
        exit 2
    }
}
catch {
    Write-Error $_
    exit 100
}
