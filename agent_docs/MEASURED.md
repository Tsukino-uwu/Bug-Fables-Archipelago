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
- **BepInEx 5.4.23.5 loads in this game.** Measured 2026-09-24 from `BepInEx/LogOutput.log` after the user
  launched the game once: `Running under Unity v2018.4.12.5889476`, `CLR runtime version: 4.0.30319.17020`,
  `System platform: Bits64, Windows`, `Chainloader startup complete`, `Loading [Script Engine 11.1]`.
- **`Supports SRE: False`** (the same log): System.Reflection.Emit isn't available. Any library that
  generates code at runtime has to fall back without it. Whether MultiClient.Net and its JSON library do
  is an **open risk**, to be settled by the first connect.
- **Unobfuscated names appear in the assembly's strings** (not yet in decompiled source): `MainManager`,
  `EventControl`, `KeyItem`, `GetItem`, `flags`, `Medal`, `MedalCheck`, `CrystalBerry`, `PlayerControl`,
  `PlayerData`. These are where to look first, not facts about what they do.

## How the game grants items (2026-09-24, read from `Assembly-CSharp.dll` decompiled with ilspycmd 10.1.1)

Read from code only; nothing observed running yet.

- **The inventory is `MainManager.instance.items`, a `List<int>[]` of length 3** (`MainManager.cs:2217`,
  allocated at `:3426`). Grants add to `items[0]` (ordinary items) and `items[1]` (key items). What
  `items[2]` holds is not measured yet.
- **Items and key items share one id space: the `MainManager.Items` enum, `None` = -1 then 0 to 186** (`MainManager.cs:1002`).
  An item is a key item because it was added to `items[1]`, not because of its id. Examples of names:
  `ExplorerPermit` 27, `FlowerKey` 54, `DesertKey` 92, `YinKey` 105, `YangKey` 106, `SandCastleBossKey` 115
  (the enum starts at `None = -1`, so `CrunchyLeaf` is 0; a first reading listed these one too high,
  and the probe's live cast, 27 = ExplorerPermit, caught it on 2026-09-24).
  The full key-item list is not yet measured.
- **Names and descriptions come from game data**: `itemdata[0, id, …]`, loaded from the `Data/ItemData` and
  `Data/Dialogues<lang>/Items` text assets (`MainManager.cs:3431`). They're read at runtime and never copied
  into the repo.
- **Grants go through commands in the dialogue text processor**, `MainManager.SetText`
  (`MainManager.cs:10626`). There are three:
  - **`Giveitem`** (`:11457`): `|giveitem,<type>,<id or var,n>,…|`. The type is -1 money, 0 item, 1 key item,
    2 medal (`badges`), or 3 crystal berry (`crystalbflags`). Adds with `items[type].Add(id)` when the type
    is below 2.
  - **`Additemtoss`** (`:12517`): world pickups. `NPCControl.CheckItem` (`NPCControl.cs:5588`) builds
    `|additemtoss,<entity.animid>,var,0|` with the id in `flagvar[0]`. `animid` is the same type code (0 item,
    1 key item, 2 medal, 3 crystal berry).
  - **`Additem`** (`:12552`): a direct add, also used for moving items between lists.
- **Some key items are added directly in code**, not through text: `EventControl.cs:19958` (`items[1].Add(116)`),
  `:22631`/`:22635`, `:32604`–`:32744`, and `BattleControl.cs:11029`. Each of these needs its own hook.
- **A world pickup records that it was collected through flags in the same text**:
  `|flag,<activationflag>,true|` and `|regionalflag,<n>,true|` (`NPCControl.cs`, inside `CheckItem`). So an
  object's flag is a natural stable location identity. The save's flags are also where offline checks can
  be recovered from.
- **Open question for location identity:** most key-item grants live in the dialogue text assets, not the
  code. A hook on the three commands catches every grant, but naming *which* location fired needs context:
  the calling NPC, the map, and the flag set in the same text. That's the next thing to measure.

## Observed in the running game (2026-09-24, GrantProbe, a new game played by the user)

Instrument: `mod/BugFablesAP/GrantProbe.cs`, which is read-only, logged to `BepInEx/LogOutput.log`, and
throttled to changes.

- **At the file select**, before any map (`map=none`): `flag[691]`, `flag[694]` and `flag[715]` go
  False -> True. This happened in both sessions, so it looks like title or settings state, not story.
- **The first key item: `id=27 (ExplorerPermit)` added to `items[1]` at frame 13844**, on
  `BugariaOutskirtsOutsideCity/BugariaOutskirts`, with `inevent=True` and `message=True`. It came during an
  event's dialogue.
- **`flag[15]` went False -> True at frame 16679 on the same map**, about 47 s later, and no other flag
  changed in between. So the event that grants the item sets flag 15 as it wraps up. **A candidate location
  identity: "the event whose completion flag is 15".** Whether flag 15 belongs to this grant alone, and
  whether the Giveitem call sits in that event's dialogue text, still has to be confirmed.
- **Starting a new game did not replace `flags` or `items[1]`.** The probe re-baselines when either
  array is replaced, and it didn't. Loading a saved game hasn't been observed yet.

## Key items: to measure

For the first version, measure and record:

1. The list of key items and their ids.
2. The one function every key-item grant goes through.
3. Each place in the world that grants one (these become locations).
4. How the save stores key items and the story flags that gate areas.
5. Where the final goal is detected.
6. Where the received-item count can live in the save without breaking its format.
7. How the game shows an item popup or text box that we can reuse to name a remote item.
