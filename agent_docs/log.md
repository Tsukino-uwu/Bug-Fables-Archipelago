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
- **Carried over from the author's other project, to confirm here:** ScriptEngine r11.1 config sections
  `[General]` (`LoadOnStart`, `ReloadKey` = F6, `QuietMode`, `IncludeSubdirectories`, `DumpAssemblies`)
  and `[AutoReload]` (`EnableFileSystemWatcher`, off by default; `AutoReloadDelay`). Those names were read out
  of `ScriptEngine.dll`. ScriptEngine also **refuses a plugin in `BepInEx/scripts/` with no `.pdb` beside
  it**, and the failure is silent.
- **Next:** the first launch with BepInEx. It should create `BepInEx/LogOutput.log` and `BepInEx/config/`,
  proving the loader runs in this game. Then decide how to identify locations (the open question in
  `MEASURED.md`).
