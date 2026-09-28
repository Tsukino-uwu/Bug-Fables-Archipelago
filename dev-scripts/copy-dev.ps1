# Copies the staged plugin into the game and optionally sets config keys, backing up each file it replaces.
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 [-GameDir <dir>]
#       [-DebugOn A,B] [-DebugOff C] [-DebugSet Key=Value] [-ConfigSet Section.Key=Value]
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 -Restore <folder under stage\backup>
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 -Layout Release|Dev   (game closed)
#   powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1 -Status   (which build the game runs; copies nothing)
param(
    # Release: the dev copies out, release\mod in, as a player's install; Dev: back to the hot-reload setup.
    [ValidateSet('', 'Release', 'Dev')][string]$Layout = '',
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables',
    [string[]]$DebugOn = @(),
    [string[]]$DebugOff = @(),
    # Key=Value, one per -DebugSet; not comma-split.
    [string[]]$DebugSet = @(),
    # Section.Key=Value for any other section, e.g. Archipelago.RandomizerEnabled=true for a test; one per -ConfigSet.
    [string[]]$ConfigSet = @(),
    [string]$Restore = '',
    [switch]$Status
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

# DevReload's one line: the loaded build's hash (as printed below for a copy), or what a new copy waits for.
function Show-ReloadStatus {
    $file = Join-Path $GameDir 'BepInEx\bugfablesap-reload.txt'
    if (Test-Path $file) { Write-Output "game: $((Get-Content $file -Raw).Trim())" }
    else { Write-Output "game: no reload status yet (the game not started, or a plugin from before the status file)" }
}
if ($Status) { Show-ReloadStatus; return }

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
# Two runs in one second would share a folder, and a later move would land inside an earlier one.
for ($n = 2; Test-Path $backup; $n++) { $backup = Join-Path $backupRoot "$(Get-Date -Format 'yyyyMMdd-HHmmss')-$n" }
New-Item -ItemType Directory -Force $backup | Out-Null

if ($Layout) {
    if (Get-Process 'Bug Fables' -ErrorAction SilentlyContinue) { throw 'close the game first: BepInEx holds the plugin DLLs' }
    $libs = 'Archipelago.MultiClient.Net.dll', 'websocket-sharp.dll', 'Newtonsoft.Json.dll'
    # Both layouts at once load the plugin twice, so each move takes the other layout's files out first.
    $devFiles = @('BepInEx\scripts\BugFablesAP.dll', 'BepInEx\scripts\BugFablesAP.pdb') + ($libs | ForEach-Object { "BepInEx\plugins\$_" })
    $releaseDir = 'BepInEx\plugins\BugFablesAP'
    function MoveOut([string]$rel) {
        $path = Join-Path $GameDir $rel
        if (-not (Test-Path $path)) { return }
        $dest = Join-Path $backup $rel
        New-Item -ItemType Directory -Force (Split-Path -Parent $dest) | Out-Null
        Move-Item $path $dest -Force
        Write-Output "moved out $rel"
    }
    if ($Layout -eq 'Release') {
        $src = Join-Path $repo 'release\mod\BepInEx'
        if (-not (Test-Path (Join-Path $src 'plugins\BugFablesAP\BugFablesAP.dll'))) { throw 'release\mod is empty: run build-release.ps1 first' }
        foreach ($rel in $devFiles + $releaseDir) { MoveOut $rel }
        Copy-Item $src (Join-Path $GameDir '.') -Recurse -Force
        Get-ChildItem (Join-Path $GameDir $releaseDir) -File | ForEach-Object { Write-Output "installed $releaseDir\$($_.Name)" }
    } else {
        $every = Join-Path $stage 'every-build\BepInEx\scripts'
        $setup = Join-Path $stage 'setup\BepInEx\plugins'
        foreach ($f in @('BugFablesAP.dll', 'BugFablesAP.pdb' | ForEach-Object { Join-Path $every $_ }) + ($libs | ForEach-Object { Join-Path $setup $_ })) {
            if (-not (Test-Path $f)) { throw "$f isn't staged: run stage-dev.ps1 first" }
        }
        MoveOut $releaseDir
        foreach ($f in 'BugFablesAP.dll', 'BugFablesAP.pdb') { Copy-Item (Join-Path $every $f) (Join-Path $GameDir "BepInEx\scripts\$f") -Force }
        foreach ($f in $libs) { Copy-Item (Join-Path $setup $f) (Join-Path $GameDir "BepInEx\plugins\$f") -Force }
        Write-Output 'installed the dev layout: the plugin in BepInEx\scripts, its libraries in BepInEx\plugins'
    }
    Write-Output "backup: stage\backup\$(Split-Path -Leaf $backup)"
    exit 0
}
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

if ($DebugOn.Count -gt 0 -or $DebugOff.Count -gt 0 -or $DebugSet.Count -gt 0 -or $ConfigSet.Count -gt 0) {
    $cfg = Join-Path $GameDir $targets['bugfables.archipelago.cfg']
    if (-not (Test-Path $cfg)) { throw "no $cfg yet: start the game once with the mod so BepInEx writes it" }
    Backup 'bugfables.archipelago.cfg'
    # Section -> ordered key -> value.
    $want = [ordered]@{ 'Debug' = [ordered]@{} }
    foreach ($k in $DebugOn) { $want['Debug'][$k] = 'true' }
    foreach ($k in $DebugOff) { $want['Debug'][$k] = 'false' }
    foreach ($pair in $DebugSet) {
        $at = $pair.IndexOf('=')
        if ($at -lt 1) { throw "-DebugSet wants Key=Value, got '$pair'" }
        $want['Debug'][$pair.Substring(0, $at).Trim()] = $pair.Substring($at + 1).Trim()
    }
    foreach ($pair in $ConfigSet) {
        if ($pair -notmatch '^\s*(\w+)\.(\w+)\s*=(.*)$') { throw "-ConfigSet wants Section.Key=Value, got '$pair'" }
        if (-not $want.Contains($Matches[1])) { $want[$Matches[1]] = [ordered]@{} }
        $want[$Matches[1]][$Matches[2]] = $Matches[3].Trim()
    }
    $lines = [System.Collections.Generic.List[string]](Get-Content $cfg)
    foreach ($section in @($want.Keys)) {
        if ($want[$section].Count -eq 0) { continue }
        $start = $lines.IndexOf("[$section]")
        if ($start -lt 0 -and $section -eq 'Debug') { throw "no [Debug] section in ${cfg}: the game runs a release build (-Layout Release), which has no dev settings" }
        if ($start -lt 0) { throw "no [$section] section in $cfg" }
        $end = $lines.Count
        for ($i = $start + 1; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^\[') { $end = $i; break } }
        foreach ($k in @($want[$section].Keys)) {
            $value = $want[$section][$k]
            $found = $false
            for ($i = $start + 1; $i -lt $end; $i++) {
                if ($lines[$i] -match "^$([regex]::Escape($k))\s*=") { $lines[$i] = "$k = $value"; $found = $true; break }
            }
            if (-not $found) { $lines.Insert($end, "$k = $value"); $lines.Insert($end, ''); $end += 2 }
        }
    }
    # UTF-8 without a BOM, as BepInEx writes it.
    [System.IO.File]::WriteAllLines($cfg, $lines)
    foreach ($section in @($want.Keys)) {
        foreach ($k in @($want[$section].Keys)) {
            $line = (Get-Content $cfg) | Where-Object { $_ -match "^$([regex]::Escape($k))\s*=" } | Select-Object -First 1
            Write-Output "config: [$section] $line"
        }
    }
}
Write-Output "backup: stage\backup\$(Split-Path -Leaf $backup) (undo with -Restore $(Split-Path -Leaf $backup))"
Show-ReloadStatus
