# Measured facts about Bug Fables

Each entry names our evidence and its date. The game can update without this repo changing, so a dated
entry is true as of that date. **Nothing here is "verified"**: that word is reserved for what the user
confirms on screen.

## The build (2026-09-24, read from the user's Steam install, game not run)

- **Unity 2018.4.12f1**, read from the header of `Bug Fables_Data\data.unity3d`.
- **Mono, not IL2CPP:** `Bug Fables_Data\Managed\Assembly-CSharp.dll` (2,196,992 bytes) has a CLR header, a
  `MonoBleedingEdge` runtime folder exists, and there is no `GameAssembly.dll` or `global-metadata.dat`.
- **x64:** `UnityCrashHandler64.exe`.
- **The game's own `version.txt` says `1.2`.**
- **`Managed\` ships `netstandard.dll`**, so a `netstandard2.0` plugin should load. Not yet tested.
- **No BepInEx installed** as of this date.
- **Unobfuscated names appear in the assembly's strings** (not yet in decompiled source): `MainManager`,
  `EventControl`, `KeyItem`, `GetItem`, `flags`, `Medal`, `MedalCheck`, `CrystalBerry`, `PlayerControl`,
  `PlayerData`. These are where to look first, not facts about what they do.

## Key items: to measure

For the first version, measure and record:

1. The list of key items and their ids.
2. The one function every key-item grant goes through.
3. Each place in the world that grants one (these become locations).
4. How the save stores key items and the story flags that gate areas.
5. Where the final goal is detected.
6. Where the received-item count can live in the save without breaking its format.
7. How the game shows an item popup or text box that we can reuse to name a remote item.
