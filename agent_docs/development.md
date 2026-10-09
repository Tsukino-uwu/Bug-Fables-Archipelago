# Building and testing from source

For developers. You need the .NET SDK and your own copy of the game. The build compiles against the game's
`Assembly-CSharp.dll` from your install and never copies it into the repo.

## Before the first commit: the hooks and the preflight

Once per clone: `git config core.hooksPath .githooks`. The hooks need Python 3.11 or newer and find one themselves
(`py -3`, `python3`, then `python`, each tried before use), or use the one you name:
`git config preflight.python <path to python>`.

- `python dev-scripts/preflight.py`: every section on what is staged; the hooks run it on every commit and push.
  `--history` checks every commit ever made; `--text-stdin LABEL` checks text such as release notes.
- `python dev-scripts/negative-test-preflight.py`: proves every section can still fail (39 s on 2026-10-01). Run it
  after any change to the preflight.
- `python dev-scripts/dotnet_metadata.py --selftest <dll>`: reads a .NET assembly and prints what it found.
- `python dev-scripts/verify-release.py --ref vX.Y.Z --zip <file> --apworld <file>`: checks a release's files against
  its tag.

**Working with Claude Code:** `.claude/settings.json` runs `.claude/hooks/agent-guard.py` before each shell command,
file edit and page fetch the agent makes. It refuses anything that gets past the hooks, without asking you. It asks
you first only before:
- any read of a GitHub project with no linked row in `licensing.md` as committed, its licence included, unless it's
  your own (`own_github_owners`): a row the agent wrote permits nothing until its commit;
- a commit that adds a GitHub project to `licensing.md`;
- an edit to `docs/capabilities.md`, the patterns file, `.claude/` or `.git/`;
- a commit that may carry one of those;
- a `gh api` write;
- a shell command that reads or changes the clone's `.git/config` or `.git/hooks`.

Its prompts start with `agent-guard:`; any other prompt is Claude Code's own.

It needs `py -3` or `python3`; if it can't run, it refuses. `/hooks` in Claude Code shows it.

When a check refuses something the code really needs (a new host, a new capability), the fix is a row in
`docs/capabilities.md` with its reason: the maintainer's decision, never a looser pattern. What each section does:
[apimplementation.md, build step
28](apimplementation.md#build-step-28-the-preflight-nothing-unpublishable-in-the-repo-or-a-release).

## The mod

`dotnet build mod/BugFablesAP/BugFablesAP.csproj`. If the game isn't in Steam's default library, add
`-p:BugFablesDir="D:\path\to\Bug Fables"`. The SDK is pinned by `global.json` and every package by
`mod/BugFablesAP/packages.lock.json`; restores are locked, so to update a package on purpose run
`dotnet restore mod/BugFablesAP/BugFablesAP.csproj -p:RestoreLockedMode=false` and commit the lock file. A release DLL
is built by `dev-scripts/build-release.ps1` (documentation.md, step 32).

The code style is in `.editorconfig`, which most editors apply: 4-space indents, braces on their own line, and lines
up to 120 characters (the limit core Archipelago's `ruff.toml` sets for Python). The docs follow `.markdownlint.json`,
which the markdownlint extension checks: lines up to 120 as well, tables and code blocks exempt. An item numbered 10 or
more indents its content by 4, where its text starts: with 3, a paragraph after a blank line falls out of the list.

## Trying it in the running game: build, then copy

The build and the copy into the game are separate steps. The build never writes to the game install.

1. **Build and stage:** `powershell -ExecutionPolicy Bypass -File dev-scripts\stage-dev.ps1`
   (add `-GameDir "D:\path\to\Bug Fables"` if needed). It writes `stage/` in the repo (gitignored), laid out
   like the game folder:
   - `stage/every-build/BepInEx`: the plugin, `BepInEx/scripts/BugFablesAP.dll` and its `.pdb`.
   - `stage/setup/BepInEx`: the client libraries (`BepInEx/plugins`) and ScriptEngine's config
     (`BepInEx/config`).
2. **First time only, with the game closed:** install BepInEx 5 and ScriptEngine (from BepInEx.Debug) in the
   game, then copy `stage/setup/BepInEx` onto the game folder.
3. **After every build:** `powershell -ExecutionPolicy Bypass -File dev-scripts\copy-dev.ps1` copies the
   staged plugin into the game (or copy `stage/every-build/BepInEx` onto the game folder by hand). The game can
   be running: the plugin notices its DLL changed and ScriptEngine reloads it within a few seconds
   (`DevReload: BugFablesAP.dll changed` then `Reloaded all plugins!` in `BepInEx/LogOutput.log`).
   - **Which build runs, in one line:** DevReload writes `BepInEx/bugfablesap-reload.txt`: `loaded <hash>` (the same
     12 digits copy-dev prints for a copy), `waiting for the scene/talk/battle to end`, or `reloading`. copy-dev prints
     it after copying, and `copy-dev.ps1 -Status` prints only it. A reload waits for a scene, talk or battle to end,
     so check once when the tester says it's in; never poll for it. **Two copies during one reload fool it**
     (2026-09-30): a copy of the old build, then the new one while the reload ran, left the old code running under a
     `loaded <new hash>` line, since the new instance hashes the file on disk when it starts. When it matters, confirm
     with a log line only the new build writes (an install line that changed), and copy only after a build succeeds.
   - `-DebugOn EntityDump,ScriptDump` / `-DebugOff GrantProbe` switch Debug settings in the mod's config
     in the same run, and read the result back. `-DebugSet Key=Value` sets one that takes a number or text (one per
     `-DebugSet`, not split at commas). `-ConfigSet Section.Key=Value` sets a key in any other section, for a
     test (`-ConfigSet Archipelago.RandomizerEnabled=true` to log in at the main menu); set it back after.
   - The copied DLL is stamped with the current time, since DevReload watches write times: copying an unchanged
     build to reload a changed `-DebugSet` did nothing until then (2026-09-25).
   - A Debug setting changed in the file while the game runs is overwritten by the game's value the next time the mod
     saves its config (the console's `onehit` saving put `TestStartMember = 2` back, 2026-09-26). Change it with a
     reload in the same run (stage, then copy), and read the new value in the reload's log line.
   - DevReload waits while a scene, a conversation or a battle runs, and logs that it's waiting: a reload in the
     middle of a scene orphaned what the old plugin had made for it (the spider fight's stand-ins, 2026-09-25).
   - Every file it replaces (the plugin, the config) is first copied to `stage/backup/<time>/`;
     `-Restore <time>` puts it back. It writes nothing else in the game: the libraries and ScriptEngine's
     config stay the once-per-setup copy of step 2.
4. **Trying the release download instead (game closed):** `copy-dev.ps1 -Layout Release` moves the dev copies out
   (the plugin in `scripts`, the libraries loose in `plugins`) and installs `release/mod`, exactly what the zip
   holds; `-Layout Dev` switches back (it needs `stage-dev.ps1` run first). Both layouts at once would load the
   plugin twice, so each switch takes the other one out, into `stage/backup/<time>/`. DevReload stays off in the
   release layout: it only starts when `BepInEx/scripts/BugFablesAP.dll` exists.
   - **Why a script with backups** (2026-09-24): Claude Code's auto mode refused, as irreversible, both a
     deploy script that rewrote configs and libraries in the game and an ad-hoc `cp` plus in-place `sed`.
     MeshGhost, which never hit this, only ever replaces its own rebuildable DLL through one named script.
   - **Why the scripts do what they do** (seen 2026-09-24):
     - The DLL and its `.pdb` always travel together: ScriptEngine silently refuses a plugin without its pdb.
     - The client libraries go to `BepInEx/plugins`, never `scripts`: ScriptEngine reloads every DLL in
       `scripts`, and two copies of Newtonsoft.Json in one process break type identity.
     - The staged DLL is stamped with the current time too: an unchanged build keeps its old time, which
       DevReload reads as no change.
     - `-DebugOn A,B` through `powershell -File` arrives as the single string "A,B" (it once wrote a key named
       "MapDump,ScriptDump"), so the script splits on commas.
     - The running game can be reading the DLL at the moment of the copy, so the copy is retried (10 times,
       300 ms apart).
     - The config is written as UTF-8 without a BOM, as BepInEx writes it (Windows PowerShell's `Set-Content`
       adds one).
     - ScriptEngine's own config turns its file watcher off: this game's Mono throws
       `NotImplementedException` from `new FileSystemWatcher`, which aborts `ScriptEngine.Awake`. DevReload
       polls instead.
5. If `stage-dev.ps1` says **the libraries differ from the game's**, copy `stage/setup/BepInEx` again with the game
   closed: a running game holds the libraries open.

The plugin reads its config when it loads, so a hot reload also picks up a changed
`BepInEx/config/bugfables.archipelago.cfg`. **But the running plugin rewrites the whole file whenever one of its
settings changes** (a panel choice, a one-shot dev setting resetting itself), with the values it holds in
memory. An edit made while the game runs can be undone before the next reload (2026-09-24: `EntityDump = true`
came back `false` after a panel change). So change a setting and reload in one go: restage, then
`copy-dev.ps1 -DebugOn ...`.

## A local server to test against

1. Link `apworld/bug_fables` into `worlds/` of an [Archipelago](https://github.com/ArchipelagoMW/Archipelago)
   source checkout.
2. Put a player file in a folder of its own, for example `name: BugTester`, `game: Bug Fables`,
   `Bug Fables: {}`, and generate: `python Generate.py --player_files_path <that folder> --outputpath <out>`.
   To put chosen items on chosen locations (a test seed), add a `plando_items` block under `Bug Fables:` in the
   player file, and generate with `--plando "bosses, items, connections, texts"`. The checkout's default
   `plando_options` leave items out, and then the block is ignored without a word (2026-09-24). Pass `--spoiler 2`
   and read the spoiler's "Locations" to confirm the placement.
   A new test seed normally needs a new file (each save is tied to its seed). To keep a test file instead, set
   `AdoptSeed = true` under `[Debug]` (`copy-dev.ps1 -DebugOn AdoptSeed`): the save moves to the new seed and
   replays its items. Test files only.
3. Host it: `python MultiServer.py <out>/AP_<seed>.zip --port 38281`.
4. In the game's Archipelago panel (or the config): address `ws://127.0.0.1`, port `38281`, slot `BugTester`.
   The mod connects by itself. Read both logs: the mod's in `BepInEx/LogOutput.log` (`[ap]`, `[ws]`,
   `[check]` lines) and the server's console.

## A second player

Some things only show with another player in the room: their items found here, and items they send you. The slot
needs no one playing it (2026-09-26, both directions seen on screen this way):

1. Two player files: yours, and one for another game in your Archipelago checkout (APQuest is small), e.g. `name:
   Other`. Place items with plando both ways: under Other's game, `plando_items` with `world: BugTester` puts Other's
   items on your locations (one per class shows every colour); under yours, `world: Other` puts your items in Other's
   locations. Generate with `--plando "bosses, items, connections, texts"` and check the spoiler.
2. Host it as usual.
3. To have Other find your items, from the checkout: `PYTHONPATH=. python <this repo>/dev-scripts/send-as-player.py
   Other APQuest "Bottom Left Chest"`. It logs in as Other and checks those locations (Archipelago's `Connect` and
   `LocationChecks`); your game gets "You got \<item> from Other!". A location is checked once: the same one twice sends
   nothing.

## The apworld's tests

With the world linked as above, in an Archipelago checkout at the tag the world targets (`0.6.8`, CI's `AP_TAG`, since
2026-10-08), run `AP_TEST_WORLDS=bug_fables python -m pytest` from the checkout's root. That is our tests plus
Archipelago's general tests, scoped to this world (`tests.md`, "Running Tests for Specific Worlds", new in 0.6.8): only
this world, the generic one and APQuest load. `python -m pytest worlds/bug_fables/test` runs ours alone. The general
tests under `test/webhost` render the world's docs and options as the website does and need WebHost's packages
(`python ModuleUpdate.py --append WebHostLib/requirements.txt`, as Archipelago's own CI installs them); without
them, add `--ignore=test/webhost`. Set `SKIP_REQUIREMENTS_UPDATE=1` to stop Archipelago's scripts from prompting to
install other games' packages.

## Fuzzing the apworld

The tests check the option sets we thought of; the [Archipelago-fuzzer](https://github.com/Eijebong/Archipelago-fuzzer)
generates seeds from random yamls and catches the rare combination that fails. **Whenever the tests run, the fuzzer
runs too** (2026-09-28); 10000 seeds take a few minutes.

1. Once: copy its `fuzz.py` (and `hooks/`) to the root of your Archipelago checkout. We use commit `53686ba`
   (`fuzz.py` 0.6.2, 2026-06-09); CI pins the same one.
2. `dev-scripts/test-apworld.ps1 -Archipelago <your checkout>` runs the tests (ours and the general ones, as above;
   it skips `test/webhost` and says so when WebHost's packages are missing), the Logic Test check (next section),
   then the fuzzer (`fuzz.py -r 10000 -j <cores> -n 1 -g bug_fables --skip-output`: one Bug Fables yaml per seed), and
   prints each error with its count. `-With apquest` puts another world in every room; `-Runs` changes the count,
   `-Jobs` the processes (every core by default). With Universal Tracker in the checkout it then runs its fuzzer hook
   (below). It fails unless every part is clean.
3. Read `fuzz_output/report.json` (counts and each error with the runs that hit it). Each failed run keeps its yaml and
   log in `fuzz_output/error/bug_fables/<run>/`; regenerate it with `Generate.py --player_files_path` on that folder.
   A new run replaces `fuzz_output`, so copy anything you still need first.

Exit code 1 only means some runs failed. The goal is 0 failures in 10000.

**Tracker Addons, for checking a room's logic in the tracker** (installed 2026-10-08, the user's yes; optional): its
`tracker_addons.apworld`, from the same repository's release `Tracker_Addons_v0.1.2`, goes next to `tracker.apworld` in
your checkout's `custom_worlds`. It was put in with its source and metadata only, both compared with the repository at
that tag, and the zip's `__pycache__` left out, so what runs is the source read. It adds commands to Universal
Tracker's client: `/get_regions [filter]` (every region, a room's parts such as `WizardTowerBasement (Top Right)`
included), `/nearest_locations <region>`, `/get_depth` (the shallowest checks in logic), `/next_progression` (what
each item would open now) and `/glp` (`get_logical_path`). Nothing in the apworld depends on it.

**Universal Tracker's fuzzer hook** (since 2026-10-03; the Archipelago guide's build step 40). Universal Tracker ships a
hook for this fuzzer that regenerates each seed from its slot_data, with no yaml, as Universal Tracker does, and checks
sphere by sphere that it puts the same locations in logic as the real generation (its `docs/apworld-integration.md`,
"Fuzzer hooks").

1. Once: put `tracker.apworld` from Universal Tracker's release v0.3.4 (2026-09-23; CI pins the same) in your
   checkout's `custom_worlds`, and make a `Players` folder at the checkout's root. Universal Tracker requires its
   players folder, and with no window open it fails every run when the folder is missing.
2. `test-apworld.ps1` then runs a second pass: `fuzz.py -r 10000 -j <cores> -n 1 -g bug_fables --hook
   tracker_fuzz_hook:Hook`, with `dev-scripts` on `PYTHONPATH`. No `--skip-output`: the hook reads the seed it made.
   No `-With`: it skips rooms with another world. It fails on any failure, timeout or ignored run, since the hook files
   a failed generation as ignored. Its report and failed runs are in `fuzz_output_tracker`; `-TrackerRuns` changes its
   count, `-TrackerOnly` runs only this pass.
3. `tracker_fuzz_hook.py` is Universal Tracker's own `YamllessHook`, emptying `TrackerCore`'s cache of the worlds it
   regenerated before each run: the cache is a class-level list, and fuzz.py's workers live for the whole run.
   Measured (2026-10-03, 1000 runs on 12 workers, 30 s): without it each worker grew from 229 to 290 MB; with it, it
   stays at 238. Its `Hook` takes the same yaml-less branch for this world (`TrackerCore.initalize_tracker_core`), so
   only `YamllessHook` runs.
4. The hook turns deferred entrances off (`enforce_deferred_connections = DeferredEntranceMode.disabled` in
   `Hook.before_generate`, which `YamllessHook` calls; `fuzzer_hook.py`, v0.3.4, read 2026-10-08), so every shuffled
   door is connected and every sphere compared (the Archipelago guide's build step 55).

Seen failing (2026-10-03): with the passthrough ignored, 19 of 20 runs failed, each log naming a location "in server
logic but not expected in UT".

**CI runs the same script on every push** (since 2026-09-29): `ci.yml`'s `fuzz` job installs the fuzzer and the
Logic Test at the commits it pins, runs `test-apworld.ps1` under `pwsh` on Linux, and stops after 30 minutes. A failed
run keeps `fuzz_output` as the artifact `fuzz-failures-<shard>` for 14 days: download it and regenerate a run as in
step 3. Beside it (since 2026-10-03), the `tracker` job puts Universal Tracker's `worlds/tracker` in at the commit v0.3.4
was released from (`TRACKER_COMMIT`; the same files as the release's `tracker.apworld`, line endings aside, compared
2026-10-03), makes the `Players` folder, and runs `test-apworld.ps1 -TrackerOnly`, also stopping after 30 minutes; a
failed run keeps `fuzz_output_tracker` as `tracker-fuzz-failures-<shard>`. **Both run as five shards of 2000 seeds**
(since 2026-10-04, the user: ten minutes a push was too slow): side by side, each draws its own seeds (fuzz.py's
`random.randint`, unseeded), so 10000 each as before; only the first fuzz shard also runs the tests and the Logic Test
check (the others pass `-FuzzOnly`). Five, not ten: each shard spends about a minute on setup, and a free account runs
20 jobs at once. **When seeds get heavier** (more locations, the room-by-room areas, the entrance randomizer; a seed
took about 0.2 s on CI's 4 cores, 2026-10-04): more shards first; fewer seeds per push with the full 10000 only at
release is the user's call, since the rule is 10000 every run (the user: "might eventually want to split it up more or
run smaller").
**Nobody waits on CI** (the user, 2026-09-29): after a push, work goes on. GitHub emails whoever pushed when a run
fails, and the agent reads `gh run list` once when a session starts and fixes anything red first. Only `release.ps1`
waits for green, on purpose.

## Play-testing the logic: the Logic Test apworld

The tests and the fuzzer check the logic against itself. The
[Logic Test](https://github.com/palex00/Archipelago/tree/logic-test-apworld/worlds/logic_test) apworld (gerbiljames
and palex00) checks it against the game: you play a seed in step with the logic's spheres. It adds a slot that
generates the room's other games a second time, reads their spheres, puts sphere i's key (`KEY_i`) at every location
of sphere i and keeps the real items. Once every `KEY_i` is found, its client opens sphere i and that sphere's items
arrive. Every location becomes a check you must do, shops included, and nothing arrives early.

**Reading the result:**

- **Stuck:** a sphere's last keys can't be reached in the game. The logic is looser than the game. In a normal seed
  that means a seed that can't be finished, which our rules never allow: fix the logic. To carry on, send the stuck
  location from the server console: `/send_location <slot> <location>`.
- **An early key:** the client logs `LOGIC LEAK: KEY_j received while on sphere i`. The logic is stricter than the
  game. Our rules allow that (the logic may be more cautious than the game), so note it and decide whether the caution
  is wanted.

**Steps:**

1. Once: copy `worlds/logic_test` from palex00's fork (branch `logic-test-apworld`) into your Archipelago checkout's
   `worlds/`, never into this repo. We use commit `795f13b` (world 0.4.0, 2026-09-28); its own 27 tests pass at
   0.6.7 (2026-09-29) and at our tag 0.6.8 (2026-10-08). CI pins the same one. Read a newer commit before using it (`licensing.md`).
2. `test-apworld.ps1` runs `dev-scripts/logic-test-check.py` whenever `worlds/logic_test` is there. The Logic Test's
   second generation must equal the seed we generated. When they differ, the tool fills the gaps from whatever is left
   over without a word, and a stall could come from the mismatch instead of the logic. The check compares the copy's
   Bug Fables slot_data and every relocated item over five presets, both `count_events` values, three room layouts
   and three seeds (90 generations, a few seconds; `--seeds N` changes the three). `--negative` gives the copy a wrong
   seed and must flag every run. It
   also fails when the world rolls with Python's global `random` instead of `self.random` (2026-09-29, a scratch
   patch on the start room: 24 of 60 runs flagged).
3. Player files: yours, plus `name: LogicTest`, `game: Logic Test`, `Logic Test: {count_events: false}`. Keep
   `count_events` off. With it on, a location behind a story event lands one sphere after the event, so reaching it
   right after the event logs an early key although the logic is right (read from its `compute_spheres`, 2026-09-29).
4. Generate and host as in "A local server to test against". Connect the game, then the Logic Test client: the
   Launcher's **Logic Test Client**, or from the checkout `python -m worlds.logic_test.client --name LogicTest
   ws://127.0.0.1:38281`. Its tab shows the sphere, the keys found and an **Open Sphere** button; in its console,
   `/status`, `/proceed` and `/keys` (the locations still holding this sphere's keys) do the same.
5. Play: do every check you can reach. When the client says ready, open the sphere; its items arrive from LogicTest.

**Measured without the game (2026-09-29):** a default seed with spheres of 41, 22, 4 and 2 keys, hosted locally. A
script played BugTester, and the client's own code opened each sphere. Every sphere's items arrived from the LogicTest
slot, 69 in all. A sphere-2 key sent first was logged as a leak, and `/keys` listed sphere 1's 41 locations. **Its
data package** holds 100,000 location names, 2.18 MB of JSON, and every client in the room is offered it. The mod
downloading it is not yet seen.

## What each item gates: a report to read against the game

The tests prove the logic agrees with itself; this shows what it says, item by item, so a person can hold it against
the game (the user, 2026-09-30: "exclude 1 or a few certain progression items to see what/if they break anything").
From your Archipelago checkout, with the world linked:

```sh
python <this repo>/dev-scripts/item-gates.py [--pairs] [--option name=value ...] > item-gates.md
```

For each progression item, every location and story event lost without it; with `--pairs`, what two items lose only
together (a rule with an *or*, where either will do). By default every member, move and Jump is an item. Story events
are earned from the item pool, never handed out, so a chain breaks where it would in play.

**Reading it:**

- **An item loses less than the game needs it for:** a rule is missing, and the logic is looser than the game. The
  report can't find this alone (a rule nobody wrote removes nothing), but it lists what each item gates, so a gap stands
  out against what the player knows.
- **An item loses more than the game needs:** a rule is too strict. Allowed (the logic may be more cautious than the
  game), but it's a stand-in to replace when the room is mapped. The ability spots' story order shows up this way today.
- **"Unreachable even with everything"** at the top: a region or rule is broken.

Once a need is confirmed on screen, it gets a test the Archipelago way, `assertAccessDependency` (`docs/tests.md` in
the checkout): the listed locations can't be reached without the item, and no other location depends on it.

**First run (2026-09-30), the default above:** 79 locations and events, 13 progression items, from Jump (45 lost) to
Bee Fly (2); no two items stand in for each other anywhere, since no rule has an *or* yet.

## Proving a refactor changed nothing

`python dev-scripts/seed-snapshot.py --archipelago <your checkout> --out <folder>` generates CI's three presets, alone
and with APQuest, each with a fixed seed, and writes each seed's Bug Fables slot_data (decoded as MultiServer does, keys
in their own order) and its spoiler. Take one snapshot before the change and one after, then `diff -r` the two
folders: a refactor leaves them identical. Two snapshots of the same code were identical (2026-09-28), so any
difference is the change's.

## Dev console (test files only)

Set `DevConsole = true` under `[Debug]` (`copy-dev.ps1 -DebugOn DevConsole`). In game, **F9** opens a command
line at the bottom of the screen; Enter runs, Escape closes. The player is frozen while it's open.

- `loc <n>`: go to pickup location n (the apworld's id, e.g. `loc 5`) and stand next to it. If it was taken,
  its flag is cleared first so it's back. A location with no flag of its own (a crystal berry, a respawning pickup)
  is refused, naming the `warp <map> @<entity>` to use instead.
- `warp <map> [flag]`: go to a map by `MainManager.Maps` name or number (`warp TestRoom` included). Without a flag it
  lands once, where walking in through a door into the map ends (a second move after arrival once came after an
  enemy had touched the party, and the battle's start froze, 2026-09-26). With a flag it lands on the entity, then
  steps beside it; with `@name` it lands at the map's origin, then steps beside the named entity; either way, not if a
  battle, event or dialogue has started by then. Before arriving it marks the map's auto-start cutscenes seen (their
  flags set in the save), since out of story order they can crash; `loc` and `enemylook` warp the same way.
  **Never onto water, a hole or spikes** (2026-10-04: beside a docked sub and on Mystery Island's door arrival, the
  party landed over water and respawned in a loop until the respawn guard sent it to the start): once landed, a warp
  checks the ground under the party and moves it beside a save point or a door when it isn't safe, and an `@name`
  warp with no safe side to its entity uses that too, never the entity's own spot over water. **A sub dock** (`warp
  <map> @Fixedsub...`) and a plain warp to Mystery Island land where the sub's own landing puts the party (the dock
  scene's six spots, Event153; seen on Mystery Island 2026-10-04, the user: "this is where i arrived").
  An `@name` entity counts as there when it's active or when the game's own existence test (`CheckIfCanExist`: its
  flags and regional flag) lets it be, since the game culls entities far from the camera (2026-10-05: Seedling
  Haven's grass, 45 units from the save crystal). When it isn't, the message says why (active state, the test, its
  regional flag). It doesn't start the room's music, so a test that needs it lands next door and walks in; starting
  it, and a command to play any track by name, are parked (the user, 2026-10-04).
- `warp <map> from <map> [door]`: arrive as the named map's door into this one does: appear at the door's spot and
  walk in, with no safe-ground move (the appear spot is often off the ground). The door's name picks one where there
  are several. Seen 2026-10-05, each entrance of the room outside the city.
- `spawn <item|key|medal> <id> [flag]`: drop a pickup next to you. With a pickup location's flag, on that
  location's map, it is that location. With `@<location id>` in place of the flag, it gets the starburst a check there
  draws, from that location's scout (looks only; taking it gives the spawned item): a class or kind's colour seen
  where no check of it is left. `spawn member <n> [x z]` drops party member n's look (0 Vi, 1 Kabbu, 2 Leif)
  at that offset from you, to see how a location holding him looks; it's a Crunchy Leaf underneath, given if taken.
- `flag <n> [on|off]`: show or set a story flag.
- `berry <n> [on|off]`: show or set a crystal berry's taken mark (`crystalbflags`); off brings it back on the next
  room load.
- `liveslot [file]`: lays `BepInEx/bugfablesap-live.json` (or the file named) over the login's `slot_data`, rebuilds
  the seed's tables and re-enters the room, logging which keys differ from the seed. Write the file with
  `python dev-scripts/live-slot-data.py --archipelago <checkout> --yaml <the seed's player file> --out <that path>`:
  it generates that player file twice and keeps only the keys both seeds agree on, so a seed's own rolls (placements,
  shuffles) stay the server's. An apworld change then shows in the running game with no new seed or file.
- `textsearch <word>`: every text file the game loads from `Resources/Data`, searched case-insensitively; up to 200
  matching lines go to the log with their file and line number (the game's own names for things, 2026-09-27).
- `discovery <n> [on|off]`: show or set a journal discovery (no pop-up), to replay a scene that records one.
- `heal`: the game's own full heal (HP and TP, the whole party). Test files only.
- `tokens [n]`: show or set the Termacade's token count (`flagvar[27]`). Test files only.
- `killall [hp]`: in a battle, every enemy's HP to 0; the battle's own death check ends them after the next action (a
  boss a test party can't hit, such as the spider in the air). With a number, every enemy above it brought down to it
  (2026-10-08, the user: to test a scripted HP end, such as the swamp boss's at 10). Test files only.
- `take <item|key> <id>`: removes one from the inventory, as the game's own `removeitem` does. Test files only.
- `give <item|key> <id>`: `take`'s other half, as a received item is added: a key item once, an item if the bag has
  room. With `take`, an ability's key item (`Abilities.Keys`: the Dash 206, the Horn Slash 202) goes and comes back
  one at a time (2026-10-09, the user: "nice to have something for giving/taking the abilities away for testing").
  Test files only.
- `warpicon leaf|key|scroll`: the Warp button's icon (the leaf is the default), shown the next time the pause menu
  opens.
- `warpcolor orange|pink|lime|<hue>`: the drawn backdrop's colour, for `warpicon scroll` (a design test).
- `enemylook <enemy id|off> [move]`: reloads the current map with every ordinary map enemy looking like that enemy (a
  visual test for enemy shuffle's map look; the fights stay the seed's); with `move`, they also move like a map enemy
  whose fight starts with that enemy. Puzzle enemies keep their own look.
- `enemyfight <enemy id> [id...] | off`: every map fight starts with those enemy ids instead of the seed's (a test).
- `unstick`: stops the last scene's coroutine (`Event<n>`: a stuck scene left running threw once the cleanup removed
  its stand-ins, 2026-09-26), runs the game's own end-of-cutscene cleanup, when a cutscene died and left you frozen, and
  ends a map transfer stuck walking to a spot it can't reach. It also takes the party off anything a scene parked it on
  and lifts a leftover fade: the boat scene crashed mid-fade and left a black screen with music playing, which the
  cleanup alone didn't clear (2026-09-25; the screen was seen coming back).
  It also resets the party's bodies (gravity, physics, forced animation), and closes a dialogue that died mid-line:
  the game kept thinking a box was open (`message`) after a city NPC's line threw, which froze the player until
  `unstick` did what the game's own dialogue end does (2026-09-25). The speech box itself stayed on screen after two
  tries (removing the text's holder, then `maintextbox`); the new `gui` command showed a `Textbox(Clone)` under the GUI
  camera that `maintextbox` no longer pointed at, so `unstick` now removes any such box once dialogue has ended.
- `hazardloop`: sets where the game puts the party back after a fall (`lastpos` and `lastloadzone`) 3 above the nearest
  point of water or a hole with no ground over it, and holds it until the next fall's respawn starts (walking in a
  `Respawn` zone would move it back), so that fall loops: the respawn-loop guard's test (documentation.md, step 40).
- `oldtravel <map> <entity>`: map travel's landing from before 2026-10-02, beside that save point through the game's
  2-argument `TransferMap`; `oldtravel SwamplandsBridge 6` gives the swamp's original respawn loop (step 40).
- `travel <area>`: map travel's own path to area n (0 the Outskirts to 24), visited or not, arriving through the door
  `WarpButton.cs`'s `AreaDoors` names: to check every travel spot (documentation.md, step 10).
- `gui`: log what hangs under the GUI camera (name, active, renderer, children), to find what's really stuck on screen.
- `display`: log the monitor's reported resolution and refresh rate, the window, the game's FPS and VSync settings,
  what Unity was given (`vSyncCount`, `targetFrameRate`), the measured frame rate, how many characters Uncap FPS draws
  smoothed and Unity's `Physics.autoSyncTransforms`.
- `fps <cap>` (-1 uncapped): the frame cap for this session only, VSync off; the game's settings put theirs back when
  applied. `interp on|off`: Unity's rigidbody interpolation on every character on the map. `camlerp on|off`: the camera
  drawn between physics steps (`FrameRate.cs`); `bodylerp on|off`: the characters drawn the same way; `scenerylerp
  on|off`: swinging and bobbing scenery drawn the same way (`FrameRate.Scenery.cs`). All three come on with Uncap FPS;
  the console turns one off to compare, until the row is next switched. `fps` is a look at higher frame rates without
  the row; frame-counted logic runs fast meanwhile.
- `bodytrace [frames]`: from when the leader or a follower starts moving, logs each drawn frame's step share, her last
  step's move, the leader's and the first follower's place on screen drawn and true, what she stands on (name/tag),
  its place drawn and true and how much scenery is drawn shifted, and her parent, then the spread of the
  frame-to-frame change of each (a steady walk, drawn smoothed, holds still).
- `trace [frames]`: while you move with an NPC's emoticon showing, logs where the player, the NPC and its emoticon land
  on screen each drawn frame, with the camera's and the emoticon's angles. `cams`: every camera, its depth, parent and
  layer mask.
- `il <Type> <Method> [iter]`: every overload's IL (a coroutine's MoveNext with `iter`) to the log, to write a
  transpiler against the real instructions. `rates [seconds]`: with Uncap FPS on, what frames were worth in sixtieths
  and how many started a new sixtieth, per second (both 60: as at 60 fps). `fpsscan`: reads every method of the game and
  compares with Uncap FPS's fixed lists.
- **A transpiler must never throw.** One that throws stays registered on its method, and the next patch of that
  method by any feature fails with it until the game restarts (the mod guide, step 24). `CodeInstruction.labels` isn't
  usable in this game's HarmonyX, and neither is Harmony's `GetOriginalInstructions` (it needs
  `System.Reflection.Emit.ILGeneration`); `PatchProcessor.ReadMethodBody` works.
- `audioprobe`: on or off; while on, logs each audio source played while disabled (Unity's "Can not play a disabled
  audio source" names nothing): its object's path, whether the object or the component is off, its clip, the map. Found
  the infinite jump firing in battles (2026-10-04, `infjump` below). Off with a reload.
- `frames [seconds]` (5 by default): logs the frame count, median and slow frames with their times, the camera's draw
  time and garbage collections. A collection is marked on the frame before the slow one it causes.
- `nudge <x> <y> <z>`: shift the party by that much on the current map.
- `script <map>`: log each row of a map's dialogue table that has `|command|`s, its commands only (`line` gives a
  row's text). `pos <map> <index...>`: log those entities' start positions
  from the map's entity table. `prices <medal id...>`: log each medal's price columns (berries and crystal berries).
- `markcolor <progression|useful|trap|filler> <hex>`: a class's starburst colour, live (a design test).
  `markclass <entity> <class>` draws one slot's backdrop as that class; `markclass off` puts them back.
- `hide <entity>`: switch an entity on this map off until the map reloads (nothing saved).
- `items`: list every pickup that exists on the current map right now (kind, id, flag, distance), in the log.
- `tree`: log the nearest pickup's whole object tree: each object, whether it's active, and its renderers, on or
  off. Settles what's really on screen when a visual fix doesn't take.
- `addleif`: add Leif to the party on a file where he hasn't joined (test files; in memory until the game next
  saves). `ChangeParty({0, 1, 2}, fromscratch: true)` rebuilds the party list, then `SetPlayers` makes all three
  characters where the party stands. The 2026-09-24 try failed because without `fromscratch` the game's copy loop never
  runs (`for m < 0`) and the list comes out empty. First run (2026-09-25): three members, three characters, no errors.
- `holdup [member n]`: queue a test hold-up (the Explorer Permit "from TestPlayer", or party member n: 0 Vi, 1 Kabbu,
  2 Leif), display only, the way an item from another player is shown.
- `articles [id...]`: log the found-item line's default article, each listed item's own, and the "You got" lines.
- `holdup ap`: the drawn Archipelago icon held up on two class backdrops (plum, cyan).
- `areas`: log the game's area names by number (`MainManager.areanames`, the numbers maps carry as
  `MapControl.areaid`), the logic's one module per area (apimplementation.md, build step 29).
- `holdup long`: four "You got" lines too wide for the box (the one seen, a longer one, the longest server name with a
  player and alone), to check the fitting (the mod guide, step 9).
- `shelflook <slot> <white|black> [current|first] [key=value ...]`: a shop slot shows the drawn icon in that look, to
  compare looks on a shelf. `<slot>` is a location id, or `#n` for a medal shelf's n-th slot from the left on screen.
  The look is `current` (the icon in use, the default) or `first` (the thin first look, kept to go back to), each
  number overridable: `rim`, `gap`, `middle`, `radius`, `distance` (shares of the icon's half width: the outline round
  the flower, the lines between circles, the outline round the open middle, the circles' size and spread), `scale`
  (the drawn size) and `sat` (the colours' strength), e.g. `shelflook #2 black current sat=1.2`. `shelflook <slot>
  item|medal <id>` shows a vanilla item's or medal's sprite there instead, beside it; `shelflook off` puts every slot
  back. Every change shows at once, with no rebuild (documentation.md, step 23).
- `iteminfo`: log every item entity on the map with its sprite, pivot, size, lift and backdrop (placement checks).
- `mark <size> <raise> [behind]`: the Item backgrounds starburst's size, the lift it and its item get, and how far
  behind the item it sits, live.
- `shelfforward <step>`: how far this map's listed item shop shelf (`ItemShops.ShelvesForward`) sits toward the
  camera, every slot together, live (documentation.md, step 22).
- `letters`: count the game's 500 text letters that are taken, by owner (to spot a leak).
  **Never log a text holder's own name:** it carries its whole text, and the game's font preloader's is every glyph it
  has. Logging it once (2026-09-26) broke BepInEx's console writer (`ConsoleEncoding.ReadByteBuffer`
  IndexOutOfRange) for the rest of the session: every later log line threw inside the plugin's update, which then
  stopped partway every frame, and the game froze. Only a restart recovers it.
- `menuinfo`: with an Archipelago panel open, its text pieces and letters, one of its letters beside one of the game's
  Settings letters in the GUI camera's own frame (position, rotation, layer, sort, visible), and the camera itself.
- `palette`: log the game's text colours by index (`|color,n|`), Archipelago's added ones included.
- `musiccheck [name...]`: load every `MainManager.Musics` name as the game loads a track (`Audio/Music`) and log which
  have no clip; with names, those from `Audio/Sounds`, where the jingles are. Found the three silent track names
  (2026-10-04, `MEASURED.md`, music and jingles).
- `colortry <hex...>`: queue a trap's "You got" line in each colour given, to compare them on screen.
- `onehit`: flips a test boost: every hit on an enemy does at least 99 (before defence). It's the `[Debug]` setting
  `OneHit` (off in the code), so it survives reloads; `copy-dev.ps1 -DebugOn OneHit` turns it on for a dev install.
- `bumpkill`: flips a test boost: every field enemy you touch dies with no battle, through the game's own bump check
  (`NPCControl.CheckBump`, the medal that kills weak enemies on contact) made to always pass. The `[Debug]` setting
  `BumpKill`, kept like `OneHit`. Whether a bump kill sends an Enemysanity check: not checked.
- `infberries`: flips the berry top-up: 999 berries once per save played, when its first map loads (the `[Debug]`
  setting `InfBerries`, off in the code; `copy-dev.ps1 -DebugOn InfBerries` turns it on for a dev test session). Once,
  not on every drop: a refill hid purchases from the item shops, which see a purchase as berries going down
  (2026-09-27).
- `infjump`: flips jumping again in mid-air. It's the `[Debug]` setting `InfJump` (off in the code; `copy-dev.ps1
  -DebugOn InfJump` turns it on for a dev test session), so it survives reloads. Only while the field player is active:
  in battles it's off and never on the ground, so each press of jump played its sound on a disabled source (three to
  nine warnings a fight, found with `audioprobe`, 2026-10-04; none after).
- **`TestDoors`** (`[Debug]`, not a console command): doors rewritten by hand, `Map/Door=LikeMap/LikeDoor;...` (entity
  names): that door leads where the other one leads, the entrance randomizer's proof of concept (`copy-dev.ps1 -DebugSet
  "TestDoors=BugariaOutskirtsOutsideCity/loadzone east=BugariaMainPlaza/LoadingZoneCommercial"`; empty turns it off).
  A door whose name its map shares with another is written `name#row`, as in `doors.json`.
- `line <map> <n> [n...]`: log the full text of a map's dialogue lines (the same table `script` reads), e.g. to find
  every line that mentions something (2026-09-25: the Outskirts lines about the rocks).
- `cam`: log what the camera follows (its target, or "DESTROYED"), the player, offsets, limits and the party with its
  characters. Settled a camera stuck on a removed character (2026-09-25).
- `who`: log every character drawn as Vi, Kabbu or Leif: name, position, what it follows, whether it's a player
  character. Found a stray second player character (2026-09-25).
- `radii`: log every entity on the map, nearest first, with its type, talk radius (`npcdata.radius`) and distance.
  Measured the NPC talking range the save crystals now use (2026-09-29).
- `follower <animid>`: make that character follow the party the story's way (the follower list, then `AddFollower`),
  e.g. `follower 46` (Maki) to replay the castle briefing, which needs her.
- `addmember <0|1|2>`: with `TestStartMember`, add Vi, Kabbu or Leif to the party, standing in for receiving them.
  It redoes the map's enemy-only walls for the new characters (`MEASURED.md`, enemy-only walls).
- `removemember <0|1|2>`: take a member out of the party; the member guard then keeps them out of the story's party
  changes (the members left are allowed, the first as the start), for trying a party the story never has.
- `switchhere`: moves this room's day/night switch NPC to where the player stands, and keeps it there for the session
  (day and night alike, nothing saved), to try spots without a new seed (2026-10-07, the settlement's switches).
- `find <name>`: logs every object on the map with that name, shown or hidden: where it is, how far from the player,
  and each collider's bounds and `ConditionChecker` (2026-10-07: the settlement's `Cube` wall, out of `solids`' reach).
- `solids`: logs every solid collider under and within 4 of the player, with its path, size, components and any
  `ConditionChecker` switch: what an invisible wall is.
- **`TestStartMember`** (`[Debug]`): a new randomizer file starts with that one member (0 Vi, 1 Kabbu, 2 Leif); the
  story adds nobody else. -1 = off. Only without a seed: a connected seed's `starting_member` wins, -1 (the story's
  party) included (2026-09-27: a story-party seed with a leftover `TestStartMember = 2` started Leif alone).
- `berries <n>`: add n berries (negative takes them), clamped to 0-999 as the game's own `money` script command does.
  For shop tests.
- **`TestStart`** (`[Debug]`, not a console command): a map name (`MainManager.Maps`), optionally `@` the map you
  arrive from, e.g. `BugariaMainPlaza@BugariaOutskirtsOutsideCity`. A new file starts there, arriving through that
  map's door into it (without `@`, the first door found), a stand-in for a random start until the seed chooses one
  (`copy-dev.ps1 -DebugSet TestStart=...`; empty turns it off).

## Every Debug setting

All live under `[Debug]` in `BepInEx/config/bugfables.archipelago.cfg`, are off by default, and are switched with
`copy-dev.ps1 -DebugOn <name>` / `-DebugOff <name>`, or set with `-DebugSet Name=Value` for the ones that take a
number or text (step 3 above). Dev installs and test files only. They exist
only in the dev (Debug) build: every one is bound in `Dev/Plugin.Dev.cs`, and the release build leaves `Dev/` out.

| Setting | What it does |
|---|---|
| `DevConsole` | F9 opens the dev console (section above). |
| `DevCommandFile` | With `DevConsole`: a text file whose lines are run as console commands, then emptied, so a test can be driven from outside the game. Blank lines and lines starting with `#` are skipped; a queued `loc` or `warp` waits until the player is free, the opening done and no fade playing (2026-10-07: one fired between the opening's end and its fade-in, whose new fade destroyed the one the transfer waited on), and `unstick` goes first, dropping any waiting warp (2026-10-06: a frozen scene held it behind one for good). |
| `InfJump`, `OneHit`, `BumpKill`, `InfBerries` | With `DevConsole`: jump again in mid-air; every hit on an enemy does at least 99; field enemies die on touch; 999 berries once per save played. The console's `infjump`, `onehit`, `bumpkill` and `infberries` flip them. |
| `AdoptSeed` | A save tied to another seed is re-tied to the connected one and replays every item (section "A local server to test against"). |
| `QuietBursts` | Items arriving several at once (a goal's release, a new seed's replay) get no hold-up; one arriving alone still does. |
| `TestStart`, `TestStartMember`, `TestDoors` | A new file's start map, its one party member, doors rewritten by hand (Dev console section). |
| `GiveMoney` | Berries to add once (capped at 999), then back to 0. |
| `GrantProbe`, `TextProbe` | Log every key item added or removed, quest board change and flip of `flags`, `regionalflags` or `crystalbflags` / every dialogue line with an item command, with the map. |
| `TlsProbe` | For each `wss://` connection, logs what this Mono's own certificate check decided (policy errors, the chain it built, each chain status), then accepts the certificate as websocket-sharp always does, so connecting is unchanged. |
| `SaveDiff` | Two save file names, `a.dat\|b.dat`: once per load, logs what differs between them. |
| `PatchDump` | Every method the mod patches (target, kind, patch method, priority), Uncap FPS's hooks included, sorted, to `bugfablesap-patches.tsv`, once per load: diff it before and after a change to how hooks are installed. The log also gets the run order wherever one target has several of the mod's hooks of a kind. |
| `SeedDump` | Once per load, when a login brings the seed: everything the mod read from its slot_data, one sorted line per entry, to `bugfablesap-seed.tsv`, to diff before and after a change to how slot_data is read. |
| `QuestDump` | Every board quest's name, its `BoardData` numbers (column 3: the flag taking it sets) and its `QuestChecks` row, to `bugfablesap-questdump.tsv`; and the Termacade's prizes (kind, item, price, once only, flag) to `bugfablesap-termacade.tsv`. |
| `EntityDump`, `ScriptDump`, `MapDump`, `VarDump`, `SpriteDump` | Write the game's entities, dialogue commands, map events, script slots or GUI, item and medal sprites (`bugfablesap-guisprites.tsv`, `bugfablesap-itemsprites.tsv` and their sheets) to `bugfablesap-*.tsv` / `.png` in the BepInEx folder. A labelled contact sheet can be made from a table and its sheet (game art: kept local, never the repo). |
