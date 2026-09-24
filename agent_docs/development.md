# Building and testing from source

For developers. You need the .NET SDK and your own copy of the game. The build compiles against the game's
`Assembly-CSharp.dll` from your install and never copies it into the repo.

## The mod

`dotnet build mod/BugFablesAP/BugFablesAP.csproj`. If the game isn't in Steam's default library, add
`-p:BugFablesDir="D:\path\to\Bug Fables"`.

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
   - `-DebugOn EntityDump,ScriptDump` / `-DebugOff GrantProbe` switch Debug settings in the mod's config
     in the same run, and read the result back.
   - Every file it replaces (the plugin, the config) is first copied to `stage/backup/<time>/`;
     `-Restore <time>` puts it back. It writes nothing else in the game: the libraries and ScriptEngine's
     config stay the once-per-setup copy of step 2.
   - **Why a script with backups** (2026-09-24): Claude Code's auto mode refused, as irreversible, both a
     deploy script that rewrote configs and libraries in the game and an ad-hoc `cp` plus in-place `sed`.
     MeshGhost, which never hit this, only ever replaces its own rebuildable DLL through one named script.
4. If the script says **the libraries differ from the game's**, copy `stage/setup/BepInEx` again with the game
   closed: a running game holds the libraries open.

The plugin reads its config when it loads, so a hot reload also picks up a changed
`BepInEx/config/bugfables.archipelago.cfg`.

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

## The apworld's tests

With the world linked as above, run `python -m pytest worlds/bug_fables/test` in the Archipelago checkout. Set
`SKIP_REQUIREMENTS_UPDATE=1` to stop Archipelago's scripts from prompting to install other games' packages.

## Dev console (test files only)

Set `DevConsole = true` under `[Debug]` (`copy-dev.ps1 -DebugOn DevConsole`). In game, **F9** opens a command
line at the bottom of the screen; Enter runs, Escape closes. The player is frozen while it's open.

- `loc <n>`: go to pickup location n (the apworld's id, e.g. `loc 5`) and stand next to it. If it was taken,
  its flag is cleared first so it's back.
- `warp <map> [flag]`: go to a map by `MainManager.Maps` name or number (`warp TestRoom` included); with a flag,
  stand next to the entity that has it, else by a save point or door.
- `spawn <item|key|medal> <id> [flag]`: drop a pickup next to you. With a pickup location's flag, on that
  location's map, it is that location.
- `flag <n> [on|off]`: show or set a story flag.
- `unstick`: runs the game's own end-of-cutscene cleanup, when a cutscene died and left you frozen.

`loc` and `warp <map> <flag>` go straight to the entity's start position, read from the map's entity table,
and land a couple of steps from it; the pickup can't be taken for about 1.5 s and you can't walk for 1 s after
arriving (the user, 2026-09-24: one step away was too close, and the old origin-then-hop looked like two warps).
`loc` and `warp` mark the target map's auto-start cutscenes as seen before arriving: one played out of the
story's order crashed and froze the game (2026-09-24). A test file only.

It uses the game's own warp and pickup functions, but it can put a save in states the story never makes.

