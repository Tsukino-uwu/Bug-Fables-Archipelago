# Builds the plugin (Release) from clean clones of HEAD and stages the mod download in release\mod, plus
# release\built-from.txt. CI can't build (no game assembly), so the staged DLLs are committed; -Check is the release's
# gate that they match.
#   powershell -ExecutionPolicy Bypass -File dev-scripts\build-release.ps1 [-GameDir <dir>] [-NewLibraries]
#   pwsh dev-scripts/build-release.ps1 -Check
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables',
    [switch]$Check,
    # A library DLL differing from the committed copy stops the build unless this says the change is deliberate.
    [switch]$NewLibraries
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$release = Join-Path $repo 'release'
$pluginDir = Join-Path $release 'mod/BepInEx/plugins/BugFablesAP'
$builtFrom = Join-Path $release 'built-from.txt'
$libraries = 'Archipelago.MultiClient.Net.dll', 'websocket-sharp.dll', 'Newtonsoft.Json.dll'
$shipped = @('BugFablesAP.dll') + $libraries + 'LICENSE.txt', 'THIRD-PARTY-NOTICES.txt'
# Packages that run code of their own during a build (build scripts or analyzers): exactly these, or the build stops.
$buildCodePackages = 'BepInEx.Core/5.4.21', 'NETStandard.Library/2.0.3'

function Get-Sha256([string]$path) { (Get-FileHash $path -Algorithm SHA256).Hash.ToLower() }

# Every build input, as its git blob id in the index: the release build's sources (Dev/ isn't compiled into it), the
# project with its lock file, and the root files that steer every build.
function Get-SourceLines([string]$dir) {
    $lines = & git -C $dir ls-files -s -- 'mod/BugFablesAP' 'global.json' 'nuget.config' 'Directory.Build.props' |
        ForEach-Object { $meta, $path = $_ -split "`t", 2; "$($path): $(($meta -split ' ')[1])" } |
        Where-Object { $_ -notmatch '^mod/BugFablesAP/Dev/' -and $_ -match
            '^(mod/BugFablesAP/.+\.(cs|csproj)|mod/BugFablesAP/packages\.lock\.json|global\.json|nuget\.config|Directory\.Build\.props): ' } |
        Sort-Object
    if (-not $lines) { throw 'no mod sources found' }
    $lines
}

function Get-DllLines {
    foreach ($d in @('BugFablesAP.dll') + $libraries) { "$($d): $(Get-Sha256 (Join-Path $pluginDir $d))" }
}

# The dev tools live in Dev/, which the Release build leaves out: every [Debug] setting must be bound there, and each
# still defaults to off (-1 is TestStartMember's off) for dev installs.
function Assert-DevToolsApart([string]$root) {
    $sources = Get-ChildItem (Join-Path $root 'mod/BugFablesAP') -Filter *.cs -Recurse |
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
function Assert-NoDevInDll([string]$dll, [string]$root) {
    $bytes = [System.IO.File]::ReadAllBytes($dll)
    $text = [System.Text.Encoding]::GetEncoding(28591).GetString($bytes)
    $names = Get-ChildItem (Join-Path $root 'mod/BugFablesAP/Dev') -Filter *.cs | ForEach-Object {
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

# Only what the zip should hold: a stray file would ship.
function Assert-ShippedSet {
    # The zip's top level lands next to Bug Fables.exe: only BepInEx and the README.
    $top = @(Get-ChildItem (Join-Path $release 'mod') | ForEach-Object Name | Sort-Object)
    if (($top -join ',') -ne 'BepInEx,README.txt') { throw "release/mod's top level must be BepInEx and README.txt, is: $($top -join ', ')" }
    $files = @(Get-ChildItem $pluginDir -File | ForEach-Object Name | Sort-Object)
    if (($files -join ',') -ne (($shipped | Sort-Object) -join ',')) {
        throw "release/mod's plugin folder must hold exactly $($shipped -join ', '); it holds $($files -join ', ')"
    }
}

# A package that brings build scripts or analyzers runs its own code inside the build.
function Assert-BuildCodePackages([string]$dir) {
    $assets = Get-Content -Raw (Join-Path $dir 'mod/BugFablesAP/obj/project.assets.json') | ConvertFrom-Json
    $target = @($assets.targets.PSObject.Properties)[0].Value
    $found = @(foreach ($lib in $assets.libraries.PSObject.Properties) {
        $t = $target.($lib.Name)
        $build = $t -and ($t.build -or $t.buildTransitive -or $t.buildMultiTargeting)
        $analyzers = @($lib.Value.files | Where-Object { $_ -like 'analyzers/*' }).Count -gt 0
        if ($build -or $analyzers) { $lib.Name }
    }) | Sort-Object
    if (($found -join ',') -ne (($buildCodePackages | Sort-Object) -join ',')) {
        throw "the packages that run code during the build changed: expected $($buildCodePackages -join ', '), got $($found -join ', ')"
    }
    Write-Output "build-time code only from $($buildCodePackages -join ' and ') ($(@($assets.libraries.PSObject.Properties).Count) packages restored, locked)"
}

if ($Check) {
    Assert-DevToolsApart $repo
    Assert-ShippedSet
    if (-not (Test-Path $builtFrom)) { throw 'release/built-from.txt is missing: run dev-scripts/build-release.ps1' }
    $recorded = @(Get-Content $builtFrom | Where-Object { $_ -notmatch '^(#|commit:|sdk:|game:)' -and $_.Trim() })
    $actual = @(Get-SourceLines $repo) + @(Get-DllLines)
    if (-not $recorded) { throw 'release/built-from.txt records nothing' }
    $diff = Compare-Object $recorded $actual
    if ($diff) {
        $diff | ForEach-Object { Write-Output "  $($_.SideIndicator) $($_.InputObject)" }
        throw 'release/mod is stale: the sources or DLLs changed since it was built. Run dev-scripts/build-release.ps1 and commit.'
    }
    Write-Output "release/mod matches its sources ($($actual.Count) entries)"
    Assert-NoDevInDll (Join-Path $pluginDir 'BugFablesAP.dll') $repo
    return
}

# What gets built is HEAD, never the working copy: a change not committed can't reach the DLL.
$dirty = & git -C $repo status --porcelain --untracked-files=no -- . ':(exclude)release'
if ($dirty) { throw "tracked files have uncommitted changes; the build uses HEAD, so commit or stash first:`n$($dirty -join "`n")" }
$head = (& git -C $repo rev-parse HEAD).Trim()
$gameDll = Join-Path $GameDir 'Bug Fables_Data\Managed\Assembly-CSharp.dll'
if (-not (Test-Path $gameDll)) { throw "no Assembly-CSharp.dll under $GameDir" }

# A git variable inherited from a hook would point every clone below at this repo.
Get-ChildItem env: | Where-Object { $_.Name -like 'GIT_*' } | ForEach-Object { Remove-Item "env:$($_.Name)" }
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) "bugfablesap-build-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
try {
    # Two clean clones at different paths: the same DLL from both proves no path, time or machine state went in.
    $builds = foreach ($dir in (Join-Path $tmp 'a'), (Join-Path $tmp 'second\b')) {
        & git clone --quiet --no-local --no-hardlinks --no-checkout -- $repo $dir
        if ($LASTEXITCODE -ne 0) { throw "git clone into $dir failed" }
        & git -C $dir checkout --quiet --detach $head
        if ($LASTEXITCODE -ne 0) { throw "checkout of $head in $dir failed" }
        # dotnet reads global.json from the working directory, so each build runs inside its clone.
        Push-Location $dir
        try {
            $sdk = (& dotnet --version).Trim()
            # Restore is locked (the csproj): each package's content hash is checked against packages.lock.json.
            # No debug info: the pdb isn't shipped, and its path would put this machine's folders into the DLL.
            & dotnet build 'mod/BugFablesAP/BugFablesAP.csproj' -c Release "-p:BugFablesDir=$GameDir" -p:DebugType=none --nologo -v q
            if ($LASTEXITCODE -ne 0) { throw "build in $dir failed ($LASTEXITCODE)" }
        }
        finally { Pop-Location }
        $dll = Join-Path $dir 'mod/BugFablesAP/bin/Release/BugFablesAP.dll'
        [pscustomobject]@{ Dir = $dir; Sdk = $sdk; Dll = $dll; Hash = Get-Sha256 $dll }
    }
    $a, $b = $builds
    if ($a.Hash -ne $b.Hash) { throw "two clean builds of $head differ ($($a.Hash) and $($b.Hash)): the build isn't reproducible" }
    if ($a.Sdk -ne $b.Sdk) { throw "the two builds used different SDKs ($($a.Sdk), $($b.Sdk))" }
    Write-Output "two clean builds of $($head.Substring(0, 8)) at different paths: byte-identical (SDK $($a.Sdk))"

    Assert-DevToolsApart $a.Dir
    Assert-BuildCodePackages $a.Dir
    Assert-NoDevInDll $a.Dll $a.Dir

    $out = Split-Path -Parent $a.Dll
    foreach ($d in $libraries) {
        $new = Join-Path $out $d
        $old = Join-Path $pluginDir $d
        if (-not (Test-Path $new)) { throw "$d is missing from the build output" }
        if ((Test-Path $old) -and -not $NewLibraries -and (Get-Sha256 $new) -ne (Get-Sha256 $old)) {
            throw "$d differs from the committed copy. If the library changed on purpose, run again with -NewLibraries"
        }
    }

    # Unchanged sources must rebuild the committed DLL exactly; anything else means it isn't what these sources make.
    $sources = @(Get-SourceLines $a.Dir)
    $committed = Join-Path $pluginDir 'BugFablesAP.dll'
    if ((Test-Path $builtFrom) -and (Test-Path $committed)) {
        $recorded = @(Get-Content $builtFrom | Where-Object { $_ -match '^(mod/|global\.json|nuget\.config|Directory\.Build\.props)' } | Sort-Object)
        if (($recorded -join "`n") -eq ($sources -join "`n")) {
            if ((Get-Sha256 $committed) -ne $a.Hash) {
                throw "the sources are unchanged since the committed BugFablesAP.dll was built, yet they build a different DLL: the committed one isn't what these sources make"
            }
            Write-Output 'the sources are unchanged since the committed DLL was built, and they rebuild it byte for byte'
        }
    }

    New-Item -ItemType Directory -Force $pluginDir | Out-Null
    foreach ($d in @('BugFablesAP.dll') + $libraries) { Copy-Item (Join-Path $out $d) (Join-Path $pluginDir $d) -Force }
    Copy-Item (Join-Path $repo 'LICENSE') (Join-Path $pluginDir 'LICENSE.txt') -Force
    Assert-ShippedSet

    $lines = @('# Written by dev-scripts/build-release.ps1; checked by its -Check at release.', "commit: $head",
        "sdk: $($a.Sdk)", "game: Assembly-CSharp.dll sha256 $(Get-Sha256 $gameDll)") + $sources + @(Get-DllLines)
    $lines | Set-Content -Path $builtFrom -Encoding ascii
}
finally {
    if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
}

$dll = Get-Item (Join-Path $pluginDir 'BugFablesAP.dll')
Write-Output "staged release/mod ($($dll.Length) bytes BugFablesAP.dll) from $($head.Substring(0, 8)); commit release/ as its own commit"
