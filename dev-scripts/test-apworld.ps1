# The apworld's tests with Archipelago's general tests on it, the Logic Test check, the fuzzer, then Universal Tracker's
# fuzzer hook: all, every time. Run from anywhere.
# -Archipelago: your Archipelago checkout (the world linked in, fuzz.py at its root, worlds/logic_test copied in, and
# Universal Tracker in custom_worlds or worlds/tracker with a Players folder for the tracker pass).
# -TrackerOnly: only the tracker pass (CI's tracker shards).
# -FuzzOnly: the first pass's fuzzer without the tests and the Logic Test check (CI's fuzz shards after the first).
param(
    [Parameter(Mandatory)] [string] $Archipelago,
    [int] $Runs = 10000,
    [int] $TrackerRuns = 10000,
    [int] $Jobs = [Environment]::ProcessorCount,
    [string[]] $With = @(),  # other worlds in every fuzzed room, by folder name (e.g. apquest)
    [switch] $TrackerOnly,
    [switch] $FuzzOnly
)
# Not 'Stop': Windows PowerShell turns a Python warning on stderr into a terminating error. Exit codes decide. Set here,
# since a host may start with another (CI runs this under pwsh on Linux).
$ErrorActionPreference = 'Continue'
$env:SKIP_REQUIREMENTS_UPDATE = '1'
$pythonPath = $env:PYTHONPATH
$testWorlds = $env:AP_TEST_WORLDS

# One line per error kind: player names differ per run, so they are folded out.
function Show-FuzzErrors($report) {
    foreach ($game in $report.errors.PSObject.Properties) {
        $game.Value.PSObject.Properties |
            Group-Object { $_.Name -replace "player \S+ pool", 'player X pool' } |
            ForEach-Object { '{0,6}  {1}: {2}' -f ($_.Group | ForEach-Object { $_.Value.Count } | Measure-Object -Sum).Sum, $game.Name, $_.Name }
    }
}

Push-Location $Archipelago
try {
    if (-not (Test-Path 'fuzz.py')) { throw "No fuzz.py in $Archipelago (development.md, Fuzzing the apworld)" }
    $tracker = (Test-Path 'custom_worlds/tracker.apworld') -or (Test-Path 'worlds/tracker')
    if ($TrackerOnly -and -not $tracker) { throw "No Universal Tracker in $Archipelago (development.md, Fuzzing the apworld)" }
    $failed = $false

    if (-not $TrackerOnly) {
        $testsFailed = $false
        $logicTest = 'skipped: -FuzzOnly'
        if (-not $FuzzOnly) {
            Write-Host '== Tests'
            # Archipelago's general tests too, scoped to this world (tests.md), for this call only: the fuzzer loads every
            # world. Its website tests need WebHost's packages, which a plain checkout lacks.
            $skip = @()
            python -c 'import flask' 2>$null
            if ($LASTEXITCODE -ne 0) {
                $skip = @('--ignore=test/webhost')
                Write-Host "test/webhost skipped: no WebHost packages (ModuleUpdate.py --append WebHostLib/requirements.txt)"
            }
            $env:AP_TEST_WORLDS = 'bug_fables'
            python -m pytest -q @skip
            $testsFailed = $LASTEXITCODE -ne 0
            $env:AP_TEST_WORLDS = $testWorlds
            $logicTest = 'skipped: no worlds/logic_test (development.md, Play-testing the logic)'
        }
        if (-not $FuzzOnly -and (Test-Path 'worlds/logic_test')) {
            Write-Host '== Logic Test check'
            python (Join-Path $PSScriptRoot 'logic-test-check.py')
            $logicTest = if ($LASTEXITCODE -eq 0) { 'passed' } else { 'FAILED' }
        }

        Write-Host "== Fuzzer: $Runs seeds"
        $games = @('-g', 'bug_fables')
        foreach ($world in $With) { $games += '-g', $world }
        python fuzz.py -r $Runs -j $Jobs -n 1 @games --skip-output | Select-Object -Last 1
        $report = Get-Content -Raw 'fuzz_output/report.json' | ConvertFrom-Json
        $stats = $report.stats
        Show-FuzzErrors $report
        Write-Host ("Fuzzer: {0} of {1} failed, {2} timed out" -f $stats.failure, $stats.total, $stats.timeout)
        Write-Host ("Tests: {0}" -f $(if ($FuzzOnly) { 'skipped: -FuzzOnly' } elseif ($testsFailed) { 'FAILED' } else { 'passed' }))
        Write-Host "Logic Test check: $logicTest"
        $failed = $testsFailed -or $logicTest -eq 'FAILED' -or $stats.failure -gt 0 -or $stats.timeout -gt 0
    }

    if ($tracker) {
        # Universal Tracker regenerates each seed from its slot_data and checks every sphere against the real one. Its
        # hook reads the seed's output, so no --skip-output; it skips rooms with other worlds, so no -With. A failed
        # generation counts as ignored there, so ignored fails too. fuzz.py clears fuzz_output: the first pass's moves
        # aside meanwhile.
        Write-Host "== Universal Tracker's fuzzer hook: $TrackerRuns seeds"
        foreach ($old in 'fuzz_output_main', 'fuzz_output_tracker') {
            if (Test-Path $old) { Remove-Item $old -Recurse -Force }
        }
        if (Test-Path 'fuzz_output') { Move-Item 'fuzz_output' 'fuzz_output_main' }
        $env:PYTHONPATH = $PSScriptRoot + [IO.Path]::PathSeparator + $pythonPath
        python fuzz.py -r $TrackerRuns -j $Jobs -n 1 -g bug_fables --hook tracker_fuzz_hook:Hook | Select-Object -Last 1
        $env:PYTHONPATH = $pythonPath
        Move-Item 'fuzz_output' 'fuzz_output_tracker'
        if (Test-Path 'fuzz_output_main') { Move-Item 'fuzz_output_main' 'fuzz_output' }
        $report = Get-Content -Raw 'fuzz_output_tracker/report.json' | ConvertFrom-Json
        Show-FuzzErrors $report
        $t = $report.stats
        Write-Host ("Universal Tracker: {0} of {1} failed, {2} timed out, {3} ignored" -f $t.failure, $t.total, $t.timeout,
            $t.ignored)
        $failed = $failed -or $t.failure -gt 0 -or $t.timeout -gt 0 -or $t.ignored -gt 0
    }
    else {
        Write-Host "Universal Tracker: skipped, not in $Archipelago (development.md, Fuzzing the apworld)"
    }
    if ($failed) { exit 1 }
    exit 0
}
finally {
    $env:PYTHONPATH = $pythonPath
    $env:AP_TEST_WORLDS = $testWorlds
    Pop-Location
}
