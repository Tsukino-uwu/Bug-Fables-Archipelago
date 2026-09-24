# Builds the plugin and stages it inside the repo, in stage\, laid out like the game folder. Nothing here
# writes to the game install: copying the staged files in is a separate step.
#
#   powershell -ExecutionPolicy Bypass -File dev-scripts\stage-dev.ps1 [-GameDir "D:\Games\Bug Fables"]
#
# stage\every-build\BepInEx\  the plugin (DLL and pdb, for BepInEx\scripts). Copy this BepInEx folder onto the
#                             game folder after each build, with the game running or not: ScriptEngine
#                             hot-reloads it (DevReload notices the DLL's new timestamp).
# stage\setup\BepInEx\        the client libraries (for BepInEx\plugins) and ScriptEngine's config. Copy it
#                             once, and again only when the script says the libraries changed, with the game
#                             closed: a running game holds the libraries open.
#
# The game install is only read: the build compiles against its Assembly-CSharp.dll, and the libraries are
# compared with the ones already there.
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'mod\BugFablesAP\BugFablesAP.csproj'
$out = Join-Path $repo 'mod\BugFablesAP\bin\Debug'
$stage = Join-Path $repo 'stage'

& dotnet build $project -c Debug "-p:BugFablesDir=$GameDir" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "build failed ($LASTEXITCODE)" }

$scripts = Join-Path $stage 'every-build\BepInEx\scripts'
New-Item -ItemType Directory -Force $scripts | Out-Null
# The pdb has to travel with the DLL: ScriptEngine refuses a plugin without one, silently.
foreach ($f in 'BugFablesAP.dll', 'BugFablesAP.pdb') {
    Copy-Item (Join-Path $out $f) (Join-Path $scripts $f) -Force
}
# Copy-Item keeps the source's timestamp, and an unchanged build doesn't rewrite the DLL, so a restage would
# look like no change to DevReload (2026-09-24). Stamp the staged DLL; copying it into the game keeps the stamp.
(Get-Item (Join-Path $scripts 'BugFablesAP.dll')).LastWriteTimeUtc = [DateTime]::UtcNow

# Libraries go to BepInEx\plugins, not scripts: ScriptEngine loads every DLL in scripts again on each reload,
# and two copies of Newtonsoft.Json in one process is a type-identity trap.
$plugins = Join-Path $stage 'setup\BepInEx\plugins'
New-Item -ItemType Directory -Force $plugins | Out-Null
$changed = @()
foreach ($lib in 'Archipelago.MultiClient.Net.dll', 'websocket-sharp.dll', 'Newtonsoft.Json.dll') {
    $src = Join-Path $out $lib
    if (-not (Test-Path $src)) { throw "$lib is missing from the build output" }
    Copy-Item $src (Join-Path $plugins $lib) -Force
    $inGame = Join-Path $GameDir "BepInEx\plugins\$lib"
    if (-not (Test-Path $inGame) -or (Get-FileHash $src).Hash -ne (Get-FileHash $inGame).Hash) {
        $changed += $lib
    }
}

# ScriptEngine's config, so a changed DLL reloads by itself. The key names are the ones ScriptEngine r11.1
# generated in this game (agent_docs/log.md, 2026-09-24); its defaults are manual-only (watcher off,
# LoadOnStart off).
$config = Join-Path $stage 'setup\BepInEx\config'
New-Item -ItemType Directory -Force $config | Out-Null
@(
    '## Written by bug_fables_ap dev-scripts\stage-dev.ps1. Dev only.',
    '',
    '[AutoReload]',
    '## Off: the Mono in this game throws NotImplementedException from new FileSystemWatcher (2026-09-24),',
    '## which aborts ScriptEngine.Awake. DevReload in the plugin polls instead.',
    'EnableFileSystemWatcher = false',
    '## Seconds after the last file change before reloading, so the DLL and pdb land together.',
    'AutoReloadDelay = 2',
    'DumpAssemblies = false',
    '',
    '[General]',
    'LoadOnStart = true',
    'ReloadKey = F6',
    'QuietMode = false',
    'IncludeSubdirectories = false'
) | Set-Content -Path (Join-Path $config 'com.bepis.bepinex.scriptengine.cfg') -Encoding ascii

$dll = Get-Item (Join-Path $scripts 'BugFablesAP.dll')
$hash = (Get-FileHash $dll.FullName -Algorithm SHA256).Hash.Substring(0, 12)
Write-Output "staged BugFablesAP.dll ($($dll.Length) bytes, sha256 $hash...)"
Write-Output "  copy stage\every-build\BepInEx onto the game folder; DevReload will pick it up"
if (-not (Test-Path (Join-Path $GameDir 'BepInEx\plugins\ScriptEngine.dll'))) {
    Write-Output "  ScriptEngine.dll is not in the game's BepInEx\plugins: nothing will hot-reload the plugin"
}
if ($changed.Count -gt 0) {
    Write-Output "  libraries differ from the game's ($($changed -join ', ')): with the game closed, also copy stage\setup\BepInEx"
}
