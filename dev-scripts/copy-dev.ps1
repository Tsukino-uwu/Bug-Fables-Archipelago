# Copies the staged plugin into the game and optionally sets [Debug] config keys, backing up each file it replaces.
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 [-GameDir <dir>]
#       [-DebugOn A,B] [-DebugOff C] [-DebugSet Key=Value]
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 -Restore <folder under stage\backup>
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables',
    [string[]]$DebugOn = @(),
    [string[]]$DebugOff = @(),
    # Key=Value, one per -DebugSet; not comma-split.
    [string[]]$DebugSet = @(),
    [string]$Restore = ''
)
$ErrorActionPreference = 'Stop'
# Through `powershell -File`, "-DebugOn A,B" arrives as one string.
$DebugOn = @($DebugOn | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$DebugOff = @($DebugOff | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$repo = Split-Path -Parent $PSScriptRoot
$stage = Join-Path $repo 'stage'
$backupRoot = Join-Path $stage 'backup'
$targets = @{
    'BugFablesAP.dll'               = 'BepInEx\scripts\BugFablesAP.dll'
    'BugFablesAP.pdb'               = 'BepInEx\scripts\BugFablesAP.pdb'
    'bugfables.archipelago.cfg'     = 'BepInEx\config\bugfables.archipelago.cfg'
}

if (-not (Test-Path (Join-Path $GameDir 'BepInEx'))) { throw "no BepInEx folder in $GameDir" }

if ($Restore) {
    $from = Join-Path $backupRoot $Restore
    if (-not (Test-Path $from)) { throw "no backup $from" }
    foreach ($name in $targets.Keys) {
        $src = Join-Path $from $name
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $GameDir $targets[$name]) -Force
            Write-Output "restored $($targets[$name]) from backup $Restore"
        }
    }
    exit 0
}

$backup = Join-Path $backupRoot (Get-Date -Format 'yyyyMMdd-HHmmss')
New-Item -ItemType Directory -Force $backup | Out-Null
function Backup([string]$name) {
    $path = Join-Path $GameDir $targets[$name]
    if (Test-Path $path) { Copy-Item $path (Join-Path $backup $name) -Force }
}

# ScriptEngine silently refuses a plugin without its pdb.
$staged = Join-Path $stage 'every-build\BepInEx\scripts'
foreach ($f in 'BugFablesAP.dll', 'BugFablesAP.pdb') {
    if (-not (Test-Path (Join-Path $staged $f))) { throw "$f isn't staged: run stage-dev.ps1 first" }
}
Backup 'BugFablesAP.dll'
Backup 'BugFablesAP.pdb'
$scripts = Join-Path $GameDir 'BepInEx\scripts'
# The running game may be reading the DLL, so retry briefly.
foreach ($f in 'BugFablesAP.pdb', 'BugFablesAP.dll') {
    for ($try = 1; $try -le 10; $try++) {
        try { Copy-Item (Join-Path $staged $f) (Join-Path $scripts $f) -Force; break }
        catch { if ($try -eq 10) { throw }; Start-Sleep -Milliseconds 300 }
    }
}
# DevReload watches the write time, which Copy-Item keeps from the source.
(Get-Item (Join-Path $scripts 'BugFablesAP.dll')).LastWriteTimeUtc = [DateTime]::UtcNow
$hash = (Get-FileHash (Join-Path $scripts 'BugFablesAP.dll') -Algorithm SHA256).Hash.Substring(0, 12)
Write-Output "copied BugFablesAP.dll (sha256 $hash...) into BepInEx\scripts; DevReload will pick it up"

if ($DebugOn.Count -gt 0 -or $DebugOff.Count -gt 0 -or $DebugSet.Count -gt 0) {
    $cfg = Join-Path $GameDir $targets['bugfables.archipelago.cfg']
    if (-not (Test-Path $cfg)) { throw "no $cfg yet: start the game once with the mod so BepInEx writes it" }
    Backup 'bugfables.archipelago.cfg'
    $want = [ordered]@{}
    foreach ($k in $DebugOn) { $want[$k] = 'true' }
    foreach ($k in $DebugOff) { $want[$k] = 'false' }
    foreach ($pair in $DebugSet) {
        $at = $pair.IndexOf('=')
        if ($at -lt 1) { throw "-DebugSet wants Key=Value, got '$pair'" }
        $want[$pair.Substring(0, $at).Trim()] = $pair.Substring($at + 1).Trim()
    }
    $lines = [System.Collections.Generic.List[string]](Get-Content $cfg)
    $start = $lines.IndexOf('[Debug]')
    if ($start -lt 0) { throw "no [Debug] section in $cfg" }
    $end = $lines.Count
    for ($i = $start + 1; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^\[') { $end = $i; break } }
    foreach ($k in @($want.Keys)) {
        $found = $false
        for ($i = $start + 1; $i -lt $end; $i++) {
            if ($lines[$i] -match "^$([regex]::Escape($k))\s*=") { $lines[$i] = "$k = $($want[$k])"; $found = $true; break }
        }
        if (-not $found) { $lines.Insert($end, "$k = $($want[$k])"); $lines.Insert($end, ''); $end += 2 }
    }
    # UTF-8 without a BOM, as BepInEx writes it.
    [System.IO.File]::WriteAllLines($cfg, $lines)
    foreach ($k in @($want.Keys)) {
        $line = (Get-Content $cfg) | Where-Object { $_ -match "^$([regex]::Escape($k))\s*=" } | Select-Object -First 1
        Write-Output "config: $line"
    }
}
Write-Output "backup: stage\backup\$(Split-Path -Leaf $backup) (undo with -Restore $(Split-Path -Leaf $backup))"
