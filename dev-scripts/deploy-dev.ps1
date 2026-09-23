# Builds the plugin and copies it into the game's BepInEx\scripts folder, where ScriptEngine hot-reloads it
# in a running game. Dev only: a release build goes to BepInEx\plugins instead.
#
#   powershell -ExecutionPolicy Bypass -File dev-scripts\deploy-dev.ps1 [-GameDir "D:\Games\Bug Fables"]
#
# It also writes ScriptEngine's config so a changed DLL reloads by itself. The key names are the ones
# ScriptEngine r11.1 generated in this game (agent_docs/log.md, 2026-09-24); its defaults are manual-only
# (watcher off, LoadOnStart off).
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'mod\BugFablesAP\BugFablesAP.csproj'
$out = Join-Path $repo 'mod\BugFablesAP\bin\Debug'

if (-not (Test-Path (Join-Path $GameDir 'BepInEx\plugins\ScriptEngine.dll'))) {
    throw "ScriptEngine.dll is not in $GameDir\BepInEx\plugins: nothing would reload the plugin."
}

& dotnet build $project -c Debug "-p:BugFablesDir=$GameDir" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "build failed ($LASTEXITCODE)" }

$scripts = Join-Path $GameDir 'BepInEx\scripts'
New-Item -ItemType Directory -Force $scripts | Out-Null
# The pdb has to travel with the DLL: ScriptEngine refuses a plugin without one, silently.
foreach ($f in 'BugFablesAP.dll', 'BugFablesAP.pdb') {
    Copy-Item (Join-Path $out $f) (Join-Path $scripts $f) -Force
}

$cfg = Join-Path $GameDir 'BepInEx\config\com.bepis.bepinex.scriptengine.cfg'
@(
    '## Written by bug_fables_ap dev-scripts\deploy-dev.ps1. Dev only.',
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
) | Set-Content -Path $cfg -Encoding ascii

$dll = Get-Item (Join-Path $scripts 'BugFablesAP.dll')
$hash = (Get-FileHash $dll.FullName -Algorithm SHA256).Hash.Substring(0, 12)
Write-Output "deployed BugFablesAP.dll ($($dll.Length) bytes, sha256 $hash...) to BepInEx\scripts; DevReload will pick it up"
