# The apworld's tests, then the fuzzer: both, every time. Run from anywhere.
# -Archipelago: your Archipelago checkout (the world linked in, fuzz.py at its root).
param(
    [Parameter(Mandatory)] [string] $Archipelago,
    [int] $Runs = 10000,
    [int] $Jobs = [Environment]::ProcessorCount,
    [string[]] $With = @()  # other worlds in every fuzzed room, by folder name (e.g. apquest)
)
# Not 'Stop': Windows PowerShell turns a Python warning on stderr into a terminating error. Exit codes decide.
$env:SKIP_REQUIREMENTS_UPDATE = '1'
Push-Location $Archipelago
try {
    if (-not (Test-Path 'fuzz.py')) { throw "No fuzz.py in $Archipelago (development.md, Fuzzing the apworld)" }

    Write-Host '== Tests'
    python -m pytest worlds/bug_fables/test -q
    $testsFailed = $LASTEXITCODE -ne 0

    Write-Host "== Fuzzer: $Runs seeds"
    $games = @('-g', 'bug_fables') + ($With | ForEach-Object { '-g', $_ })
    python fuzz.py -r $Runs -j $Jobs -n 1 @games --skip-output | Select-Object -Last 1
    $report = Get-Content -Raw 'fuzz_output\report.json' | ConvertFrom-Json
    $stats = $report.stats
    foreach ($game in $report.errors.PSObject.Properties) {
        # One line per error kind: player names differ per run, so they are folded out.
        $game.Value.PSObject.Properties |
            Group-Object { $_.Name -replace "player \S+ pool", 'player X pool' } |
            ForEach-Object { '{0,6}  {1}: {2}' -f ($_.Group | ForEach-Object { $_.Value.Count } | Measure-Object -Sum).Sum, $game.Name, $_.Name }
    }
    Write-Host ("Fuzzer: {0} of {1} failed, {2} timed out" -f $stats.failure, $stats.total, $stats.timeout)
    Write-Host ("Tests: {0}" -f $(if ($testsFailed) { 'FAILED' } else { 'passed' }))
    if ($testsFailed -or $stats.failure -gt 0 -or $stats.timeout -gt 0) { exit 1 }
}
finally { Pop-Location }
