# Licensing

**Rule: read a project's licence from its own file before reading its source.** A project with no row here
has not been checked and may not be used. The test for what enters this repo is "fine in a public repo
forever?", not "does a licence permit it?". So even a permissive licence never lets us copy source: we
read for facts and write our own. **And an author's wishes count as much as their licence** (the user, 2026-09-25):
when an author objects to their work being used here, we don't use it, even where it's technically allowed.

| Project | Licence | Checked | Use |
|---|---|---|---|
| [Archipelago](https://github.com/ArchipelagoMW/Archipelago) | MIT (`LICENSE`, "Copyright (c) 2017 LLCoolDave") | 2026-09-24, the file in the local checkout at tag `0.6.7`. GitHub's API reports `NOASSERTION`, so the file is what counts | The apworld runs inside it and imports its API (`worlds.AutoWorld`, `BaseClasses`, `rule_builder`). Its `docs/` and `worlds/apquest` are read as references |
| [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net) | MIT (`LICENSE.txt`, "Copyright (c) 2022 Hussein Farran, Jarno Westhof") | 2026-09-24, via `gh api .../license` and by reading the file; re-read at tag `v6.7.1` 2026-09-26 | The mod's client library, pulled from NuGet. Latest release `v6.7.1` (2026-03-21). Shipped in the release zip. The NuGet package carries no licence file (only `<license>MIT</license>` in its nuspec, checked 2026-09-26), so all three libraries' notices are copied verbatim from upstream into `release/mod/.../THIRD-PARTY-NOTICES.txt` |
| [websocket-sharp](https://github.com/sta/websocket-sharp) | MIT (`LICENSE.txt`, "Copyright (c) 2010-2026 sta.blockhead") | 2026-09-24, via `gh api .../license` and by reading the file. Recorded after its decompiled source had already been read that day, which breaks the rule above; noted here honestly | Bundled inside the Archipelago.MultiClient.Net NuGet package (its net40 build, `DLLs/websocket-sharp.dll` upstream). The mod ships it next to MultiClient.Net and patches two of its methods at runtime with Harmony (compression). No source copied |
| [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) | MIT (`LICENSE.md`, "Copyright (c) 2007 James Newton-King") | 2026-09-25, the file in the NuGet cache's 13.0.3 package; the shipped 11.0.1's notice re-read at tag `11.0.1` 2026-09-26 (same text; the DLL's own FileVersion is 11.0.1, "Copyright © James Newton-King 2008") | Shipped next to MultiClient.Net (its netstandard2.0 build). Not read, not patched. Its notice is in `THIRD-PARTY-NOTICES.txt` |
| [ArchipelagoBepInExPluginTemplate](https://github.com/alwaysintreble/ArchipelagoBepInExPluginTemplate) | MIT (`LICENSE.txt`) | 2026-09-24, same way | Read for the project shape only. Not a dependency |
| [BepInEx](https://github.com/BepInEx/BepInEx) | LGPL-2.1 (`LICENSE`) | 2026-09-24, by reading the file | The mod loader. The mod compiles against `BepInEx.Core` from NuGet and runs inside the user's own separately installed BepInEx 5. Not vendored, not in the release. Its source read at tag `v5.4.23.5` (2026-09-26) for how plugins in subfolders load (`apimplementation.md`, build step 17) |
| [actions/checkout](https://github.com/actions/checkout), [setup-python](https://github.com/actions/setup-python), [upload-artifact](https://github.com/actions/upload-artifact), [download-artifact](https://github.com/actions/download-artifact) | MIT (`LICENSE`, "Copyright (c) 2018 GitHub, Inc. and contributors") | 2026-09-26, each file via `gh api` | Used by the workflows (`.github/workflows/`). Not copied, not shipped |
| [softprops/action-gh-release](https://github.com/softprops/action-gh-release) | MIT (`LICENSE`, "Copyright (c) 2019-current Doug Tangren") | 2026-09-26, the file via `gh api` | Publishes the release; pinned to commit `efb35369` (v3.0.3). Not copied, not shipped |
| [Harmony](https://github.com/pardeike/Harmony) | MIT | Recorded 2026-08-12 in the author's other project; re-read before first use | Method patching, shipped inside BepInEx |
| [BepInEx.Debug](https://github.com/BepInEx/BepInEx.Debug) (`ScriptEngine`) | LGPL-3.0 | Recorded 2026-08-28 in the author's other project; re-read before first use | Dev-machine hot reload only. Never shipped |
| [ILSpy](https://github.com/icsharpcode/ILSpy) (`ilspycmd`) | MIT | Recorded 2026-08-12 in the author's other project; re-read before first use | Decompiles the user's own `Assembly-CSharp.dll` into gitignored `decompiled/` to read names. The output is never committed |
| [Tevi_Randomizer](https://github.com/BlackSoulKnight/Tevi_Randomizer) | MIT (`LICENSE`, "Copyright (c) 2024 BlackSoulKnight") | 2026-09-24, the file in a fresh clone (last commit 2026-07-01) | Read for its approach to Archipelago in a Unity Mono BepInEx mod. No code copied. See `references.md` |

## Reference sites (facts only)

| Source | Licence | Checked | How it's used |
|---|---|---|---|
| Bug Fables wiki (bugfables.fandom.com), the Crystal Berry and Medal pages | CC BY-SA ("Community content is available under CC-BY-SA unless otherwise noted", on the page) | 2026-09-24; the user pasted the page, since the site blocks automated readers | Leads only: which chapter and what each berry needs. Facts are restated in our own words and checked against the game data or on screen; no wiki text is copied into the repo. |

## The game

*Bug Fables: The Everlasting Sapling* is the user's own Steam copy. Its assemblies, decompiled output, assets
and saves never enter the repo (`.gitignore`). The mod is our code, compiled against the user's local DLL
through `HintPath`, the same way the TEVI randomizer is built. The committed release DLL references `Assembly-CSharp`
and contains none of it (no embedded resources, checked 2026-09-26).

**Its terms, checked 2026-09-26:** the install has no EULA or licence file (`changelog.txt` there is BepInEx's).
Steam's `appdetails` for app 1082710 names the developer Moonsprout Games and the publisher DANGEN Entertainment; its
only legal text is "DANGEN ENTERTAINMENT is a trademark of Dangen Entertainment. Copyright © 2019 Dangen
Entertainment. All rights reserved.", with no custom EULA and nothing about mods. The user's search: mods are not
officially supported, and not blocked. The apworld's data (`data/*.json`) is map, door and entity names and numeric
ids read from the game: facts, no game text or assets.

**The disclaimer** (courtesy, not a requirement): the mod zip's `BugFablesAP-README.txt` says "Unofficial fan project. Not
affiliated with or endorsed by Moonsprout Games or DANGEN Entertainment." Only there: not the root README, not the
release page (the user, 2026-09-26).
