# Builds the plugin and stages it in stage\ (every-build: the plugin; setup: libraries and ScriptEngine's config).
# Only reads the game install.
#   powershell -ExecutionPolicy Bypass -File dev-scripts\stage-dev.ps1 [-GameDir <dir>]
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
# ScriptEngine silently refuses a plugin without its pdb.
foreach ($f in 'BugFablesAP.dll', 'BugFablesAP.pdb') {
    Copy-Item (Join-Path $out $f) (Join-Path $scripts $f) -Force
}
# An unchanged build keeps its old write time, which DevReload would read as no change.
(Get-Item (Join-Path $scripts 'BugFablesAP.dll')).LastWriteTimeUtc = [DateTime]::UtcNow

# Not scripts: ScriptEngine reloads every DLL there, and two Newtonsoft.Json copies break type identity.
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

# ScriptEngine's defaults are manual-only; this config makes a changed DLL reload by itself.
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
