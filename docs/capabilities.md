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
| `api.nuget.org` | The NuGet feed the mod's libraries are restored from at build time (`nuget.config`) |
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
| `mod/BugFablesAP/Ui/ApMenu.TextEntry.cs` | uses the clipboard | Pastes into or copies from the server-address box, only when the player presses paste or copy there |
| `mod/BugFablesAP/Dev/DevConsole.cs` | finds code by name | Dev build only: finds a game type, only in the game's own assembly, by the name typed into the dev console |
| `mod/BugFablesAP/Dev/DevConsole.cs` | reads files | Dev build only: reads console commands from a file the developer names in the config (none by default) |
| `mod/BugFablesAP/Dev/DevConsole.cs` | writes files | Dev build only: empties that command file once its commands have run |
| `mod/BugFablesAP/Dev/DevReload.cs` | finds code by name | Dev build only: finds BepInEx's ScriptEngine among the loaded assemblies, to reload the plugin in a running game |
| `mod/BugFablesAP/Dev/DevReload.cs` | reads files | Dev build only: hashes the plugin DLL to notice a new build |
| `mod/BugFablesAP/Dev/DevReload.cs` | writes files | Dev build only: writes a one-line reload status next to BepInEx |
| `mod/BugFablesAP/Dev/EntityDump.cs` | writes files | Dev build only: dumps every map's entities, the item and medal names and the enemy table to files in the BepInEx folder |
| `mod/BugFablesAP/Dev/MapDump.cs` | writes files | Dev build only: dumps each map's auto-start events, hazards and flag-switched scenery to a file in the BepInEx folder |
| `mod/BugFablesAP/Dev/PatchDump.cs` | writes files | Dev build only: dumps every patch the mod made to a file in the BepInEx folder, to compare before and after a change |
| `mod/BugFablesAP/Dev/QuestDump.cs` | writes files | Dev build only: dumps every board quest to a file in the BepInEx folder |
| `mod/BugFablesAP/Dev/SaveDiff.cs` | reads files | Dev build only: reads two save files and logs what changed between them |
| `mod/BugFablesAP/Dev/ScriptDump.cs` | writes files | Dev build only: dumps the item, flag, event and transfer tokens of every dialogue line to a file in the BepInEx folder |
| `mod/BugFablesAP/Dev/SeedDump.cs` | writes files | Dev build only: dumps every table the mod read from the seed's slot_data to a file in the BepInEx folder |
| `mod/BugFablesAP/Dev/SpriteDump.cs` | writes files | Dev build only: saves the game's interface sprite sheets and an index as images in the BepInEx folder |
| `mod/BugFablesAP/Dev/VarDump.cs` | writes files | Dev build only: dumps which flag slots the game's text uses to a file in the BepInEx folder |

## Mod: patches outside the game

The mod changes the game's own code at run time with Harmony, as every BepInEx mod does; those patches are its
purpose and are read in `code-map.md`. These are the only ones that change code that isn't the game's: a library or
Unity itself. Read from the shipped DLL's `[HarmonyPatch]` attributes, as `assembly:type::method`.

| Target | Why |
|---|---|
| `Archipelago.MultiClient.Net:Archipelago.MultiClient.Net.Helpers.ArchipelagoSocketHelper::CreateWebSocket` | Turns on compression (permessage-deflate) for the connection, which MultiClient.Net never enables (`Core/WebSocketCompression.cs`) |
| `websocket-sharp:WebSocketSharp.WebSocket::validateSecWebSocketExtensionsServerHeader` | Accepts the server's compression reply, whose `server_max_window_bits` websocket-sharp would otherwise reject; dropping it is safe (`Core/WebSocketCompression.cs`) |
| `UnityEngine.AnimationModule:UnityEngine.Animator::Play` | Skips a play of an animation state a character lacks, which would only log two Unity warnings and play nothing; only while Archipelago is on (`Guards/AnimGuard.cs`) |

## Dev scripts and hooks: what they touch

These run on the maintainer's machine (and on yours, if you run them); nothing here ships to players. Reading files
is left out; everything else they do is listed.

| File | Does | Why |
|---|---|---|
| `.githooks/doc-coverage.py` | runs programs | `git ls-files`, to list the sources every guide must name |
| `dev-scripts/preflight.py` | runs programs | `git`, to read exactly what a commit holds |
| `dev-scripts/negative-test-preflight.py` | runs programs | `git` in a throwaway clone, and preflight itself |
| `dev-scripts/negative-test-preflight.py` | writes files | The throwaway clone, in the system's temp folder, removed afterwards |
| `dev-scripts/verify-release.py` | runs programs | `git`, to read what a commit holds, and preflight itself, on the release's DLL |
| `dev-scripts/verify-release.py` | writes files | The release's DLL, copied next to the zip for preflight to read, and removed again |
| `dev-scripts/build-release.ps1` | runs programs | `git` (clean clones of HEAD) and `dotnet build` |
| `dev-scripts/build-release.ps1` | writes files | The mod download in `release/`, and the temporary clones it builds in |
| `dev-scripts/stage-dev.ps1` | runs programs | `dotnet build`, the dev build |
| `dev-scripts/stage-dev.ps1` | writes files | The dev build, staged in the gitignored `stage/` folder |
| `dev-scripts/copy-dev.ps1` | writes files | Copies the staged plugin into your Bug Fables install and sets config keys, keeping a backup of each file it replaces |
| `dev-scripts/release.ps1` | runs programs | `git`, `gh` and the build script, to cut a release |
| `dev-scripts/release.ps1` | talks to GitHub | Pushes `main`, then starts and watches the release workflow with `gh` |
| `dev-scripts/test-apworld.ps1` | runs programs | `python`: the apworld's tests and the fuzzer, in your Archipelago checkout |
| `dev-scripts/seed-snapshot.py` | runs programs | Archipelago's generator, in your Archipelago checkout |
| `dev-scripts/seed-snapshot.py` | writes files | The player files it generates with, and each case's slot data and spoiler, in the folder given with `--out` |
| `dev-scripts/seed-snapshot.py` | unpickles Archipelago's own output | Reads a generated seed file with Archipelago's own restricted loader |
| `dev-scripts/send-as-player.py` | connects to a server | A local test server (`127.0.0.1`), logged into as a second player |
| `dev-scripts/door-graph.py` | loads a script by path | Loads `gate-table.py`, next to it, to share its reader |
| `dev-scripts/door-graph.py` | writes files | `--export` writes `data/doors.json` for the apworld |
| `dev-scripts/event-transfers.py` | loads a script by path | Loads `gate-table.py`, next to it, to share its reader |
| `dev-scripts/enemy-table.py` | writes files | `--export` writes `data/enemies.json` for the apworld |
| `dev-scripts/save-points.py` | writes files | `--export` writes `data/starts.json` for the apworld |
