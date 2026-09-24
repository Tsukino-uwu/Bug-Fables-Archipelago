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
  - **`Giveitem`** (`:11457`): `|giveitem,<type>,<id or var,n>,<redirect>[,<entity>]|`. **Corrected
    2026-09-24:** the third number is `redirect`, the dialogue line to continue with afterwards
    (`GetDialogueText(redirect)`), and the optional fourth is the entity the item sprite rises over. Earlier
    entries read the third number as an entity, which was wrong. With `flags[681]` set, every medal but #11
    becomes `GetRandomMedal()`. The item name goes to `flagstring[0]` for the "You got" box (`menutext[106]`). The type is -1 money, 0 item, 1 key item,
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
    ends `|giveitem,1,27,13|` (type 1 key item, id 27, then dialogue line 13), with `caller=none`. GrantProbe
    logged `KEYITEM +1 id=27` at frame 31685, and `flag[15]` False -> True at frame 34349. That's the same
    order and about the same gap (~2,660 frames) as the first run. **So the grant and flag 15 happen at
    separate moments:** the `giveitem` is in the dialogue, and flag 15 is set in code when `Event16` ends.
    The local grant happened because sending checks doesn't exist yet. Log kept only in that session's
    scratchpad.
- **The first medal, captured (2026-09-24, same run):** Artis's dialogue (`caller=ShwEmArtys`) on
  `BugariaOutskirtsOutsideCity/BugariaOutskirts` ends `|giveitem,2,11,45|`: type 2 medal, id 11, then dialogue
  line 45. An NPC talk started it, not an event. Right after, `flag[31]` flipped (frame 40171), then `flag[32]`
  (40278, about 107 frames later). **`flag[30]` flipped before this talk** (frame 38697), so it isn't part of
  the medal. In the first run it came after 31 and 32, so 30 belongs to something else nearby. Flag 31 fits
  "first medal ever" (see above). **Flag 32 is Artis's medal:** confirmed 2026-09-24 by reloading a save from before him and talking to him
  again. Flag 31 flipped (frame 2279), then flag 32 (2419), the same order as both first runs, and the mod sent
  location 7720003 from flag 32.
  GrantProbe doesn't watch `badges`, so no item line appeared.
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
- **A key item from a conversation:** `KEYITEM +1 id=25` on `BugariaTheater` (`GBugRangerPlushie`: the enum literal in the IL is 25, checked 2026-09-24; an earlier note called it "the doll", and `MothivaDoll` is 57) (frame 719,
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

## Free save slots for the mod (2026-09-24)

`MainManager.SaveFile` (`MainManager.cs:6900`) writes, among much else, all of `flags` (bool[750]),
**`flagstring` (string[15])** and **`flagvar` (int[70])** (sizes from `MainManager.cs:3604-3611`). One unused
slot of each can hold the mod's own state in the game's own save, with no new format.

- **The code's uses** (grep of the decompiled source): `flagvar` 0-6, 9-17, 22-24, 26-29, 32, 35, 37-43, 47, 50,
  53-56, 62, 66-68, plus the prize table `prizeflags` = 13, 17-21, 25, 30, 31, 33, 34, 36, 44-46, 48, 51, 52, 57,
  61, 63-65 (`MainManager.cs:3401`, the same live in the game). `flagstring` 0-4, 6-14, and `flagstring[listtype]`
  with `listtype` 9, 10 (letter prompts), 14 or 16.
- **The text's uses** (`VarDump`, every TextAsset under Resources, 2,437 assets, 239 distinct slot tokens):
  `flagvar` 49 (`addvar,49`), 58 (`checkvar,58`, `setvar,add,58`) and 59 (`checkvar,59`) are used. 7 and 8
  appear only as values or line numbers (`define` is a text macro, not a slot). **60 appears nowhere.** 69
  appears only as a line argument of `numberprompt`, whose slot is always 0. `string,N` uses `flagstring` 0-4, 9
  and 10.
- **Chosen:** **`flagvar[60]`** for the received-item count, and **`flagstring[5]`** for the seed's name.
- **A battle retry rolls `flagvar` back:** `BattleControl` snapshots `flags`, `flagvar` and `items[0]` at battle
  start (`BattleControl.cs:614-624`) and restores them on retry (`SetFlags`, `:3443`), but not key items. So
  the mod must never give an item during a battle. Then the count and the inventory stay consistent.
- **Correction:** `flagstring` IS saved. The item swap's use of `flagstring[0]` is harmless, because the
  game writes that slot itself for every item-get.

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

## World pickups and their gates (2026-09-24, EntityDump)

`EntityDump` (`mod/BugFablesAP/EntityDump.cs`) read every map's entity table in the running game, at the
same field positions as `MapControl.CreateEntities` (`MapControl.cs:1446-1640`): **4072 entities from all
246 maps, none unreadable**; names for **187 items and 91 medals** (`itemdata[0,id,0]`, `badgedata[id,0]`).
The output stays in the BepInEx folder.

- **Floor pickups** (`objecttype == Item`; `data[0]` is the kind, `animid` the id): 55 key items, 24 medals,
  43 ordinary items, 21 crystal berries.
- **One-time:** 54 of 55 key items, 23 of 24 medals and 30 ordinary items have an `activationflag`, set when
  picked up (`NPCControl.cs:5714`). Berries are tracked by `crystalbflags` instead (1 has a flag too).
- **Hiding flags (`limit`):** every one-time pickup lists its own `activationflag` there, which is how it
  stays gone once collected. **Only 5 pickups, all ordinary items, are also hidden by some other flag**:
  the only floor missables. No floor key item or medal is missable.
- **Required flags (`requires`):** only 10 pickups have any (4 key items, 1 medal, 4 items, 1 berry). Most
  pickups are gated by the map they lie on, not by a flag of their own.
- **Indoor pickups (the user, on screen, 2026-09-24):** the pickup with flag 686 on
  `BugariaOutskirtsOutsideCity` is inside a building (an *inside*) that isn't open in chapter 1, next to a
  second item; seen after the dev console's `loc` put the party by it. It couldn't be picked up, since the warp
  hadn't entered the inside the way its door does. **An entity's `insideid` (field 178, `MapControl.cs:1609`)
  says which inside it's in; -1 is outdoors.** EntityDump now writes it. An indoor pickup is gated by its
  inside's door (`DoorSameMap`), not only by its map.
- **The ladybug siblings are Leby (the sister) and Dib (the lost kid at the lake)** (the user, 2026-09-24).
- **Crystal berries around Snakemouth** (2026-09-24, dev warps with the user): #2 in the underground door room is
  reachable in chapter 1 from the room's upper-left entrance with nothing, from below only with Leif (a droplet);
  #32 (`VinedItem`, bridge room) sits up on the vines at the far side and only exists after the first boss
  (requires flag 41). The user: not reachable in chapter 1; very likely needs **hover** to get onto the
  platforms/pillars, then the beemerang to grab it (hover not yet confirmed). Waits until hover is in the logic.
  #3 (`ChucksAbode`) lies behind the house, out of reach in normal play (the user, 2026-09-24, after the first
  boss); what opens the way (a quest, an ability) is unknown. Not a location until that's found.
- **Houses outside the city** (`BugariaOutskirtsOutsideCity`, 2026-09-24): the ladybug siblings' house
  (`DoorLadybug`, inside 1) has no gate flags; the user found its Mistake (flag 679) after the first boss and
  remembers it locked earlier (to check on an earlier save). The other house (`doormadeleine`, inside 2, with a
  Lore Book and Burly Tea) needs flag 390, set by dialogue on `Swamplands8` line 4; a `lockeddoor` character
  stands there until then. Confirmed by the user after the first boss: that house is locked, with a pop-up saying so.
- **Doors:** 567 `DoorOtherMap` entities, 59 of them with required or hiding flags. Those are the map graph
  and its story gates, for the regions.
- **Not in this dump:** the one key item and one medal without an `activationflag` still need judging.
  Items given by NPCs and events are in the `ScriptDump` and code lists above.

## Hard Mode boss prize medals (2026-09-24, code read; not seen in game)

- **Hard Mode is on** when medal 11 is equipped (`BadgeIsEquipped(11)`, Artis's medal) or flag 614 is set
  (the new-game code, `EventControl.cs:2432-2554`).
- **Two levels, not one** (code read, 2026-09-24): nearly every check is `BadgeIsEquipped(11) || flags[614]`,
  so the medal and the HARDEST code share the Hard Mode effects. The code adds more on top, only with flag
  614: enemy HP x1.15 more and +1 defence after flag 300 (`PauseMenu.cs:2001-2002`, the enemy info shown),
  and more in `MainManager.cs:6273` and `PauseMenu.cs:1819/2452`. The one check needing both
  (`EntityControl.cs:3645`) is cosmetic, a model swap, not difficulty.
- **23 prize slots:** `prizeflags` (flagvar indices), `prizeids` (the medal) and `prizeenemyids` (the enemy),
  parallel arrays of 23 (`MainManager.cs:3401-3418`). One slot's enemy is -1 (no single enemy).
- **Beating the boss writes the slot** (`AddPrizeMedal(id)`, `MainManager.cs:3981`; called from 23 story
  events): **1** with Hard Mode on (and `flags[56]`, "a prize waits"), **2** without. `flagvar[55]` counts.
- **Value 1:** `Event33` hands every waiting prize over with `giveitem,2,<medal>` from an NPC
  (`EventControl.cs:5724-5752`), then sets the slot to **3**. **Event33 is started by talking to Artis**
  (`ShwEmArtys`, outside the city; seen in the event log, 2026-09-24).
- **Seen in play (the user, 2026-09-24):** the first boss beaten on Normal wrote its slot as missed; talking to Artis
  then gave nothing, and the caravan (open after flag 41) offered a medal: **Quick Flea, medal 5 = `prizeids[0]`**,
  which the user bought. Confirmed: a missed prize is sold at the caravan. Event26 writes a Normal kill's slot
  directly (`flagvar[13] = 2`, `EventControl.cs:4962`), not through `AddPrizeMedal`; eight boss events test Hard
  Mode themselves like this.
- **Value 2 is not lost:** a caravan medal seller (`Interaction.CaravanBadge`, `NPCControl.CaravanMedalSet`,
  `NPCControl.cs:1462`) offers the missed ones one at a time, in random order (`PrizeBadges(caravan: true)`),
  and buying one sets its slot to **3** (`Setprize`, `MainManager.cs:11090-11121`). `CaravanBadge`
  entities are on `BugariaOutskirtsOutsideCity`, `DesertDRSouthEntrance` and three "Duplicate" copies
  (EntityDump).
- **So a prize's location identity is "its slot reached 3"**, the same whichever way it was obtained.

## Chapters (2026-09-24, code read and EntityDump) — SPOILERS: map names

- **Chapter ends** are the artifact flags, set by: 41 `Event26`, 88 `Event73`, 299 `Event99`, 345 `Event118`,
  347 `Event142`, 346 `Event194`, 555 `Event200`/`Event203`. **Title cards** run in Events 16, 45, 74, 105,
  120, 142 and 194.
- **Hypothesis, not yet seen in game:** story events are numbered in story order, since both lists rise with
  the chapters. `dev-scripts/gate-table.py` uses it: an event below 16 is prologue, 16-44 chapter 1, 45-73
  chapter 2, 74-104 chapter 3, 105-119 chapter 4, 120-141 chapter 5, 142-193 chapter 6, 194 on chapter 7.
  Side events added late carry high numbers, so the rule errs toward a later chapter, the safe direction.
- **Leif's joining chain, played through by the user with the event log on** (2026-09-24): Event4 on
  `SnakemouthDoorRoom` (the trapdoor, started by the map: a rock-and-pressure-plate puzzle's AND gate; flag 13) →
  Event5, started by picking up the Mushroom the trapdoor scene creates (`tempitem`, data {0,5,1}; flag 14; the
  first spider fight, scripted so damage can't win it) → Event6, the `SnakemouthFallRoom` trigger (flag 27: Leif
  follows, not yet in the party) → Event18 on `SnakemouthLake`, a switch (flag 29) → Event14, the lake's
  `MothEvent` trigger (flag 16: Leif joins the party; then flag 24). The user confirmed him a full member: in the
  pause menu and usable in battle. Each step expects the one before: a file that skipped part of the chain
  crashes entering its middle.
- **The party's basic moves** (the user, 2026-09-24, matching `PlayerControl.cs`): Vi (bee) throws the
  beemerang, which hits and grabs at range (flag 11, on from the start; Event109 takes it away in the bandit
  hideout and gives it back); Kabbu (beetle) uses the horn, a knock-up and melee hit that also cuts grass (always
  on); Leif (moth) freezes, droplets included (always on, once he has joined at the Snakemouth lake). Vi and
  Kabbu are in the party from a new game, so while the party is vanilla only Leif gates anything.
- **Ability flags, confirmed as reads in `PlayerControl.cs`:** 11 (beemerang, with `!flags[41]`), 699
  (horn dash), 39 (heavy dash: its absence changes the dash), 171 (big icicle), 19 (hover), 18 (dig), 20
  (bubble shield).
- **59 doors to other maps have required or hiding flags, on 22 flags.** Setters: 11 Events 0/1/109,
  18 `Event109`, 20 `Event95`, 41 `Event26`, 67 `Event45`, 85 `Event52`, 86 `Event58`, 107 `Event60`,
  160 `Event84`, 169 `Event87`, 211 `Event98`, 226 and 239 dialogue only, 280 `Event112`, 299 `Event99`,
  348 `Event120`, 359 `Event137`, 370 `Event140`, 384 `Event149`, 449 `Event166`, 555 `Event200`/`Event203`,
  568 `Event194`.
  - **Doors that need an ability flag:** `HideoutEntrance` → `HideoutStairsRoom` (18, dig);
    `GoldenHillsPath3` → `ChomperCave1` (20, bubble shield); `HideoutGarden` → `DefiantRootWell` and
    `HideoutStairsRoom` → `HideoutEntrance` (11, beemerang, which `Event109` takes away and gives back).
  - **Doors hidden by a later flag:** the Golden Settlement day/night set (85, 86), `BeehiveOutside` →
    `BeehiveScannerRoom` (160), `BarrenLandsCD` → `BarrenLandsEntrance` (384), `WaspKingdomOutside` →
    `WaspKingdom1` (370). Each still to judge: an alternate version of the map, or a place that closes.
- **Snakemouth Den's doors by direction** (2026-09-24, EntityDump; each side of a door is its own entity):
  most are matched pairs. `SnakemouthDoorRoom` ↔ `SnakemouthFallRoom` exists both ways but only after flag 41
  (the first boss); in chapter 1 the way down is the trapdoor drop (flag 13), which is no door entity, so it
  is one-way until then. `SnakemouthEmpty` → `SnakemouthDoorRoom` and `UpperSnekEntrance` →
  `UpperSnekTransition` have no door back. The user saw a door block the way back into a room while its
  pickups were reachable from either side (2026-09-24). **Room-level regions need drops and scripted moves as
  connections of their own**, found from events, not doors.
- **Not in this table:** gates that aren't doors (objects only an ability passes, characters that block a
  path), and `CheckIfCanExist` on non-door entities.

## What starts the gate events (2026-09-24, EntityDump, ScriptDump with event lines, MapDump) — SPOILERS

`dev-scripts/event-triggers.py` looks in every place an event can start. **How events start:**
- talking to or touching an entity whose `eventid` is the event (`NPCControl.cs:4358`);
- an `EventTrigger` object, `data[0]` (`NPCControl.cs:5525`);
- a dig spot with `data[0] >= 2`, `data[1]` (`:5530`);
- a pickup chain, `data[1]` (`MainManager.cs:12543`);
- a locked door's `dialogues[1].y` once the right key is used (`EventControl.cs:9606`);
- a dialogue line's `|event,N|`;
- a map's `autoevent` (only 5 maps have any, e.g. `HoneycombsLab` 175:80, `Swamplands5` 383:147);
- a literal `StartEvent(N)` in code.

**The gate events:**
- **26** is started by the `SnakemouthTreasureRoom/MaskEvent` trigger. **45** by `AntPalace1/Chapter1StartEvent`.
- **52** and **58** by dialogue on `GoldenSettlement1` (lines 20, and 123/124).
- **60** by the `BugariaOutskirtsOutsideCity/DoorBugaria - Duplicate` trigger (hidden by 107).
- **84** by triggers on `BeehiveScannerRoom`. **87** by a trigger on `BeehiveMainArea`, which needs flags
  167, 168 and 173.
- **98** by a trigger on `FactoryProcessingMalbee`. **99** by dialogue on `HoneyFactoryCore` line 6.
- **109** by triggers on `HideoutEntrance`/`HideoutCell`, and dialogue on `DesertRoachVillage` line 3.
- **112 is a key-item gate:** the locked door `DesertSandCastle/keycheck` starts it once its key is used,
  setting 280, which opens the door to `SandCastleEntrance`.
- **120** by a trigger on `BugariaCastleAttack`. **137** by one on `SwamplandsBoss`. **140** by ones on
  `WaspKingdomThrone`/`WaspKingdomQueen`.
- **149** by talking to the gates on `TermiteOutside`/`TermiteMainPlaza`, and a trigger on `TermiteOutside`.
- **166** by a trigger on `FarGrasslandsWizard`, and dialogue on `WizardTowerAttic` line 14.
- **194** by triggers on `RubberPrisonGiantLairBridge`/`GiantLairEntrance`.
- **200** by `Swamplands7/archertop` and a trigger on `GiantLairSaplingPlains`. **203** is called from
  `Event200`.
- **Not found:** Events 0 and 1 (the prologue: flag 11, the beemerang, is on from the start) and **Event95**
  (flag 20, the bubble shield). 95 has no data trigger, no dialogue line and no literal call; still to find.

**Dig spots bury things** (`NPCControl.cs:5396-5420`): `data[0]` 0 = an item (kind `data[1]`, id `data[2]`,
with its own `activationflag`), 1 = a crystal berry (index `data[1]`), 2 or more = an event (`data[1]`).
Outside `TestRoom`: 14 ordinary items, 4 key items, 12 berries and 1 event; 15 buried items have a one-time
flag. **Buried items are locations the floor-pickup count missed, and every one of them needs dig.**

**Hazards (MapDump):** `WalkableSpike`, what the bubble shield crosses, is on 12 maps (e.g. `GoldenHillsPath3`,
the desert maps, `SandCastleRockRoom`, `FarGrasslands4`, `RubberPrisonSpikeRoom`). `Hole` hazards (pits) are
on many maps from the first dungeon on, so a pit doesn't mean hover.

## Lore Books at the library (2026-09-24, the user's play-through)

- **Placing Lore Books uses them up and gives only reading**: two placed at once logged `KEYITEM -1 id=52` twice
  on `AntPalaceLibrary`, then the user could choose which to read; no item came back (Event189 ran there first,
  started by `LibrayantDiscovery`). The count, `flagvar[15]`, is used only by the shelf's display
  (`LibraryShelf.cs:27`) and the reading list (`MainManager.cs:15372`), and by no game text (VarDump). **No count
  reward: the Lore Book is useful, not progression.**
- **A delivery quest's reward is a Lore Book**: on `BugariaResidential` a cicada ("Oh, you delivered it!") gave
  `giveitem` of item 52, then flag 243 (quest 33 done; see "Key items" above for flags 241-243).

## All crystal berries (2026-09-24, entity dump and ScriptDump, matched to the Bug Fables wiki)

**41 of the 50 are placed by data** (ground pickups: index in `data[3]`; dig spots with `data[0] = 1`: index in
`data[1]`; cut grass with `data[1] > -1`; dialogue `giveitem,3,N`). The other 9 (#11, 13, 19, 38, 43-47) come from
code with computed values (discovery rewards and quest rewards, per the wiki). By index: #0 OutsideSnakemouth
(ground), #1 SnakemouthLake (grass), #2 SnakemouthUndergrondDoor, #3 ChucksAbode, #4 GoldenSettlement2, #5
AntPalace2 (gift, dialogue line 15), #6 BOGoldenPath (dig), #7 GoldenSettlement2 (dig), #8 GoldenHillsCableCar, #9
GoldenHillsDungeonLeftMain, #10 BugariaPier, #12 AntPalace2 (gift, line 48), #14 DesertCaravanMap (dig), #15
DefiantRoot1 and a code gift, #16 FactoryProcessingPuzzle3, #17 FactoryStorageMaze, #18 code gift, #20 HideoutRightA
(dig), #21 DesertRoachVillage (dig), #22 GoldenSettlementEntrance (dig), #23 SandCastleBasement, #24
SandCastleRockRoom, #25 AntPalaceLibrary (gift, line 27), #26 FarGrasslands2, #27 Swamplands5 (dig), #28 TermitePier,
#29 BugariaMainPlaza (dig), #30 BugariaOutskirtsOutsideCity (dig), #31 FarGrasslands1 (dig), #32 SnakemouthBridgeRoom
(requires flag 41), #33 TermiteRoyalChamber, #34 AntMinesBreakRoom (dig), #35 BeehiveMainArea (gift, line 64), #36
MetalIsland1, #37 WizardTowerBasement, #39 code gift, #40 GoldenPitcher2, #41 FishingVillage (gift, line 11), #42
UpperSnekPressurePlateRoom, #48 GiantLairFridgeInside, #49 GiantLairDeadLands1 (dig).

**Chapter 1 per the wiki, matched:** #0 (behind a bush outside the cave), #1 (cut the bush by the sign, lake room's far
left), #2 (behind the large mushroom; the user: upper-left entrance free, from below Leif), #5 (the Queen, for the
Ancient Mask). **Later:** #3 behind Chuck's house needs a large boulder smashed (chapter 5; heavy dash, to confirm);
#32 in the bridge room needs Vi's fly (hover) over two pillars and the beemerang on a vine (chapter 6), as the user
guessed. The wiki is a lead, not proof: each entry is checked against the data or on screen before it's logic.

## Respawning pickups, seen in play (2026-09-24, the user, with the dev log)

- **The respawn cycle works as designed:** on `SnakemouthUndergrondDoor`, the Honey Drop (regional flag 24) and
  the Mushroom (29) each showed the seed's item and sent their check on the first pickup (server confirmed
  7720022, 7720023). After a trip outside (area change to the Outskirts; the probe logged `regionalflag[29]`
  True -> False), both were back, and the Honey Drop gave a real Honey Drop with no check (`check already done:
  vanilla item`). The Crunchy Leaf on `SnakemouthUndergroundRightB` (28) showed Mushroom Gummies and sent 7720024.
- **Where they are:** the Honey Drop sits on top of a pillar, reached with Leif's ice. The Mushroom is reached with
  nothing from the room's left side, with ice from the bottom or right, like crystal berry #2. The Crunchy Leaf is
  behind a pillar, out of sight: from the room's bottom-middle entrance, walk right and behind it.
- **Many items may be hidden behind walls or pillars** (the user): the camera never shows them. The dev console's
  `items` lists every pickup with its position, and `nudge` moves the party by an exact amount.
- **Flag 281** (in all three pickups' hiding flags) read False in play.
- **The underground's door layout** (the user, 2026-09-24), for room-level regions later: the big-door room has a
  switch room on each side (`SnakemouthUndergroundRightB` is one, with its switches and rotating bridge). Hitting
  a room's switch also opens a small gate back toward the big-door room, between the two. In
  `SnakemouthUndergroundRightB` (the user's screenshot, 2026-09-24): the switch is the pentagon crystal on a
  pedestal at the top left of the room; hitting it lowers a pillar barrier beside it, the way back to the big-door
  room. The left and right
  switches can be done in either order; both are needed to open the middle door, which leads on to the first boss.
  The Crunchy Leaf behind the pillar needs nothing once you're in its room (the user).

## What the Explorer Permit opens (2026-09-24, code read and ScriptDump; the wiki lists four uses)

- **The Outskirts gate:** `BugariaOutskirtsOutsideCity` line 31 asks for a key item, line 33 starts `Event17`
  (flag 28). Already the logic's gate.
- **A Rubber Prison door, confirmed in code:** `Event59` (the shared locked-door routine, `EventControl.cs:9575-9600`)
  compares the shown key item with a list indexed by the door's `dialogues[0].y`; index 16 is 27, the permit. The
  only `LockedDoor` with index 16 is `PrisonDoor` on `RubberPrisonCheckpointCorridor` (its flag 538).
- **B.O.S.S. and the Cave of Trials, not yet confirmed:** both maps have a key-item prompt (`HBsLab` line 50,
  `CaveOfTrials` line 11). Which item they accept is decided by a dialogue command ScriptDump doesn't keep, so the
  permit there is the wiki's word only. Until measured, every B.O.S.S. and Cave of Trials location requires the
  permit: cautious, never wrong.

## All medals by source (2026-09-24, entity dump, ScriptDump, code read, matched to the Bug Fables wiki)

91 medal kinds (ids 0-90, `badgedata`); the wiki counts 120 copies in all. Where each comes from in the data:

- **On the floor (23, each with its own global flag):** HP Plus 0 on SnakemouthLake (flag 23), BugariaOutskirtsEast1
  (137, behind the waterfall; wiki: needs the beemerang) and DesertBadlands (413); Poison Defender 9
  SnakemouthUndergrondDoor (60); Poison Resistance 7 SnakemouthMushroomPit (42); Bug Me Not! 18 BugariaResidential
  (59); Charge Up 52 BugariaMainPlaza (230; wiki: the locked house, key bought at the Hive); Super Block+ 19
  GoldenHillsCableCar (534); Life Cast 72 GoldenPathTunnel and GoldenPathTunnel2 (one flag, 462: one medal, two
  spots); Back Support 36 GoldenHillsDungeonLeftCrankHalf (121); Fortify 39 DefiantRoot2 (149); Meditation 56
  DesertBadgeAlcove (262); Strong Start 23 DesertBeforeGH (415); Tardigrade Shield 51 DesertRockFormation (343);
  Shock Trooper 34 FactoryStorageMaze (220); Frostbite 46 SandCastleSlidePuzzle (285); Antlion Jaws 63 StreamMountain3
  (418); Berserker 8 ChomperCaves2 (338); Eternal Venom 27 Swamplands7 (355); Extra Freeze 59
  UpperSnekPressurePlateRoom (522); Status Mirror 75 GiantLairDeadLands2 (611); Royal Calling 80 AntPalaceWarRoom
  (717, requires 555: postgame). A TP Plus in TestRoom (no flag) is the debug room's, not a location.
- **Dialogue gifts (`giveitem,2` in map dialogue):** Hard Mode 11 (Artis, BugariaOutskirtsOutsideCity line 44); Sleep
  Resistance 12 (BugariaMainPlaza line 33; line 35 takes key item 24 and sets flag 52); Favorite One 20
  (BugariaResidential line 45, takes key item 117); Weak Stomach 24 (GoldenSettlement2 lines 69/80, flag 102); Heavy
  Sleeper 47 (GoldenSettlement2 158); Crazy Prepared 71 (BugariaPier 46, flag 481); HP Core 64 (DefiantRoot1 79,
  flag 396; BarrenLandsBeefly 10); Reflection 61 (DefiantRoot1 106, DefiantRoot3 190); First Plating 77 (DefiantRoot3
  144); A.D.B.P. Enhancer 28 (HoneycombsLab 5, flag 352); Power Exchange 49 (HoneyFactoryWorkerRooms 24); Heal Plus
  74 (FishingVillage 14); TP Plus 1 (TermiteMainPlaza 56, flag 627).
- **Code gifts (`giveitem,2` built in `EventControl`):** Mighty Pebble 13 (`:5855`, Chuck's quest); Spy Specs 17 and
  Detector 2 (`:14024-14065`, B.O.S.S., flags 164/165); Mightier Pebble 29 (`:18821`); Prayer 62 (`:25648`); Freeze
  Resistance 33 (`:26535`, flag 430) and Seedling Affinity 78 (`:26575`, flag 477); TP Plus 1 (`:35843`); the Hard
  Mode prizes (`:5747`, see above). Quest-board rewards not yet read (see "Quests: to measure").
- **Shops (`badgeshops[0]` is Merab's, `[1]` Shades's, for crystal berries):** new game (`MainManager.cs:4010`)
  Merab 0, 1, 7, 12, 30, 86, 84, 87, 88, 81 and Shades 19, 6, 9, 43, 42 (both open later in the story); Event73
  (chapter 2's end) Merab +21, 22, 48; Event99 (chapter 3's end) Merab +33, 56, 74, Shades +0, 49; Event118
  (chapter 4's end) Shades +76; Event120 (chapter 5's start) Merab +45, 1, Shades +44, 6, 50; Event142 (chapter 6's
  start) Merab +86, 62, 41, Shades +57, 35. We Owe Ya! 85 joins Merab's once a helper is unlocked
  (`MapControl.HelperMedalCheck`, flag 716). Termacade, the bomb shop and the caravan prizes are separate sellers.
- **Chapter 1 per the wiki, matched:** HP Plus (lake pillar), Poison Defender (underground door room), Poison
  Resistance (mushroom pit), Hard Mode (Artis) and Quick Flea (the first boss's prize). **All five are locations
  already.** Mighty Pebble (Chuck) waits for the source of a Hearty Breakfast. Next in story order: chapter 2's floor
  medals and dialogue gifts around the city (Bug Me Not!, Sleep Resistance, Favorite One) and the waterfall HP Plus,
  each checked on screen first.

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
