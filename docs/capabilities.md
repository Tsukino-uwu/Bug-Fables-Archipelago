# What this project's code may do

Everything in this repository that reaches beyond its own files, listed with the reason. The list is checked, not
just written: `dev-scripts/preflight.py` refuses a commit that does something listed nowhere here, and a row that
nothing uses any more. So this page is exactly what the code does, no more and no less; `git log -p` on it is the
history of every capability the project gained or dropped.

Adding a row is the maintainer's decision, made in a commit of its own. How to check all of this yourself:
[reviewing.md](reviewing.md).

## Hosts

Every place a file in this repository points to, in code, docs or build files. Loopback (`127.0.0.1`,
`localhost`) is left out: it is the player's own machine.

| Host | Why |
|---|---|
| `github.com` | This repository and its releases, the projects cited in the docs, and the workflow actions |
| `archipelago.gg` | Archipelago's site, and the mod's default server address (`Core/Plugin.cs`), which the player can change |
| `api.nuget.org` | The NuGet feed the mod's libraries are restored from at build time (`nuget.config`), and the package CI checks the three shipped libraries against |
| `nuget.bepinex.dev` | BepInEx's own package feed, for its packages only (`nuget.config`) |
| `steam://rungameid` | Starting the game through Steam, in a dev log entry |

## Apworld: reflection by name

The apworld runs on whichever machine generates a seed. It imports only the names listed in the patterns file
(nothing that reads or writes files, runs programs or opens connections); it reads its own data files, which ship
inside it, through `pkgutil.get_data`. A few places look an attribute up by a name held in a variable rather than
written in the code. Each is listed here with where the name comes from, since a name in a variable is where a
reviewer can't see at a glance what is reached.

| Where (file and function) | What it looks up, and where the name comes from |
|---|---|
| `apworld/bug_fables/abilities.py item_count` | The yaml option an ability's item count depends on; the option names are written in `ABILITIES`, in the same file |
| `apworld/bug_fables/locations.py category_on` | The yaml option that switches a location category on or off; the names are written in `CATEGORY_OPTIONS`, in `options.py` |
| `apworld/bug_fables/data_types.py present` | A record's own fields, as `dataclasses.fields` lists them |
| `apworld/bug_fables/slot_data.py _by_location` | One named field of each location's source record; each name is written at the calls in the same file |

## Mod: what the code touches

The mod runs inside Bug Fables on the player's machine. Beyond the game itself, this is everything its source does,
file by file. Files under `Dev/` are compiled only into the developer's build: the release build leaves `Dev/` out,
and preflight checks the shipped DLL holds none of it.

| File | Does | Why |
|---|---|---|
| `mod/BugFablesAP/Core/ApConnection.cs` | connects to a server | The mod's one connection: to the Archipelago server address the player types in the in-game menu, through Archipelago.MultiClient.Net |
| `mod/BugFablesAP/Core/SaveRedirect.cs` | reads files | Reads the randomizer's own save files, which live in a folder of their own so normal saves are never read or written |
| `mod/BugFablesAP/Core/SaveRedirect.cs` | writes files | Writes those save files the way the game writes its own: to a temporary file first, keeping the previous one as a backup |
| `mod/BugFablesAP/Gameplay/FrameRate.cs` | finds code by name | Finds game types by name, only in the game's own assembly, from a fixed list in the same file, to patch their frame-rate timers |
| `mod/BugFablesAP/Ui/ApMenu.TextEntry.cs` | uses the clipboard | Pastes into or copies from the panel's text boxes (address, port, slot, password), only when the player presses paste or copy there |
| `mod/BugFablesAP/Dev/DevConsole.cs` | finds code by name | Dev build only: finds a game type, only in the game's own assembly, by the name typed into the dev console |
| `mod/BugFablesAP/Dev/DevConsole.cs` | reads files | Dev build only: reads console commands from a file the developer names in the config (none by default) |
| `mod/BugFablesAP/Dev/DevConsole.cs` | writes files | Dev build only: empties that command file once its commands have run |
| `mod/BugFablesAP/Dev/DevReload.cs` | finds code by name | Dev build only: finds BepInEx's ScriptEngine among the loaded assemblies, to reload the plugin in a running game |
| `mod/BugFablesAP/Dev/DevReload.cs` | reads files | Dev build only: hashes the plugin DLL to notice a new build |
| `mod/BugFablesAP/Dev/DevReload.cs` | writes files | Dev build only: writes a one-line reload status in the BepInEx folder |
| `mod/BugFablesAP/Dev/EntityDump.cs` | writes files | Dev build only: dumps every map's entities, the item and medal names and the enemy table to files in the BepInEx folder |
| `mod/BugFablesAP/Dev/LiveSlotData.cs` | reads files | Dev build only: reads the live slot_data file in the BepInEx folder that `live-slot-data.py` writes |
| `mod/BugFablesAP/Dev/MapDump.cs` | writes files | Dev build only: dumps each map's auto-start events and hazards to one file, and its flag-switched scenery to another, in the BepInEx folder |
| `mod/BugFablesAP/Dev/PatchDump.cs` | writes files | Dev build only: dumps every patch the mod made to a file in the BepInEx folder, to compare before and after a change |
| `mod/BugFablesAP/Dev/QuestDump.cs` | writes files | Dev build only: dumps every board quest to a file in the BepInEx folder |
| `mod/BugFablesAP/Dev/SaveDiff.cs` | reads files | Dev build only: reads two save files and logs what changed between them |
| `mod/BugFablesAP/Dev/ScriptDump.cs` | writes files | Dev build only: dumps the item, flag, event and transfer tokens of every dialogue line to a file in the BepInEx folder |
| `mod/BugFablesAP/Dev/SeedDump.cs` | writes files | Dev build only: dumps every table the mod read from the seed's slot_data to a file in the BepInEx folder |
| `mod/BugFablesAP/Dev/SpriteDump.cs` | writes files | Dev build only: saves the game's interface sprite sheets and its item and medal sprites, each with an index, as images in the BepInEx folder |
| `mod/BugFablesAP/Dev/VarDump.cs` | writes files | Dev build only: dumps which flag slots the game's text uses to a file in the BepInEx folder |

## Mod: patches outside the game

The mod changes the game's own code at run time with Harmony, as every BepInEx mod does; those patches are its
purpose and are read in `code-map.md`. These are the only ones that change code that isn't the game's: a library or
Unity itself. Read from the shipped DLL's `[HarmonyPatch]` attributes, as `assembly:type::method`; between releases, a
row the committed DLL predates stands while today's source makes that patch.

| Target | Why |
|---|---|
| `Archipelago.MultiClient.Net:Archipelago.MultiClient.Net.Helpers.ArchipelagoSocketHelper::CreateWebSocket` | Turns on compression (permessage-deflate) for the connection, which MultiClient.Net never enables (`Core/WebSocketCompression.cs`) |
| `websocket-sharp:WebSocketSharp.WebSocket::validateSecWebSocketExtensionsServerHeader` | Accepts the server's compression reply, whose `server_max_window_bits` websocket-sharp would otherwise reject; dropping it is safe (`Core/WebSocketCompression.cs`) |
| `Archipelago.MultiClient.Net:Archipelago.MultiClient.Net.DataPackage.FileSystemCheckSumDataPackageProvider::GetFileSystemSafeFileName` | Makes the game name and checksum the server sends a plain file name before MultiClient.Net caches its data under it; in 6.7.1 this function returns its input unchanged, so a server could pick the path (`Core/CachePaths.cs`) |
| `Archipelago.MultiClient.Net:Archipelago.MultiClient.Net.DataPackage.FileSystemCheckSumDataPackageProvider::TryGetDataPackage` | Cleans the checksum the same way before the cache is read, which 6.7.1 never does (`Core/CachePaths.cs`) |
| `UnityEngine.AnimationModule:UnityEngine.Animator::Play` | Skips a play of an animation state a character lacks, which would only log two Unity warnings and play nothing; only while Archipelago is on, or with *Use on normal saves* (`Guards/AnimGuard.cs`) |

## Dev scripts and hooks: what they touch

These run on the maintainer's machine (and on yours, if you run them); nothing here ships to players. The git hooks
run by themselves on each commit and push, once a clone arms them (`git config core.hooksPath .githooks`). Reading
files is left out; everything else they do is listed.

| File | Does | Why |
|---|---|---|
| `.claude/hooks/agent-guard.py` | runs programs | `git status`, to see whether a commit the coding agent makes may carry a change the maintainer decides: this list, the patterns file, or the guard itself |
| `.githooks/pre-commit` | runs programs | `git`, to find the repository, then Python: preflight and doc-coverage on what is staged |
| `.githooks/commit-msg` | runs programs | `git`, to list what is staged, and Python, to count the subject's characters |
| `.githooks/pre-push` | runs programs | `git`, to list what each push carries, then Python: preflight on each pushed tip and on its new history, and the gate's own test when the push changes the gate |
| `.githooks/python.sh` | runs programs | `git config`, for a Python this clone names, then each Python it tries, once, to see that it runs and is 3.11 or newer |
| `.githooks/doc-coverage.py` | runs programs | `git ls-files`, to list the sources every guide must name, the Markdown files whose links it checks, and the files and folders a link may lead to |
| `dev-scripts/preflight.py` | runs programs | `git`, to read exactly what a commit holds |
| `dev-scripts/negative-test-preflight.py` | runs programs | `git` in a throwaway clone, preflight itself, and the agent guard: directly, and through the settings' hook command under `sh -c` |
| `dev-scripts/negative-test-preflight.py` | writes files | The throwaway clone, in the system's temp folder, removed afterwards |
| `dev-scripts/verify-release.py` | runs programs | `git`, to read what a commit holds, and preflight itself, on the release's DLL and on the yaml's text |
| `dev-scripts/verify-release.py` | writes files | The release's DLL, copied next to the zip for preflight to read, and removed again |
| `dev-scripts/build-release.ps1` | runs programs | `git` (clean clones of HEAD), `dotnet build`, and Python for preflight |
| `dev-scripts/build-release.ps1` | writes files | The mod download in `release/`, and the temporary clones it builds in |
| `dev-scripts/stage-dev.ps1` | runs programs | `dotnet build`, the dev build |
| `dev-scripts/stage-dev.ps1` | writes files | The dev build, staged in the gitignored `stage/` folder |
| `dev-scripts/copy-dev.ps1` | writes files | Copies the staged plugin into your Bug Fables install and sets config keys, keeping a backup of each file it replaces |
| `dev-scripts/release.ps1` | runs programs | `git`, `gh` and the build script, to cut a release |
| `dev-scripts/release.ps1` | talks to GitHub | Pushes `main`, then starts and watches the release workflow with `gh` |
| `dev-scripts/test-apworld.ps1` | runs programs | `python`: the apworld's tests, the fuzzer, and the fuzzer again with Universal Tracker's hook (`tracker_fuzz_hook.py`), in your Archipelago checkout |
| `dev-scripts/test-apworld.ps1` | writes files | Renames the fuzzer's output folders in your Archipelago checkout (`fuzz_output`, `fuzz_output_tracker`), so the second pass keeps the first one's results |
| `dev-scripts/seed-snapshot.py` | runs programs | Archipelago's generator, in your Archipelago checkout |
| `dev-scripts/seed-snapshot.py` | writes files | Each case's slot data and spoiler, in the folder given with `--out`; the player files and the generator's output in a temporary folder, removed afterwards |
| `dev-scripts/seed-snapshot.py` | unpickles Archipelago's own output | Reads a generated seed file with Archipelago's own restricted loader |
| `dev-scripts/send-as-player.py` | connects to a server | A local test server (`127.0.0.1`), logged into as a second player |
| `dev-scripts/live-slot-data.py` | loads a script by path | Loads `seed-snapshot.py`, next to it, to share its generate and decode |
| `dev-scripts/live-slot-data.py` | writes files | The file given with `--out`; the player file and the generator's output in a temporary folder, removed afterwards |
| `dev-scripts/door-graph.py` | loads a script by path | Loads `gate-table.py`, next to it, to share its reader |
| `dev-scripts/door-graph.py` | writes files | `--export` writes `data/doors.json` for the apworld |
| `dev-scripts/event-transfers.py` | loads a script by path | Loads `gate-table.py`, next to it, to share its reader |
| `dev-scripts/enemy-table.py` | writes files | `--export` writes `data/enemies.json` for the apworld |
| `dev-scripts/save-points.py` | writes files | `--export` writes `data/starts.json` for the apworld |

## CI workflows: what they reach

These run on GitHub's own runners, never on a player's or a reviewer's machine: on every push and pull request, and
when the maintainer starts a release. Each action and each other repository is named in its row; each action is pinned
to a commit, each repository to a tag or a commit.

| Workflow | Does | Why |
|---|---|---|
| `.github/workflows/ci.yml` | uses actions | `actions/checkout`, `actions/setup-python` and `actions/upload-artifact`: the code, Python, and the built apworld and yaml kept for the release (and a failed fuzz run's files) |
| `.github/workflows/ci.yml` | checks out other repositories | `ArchipelagoMW/Archipelago` at the tag the world targets, to test and build the apworld inside it; `Eijebong/Archipelago-fuzzer`, for its fuzzer; `palex00/Archipelago`, only its Logic Test world's folder; `FarisTheAncient/Archipelago`, only its Universal Tracker world's folder, for its fuzzer hook |
| `.github/workflows/ci.yml` | runs programs | Archipelago's own install, its tests (`pytest`), its generator and its apworld builder, and `dev-scripts/test-apworld.ps1` (the Logic Test check, the fuzzer, and Universal Tracker's fuzzer hook) |
| `.github/workflows/ci.yml` | downloads packages | Archipelago's requirements from PyPI (`pip install`, `ModuleUpdate.py`), the way Archipelago's own CI installs them |
| `.github/workflows/preflight.yml` | uses actions | `actions/checkout` and `actions/setup-python` |
| `.github/workflows/preflight.yml` | runs programs | Preflight on the commit and on all history, its negative test, and `dev-scripts/verify-release.py` |
| `.github/workflows/preflight.yml` | downloads packages | Archipelago.MultiClient.Net 6.7.1's package from NuGet (`curl`), to compare the three shipped library DLLs with it byte for byte |
| `.github/workflows/release.yml` | uses actions | `actions/checkout`, `actions/download-artifact` (the apworld and yaml CI built), `actions/attest-build-provenance` and `softprops/action-gh-release` |
| `.github/workflows/release.yml` | runs programs | The version and text checks (preflight on the highlights and the commit subjects), `dev-scripts/build-release.ps1 -Check` (the DLL's stale gate), `zip`, and `dev-scripts/verify-release.py` before and after publishing |
| `.github/workflows/release.yml` | downloads packages | The same NuGet package, to check the published files against it |
| `.github/workflows/release.yml` | talks to GitHub | `git fetch --tags` and `gh release download`, to check what was published against its tag |
| `.github/workflows/release.yml` | publishes to GitHub | The publish job, the only one with write permissions: the release, its tag and its three files (`softprops/action-gh-release`), and the provenance attestations for the apworld and the yaml (`actions/attest-build-provenance`) |
