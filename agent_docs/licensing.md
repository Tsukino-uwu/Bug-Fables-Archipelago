# Licensing

**Rule: read a project's licence from its own file before reading its source.** A project with no row here
has not been checked and may not be used. The test for what enters this repo is "fine in a public repo
forever?", not "does a licence permit it?". So even a permissive licence never lets us copy source: we
read for facts and write our own.

| Project | Licence | Checked | Use |
|---|---|---|---|
| [Archipelago](https://github.com/ArchipelagoMW/Archipelago) | MIT (`LICENSE`, "Copyright (c) 2017 LLCoolDave") | 2026-09-24, the file in the local checkout at tag `0.6.7`. GitHub's API reports `NOASSERTION`, so the file is what counts | The apworld runs inside it and imports its API (`worlds.AutoWorld`, `BaseClasses`, `rule_builder`). Its `docs/` and `worlds/apquest` are read as references |
| [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net) | MIT (`LICENSE.txt`) | 2026-09-24, via `gh api .../license` and by reading the file | The mod's client library, pulled from NuGet. Latest release `v6.7.1` (2026-03-21). MIT requires its notice to travel with the DLL if we ship it |
| [websocket-sharp](https://github.com/sta/websocket-sharp) | MIT (`LICENSE.txt`, "Copyright (c) 2010-2026 sta.blockhead") | 2026-09-24, via `gh api .../license` and by reading the file. Recorded after its decompiled source had already been read that day, which breaks the rule above; noted here honestly | Bundled inside the Archipelago.MultiClient.Net NuGet package (its net40 build, `DLLs/websocket-sharp.dll` upstream). The mod ships it next to MultiClient.Net and patches two of its methods at runtime with Harmony (compression). No source copied |
| [ArchipelagoBepInExPluginTemplate](https://github.com/alwaysintreble/ArchipelagoBepInExPluginTemplate) | MIT (`LICENSE.txt`) | 2026-09-24, same way | Read for the project shape only. Not a dependency |
| [BepInEx](https://github.com/BepInEx/BepInEx) | LGPL-2.1 (`LICENSE`) | 2026-09-24, by reading the file | The mod loader. The mod compiles against `BepInEx.Core` from NuGet and runs inside the user's own separately installed BepInEx 5. Not vendored |
| [Harmony](https://github.com/pardeike/Harmony) | MIT | Recorded 2026-08-12 in the author's other project; re-read before first use | Method patching, shipped inside BepInEx |
| [BepInEx.Debug](https://github.com/BepInEx/BepInEx.Debug) (`ScriptEngine`) | LGPL-3.0 | Recorded 2026-08-28 in the author's other project; re-read before first use | Dev-machine hot reload only. Never shipped |
| [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`) | MIT | Recorded 2026-08-12 in the author's other project; re-read before first use | Decompiles the user's own `Assembly-CSharp.dll` into gitignored `decompiled/` to read names. The output is never committed |
| [Tevi_Randomizer](https://github.com/BlackSoulKnight/Tevi_Randomizer) | MIT (`LICENSE`, "Copyright (c) 2024 BlackSoulKnight") | 2026-09-24, the file in a fresh clone (last commit 2026-07-01) | Read for its approach to Archipelago in a Unity Mono BepInEx mod. No code copied. See `references.md` |

## Reference sites (facts only)

| Source | Licence | Checked | How it's used |
|---|---|---|---|
| Bug Fables wiki (bugfables.fandom.com), the Crystal Berry page | CC BY-SA ("Community content is available under CC-BY-SA unless otherwise noted", on the page) | 2026-09-24; the user pasted the page, since the site blocks automated readers | Leads only: which chapter and what each berry needs. Facts are restated in our own words and checked against the game data or on screen; no wiki text is copied into the repo. |

## The game

*Bug Fables: The Everlasting Sapling* is the user's own Steam copy. Its assemblies, decompiled output, assets
and saves never enter the repo (`.gitignore`). The mod is our code, compiled against the user's local DLL
through `HintPath`, the same way the TEVI randomizer is built.
