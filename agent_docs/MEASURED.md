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
- **`Supports SRE: False`** (the same log): System.Reflection.Emit isn't available. **Closed 2026-09-24:**
  Archipelago.MultiClient.Net 6.7.1 (netstandard2.0 build, runtime `ClientWebSocket`) and its bundled
  Newtonsoft.Json logged in from the running game and read `slot_data`. From BepInEx/plugins, the libraries
  resolved into the hot-reloaded plugin without a restart.
- **Connecting needs an explicit `ws://`** for a local, unencrypted server. With a bare `localhost:38281` the
  server logged `connection rejected (400 Bad Request)` before a plain connection opened, and the login hit
  its timeout. `ws://127.0.0.1:38281` logged in at once (2026-09-24).
- **Unobfuscated names appear in the assembly's strings** (not yet in decompiled source): `MainManager`,
  `EventControl`, `KeyItem`, `GetItem`, `flags`, `Medal`, `MedalCheck`, `CrystalBerry`, `PlayerControl`,
  `PlayerData`. These are where to look first, not facts about what they do.

## How the game grants items (2026-09-24, read from `Assembly-CSharp.dll` decompiled with ilspycmd 10.1.1)

Read from code only; nothing observed running yet.

- **The inventory is `MainManager.instance.items`, a `List<int>[]` of length 3** (`MainManager.cs:2217`,
  allocated at `:3426`). Grants add to `items[0]` (ordinary items) and `items[1]` (key items).
  `items[2]` is storage (see "Observed in the running game").
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
    **Settled 2026-09-24, second new game (TextProbe and GrantProbe both on, Archipelago mod enabled):** the
    permit comes from Maki and Eetl's dialogue line on `BugariaOutskirtsOutsideCity/BugariaOutskirts`, which
    ends `|giveitem,1,27,13|` (type 1 key item, id 27, shown over entity 13), with `caller=none`. GrantProbe
    logged `KEYITEM +1 id=27` at frame 31685, and `flag[15]` False -> True at frame 34349. That's the same
    order and about the same gap (~2,660 frames) as the first run. **So the grant and flag 15 happen at
    separate moments:** the `giveitem` is in the dialogue, and flag 15 is set in code when `Event16` ends.
    The local grant happened because sending checks doesn't exist yet. Log kept only in that session's
    scratchpad.
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
- **A medal on the ground, the first pickup with a GLOBAL flag:** on `SnakemouthUndergrondDoor` (the
  game's own spelling), `|flag,60,true||additemtoss,2,var,0|` with `caller=PoisonDefender` (the object is
  named after the medal) and `flagvar[0]=9`, the medal's id in the medal (badge) list. TextProbe's name
  label casts to `MainManager.Items`, which is wrong for medals, so "CookedLeaf" there means nothing. A global
  flag is never wiped, **so a medal pickup is a one-time location**, identified by its flag, once medals are
  in scope. That confirms the rule from the other side: ordinary items use regional flags and respawn,
  medals use global flags and don't.
- **A second medal, with BOTH kinds of flag:** on `SnakemouthMushroomPit`, `caller=PoisonResistance`, the
  script was `|flag,42,true||regionalflag,16,true||additemtoss,2,var,0|`, and `flag[42]` and
  `regionalflag[16]` flipped in the same frame (47273). The spawn check (`MainManager.cs:7783`) hides an
  object while its flags *or* its regional flag are set, and the global flag is never wiped. **The rule,
  refined: a pickup with a global flag is one-time even if it also has a regional one, and the global flag
  is its location identity.** Not yet checked on screen that it stays gone.
- **An ordinary item with a global flag:** a Mushroom Candy (id 144) on `SnakemouthMushroomPit`,
  `caller=Item - Duplicate`, script `|flag,724,true||additemtoss,0,var,0|`, with no regional flag. **So one-time
  pickups are not only medals: the kind of flag decides, not the kind of item.**
- **Regional flags are also cleared without an area change:** `regionalflag[16]` (set by the second medal)
  went True -> False on the same map at frame 52352. The medal is unaffected, since its global flag 42
  holds. **Some maps wipe every regional flag when they load:** `MapControl.cs:635` does it for
  `SnakemouthTreasureRoom`, and the probe saw four regional flags clear on entering that room (frame 56878).
- **`items[2]` is storage:** `maxstorage - items[2].Count` (`MainManager.cs:5655`, `:13050`).
- **The first boss:** `Event26` (`EventControl.cs:4342`) starts the battle (`StartBattle`, enemy id 13) and
  sets `flags[41]` at its end. It grants no item. `flag[41]` flipped at frame 119806 in
  `SnakemouthTreasureRoom`. **"Beat the first boss" is flag 41.**
- **The treasure after the boss leaves no trace of its own** (the user picked it up, 2026-09-24). Nothing was
  logged at the pickup. `SaveDiff` of the user's save before the boss (`save2backup.dat`, 02:37) against after
  the treasure (`save2.dat`, 02:55), decoded in-game with `InputIO.Encrypt`, found:
  - line 11 (the 750 global flags): only `[41]` changed;
  - line 6 (the items, `items[0]@items[1]@items[2]`): the key items are still just `27`;
  - line 14 (regional flags): the treasure room's wipe;
  - line 10 (a 5-row true/false table): only `[4,0]`. That's `librarystuff[4, area]`, which `UpdateArea` sets
    on entering an area (`MainManager.cs:4083`).

  **So the treasure is a story moment, and flag 41 carries it.**
- **The artifacts are a count of story flags, not items** (the user saw the first one in the pause menu and
  on the save file, 2026-09-24). `MainManager.SaveProgressIcons()` counts the set flags among
  **41, 88, 299, 345, 347, 346, 555**, one artifact each (7 in all, `StartMenu.psprite` has 7 icons). The pause
  menu draws that many (`PauseMenu.cs:2398`). The save stores the count as `LoadData.progression`
  (`MainManager.cs:17167`, field 15 of its line), and the file select draws that many icons
  (`StartMenu.cs:789`). **Having an artifact = its flag being set**: usable as checks, or as a "collect N"
  goal.
- **The save file's layout, as far as seen:** 18 lines, where line 6 is the three item lists joined by `@`,
  line 10 is `librarystuff` (5 rows), line 11 the 750 `flags`, and line 14 the 100 `regionalflags`. Other
  lines changed with ordinary play (position, stats, counters) and aren't identified yet.
- **A two-part door, no item involved:** `flag[33]` on `SnakemouthUndergroundLeftB` (frame 25491), then
  `flag[34]` on `SnakemouthUndergroundRightB` (38629), then `flag[35]` on `SnakemouthUndergrondDoor`
  (39302). **The user, on screen:** they did the left side, then the right, and the door opened. So in the
  logic that door is "both sides done", with no key item.
- **A crystal berry given by a character** (for handing over the first artifact, the user's report): on
  `AntPalace2`, `caller=none`, the script was `…|giveitem,3,5,16,-4|` (type 3 crystal berry, berry **5**, over
  entity 16), and `crystalbflag[5]` flipped (frame 46414). **Found or given, a crystal berry's identity is its
  `crystalbflags` index, and a `giveitem,3,<n>` names that index directly**, so the full berry list can come
  from the dialogue dump plus the code.
- **The second key item: `id=41 (Map)`** added to `items[1]` on `AntPalace2` (frame 52686, during an event).
  It matches the dialogue dump's `AntPalace2` line 14 → `giveitem,1,41`, so the dump named it before it happened.
  **`flag[67]` followed (frame 54157), set at the end of `Event45`** (`EventControl.cs:7181`, flag at `:7505`):
  the Map's location is Event45 / flag 67, the same shape as the permit. `flag[68]` (frame 55406, on
  `AntPalace1`) is `Event46` (`:7511`), the next story step; the user saw it as walking out of the throne room.
  **TextProbe logged nothing for this grant**: the event seems to pass `SetText` a reference to the map's
  dialogue line, not the text, so the probe never sees the `giveitem`. The dump and GrantProbe covered it.
- **A pickup with no flag at all: the inn's item.** A Honey Drop (id 1) with `caller=Fixedtempitem` and the
  script `|additemtoss,0,var,0|`: no global flag, no regional flag. **The user, on screen:** it appears when
  they pay for and use the inn, not otherwise. So it's a repeatable reward spawned on the spot, recorded
  nowhere, and **not a location**. Three kinds of world pickup so far: a global flag (one-time, can be a
  location), a regional flag (respawns, not one), and no flag (repeatable, not one).
- **A key item lying in the world, with a global flag:** on `BugariaResidential`, `caller=badbook`, the script
  `|flag,621,true||additemtoss,1,var,0|` (type 1, key item), and `KEYITEM +1 id=174` plus `flag[621]` in the
  same frame (17532). The first grant all three probes caught together. **A clean key-item location: flag
  621.** Also `flag[43]` flipped (frame 14950): the flag `Event27` sets, the event after the first boss.
- **A quest hand-off key item:** during a taken quest, a character on `BugariaResidential` gave
  `KEYITEM +1 id=93 (QuestBook)` (frame 19312), and `flag[241]` followed as the talk ended (20022). The dump
  has it (`BugariaResidential` line 26 → `giveitem,1,93`). This item exists to be delivered to finish the quest,
  **so if it's shuffled, that quest's completion must require it in the logic.** The hand-off itself is a
  location (flag 241).
- **A key item that is CONSUMED:** delivering the Bad Book (id 174) in `AntPalaceLibrary`: `flag[618]` flipped
  (frame 35577), then `KEYITEM -1 id=174` (36206). The item left `items[1]`, unlike the permit, which is only
  shown. The 35-berry reward went through `money`, which TextProbe filters out. **So the delivery is a
  location (flag 618) that requires the Bad Book.** With remote items, the server sends it once and the game
  consumes it once; the received-item count in the save keeps a reload from giving it back.
- **`flag[349]` toggles on and off** at the `BugariaCommercial` shops (frames 31742–32369): temporary shop
  screen state, not progress. `flag[180]` flipped there too.
- **A key item from a conversation:** `KEYITEM +1 id=25` (the doll) on `BugariaTheater` (frame 719,
  `message=True`), then `flag[58]` when the user confirmed the dialogue (frame 2157). The dump predicted it
  (`BugariaTheater` line 7 → `giveitem,1,25`). **Location: flag 58.**
- **A multi-step quest, measured step by step:** the quest book (id 93) was handed over with `flag[241]`, then
  delivered in `AntPalaceLibrary` with `flag[242]` and `KEYITEM -1 id=93` in the same frame (9030). The quest was
  still not done (the user; its id hadn't reached `boardquests[2]`). So one quest can be several locations,
  chained in the logic: the delivery needs the book, and completion needs the delivery. The `0` placeholder in
  `boardquests[0]` comes and goes on map changes (frames 5855, 7865), so it's a list refresh, not a quest.
- **The multi-step quest completed:** quest **33** moved `boardquests[1]` → `[2]` with `flag[243]` in the same
  frame (12545). Chain: 241 (book given) → 242 (delivered, book consumed) → 243 plus the done list. Rewards:
  `giveitem,-1,15,31` (15 berries, caught by TextProbe since it's a `giveitem`) and `KEYITEM +1 id=52 (LoreBook)`.
  **Id 52 is granted many times** (code: Events 31, 103, 160, 172, 173, 175, 195; the dump:
  `BugariaResidential` 31, `GoldenSettlement3` 46), so Lore Books are a repeated collectible: in the apworld,
  an item with several copies, and any rule needing them counts copies. The library also added quest **27** to
  the taken list with `flag[70]` (frame 9985), then `flag[579]`.
- **Journal rewards (partly measured; the first reading was wrong).** 20 berries in `AntPalaceLibrary`
  (`|giveitem,-1,20,-11|`, `caller=none`); the NPC spoke of 5 discoveries both times (the user). **Which code pays
  it is NOT found.** `Event156`'s `10 × (thisdecimal + 1)` payout (`EventControl.cs:26186`, item tiers with
  `rewardflags` 503–506, 673) was first taken for it, but it sits in a hologram minigame; the link is unproven.
  **Measured:** the journal is `librarystuff`, 5 rows (`MainManager.Library`: Discovery, Bestiary, Recipes,
  Logbook, Map). Completing Discovery / Bestiary / Recipes unlocks Logbook entries 10 / 9 / 8, and nearly
  finishing the Logbook sets `flag[63]` (`MainManager.cs:4294–4371`, counts via `HowManyTrue(GetLibraryBools(n))`
  against `librarylimit[n]`). The library's dialogue also takes Lore Books (`removeitem,1,52`, line 7) and has
  crystal berry 25 (`giveitem,3,25`, line 27).
- **Turning in a Lore Book** (the user, `AntPalaceLibrary`): `KEYITEM -1 id=52` (frame 40180), **no global flag and
  no item script**. On screen, the book was placed on a shelf and became readable. So placed books are
  recorded outside the global flags. **Measured in code: a counter, `flagvar[15]`.** The dialogue command
  `Librarybook` (`MainManager.cs:11032`) does `flagvar[15]++` and refreshes `LibraryShelf`, which draws that many
  books left to right, 14 per row, then a second row (`LibraryShelf.cs`, `breakpoint = 14`). Only the count is
  kept, not which book, so Lore Books are interchangeable and any milestone is count-based.
- **A second crystal berry:** on `SnakemouthLake`, `crystalbflag[1]` flipped (frame 111883), with no
  tutorial flag this time. The script was `|additemtoss,3,var,0|` with `caller=tempitem`, and `flagvar[0]`
  read 1 (HoneyDrop), **a stale value left from an earlier pickup**. `flagvar[0]` means nothing for crystal
  berries; their `crystalbflags` index is their identity. `flag[25]` flipped earlier on that map, unrelated.
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
- **The third party member joining for good** (the user's report): `flag[29]`, `flag[16]` and `flag[24]` on
  `SnakemouthLake` (frames 117036–119390). The earlier `flag[27]` in `SnakemouthFallRoom` was likely the
  first meeting.
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

## Save files (2026-09-24, decompiled `InputIOManager/InputIO.cs`)

- **Saves are relative paths in the game folder**, the working directory: `save<N>.dat`, where N is the
  slot starting at 0.
- **`InputIO.Save` (`InputIO.cs:510`)** calls `File.*` directly, not through the wrappers. It writes
  `save<N>t.dat`, deletes `save<N>backup.dat`, moves `save<N>.dat` to `save<N>backup.dat`, then moves the temp
  file to `save<N>.dat`. The content is `Encrypt(MainManager.SaveFile(savepos))`. Called from
  `MainManager.cs:17413`.
- **The wrappers take a path:** `ReadFile` (`:416`), `DeleteFile` (`:425`), `CreateFile` (`:457`), and
  `SaveExists(int id)` (`:249`, `File.Exists("save"+id+".dat")`).
- **Every other site that builds a save name:** load at `MainManager.cs:17034/17040` (`ReadFile`), copy at
  `StartMenu.cs:685` (`CreateFile` + `ReadFile`, prefix `text = ""`), delete at `StartMenu.cs:705`
  (`DeleteFile`), and the existence check at `BattleControl.cs:3554` (`SaveExists`).
- **So a complete redirect** rewrites `save<N>[t|backup].dat` in the four wrappers and replaces `Save`'s file
  handling. Everything else goes through those.
- **Steam Cloud:** the game folder has `steam_autocloud.vdf` (holding only an account id, which never goes
  into the repo). That marks Steam **Auto-Cloud**, whose file patterns are configured on Steam's side and
  can't be read locally. It doesn't affect safety: randomizer saves use different names in another folder,
  so the game never writes a normal save's file. Whether Steam also syncs the randomizer folder only decides
  whether those saves roam between PCs.

## The main menu (2026-09-24, decompiled `StartMenu.cs`)

- **Three fixed options.** `selections = new Transform[3]` in `Intro` (`StartMenu.cs:117`). `SetMenuText`
  (`:301`) draws them from `menutext` ids `{123, 13, 124}`, one line each at `y = -0.5 - i`, then sets
  `MainManager.instance.maxoptions = selections.Length` and `menuid = 1`.
- **Choosing an option** is handled in `Update` (`:357`) under `menuid == 1`, branching on
  `MainManager.instance.option == 0 / 1 / 2`.
- **So a fourth "Archipelago: On/Off" entry needs:** `selections` enlarged to 4 before `SetMenuText` runs, a
  4th line drawn (our own text, not a `menutext` id), and `option == 3` handled in `Update`'s `menuid == 1`
  branch. None of it has been tried yet.

## The quest board (2026-09-24)

- **Flags 1 and 2 are board UI state, not quest progress.** `flag[2]` is set by `OpenQuestBoard`
  (`MainManager.cs:17827`) and cleared by `ChangeBoardQuest` when a new quest arrives (`:17953`): a "seen"
  marker. `flag[1]` isn't set in code (a dialogue script sets it, likely the board's first-time talk). Both
  flipped on `BugariaMainPlaza` (frames 58894 and 59211).
- **Quests are `MainManager.instance.boardquests`, 3 lists of quest ids** (`MainManager.cs:2219`, allocated
  `:3576`; the `BoardQuests` enum at `:535`; data from `Data/Dialogues<lang>/BoardQuests`). `ChangeBoardQuest`
  moves an id into a list (`:17945`), and taking a quest can also set a flag named in its data
  (`boardquestdata[id, 3]`, `:13906`). After the user took
  several quests: `[0]` = 8,9,10,21,23; `[1]` = 12,1,2,4,33,49,56; `[2]` = 11,0. GrantProbe logs every
  change, so finishing one quest will show it.
- **Taking quests set a burst of flags:** 3, 64, 44, 50, 240, 479, 617 on `BugariaMainPlaza` (frames
  60502–61679; the user saw them), consistent with each taken quest setting its `boardquestdata[id, 3]` flag.
  **Hypothesis, unmeasured:** `boardquests[1]` (7 ids) holds the taken quests. Those flags mark "taken", not
  "done"; finishing one will show which list completion moves an id to, and what the reward sets.
- **Quest completion measured** (the user completed a quest, 2026-09-24): at frame 9844, quest **1** moved
  from `boardquests[1]` (`12,1,2,4,33,49,56` → `12,2,4,33,49,56`) to `boardquests[2]` (`11,0` → `11,1`, the `0`
  placeholder dropped as `ChangeBoardQuest` does), and `flag[5]` flipped in the same frame. **So `[2]` = done,
  `[1]` = taken, `[0]` = most likely open on the board.** A quest's location identity is "its id is in
  `boardquests[2]`": saved, permanent, re-readable on connect. The reward left no TextProbe line.

## Input (2026-09-24)

- **Game actions:** `MainManager.GetKey(id, hold)`. The keyboard defaults are `InputIO.keys`: 0 up, 1 down, 2 left,
  3 right, 4 confirm (C), 5 cancel (X), 6 Z, 7 V, 8 Escape, 9 Return (`InputIO.cs:444`).
- **Gamepad:** `InputIO.joykeys` are **raw buttons, not actions**: `[0]` Button0, `[1]` Button1, `[2]` Button2,
  `[3]` Button3, `[4]` Button7 (Start), `[5]` Button6 (Back) (`InputIO.cs:571–576`). With `usejoystick > 0`,
  `GetKey` maps action 4 (confirm) to `joykeys[0]`, 5 (cancel) to `[1]`, 6 to `[2]`, 7 to `[3]`, 8 to `[4]` and
  9 to `[5]`. A first read of `joykeys[4]/[5]` as confirm/cancel was wrong; the user saw Start act as "done".
- **The game never reads typed text** (no `Input.inputString` anywhere); its name entry is a letter grid.

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

## Quests: to measure (when quests come into scope)

- **The pause menu's quest list groups quests by chapter and shows done / not done** (the user,
  2026-09-24). In the logic, a quest is reachable only once its chapter is.
- **Where that lives:** `boardquestdata` merges `Data/Dialogues<lang>/BoardQuests` (text columns) with
  `Data/BoardData` (numeric columns) per quest id (`MainManager.cs:3496`). Which column is the chapter, the
  taken-flag (`[id, 3]` is used as one, `:13906`) and the reward isn't read yet. Plan: an in-game dump of
  `boardquestdata`'s numeric columns, like ScriptDump, instead of tracing it through the code.

## Key items: to measure

For the first version, measure and record:

1. The list of key items and their ids.
2. The one function every key-item grant goes through.
3. Each place in the world that grants one (these become locations).
4. How the save stores key items and the story flags that gate areas.
5. Where the final goal is detected.
6. Where the received-item count can live in the save without breaking its format.
7. How the game shows an item popup or text box that we can reuse to name a remote item.
