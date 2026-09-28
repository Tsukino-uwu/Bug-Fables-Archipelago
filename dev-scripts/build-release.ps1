# Builds the plugin (Release) and stages the mod download in release\mod, plus release\built-from.txt.
# CI can't build (no game assembly), so the staged DLLs are committed; -Check is the release's gate that they match.
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
    # Dev/ isn't compiled into the release build, so a dev-only change doesn't make it stale.
    $files = & git -C $repo ls-files -co --exclude-standard -- 'mod/BugFablesAP' |
        Where-Object { $_ -match '\.(cs|csproj)$' -and $_ -notmatch '^mod/BugFablesAP/Dev/' } | Sort-Object
    if (-not $files) { throw 'no mod sources found' }
    foreach ($f in $files) { "$($f): $(& git -C $repo hash-object -- $f)" }
}

function Get-DllLines {
    foreach ($d in @('BugFablesAP.dll') + $libraries) {
        "$($d): $((Get-FileHash (Join-Path $pluginDir $d) -Algorithm SHA256).Hash.ToLower())"
    }
}

# The dev tools live in Dev/, which the Release build leaves out: every [Debug] setting must be bound there, and each
# still defaults to off (-1 is TestStartMember's off) for dev installs.
function Assert-DevToolsApart {
    $sources = Get-ChildItem (Join-Path $repo 'mod/BugFablesAP') -Filter *.cs -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    $binds = foreach ($f in $sources) {
        # Case-sensitive, on the mod's own folder: a checkout under a folder named dev must not count.
        $dev = $f.FullName -cmatch '[\\/]BugFablesAP[\\/]Dev[\\/]'
        [regex]::Matches((Get-Content -Raw $f.FullName), 'Config\.Bind\(\s*"Debug"\s*,\s*"(\w+)"\s*,\s*([^,]+?)\s*,') |
            ForEach-Object { [pscustomobject]@{ Key = $_.Groups[1].Value; Default = $_.Groups[2].Value; Dev = $dev; File = $f.Name } }
    }
    if (-not $binds) { throw 'no [Debug] settings found: the check is reading the wrong pattern' }
    $outside = @($binds | Where-Object { -not $_.Dev })
    if ($outside) { throw "[Debug] settings outside Dev/, so in the release build: $(($outside | ForEach-Object { "$($_.Key) ($($_.File))" }) -join ', ')" }
    $on = @($binds | Where-Object { $_.Default -notin @('false', '0', '""', '-1') })
    if ($on) { throw "[Debug] settings on by default: $(($on | ForEach-Object { "$($_.Key) = $($_.Default)" }) -join ', ')" }
    Write-Output "all $(@($binds).Count) [Debug] settings in Dev/, off by default"
}

# The shipped DLL holds none of Dev/'s own types (a type's name sits in the metadata as UTF-8 between zero bytes) and no
# "Dev only" config text (string literals are UTF-16).
function Assert-NoDevInDll {
    $dll = Join-Path $pluginDir 'BugFablesAP.dll'
    $bytes = [System.IO.File]::ReadAllBytes($dll)
    $text = [System.Text.Encoding]::GetEncoding(28591).GetString($bytes)
    $names = Get-ChildItem (Join-Path $repo 'mod/BugFablesAP/Dev') -Filter *.cs | ForEach-Object {
        [regex]::Matches((Get-Content -Raw $_.FullName), '(?m)^\s*(?:(?:public|internal|private|static|sealed)\s+)*class\s+(\w+)') |
            ForEach-Object { $_.Groups[1].Value }
    } | Sort-Object -Unique
    if (-not $names) { throw 'no Dev/ types found: the check is reading the wrong pattern' }
    $found = @($names | Where-Object { $text.Contains("$([char]0)$_$([char]0)") })
    if ($found) { throw "the release DLL holds dev types: $($found -join ', ')" }
    $devOnly = [System.Text.Encoding]::GetEncoding(28591).GetString([System.Text.Encoding]::Unicode.GetBytes('Dev only'))
    if ($text.Contains($devOnly)) { throw 'the release DLL holds a "Dev only" setting description' }
    Write-Output "release DLL holds none of Dev/'s $(@($names).Count) types"
}
Assert-DevToolsApart
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
    Assert-NoDevInDll
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
$lines = @('# Written by dev-scripts/build-release.ps1; checked by its -Check at release.', "commit: $commit") +
    @(Get-SourceLines) + @(Get-DllLines)
$lines | Set-Content -Path $builtFrom -Encoding ascii
Assert-NoDevInDll

$dll = Get-Item (Join-Path $pluginDir 'BugFablesAP.dll')
Write-Output "staged release/mod ($($dll.Length) bytes BugFablesAP.dll); commit release/ together with the sources"
