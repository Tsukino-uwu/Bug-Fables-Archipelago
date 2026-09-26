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
   - The copied DLL is stamped with the current time, since DevReload watches write times: copying an unchanged
     build to reload a changed `-DebugSet` did nothing until then (2026-09-25).
   - DevReload waits while a scene, a conversation or a battle runs, and logs that it's waiting: a reload in the
     middle of a scene orphaned what the old plugin had made for it (the spider fight's stand-ins, 2026-09-25).
   - Every file it replaces (the plugin, the config) is first copied to `stage/backup/<time>/`;
     `-Restore <time>` puts it back. It writes nothing else in the game: the libraries and ScriptEngine's
     config stay the once-per-setup copy of step 2.
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
4. If the script says **the libraries differ from the game's**, copy `stage/setup/BepInEx` again with the game
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
- `heal`: the game's own full heal (HP and TP, the whole party). Test files only.
- `take <item|key> <id>`: removes one from the inventory, as the game's own `removeitem` does. Test files only.
- `warpicon leaf|key|scroll`: the Warp button's icon (the leaf is the default), shown the next time the pause menu opens.
- `warpcolor orange|pink|lime|<hue>`: the drawn backdrop's colour, for `warpicon scroll` (a design test).
- `enemylook <enemy id|off>`: reloads the current map with every ordinary map enemy looking like that enemy (a
  visual test for enemy shuffle's map look; the fights stay the seed's). Puzzle enemies keep their own look.
- `enemyfight <enemy id> [id...] | off`: every map fight starts with those enemy ids instead of the seed's (a test).
- `unstick`: runs the game's own end-of-cutscene cleanup, when a cutscene died and left you frozen, and ends a
  map transfer stuck walking to a spot it can't reach. It also takes the party off anything a scene parked it on
  and lifts a leftover fade: the boat scene crashed mid-fade and left a black screen with music playing, which the
  cleanup alone didn't clear (2026-09-25; the user saw the screen come back).
  It also resets the party's bodies (gravity, physics, forced animation), and closes a dialogue that died mid-line:
  the game kept thinking a box was open (`message`) after a city NPC's line threw, which froze the player until
  `unstick` did what the game's own dialogue end does (2026-09-25). The speech box itself stayed on screen after two tries (removing the text's
  holder, then `maintextbox`); the new `gui` command showed a `Textbox(Clone)` under the GUI camera that
  `maintextbox` no longer pointed at, so `unstick` now removes any such box once dialogue has ended.
- `gui`: log what hangs under the GUI camera (name, active, renderer, children), to find what's really stuck on screen.
- `nudge <x> <y> <z>`: shift the party by that much on the current map.
- `items`: list every pickup that exists on the current map right now (kind, id, flag, distance), in the log.
- `tree`: log the nearest pickup's whole object tree: each object, whether it's active, and its renderers, on or
  off. Settles what's really on screen when a visual fix doesn't take.
- `addleif`: add Leif to the party on a file where he hasn't joined (test files, never saved). `ChangeParty({0, 1, 2},
  fromscratch: true)` rebuilds the party list, then `SetPlayers` makes all three characters where the party stands.
  The 2026-09-24 try failed because without `fromscratch` the game's copy loop never runs (`for m < 0`) and the list
  comes out empty. First run (2026-09-25): three members, three characters, no errors.
- `holdup`: queue a test hold-up (the Explorer Permit "from TestPlayer"), display only, the way an item from another
  player is shown.
- `onehit`: flips a test boost: every hit on an enemy does at least 99 (before defence). It's the `[Debug]` setting
  `OneHit` (off in the code), so it survives reloads; `copy-dev.ps1 -DebugOn OneHit` turns it on for a dev install.
- `infjump`: flips jumping again in mid-air. It's the `[Debug]` setting `InfJump` (off in the code), on in the dev
  install (`copy-dev.ps1 -DebugOn InfJump`), so it survives reloads.
- **`TestDoors`** (`[Debug]`, not a console command): doors rewritten by hand, `Map/Door=LikeMap/LikeDoor;...` (entity
  names): that door leads where the other one leads, the entrance randomizer's proof of concept (`copy-dev.ps1 -DebugSet
  "TestDoors=BugariaOutskirtsOutsideCity/loadzone east=BugariaMainPlaza/LoadingZoneCommercial"`; empty turns it off).
- `line <map> <n> [n...]`: log the full text of a map's dialogue lines (the same table `script` reads), e.g. to find
  every line that mentions something (2026-09-25: the Outskirts lines about the rocks).
- `cam`: log what the camera follows (its target, or "DESTROYED"), the player, offsets, limits and the party with its
  characters. Settled a camera stuck on a removed character (2026-09-25).
- `who`: log every character drawn as Vi, Kabbu or Leif: name, position, what it follows, whether it's a player
  character. Found a stray second player character (2026-09-25).
- `follower <animid>`: make that character follow the party the story's way (the follower list, then `AddFollower`),
  e.g. `follower 46` (Maki) to replay the castle briefing, which needs her.
- `addmember <0|1|2>`: with `TestStartMember`, add Vi, Kabbu or Leif to the party, standing in for receiving them.
  It redoes the map's enemy-only walls for the new characters (`MEASURED.md`, enemy-only walls).
- `solids`: logs every solid collider under and within 4 of the player, with its path, size, components and any
  `ConditionChecker` switch: what an invisible wall is.
- **`TestStartMember`** (`[Debug]`): a new randomizer file starts with that one member (0 Vi, 1 Kabbu, 2 Leif); the
  story adds nobody else. -1 = off.
- `berries <n>`: add n berries (negative takes them), clamped to 0-999 as the game's own `money` script command does.
  For shop tests.
- **`TestStart`** (`[Debug]`, not a console command): a map name (`MainManager.Maps`), optionally `@` the map you
  arrive from, e.g. `BugariaMainPlaza@BugariaOutskirtsOutsideCity`. A new file starts there, arriving through that
  map's door into it (without `@`, the first door found), a stand-in for a random start until the seed chooses one
  (`copy-dev.ps1 -DebugSet TestStart=...`; empty turns it off).

## Every Debug setting

All live under `[Debug]` in `BepInEx/config/bugfables.archipelago.cfg`, are off by default, and are switched with
`copy-dev.ps1 -DebugOn <name>` / `-DebugOff <name>` (step 3 above). Dev installs and test files only.

| Setting | What it does |
|---|---|
| `DevConsole` | F9 opens the dev console (section above). |
| `DevCommandFile` | With `DevConsole`: a text file whose lines are run as console commands, then emptied, so a test can be driven from outside the game. |
| `InfJump`, `OneHit` | With `DevConsole`: jump again in mid-air; every hit on an enemy does at least 99. The console's `infjump` and `onehit` flip them. |
| `AdoptSeed` | A save tied to another seed is re-tied to the connected one and replays every item (section "A local server to test against"). |
| `TestStart`, `TestStartMember`, `TestDoors` | A new file's start map, its one party member, doors rewritten by hand (Dev console section). |
| `GiveMoney` | Berries to add once (capped at 999), then back to 0. |
| `GrantProbe`, `TextProbe` | Log every key item added and flag flipped / every dialogue line with an item command, with the map. |
| `SaveDiff` | Two save file names, `a.dat\|b.dat`: once per load, logs what differs between them. |
| `QuestDump` | Every board quest's name, its `BoardData` numbers (column 3: the flag taking it sets) and its `QuestChecks` row, to `bugfablesap-questdump.tsv`. |
| `EntityDump`, `ScriptDump`, `MapDump`, `VarDump`, `SpriteDump` | Write the game's entities, dialogue commands, map events, script slots or GUI, item and medal sprites (`bugfablesap-guisprites.tsv`, `bugfablesap-itemsprites.tsv` and their sheets) to `bugfablesap-*.tsv` / `.png` in the BepInEx folder. A labelled contact sheet can be made from a table and its sheet (game art: kept local, never the repo). |
