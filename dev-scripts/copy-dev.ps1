# Copies the plugin staged by stage-dev.ps1 into the game, and optionally switches Debug settings in the mod's
# config. Every file it replaces is backed up first, so each run can be undone. Dev only.
#
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 [-GameDir "D:\Games\Bug Fables"]
#       [-DebugOn EntityDump,ScriptDump] [-DebugOff GrantProbe] [-DebugSet DevCommandFile=C:\path\cmds.txt]
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 -Restore <backup folder name>
#
# What it writes in the game, and nothing else:
#   BepInEx\scripts\BugFablesAP.dll and .pdb      our own build, rebuilt from the repo at any time
#   BepInEx\config\bugfables.archipelago.cfg      only the [Debug] keys named, and only with -DebugOn/-DebugOff
# The client libraries (BepInEx\plugins) and ScriptEngine's config stay a manual, once-per-setup copy of
# stage\setup (agent_docs/development.md).
#
# Backups go to stage\backup\<time>\ in the repo (gitignored), holding the files exactly as they were.
# -Restore puts one back. Why this shape: an overwrite with no copy kept is what Claude Code's auto mode
# refused twice (agent_docs/log.md, 2026-09-24).
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables',
    [string[]]$DebugOn = @(),
    [string[]]$DebugOff = @(),
    # Text settings, as Key=Value (e.g. DevCommandFile=C:\path\cmds.txt). One per -DebugSet; not comma-split.
    [string[]]$DebugSet = @(),
    [string]$Restore = ''
)
$ErrorActionPreference = 'Stop'
# Through `powershell -File`, "-DebugOn A,B" arrives as one string "A,B", not two: split it (2026-09-24,
# that wrote a key literally named "MapDump,ScriptDump").
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

# The plugin. The pdb has to travel with the DLL: ScriptEngine refuses a plugin without one, silently.
$staged = Join-Path $stage 'every-build\BepInEx\scripts'
foreach ($f in 'BugFablesAP.dll', 'BugFablesAP.pdb') {
    if (-not (Test-Path (Join-Path $staged $f))) { throw "$f isn't staged: run stage-dev.ps1 first" }
}
Backup 'BugFablesAP.dll'
Backup 'BugFablesAP.pdb'
$scripts = Join-Path $GameDir 'BepInEx\scripts'
# The running game may be reading the DLL at that instant (seen 2026-09-24), so retry briefly.
foreach ($f in 'BugFablesAP.pdb', 'BugFablesAP.dll') {
    for ($try = 1; $try -le 10; $try++) {
        try { Copy-Item (Join-Path $staged $f) (Join-Path $scripts $f) -Force; break }
        catch { if ($try -eq 10) { throw }; Start-Sleep -Milliseconds 300 }
    }
}
$hash = (Get-FileHash (Join-Path $scripts 'BugFablesAP.dll') -Algorithm SHA256).Hash.Substring(0, 12)
Write-Output "copied BugFablesAP.dll (sha256 $hash...) into BepInEx\scripts; DevReload will pick it up"

# Debug settings: set only the named keys inside [Debug], adding a key the file doesn't have yet.
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
    # UTF-8 without a BOM, as BepInEx writes it (Set-Content's UTF8 adds one in Windows PowerShell).
    [System.IO.File]::WriteAllLines($cfg, $lines)
    # Read back what the file now says, rather than trusting what was written.
    foreach ($k in @($want.Keys)) {
        $line = (Get-Content $cfg) | Where-Object { $_ -match "^$([regex]::Escape($k))\s*=" } | Select-Object -First 1
        Write-Output "config: $line"
    }
}
Write-Output "backup: stage\backup\$(Split-Path -Leaf $backup) (undo with -Restore $(Split-Path -Leaf $backup))"
