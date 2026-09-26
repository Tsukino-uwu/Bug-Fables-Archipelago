# Builds the plugin (Release) and stages the mod download in release\mod, plus release\built-from.txt.
# CI can't build (no game assembly), so the staged DLLs are committed; -Check is CI's gate that they match the sources.
#   powershell -ExecutionPolicy Bypass -File dev-scripts\build-release.ps1 [-GameDir <dir>]
#   pwsh dev-scripts/build-release.ps1 -Check
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables',
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'mod/BugFablesAP/BugFablesAP.csproj'
$out = Join-Path $repo 'mod/BugFablesAP/bin/Release'
$release = Join-Path $repo 'release'
$pluginDir = Join-Path $release 'mod/BepInEx/plugins/BugFablesAP'
$builtFrom = Join-Path $release 'built-from.txt'
$libraries = 'Archipelago.MultiClient.Net.dll', 'websocket-sharp.dll', 'Newtonsoft.Json.dll'

# git hash-object normalises line endings, so a Windows checkout (CRLF) and CI (LF) hash alike.
function Get-SourceLines {
    $files = & git -C $repo ls-files -co --exclude-standard -- 'mod/BugFablesAP' |
        Where-Object { $_ -match '\.(cs|csproj)$' } | Sort-Object
    if (-not $files) { throw 'no mod sources found' }
    foreach ($f in $files) { "$($f): $(& git -C $repo hash-object -- $f)" }
}

function Get-DllLines {
    foreach ($d in @('BugFablesAP.dll') + $libraries) {
        "$($d): $((Get-FileHash (Join-Path $pluginDir $d) -Algorithm SHA256).Hash.ToLower())"
    }
}

# Dev tools and cheats ship, but off: every [Debug] setting must default to off (-1 is TestStartMember's off).
function Assert-DebugDefaultsOff {
    $binds = Get-ChildItem (Join-Path $repo 'mod/BugFablesAP') -Filter *.cs | ForEach-Object {
        [regex]::Matches((Get-Content -Raw $_.FullName), 'Config\.Bind\(\s*"Debug"\s*,\s*"(\w+)"\s*,\s*([^,]+?)\s*,') |
            ForEach-Object { [pscustomobject]@{ Key = $_.Groups[1].Value; Default = $_.Groups[2].Value } }
    }
    if (-not $binds) { throw 'no [Debug] settings found: the default check is reading the wrong pattern' }
    $on = @($binds | Where-Object { $_.Default -notin @('false', '0', '""', '-1') })
    if ($on) { throw "[Debug] settings on by default, never in a release: $(($on | ForEach-Object { "$($_.Key) = $($_.Default)" }) -join ', ')" }
    Write-Output "all $(@($binds).Count) [Debug] settings default to off"
}
Assert-DebugDefaultsOff
# The zip's top level lands next to Bug Fables.exe: only BepInEx and the README.
$top = @(Get-ChildItem (Join-Path $release 'mod') | ForEach-Object Name | Sort-Object)
if (($top -join ',') -ne 'BepInEx,README.txt') { throw "release/mod's top level must be BepInEx and README.txt, is: $($top -join ', ')" }

if ($Check) {
    if (-not (Test-Path $builtFrom)) { throw 'release/built-from.txt is missing: run dev-scripts/build-release.ps1' }
    $recorded = Get-Content $builtFrom | Where-Object { $_ -notmatch '^(#|commit:)' -and $_.Trim() }
    $actual = @(Get-SourceLines) + @(Get-DllLines)
    $diff = Compare-Object $recorded $actual
    if ($diff) {
        $diff | ForEach-Object { Write-Output "  $($_.SideIndicator) $($_.InputObject)" }
        throw 'release/mod is stale: the sources or DLLs changed since it was built. Run dev-scripts/build-release.ps1 and commit.'
    }
    Write-Output "release/mod matches its sources ($($actual.Count) entries)"
    return
}

# No debug info: the pdb isn't shipped, and its path would put this machine's folders into the DLL.
& dotnet build $project -c Release "-p:BugFablesDir=$GameDir" -p:DebugType=none --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "build failed ($LASTEXITCODE)" }

New-Item -ItemType Directory -Force $pluginDir | Out-Null
foreach ($d in @('BugFablesAP.dll') + $libraries) {
    $src = Join-Path $out $d
    if (-not (Test-Path $src)) { throw "$d is missing from the build output" }
    Copy-Item $src (Join-Path $pluginDir $d) -Force
}
Copy-Item (Join-Path $repo 'LICENSE') (Join-Path $pluginDir 'LICENSE.txt') -Force
# Only what the zip should hold: a stray file here would ship.
$expected = @('BugFablesAP.dll') + $libraries + 'LICENSE.txt', 'THIRD-PARTY-NOTICES.txt'
$extra = Get-ChildItem $pluginDir -File | Where-Object { $expected -notcontains $_.Name }
if ($extra) { throw "unexpected files in release/mod: $($extra.Name -join ', ')" }

$commit = & git -C $repo rev-parse --short HEAD
$lines = @('# Written by dev-scripts/build-release.ps1; checked by its -Check in CI.', "commit: $commit") +
    @(Get-SourceLines) + @(Get-DllLines)
$lines | Set-Content -Path $builtFrom -Encoding ascii

$dll = Get-Item (Join-Path $pluginDir 'BugFablesAP.dll')
Write-Output "staged release/mod ($($dll.Length) bytes BugFablesAP.dll); commit release/ together with the sources"
