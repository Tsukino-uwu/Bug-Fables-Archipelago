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
- **Then `flag[31]` (frame 18896), `flag[32]` (19202) and `flag[30]` (19979)**, all on the same map. Flag 31
  is the one `NPCControl.CheckItem` sets the first time a medal is picked up (`|flag,31,true|` when
  `animid == 2`), so the user's "first item" was probably a medal. GrantProbe doesn't watch `badges`, so no
  item line appeared. 32 and 30 are unexplained so far.
- **Story events are numbered coroutines in `EventControl`** (`private IEnumerator Event<N>()`). They run their
  dialogue from the current map's `MainManager.map.dialogues[]` text table and **set their flags in code**:
  - **`Event16` (`EventControl.cs:3538`) is the Explorer Permit's event.** It ends with
    `MainManager.instance.flags[15] = true`, which matches the live flag 15 that followed the grant. The
    permit itself arrives through one of its dialogue lines. Which line carries the `giveitem` is for
    TextProbe to show; that probe wasn't running in that session.
  - **`Event17` (`:3824`) is the gate the permit opens.** It sets `flags[28] = true`. Observed live: when the
    user showed the permit (frame 21527), `flag[28]` flipped, **and the permit stayed in `items[1]`**. It
    is shown, not consumed. The user saw a gate open.
  - **`flag[26]` and `flag[92]`** flipped earlier on the same map, *before* the permit was shown (the user
    confirmed they hadn't used it yet), so they belong to something else there.
- **An NPC reward, first TextProbe capture:** on `NearSnakemouth`, the reward script ended
  `|giveitem,-1,10,6|`: type -1 (money), amount 10, over entity 6. The 4th argument is the entity the item
  sprite shows above (`id3` in `Giveitem`). `caller=none`, so an event started it. **`flag[17]` flipped
  right after** on the same map. Same shape as the permit: a grant plus a completion flag.
- **A ground pickup, captured:** `|regionalflag,7,true||additemtoss,0,var,0|` on `NearSnakemouth`, with
  `caller=tempitem` (the pickup object). It was an ordinary item (type 0), with the id in `flagvar[0]`; later
  captures log that id. GrantProbe logged `regionalflag[7] False -> True` at frame 17763.
- **Regional flags are wiped on every area change.** `MainManager.UpdateArea` sets
  `instance.regionalflags = new bool[100]` (`MainManager.cs:4082`). So a pickup guarded by a
  `regionalflag` **comes back** after the player leaves the area. It repeats, **so it is not a location.**
  Only a pickup guarded by a global `activationflag` (`flags[]`) is a one-time find that can be a location.
  Both are set in `CheckItem`'s text, which is how to tell them apart.
- **A crystal berry, captured:** on `OutsideSnakemouth`, the script ran `|additemtoss,3,var,0||flag,108,true|…`
  with `caller=CrystalBerry` and `flagvar[0]=-1`. **`crystalbflag[0]` flipped** (frame 6299), set in code by
  `CheckItem` (`crystalbflags[data[0]] = true`), then `flag[108]` flipped: the one-time first-crystal-berry
  tutorial (`!flags[108] && animid == 3` in `CheckItem`, like flag 31 for the first medal). **A crystal
  berry's location identity is its `crystalbflags` index**: permanent, saved, and re-readable on connect.
  `flag[22]` flipped earlier on the same map with no item script, so it belongs to something else.
- **A second ground pickup:** a Crunchy Leaf (id 0) on `BugariaOutskirtsSnakemouthCorridor2` with
  `regionalflag,13`, so it respawns. `regionalflag[5]` flipped there too, with no item script. Both maps are
  in area `BugariaOutskirts`, so regional flags carry across the maps of one area and are wiped only on an
  area change.
- **The regional wipe, observed live:** on entering area `Snakemouth` (`SnakemouthBridgeRoom`, frame 10869),
  `regionalflag` 5, 7 and 13 all went True -> False in the same frame, as `UpdateArea` predicts.
- **An event-placed pickup:** a Mushroom (id 13) on `SnakemouthDoorRoom`. Its script had **no flag of its
  own** and ended `|event,5|`, and `flag[13]` flipped on the same map just before (frame 18327). So some
  world items belong to a story event and are recorded by that event's flag, not by a pickup flag.
  **The user, on screen (2026-09-24):** picking it up drops the party through a trapdoor, which stays open
  afterwards, so the item can't appear again. That's consistent with a one-time location identified by the
  event's flag.
- **After the trapdoor and the third party member joining** (the user's report), `flag[14]` flipped on
  `SnakemouthDoorRoom` (frame 24876) and `flag[27]` on `SnakemouthFallRoom` (frame 38710). No item was
  involved, so joining is story flags only. **Open for the logic:** party members bring field abilities that
  gate areas, so the apworld will need abilities as requirements (fixed, or shuffled).
- **Loose berries (money pickups) leave no flag.** `CheckItem` takes its `ismoney` path (anim states 6, 7 and
  186), and no flag flipped when the user picked one up. They can't be recovered from the save, which is fine:
  they're out of scope.
- **A working model for locations:** a story-event grant is a location identified by its event number and
  the flag it sets. A world pickup is identified by its object's `activationflag` or `regionalflag`. A gate
  in the logic is "has item X", when the game shows the item rather than consuming it (true for the
  permit's gate).
- **Saves live in the game folder as `save<slot>.dat`**, numbered from 0, with `save<slot>backup.dat` written
  at the same moment. The user's slot 3 save is `save2.dat` (29,264 bytes, 01:11). These are the user's
  files; nothing we build ever touches them directly.
- **Starting a new game did not replace `flags` or `items[1]`.** The probe re-baselines when either
  array is replaced, and it didn't. Loading a saved game hasn't been observed yet.

## Key-item grant sources, raw (2026-09-24) — SPOILERS for the whole game

Two instruments, both read-only. **Code:** `giveitem,1,<id>` literals and `items[1].Add(...)` in the
decompiled `EventControl.cs`/`BattleControl.cs`, each with its enclosing `Event<N>`. **Data:** `ScriptDump`
(`mod/BugFablesAP/ScriptDump.cs`), which loads every map's dialogue table in the running game and keeps only
the command tokens: 179 item lines from all 246 maps, 30 of them `giveitem,1`. Ids are `MainManager.Items`
ordinals. **Raw material, not locations yet:** some ids are granted more than once (83, 84 and 52 especially),
so each grant has to be judged as a one-time location, a repeatable, or a quest hand-off before it goes into
the apworld. World pickups of key items (`animid == 1` objects in map entity data) are **not** in either list.

**Code** (`EventControl.cs` line, event: id)
3687 Event16: 27 · 5149/5172 Event28: 83, 83 · 5525 Event31: 52 · 5714 Event32: 24 · 8756/8781 Event55: 83, 92 ·
10264 Event65: 63 · 10315 Event66: 60 · 13131 Event83: 84 · 15084 Event90: 100 · 17337 Event101: 95 ·
17517 Event103: 52 · 17875 Event106: a variable (`num`) · 18220/18503/18730 Event109: 105, 106, 113 ·
19958 Event117: `Add(116)` · 22631/22635 Event134: `Add(94)`, `Add(flagvar[56])` · 23360 Event139: 119 ·
25716–25821 Event155: 83, 84, 4, 5, 83, 84 · 26555 Event160: 52 · 27109 Event162: 143 · 28944 Event172: 52 ·
29073 Event173: 52 · 29828 Event175: 52 · 32375 Event195: 52 · 32604–32744 Event197: `Add` of 135, 131, 132,
133, 137, 136, 134 and a menu choice · 37342 Event222: 83 · 37555 Event223: 84 ·
`BattleControl.cs:11029` (no event): `Add(flagvar[56])`

**Data** (map, dialogue line: id [flags set on the same line])
AntTunnels 8: 37 · BugariaMainPlaza 82: 142 [flag 442] · BugariaCommercial 92/112/143: 110 ×3 ·
BugariaOutskirtsOutsideCity 114: 149 [flag 480], 128: 167 · BugariaTheater 7: 25 · BugariaResidential 26: 93,
31: 52, 82: 176 [flag 630] · UndergroundBar 78: 138 · AntPalace2 14: 41, 71: 109 · GoldenSettlement2 45: 55,
67/77: 56 ×2, 139: 140 [flag 444] · DefiantRoot1 24: 89 [event, flags 157 and 150], 28: 89 [flag 150] ·
DefiantRootWell 3: 111 [flag 239] · DefiantRoot3 126: 83, 163: 141 [flag 443] · GoldenSettlement3 46: 52
[flag 603] · BeehiveMainArea 48: 99 [flag 251], 54: 94 [flag 252] · BeehiveBalcony 21: 54 ·
DesertRoachVillage 1: 105 · TermiteIndustrial 31: 139, 46: 145

## Key items: to measure

For the first version, measure and record:

1. The list of key items and their ids.
2. The one function every key-item grant goes through.
3. Each place in the world that grants one (these become locations).
4. How the save stores key items and the story flags that gate areas.
5. Where the final goal is detected.
6. Where the received-item count can live in the save without breaking its format.
7. How the game shows an item popup or text box that we can reuse to name a remote item.
