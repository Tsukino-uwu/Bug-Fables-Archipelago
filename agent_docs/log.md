# Session log

Newest last. What was tried, what happened, what the user said.

## 2026-09-24: the project starts

- **The user's decisions:** a separate project from their other work, built on its stricter rules. The first
  version shuffles **key items only**. **Remote items only, permanently**, after comparing it with local
  items: for a mod, remote is simpler, recovers a lost save and allows co-op on one slot, at the cost of
  needing the connection up. Local git on `main`, GitHub later.
- **Measured:** the game build facts in `MEASURED.md`. A web search found no existing Bug Fables Archipelago
  world.
- **Read:** the TEVI randomizer and Emerald's `remote_items` (`references.md`), and Archipelago's `docs/` at
  `0.6.7` (`client-requirements.md`). Licences in `licensing.md`.
- **Decompiled** `Assembly-CSharp.dll` with ilspycmd 10.1.1 into the gitignored `decompiled/`, which holds
  91 files. How items are granted is recorded in `MEASURED.md`.
- **Installed, with the user's yes:** BepInEx `v5.4.23.5` win_x64 (zip sha256 `82f98785…32c4`) and
  BepInEx.Debug ScriptEngine `r11.1` (zip sha256 `7f4a385f…b339b`), from their GitHub releases, into the
  Bug Fables install. Nothing was overwritten; the install had no BepInEx before. To undo, remove
  `BepInEx/`, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt` from the game
  folder. **The game keeps its save (`save0.dat`) in the same folder**, and nothing we do may touch it.
- **Carried over from the author's other project:** ScriptEngine **refuses a plugin in `BepInEx/scripts/`
  with no `.pdb` beside it**, and the failure is silent. Not yet confirmed here. The config keys are
  confirmed here (below).
- **The user launched and closed the game once.** BepInEx's log (`BepInEx/LogOutput.log`) shows it working;
  the lines are in `MEASURED.md`. The generated `com.bepis.bepinex.scriptengine.cfg` has `[AutoReload]`
  `EnableFileSystemWatcher = false`, `AutoReloadDelay = 3`, `DumpAssemblies = false`, and `[General]`
  `LoadOnStart = false`, `ReloadKey = F6`, `QuietMode = false`, `IncludeSubdirectories = false`. Those are
  the defaults, so hot reload still needs the watcher turned on and a `BepInEx/scripts/` folder, which
  doesn't exist yet.
- **The first in-game session of the plugin** (the agent launched the game, the user played a new game
  until quitting without saving):
  - **ScriptEngine loaded the plugin at startup** (`LoadOnStart`). The log shows
    `Loading bugfables.archipelago`, `Reloaded all plugins!` and our `loaded. GrantProbe=True`.
  - **ScriptEngine's FileSystemWatcher never fires in this game:** its handler's `File <name> changed` line
    never appeared after a redeploy. **F6 didn't reload either**, pressed in the focused window during play.
    Both are unexplained. `DevReload.cs` now polls our DLL and sets ScriptEngine's `shouldReload`; it's
    untested in the game so far.
  - **The log was a false lead:** it stopped at 00:56:53, which looked like a buffer. BepInEx flushes its
    disk log every 2 s (`DiskLogListener`, read with ilspycmd), and on close one more line appeared at frame
    25478 (about 58 fps since launch). So the plugin ran the whole time. **The probe's safety check at the
    top returned early silently through the entire session.** It now logs why it's waiting each time the
    reason changes. Which check it was is the next measurement.
  - **Focus:** the game has its own pause-when-unfocused option (`MainManager.pauseonfocus` drives
    `Application.runInBackground`, `MainManager.cs:16737`; set in `PauseMenu.cs:1626`). Use that rather
    than the mod overriding it.
  - **The user asked for spoiler-free chat:** the docs and the apworld hold everything, but chat refers to
    late content by id or chapter only. They haven't finished the game.
- **Earlier next step:** the first launch with BepInEx. It should create `BepInEx/LogOutput.log` and `BepInEx/config/`,
  proving the loader runs in this game. Then decide how to identify locations (the open question in
  `MEASURED.md`).
