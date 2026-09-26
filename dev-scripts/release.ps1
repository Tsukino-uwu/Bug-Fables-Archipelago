# Cuts a release: preflight (rebuild the mod DLL if stale), push, wait for CI, dispatch release.yml, wait for it.
# Refuses at the first red, so a release can't be cut past a stale DLL. Running it is the go-ahead to push.
#   powershell -ExecutionPolicy Bypass -File dev-scripts\release.ps1 -Version v0.1.0 [-Prerelease] [-HighlightsFile <file>] [-GameDir <dir>]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$HighlightsFile = '',
    [switch]$Prerelease,
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Bug Fables'
)
# Continue, not Stop: git and gh write to stderr on success; every exit code is checked by hand.
$ErrorActionPreference = 'Continue'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo
# By path: another git on PATH has misread CRLF working copies as changes.
$git = @('C:\Program Files\Git\cmd\git.exe', 'C:\Program Files\Git\bin\git.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $git) { $git = 'git' }
$buildRelease = Join-Path $PSScriptRoot 'build-release.ps1'

function Step($msg) { Write-Host ''; Write-Host "== $msg ==" -ForegroundColor Cyan }
function Refuse($msg) { Write-Host ''; Write-Host "RELEASE REFUSED: $msg" -ForegroundColor Red; exit 1 }
function Preflight { & powershell -NoProfile -ExecutionPolicy Bypass -File $buildRelease -Check 2>&1 | ForEach-Object { Write-Host "  $_" }; return ($LASTEXITCODE -eq 0) }

if ($Version -notmatch '^v\d+\.\d+\.\d+$') { Refuse "version must look like v0.1.0, got '$Version'" }
if ($HighlightsFile -ne '' -and -not (Test-Path -LiteralPath $HighlightsFile)) { Refuse "no highlights file at $HighlightsFile" }

Step 'Versions'
$bare = $Version.Substring(1)
$pluginVersion = [regex]::Match((Get-Content -Raw 'mod\BugFablesAP\Plugin.cs'), 'Version = "([^"]+)"').Groups[1].Value
$worldVersion = (Get-Content -Raw 'apworld\bug_fables\archipelago.json' | ConvertFrom-Json).world_version
if ($pluginVersion -ne $bare -or $worldVersion -ne $bare) {
    Refuse "Plugin.cs says $pluginVersion and archipelago.json says $worldVersion; both must be $bare"
}
Write-Host "mod and world are both $bare"

Step 'Repository state'
$branch = (& $git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -ne 'main') { Refuse "on branch '$branch'; releases are cut from main" }
& $git diff --quiet --ignore-cr-at-eol HEAD -- 2>$null
if ($LASTEXITCODE -ne 0) {
    & $git --no-pager diff --ignore-cr-at-eol --stat HEAD -- 2>$null | ForEach-Object { Write-Host $_ }
    Refuse 'tracked files have uncommitted changes; commit first'
}
& $git fetch origin --tags --quiet 2>$null
if (& $git ls-remote --tags origin "refs/tags/$Version") { Refuse "tag $Version already exists on origin" }
$behind = (& $git rev-list --count 'HEAD..origin/main').Trim()
if ($behind -ne '0') { Refuse "HEAD is $behind commit(s) behind origin/main; pull first" }
Write-Host "main, clean, $Version is free"

Step 'Preflight: is the committed mod DLL fresh, with every dev tool off by default?'
if (-not (Preflight)) {
    Step 'Rebuilding the stale mod DLL'
    & powershell -NoProfile -ExecutionPolicy Bypass -File $buildRelease -GameDir $GameDir
    if ($LASTEXITCODE -ne 0) { Refuse 'build-release.ps1 failed' }
    if (-not (Preflight)) { Refuse 'preflight still fails after the rebuild (stale, or a [Debug] setting on by default); see above' }
    & $git add -- release
    & $git commit -q -m "Release prep: mod DLL rebuilt from current sources for $Version"
    if ($LASTEXITCODE -ne 0) { Refuse 'commit of the rebuilt DLL failed' }
    Write-Host 'committed the rebuilt DLL'
}
Write-Host 'preflight clean'

Step 'Push'
& $git push origin main
if ($LASTEXITCODE -ne 0) { Refuse 'push failed' }
$sha = (& $git rev-parse HEAD).Trim()
Write-Host "pushed $($sha.Substring(0, 8))"

Step "Waiting for CI on $($sha.Substring(0, 8))"
Start-Sleep -Seconds 20
$deadline = (Get-Date).AddMinutes(40)
while ($true) {
    $json = & gh run list --commit $sha --workflow ci.yml -L 5 --json status,conclusion,databaseId | Out-String
    # Windows PowerShell hands a JSON array back as one object; ForEach-Object unrolls it.
    $runs = @(if ($json.Trim()) { $json | ConvertFrom-Json | ForEach-Object { $_ } })
    if ($runs.Count -gt 0 -and -not ($runs | Where-Object { $_.status -ne 'completed' })) { break }
    if ((Get-Date) -gt $deadline) { Refuse 'CI did not finish within 40 minutes' }
    Start-Sleep -Seconds 20
}
$red = @($runs | Where-Object { $_.conclusion -ne 'success' })
if ($red.Count -gt 0) { Refuse "CI is red on HEAD: gh run view $($red[0].databaseId) --log-failed" }
Write-Host 'CI is green'

Step "Dispatching release.yml for $Version"
$ghArgs = @('workflow', 'run', 'release.yml', '-f', "version=$Version", '-f', "prerelease=$($Prerelease.IsPresent.ToString().ToLower())")
# The @ makes gh read the file; without it gh sends the path itself as the release body.
if ($HighlightsFile -ne '') { $ghArgs += @('-F', "highlights=@$HighlightsFile") }
& gh @ghArgs
if ($LASTEXITCODE -ne 0) { Refuse 'gh workflow run failed' }
Start-Sleep -Seconds 20
$latest = @((& gh run list --workflow release.yml -L 1 --json databaseId | Out-String) | ConvertFrom-Json | ForEach-Object { $_ })
if ($latest.Count -eq 0) { Refuse 'no release run found after the dispatch' }
$runId = $latest[0].databaseId
Write-Host "release run $runId"
$deadline = (Get-Date).AddMinutes(40)
while ($true) {
    $run = (& gh run view $runId --json status,conclusion,jobs | Out-String) | ConvertFrom-Json
    if ($run.status -eq 'completed') { break }
    if ((Get-Date) -gt $deadline) { Refuse 'the release run did not finish within 40 minutes' }
    Start-Sleep -Seconds 20
}
@($run.jobs | ForEach-Object { $_ }) | ForEach-Object { Write-Host "$($_.conclusion)`t$($_.name)" }
if ($run.conclusion -ne 'success') { Refuse "the release run failed: gh run view $runId --log-failed" }

Step 'Published'
$rel = (& gh release view $Version --json name,url,isPrerelease,assets | Out-String) | ConvertFrom-Json
Write-Host "$($rel.name)`t$($rel.url)`tprerelease: $($rel.isPrerelease)"
@($rel.assets | ForEach-Object { $_ }) | ForEach-Object { Write-Host "  $($_.name)`t$($_.size) bytes" }
