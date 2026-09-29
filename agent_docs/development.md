# Building and testing from source

For developers. You need the .NET SDK and your own copy of the game. The build compiles against the game's
`Assembly-CSharp.dll` from your install and never copies it into the repo.

## Before the first commit: the hooks and the preflight

Once per clone: `git config core.hooksPath .githooks`. The hooks need Python 3.11 or newer and find one themselves
(`py -3`, `python3`, then `python`, each tried before use), or use the one you name:
`git config preflight.python <path to python>`.

- `python dev-scripts/preflight.py`: every section on what is staged; the hooks run it on every commit and push.
  `--history` checks every commit ever made; `--text-stdin LABEL` checks text such as release notes.
- `python dev-scripts/negative-test-preflight.py`: proves every section can still fail (about 25 s). Run it after any
  change to the preflight.
- `python dev-scripts/dotnet_metadata.py --selftest <dll>`: reads a .NET assembly and prints what it found.
- `python dev-scripts/verify-release.py --ref vX.Y.Z --zip <file> --apworld <file>`: checks a release's files against
  its tag.

When a check refuses something the code really needs (a new host, a new capability), the fix is a row in
`docs/capabilities.md` with its reason: the maintainer's decision, never a looser pattern. What each section does:
[apimplementation.md, build step 28](apimplementation.md#build-step-28-nothing-unpublishable-in-the-repo-or-a-release).

## The mod

`dotnet build mod/BugFablesAP/BugFablesAP.csproj`. If the game isn't in Steam's default library, add
`-p:BugFablesDir="D:\path\to\Bug Fables"`. The SDK is pinned by `global.json` and every package by
`mod/BugFablesAP/packages.lock.json`; restores are locked, so to update a package on purpose run
`dotnet restore mod/BugFablesAP/BugFablesAP.csproj -p:RestoreLockedMode=false` and commit the lock file. A release DLL
is built by `dev-scripts/build-release.ps1` (documentation.md, step 32).

The code style is in `.editorconfig`, which most editors apply: 4-space indents, braces on their own line, and lines
up to 120 characters (the limit core Archipelago's `ruff.toml` sets for Python).

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
     so check once when the tester says it's in; never poll for it.
   - `-DebugOn EntityDump,ScriptDump` / `-DebugOff GrantProbe` switch Debug settings in the mod's config
     in the same run, and read the result back. `-ConfigSet Section.Key=Value` sets a key in any other section, for a
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
   `LocationChecks`); your game gets "You got <item> from Other!". A location is checked once: the same one twice sends
   nothing.

## The apworld's tests

With the world linked as above, run `python -m pytest worlds/bug_fables/test` in the Archipelago checkout. Set
`SKIP_REQUIREMENTS_UPDATE=1` to stop Archipelago's scripts from prompting to install other games' packages.

## Fuzzing the apworld

The tests check the option sets we thought of; the [Archipelago-fuzzer](https://github.com/Eijebong/Archipelago-fuzzer)
generates seeds from random yamls and catches the rare combination that fails. **Whenever the tests run, the fuzzer
runs too** (2026-09-28); 10000 seeds take a few minutes.

1. Once: copy its `fuzz.py` (and `hooks/`) to the root of your Archipelago checkout.
2. `dev-scripts/test-apworld.ps1 -Archipelago <your checkout>` runs the tests, then the fuzzer
   (`fuzz.py -r 10000 -n 1 -g bug_fables --skip-output`: one Bug Fables yaml per seed), and prints each error with its
   count. `-With apquest` puts another world in every room; `-Runs` changes the count. It fails unless both are clean.
3. Read `fuzz_output/report.json` (counts and each error with the runs that hit it). Each failed run keeps its yaml and
   log in `fuzz_output/error/bug_fables/<run>/`; regenerate it with `Generate.py --player_files_path` on that folder.
   A new run replaces `fuzz_output`, so copy anything you still need first.

Exit code 1 only means some runs failed. The goal is 0 failures in 10000.

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
  its flag is cleared first so it's back.
- `warp <map> [flag]`: go to a map by `MainManager.Maps` name or number (`warp TestRoom` included). Without a flag it
  lands once, where walking in through a door into the map ends (a second move after arrival once came after an
  enemy had touched the party, and the battle's start froze, 2026-09-26). With a flag (or `@name`) it lands on the
  entity, then steps beside it, unless a battle, event or dialogue has started by then.
- `spawn <item|key|medal> <id> [flag]`: drop a pickup next to you. With a pickup location's flag, on that
  location's map, it is that location. `spawn member <n> [x z]` drops party member n's look (0 Vi, 1 Kabbu, 2 Leif)
  at that offset from you, to see how a location holding him looks; it's a Crunchy Leaf underneath, given if taken.
- `flag <n> [on|off]`: show or set a story flag.
- `textsearch <word>`: every text file the game loads from `Resources/Data`, searched case-insensitively; up to 200
  matching lines go to the log with their file and line number (the game's own names for things, 2026-09-27).
- `discovery <n> [on|off]`: show or set a journal discovery (no pop-up), to replay a scene that records one.
- `heal`: the game's own full heal (HP and TP, the whole party). Test files only.
- `killall`: in a battle, every enemy's HP to 0; the battle's own death check ends them after the next action (a boss
  a test party can't hit, such as the spider in the air). Test files only.
- `take <item|key> <id>`: removes one from the inventory, as the game's own `removeitem` does. Test files only.
- `warpicon leaf|key|scroll`: the Warp button's icon (the leaf is the default), shown the next time the pause menu opens.
- `warpcolor orange|pink|lime|<hue>`: the drawn backdrop's colour, for `warpicon scroll` (a design test).
- `enemylook <enemy id|off>`: reloads the current map with every ordinary map enemy looking like that enemy (a
  visual test for enemy shuffle's map look; the fights stay the seed's). Puzzle enemies keep their own look.
- `enemyfight <enemy id> [id...] | off`: every map fight starts with those enemy ids instead of the seed's (a test).
- `unstick`: stops the last scene's coroutine (`Event<n>`: a stuck scene left running threw once the cleanup removed
  its stand-ins, 2026-09-26), runs the game's own end-of-cutscene cleanup, when a cutscene died and left you frozen, and ends a
  map transfer stuck walking to a spot it can't reach. It also takes the party off anything a scene parked it on
  and lifts a leftover fade: the boat scene crashed mid-fade and left a black screen with music playing, which the
  cleanup alone didn't clear (2026-09-25; the screen was seen coming back).
  It also resets the party's bodies (gravity, physics, forced animation), and closes a dialogue that died mid-line:
  the game kept thinking a box was open (`message`) after a city NPC's line threw, which froze the player until
  `unstick` did what the game's own dialogue end does (2026-09-25). The speech box itself stayed on screen after two tries (removing the text's
  holder, then `maintextbox`); the new `gui` command showed a `Textbox(Clone)` under the GUI camera that
  `maintextbox` no longer pointed at, so `unstick` now removes any such box once dialogue has ended.
- `gui`: log what hangs under the GUI camera (name, active, renderer, children), to find what's really stuck on screen.
- `display`: log the monitor's reported resolution and refresh rate, the window, the game's FPS and VSync settings,
  what Unity was given (`vSyncCount`, `targetFrameRate`) and the measured frame rate.
- `fps <cap>` (-1 uncapped): the frame cap for this session only, VSync off; the game's settings put theirs back when
  applied. `interp on|off`: Unity's rigidbody interpolation on every character on the map. `camlerp on|off`: the camera
  drawn between physics steps (`FrameRate.cs`). A look at higher frame rates; frame-counted logic runs fast meanwhile.
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
- `frames [seconds]` (5 by default): logs the frame count, median and slow frames with their times, the camera's draw
  time and garbage collections. A collection is marked on the frame before the slow one it causes.
- `nudge <x> <y> <z>`: shift the party by that much on the current map.
- `script <map>`: log a map's dialogue table, row by row. `pos <map> <index...>`: log those entities' start positions
  from the map's entity table. `prices <medal id...>`: log each medal's price columns (berries and crystal berries).
- `markcolor <progression|useful|trap|filler> <hex>`: a class's starburst colour, live (a design test).
  `markclass <entity> <class>` draws one slot's backdrop as that class; `markclass off` puts them back.
- `hide <entity>`: switch an entity on this map off until the map reloads (nothing saved).
- `items`: list every pickup that exists on the current map right now (kind, id, flag, distance), in the log.
- `tree`: log the nearest pickup's whole object tree: each object, whether it's active, and its renderers, on or
  off. Settles what's really on screen when a visual fix doesn't take.
- `addleif`: add Leif to the party on a file where he hasn't joined (test files, never saved). `ChangeParty({0, 1, 2},
  fromscratch: true)` rebuilds the party list, then `SetPlayers` makes all three characters where the party stands.
  The 2026-09-24 try failed because without `fromscratch` the game's copy loop never runs (`for m < 0`) and the list
  comes out empty. First run (2026-09-25): three members, three characters, no errors.
- `holdup [member n]`: queue a test hold-up (the Explorer Permit "from TestPlayer", or party member n: 0 Vi, 1 Kabbu,
  2 Leif), display only, the way an item from another player is shown.
- `articles [id...]`: log the found-item line's default article, each listed item's own, and the "You got" lines.
- `holdup ap`: the drawn Archipelago icon held up on two class backdrops (plum, cyan).
- `shelflook <location id> <white|black> <rim share>`: a shop slot shows the drawn icon with that outline, to compare
  looks on a shelf; `shelflook off` puts every slot back.
- `iteminfo`: log every item entity on the map with its sprite, pivot, size, lift and backdrop (placement checks).
- `mark <size> <raise>`: the Item backgrounds starburst's size and the lift it and its item get, live.
- `letters`: count the game's 500 text letters that are taken, by owner (to spot a leak).
  **Never log a text holder's own name:** it carries its whole text, and the game's font preloader's is every glyph it
  has. Logging it once (2026-09-26) broke BepInEx's console writer (`ConsoleEncoding.ReadByteBuffer`
  IndexOutOfRange) for the rest of the session: every later log line threw inside the plugin's update, which then
  stopped partway every frame, and the game froze. Only a restart recovers it.
- `menuinfo`: with an Archipelago panel open, its text pieces and letters, one of its letters beside one of the game's
  Settings letters in the GUI camera's own frame (position, rotation, layer, sort, visible), and the camera itself.
- `palette`: log the game's text colours by index (`|color,n|`), Archipelago's added ones included.
- `colortry <hex...>`: queue a trap's "You got" line in each colour given, to compare them on screen.
- `onehit`: flips a test boost: every hit on an enemy does at least 99 (before defence). It's the `[Debug]` setting
  `OneHit` (off in the code), so it survives reloads; `copy-dev.ps1 -DebugOn OneHit` turns it on for a dev install.
- `infberries`: flips the berry top-up: 999 berries once per save played, when its first map loads (the `[Debug]`
  setting `InfBerries`, off in the code, on in the dev install: `copy-dev.ps1 -DebugOn InfBerries`). Once, not on
  every drop: a refill hid purchases from the item shops, which see a purchase as berries going down (2026-09-27).
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
- `radii`: log every entity on the map, nearest first, with its type, talk radius (`npcdata.radius`) and distance.
  Measured the NPC talking range the save crystals now use (2026-09-29).
- `follower <animid>`: make that character follow the party the story's way (the follower list, then `AddFollower`),
  e.g. `follower 46` (Maki) to replay the castle briefing, which needs her.
- `addmember <0|1|2>`: with `TestStartMember`, add Vi, Kabbu or Leif to the party, standing in for receiving them.
  It redoes the map's enemy-only walls for the new characters (`MEASURED.md`, enemy-only walls).
- `removemember <0|1|2>`: take a member out of the party; the member guard then keeps them out of the story's party
  changes (the members left are allowed, the first as the start), for trying a party the story never has.
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
`copy-dev.ps1 -DebugOn <name>` / `-DebugOff <name>` (step 3 above). Dev installs and test files only. They exist
only in the dev (Debug) build: every one is bound in `Dev/Plugin.Dev.cs`, and the release build leaves `Dev/` out.

| Setting | What it does |
|---|---|
| `DevConsole` | F9 opens the dev console (section above). |
| `DevCommandFile` | With `DevConsole`: a text file whose lines are run as console commands, then emptied, so a test can be driven from outside the game. |
| `InfJump`, `OneHit`, `InfBerries` | With `DevConsole`: jump again in mid-air; every hit on an enemy does at least 99; 999 berries once per save played. The console's `infjump`, `onehit` and `infberries` flip them. |
| `AdoptSeed` | A save tied to another seed is re-tied to the connected one and replays every item (section "A local server to test against"). |
| `TestStart`, `TestStartMember`, `TestDoors` | A new file's start map, its one party member, doors rewritten by hand (Dev console section). |
| `GiveMoney` | Berries to add once (capped at 999), then back to 0. |
| `GrantProbe`, `TextProbe` | Log every key item added and flag flipped / every dialogue line with an item command, with the map. |
| `SaveDiff` | Two save file names, `a.dat\|b.dat`: once per load, logs what differs between them. |
| `PatchDump` | Every method the mod patches (target, kind, patch method, priority), Uncap FPS's hooks included, sorted, to `bugfablesap-patches.tsv`, once per load: diff it before and after a change to how hooks are installed. The log also gets the run order wherever one target has several of the mod's hooks of a kind. |
| `SeedDump` | Once per load, when a login brings the seed: everything the mod read from its slot_data, one sorted line per entry, to `bugfablesap-seed.tsv`, to diff before and after a change to how slot_data is read. |
| `QuestDump` | Every board quest's name, its `BoardData` numbers (column 3: the flag taking it sets) and its `QuestChecks` row, to `bugfablesap-questdump.tsv`. |
| `EntityDump`, `ScriptDump`, `MapDump`, `VarDump`, `SpriteDump` | Write the game's entities, dialogue commands, map events, script slots or GUI, item and medal sprites (`bugfablesap-guisprites.tsv`, `bugfablesap-itemsprites.tsv` and their sheets) to `bugfablesap-*.tsv` / `.png` in the BepInEx folder. A labelled contact sheet can be made from a table and its sheet (game art: kept local, never the repo). |
