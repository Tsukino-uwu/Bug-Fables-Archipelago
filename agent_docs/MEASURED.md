# Measured facts about Bug Fables

Each entry names our evidence and its date. The game can update without this repo changing, so a dated
entry is true as of that date. **Nothing here is "verified"**: that word is reserved for what the user
confirms on screen.

## Contents

- [The build](#the-build-2026-09-24-read-from-the-users-steam-install-game-not-run)
- [How the game grants items](#how-the-game-grants-items-2026-09-24-read-from-assembly-csharpdll-decompiled-with-ilspycmd-1011)
- [Observed in the running game](#observed-in-the-running-game-2026-09-24-grantprobe-a-new-game-played-by-the-user)
- [Save files](#save-files-2026-09-24-decompiled-inputiomanagerinputiocs)
- [Free save slots for the mod](#free-save-slots-for-the-mod-2026-09-24)
- [The main menu](#the-main-menu-2026-09-24-decompiled-startmenucs)
- [The quest board](#the-quest-board-2026-09-24)
- [Input](#input-2026-09-24)
- [Key-item grant sources, raw — SPOILERS for the whole game](#key-item-grant-sources-raw-2026-09-24--spoilers-for-the-whole-game)
- [World pickups and their gates](#world-pickups-and-their-gates-2026-09-24-entitydump)
- [Hard Mode boss prize medals](#hard-mode-boss-prize-medals-2026-09-24-code-read-not-seen-in-game)
- [Chapters — SPOILERS: map names](#chapters-2026-09-24-code-read-and-entitydump--spoilers-map-names)
- [What starts the gate events — SPOILERS](#what-starts-the-gate-events-2026-09-24-entitydump-scriptdump-with-event-lines-mapdump--spoilers)
- [Lore Books at the library](#lore-books-at-the-library-2026-09-24-the-users-play-through)
- [All crystal berries](#all-crystal-berries-2026-09-24-entity-dump-and-scriptdump-matched-to-the-bug-fables-wiki)
- [Respawning pickups, seen in play](#respawning-pickups-seen-in-play-2026-09-24-the-user-with-the-dev-log)
- [The door graph](#the-door-graph-2026-09-25-dev-scriptsdoor-graphpy-on-the-entitydump)
- [Doors paired with their way back](#doors-paired-with-their-way-back-2026-09-25-a-new-entitydump-with-positions-door-graphpy)
- [Transfers that aren't doors](#transfers-that-arent-doors-2026-09-25-scriptdumps-transfer-column-dev-scriptsevent-transferspy)
- [What the Explorer Permit opens](#what-the-explorer-permit-opens-2026-09-24-code-read-and-scriptdump-the-wiki-lists-four-uses)
- [All medals by source](#all-medals-by-source-2026-09-24-entity-dump-scriptdump-code-read-matched-to-the-bug-fables-wiki)
- [What the mod's code relies on](#what-the-mods-code-relies-on-code-read-2026-09-24-and-2026-09-25-moved-here-from-code-comments-2026-09-25)
- [Battles, for enemy shuffle — SPOILERS: boss ids](#battles-for-enemy-shuffle-2026-09-26-code-read-nothing-seen-in-game--spoilers-boss-ids)
- [The round pause-menu icons' colours](#the-round-pause-menu-icons-colours-2026-09-26-sampled-from-the-spritedump-sheet)
- [Visited areas and the pause-menu map](#visited-areas-and-the-pause-menu-map-2026-09-26-code-read-and-the-mods-diagnostic)
- [The item table's fields](#the-item-tables-fields-2026-09-26-code-read)
- [Quests: to measure](#quests-to-measure-when-quests-come-into-scope)
- [Key items: to measure](#key-items-to-measure)

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
- **What `Giveitem` writes, set against the mod's `ItemReceiver.Give`** (2026-09-25, code read,
  `MainManager.cs:11457-11563`). The game has no setter functions for this state: its own code writes the
  fields directly, and so does the mod, write for write:
  - item and key item: `items[type].Add(id)`. The game drops an item when the bag is full
    (`items[0].Count + 1 > maxitems`); the mod puts it in storage (`items[2]`, capped at `maxstorage` as the
    game's `Additem` does, `:12503`), else holds it back.
  - money: `showmoney = 1`, `money = Clamp(money + n, 0, 999)`: the same two lines.
  - medal: `badges.Add({id, -2})`, which is the game's `AddBadge` (`:16974`); the mod calls `AddBadge`.
  - crystal berry: `flagvar[14]++` (the shop currency) **and** `crystalbflags[n] = true`. The mod does only the
    first. `crystalbflags[n]` marks berry location n as found (the pickup's presence, `NPCControl.cs:818`, `:1349`),
    so a received berry must not set it. **Consequence:** the game's own berry total, `CrystalBerryAmmount()`
    (`:10212`, counts `crystalbflags`), counts berry *locations checked*, not berries received. It is shown by the
    `|cberrytotal|` text command (`:12744`) and unlocks the "all 50 berries" logbook entry (`:4343`).
  - flags: the game's `|flag,n,v|` command is a plain `flags[n] = v` (`:12462`), and `EventControl` alone
    sets a literal flag directly 338 times.
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
- **Chosen (2026-09-25):** **`flagvar[7]`** and **`[8]`** for the bits of the copies bought from Merab's and
  Shades's medal shops (a copy per bit, in location id order). Both free by the same two scans; `flagvar[69]` is the
  last one left.
- **Chosen (2026-09-25):** **`flagvar[69]`**, that last one, for the crystal berries received (`CrystalBerryTotal.cs`).
  No `flagvar` slot is left free by the two scans.
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
- **Every board shows one shared list** (2026-09-25, code read). `OpenQuestBoard(caretaker, caller)`
  (`MainManager.cs:17823`) lists `boardquests[0]` whatever board called it, so the town's board and the bar's show
  the same quests. A board is an entity with `Interaction.QuestBoard` (`NPCControl.cs:4435`): `data[0]` its
  caretaker (an entity on the same map), `data[1]` the caretaker's dialogue line played on taking a quest,
  `data[2]` a flag the board waits for (else the caretaker just talks). Opening copes with no caretaker or caller
  (null checks); taking a quest needs `boardcaller`: it plays `|questprompt|` + that line (`:5683-5694`), whose
  `|activateselectedquest|` moves the quest to taken and sets its `boardquestdata[id, 3]` flag (`:13899-13910`).
  The story adds quests to the list (`ChangeBoardQuest(id, 0)`).
- **But each board filters that list** (2026-09-25, code read, `MainManager.GetQuestsBoard`, `:15214`): the five
  bounties (`BoardQuests` 8 SeedlingKing, 9 FalseMonarch, 10 MotherChomper, 21 Sandwyrm, 23 PeacockSpider) show only on
  map 30, `UndergroundBar`; every other board hides them. Every board hides 11-17 (Prologue to Chapter6), 26 (Leif)
  and 30 (only the test room shows all). So the starting house's board (`BugariaOutskirtsOutsideCity` entity 26,
  inside 0, waits for flag 67 like the plaza's) lists exactly what the plaza's does. The open list measured on
  2026-09-24, `[0]` = 8,9,10,21,23, was all bounties: the bar showed them and any other board showed nothing.
- **How many, and how they arrive** (2026-09-25, code read). `BoardQuests` has 63 entries after `None`; 9 never show
  on a board (11-17 the chapter entries, 26 Leif, 30 Bee), so 54 board quests, 5 of them the bar's bounties. A quest
  joins the open list when its row in the `Data/QuestChecks` table is met (flags, or a visited area as a negative
  number, `MainManager.CheckQuests`, `:4027`), or by a dialogue command (`|addquest|`, `|addboard|`, `:13715`,
  `:13839`).
- **Every board quest, dumped** (2026-09-25, `QuestDump` in the running game, joined with EntityDump and the code).
  `BoardData` column 3 is the flag taking the quest sets (none for the bounties and a few others, whose NPCs check the
  quest lists instead); column 5 is its difficulty, 1 to 3. The unlock is its `QuestChecks` row (negative: a visited
  area; 0: never automatic, a dialogue adds it). **No accept flag does anything outside its own quest:** most are read
  only by the quest's NPCs; the code's five (131, 186, 187, 197, 423) are the quest's own state, checked while it runs
  (the chefs' dish checks, `EventControl.cs:574-597`) and cleared when it ends. Maps are where an entity requires,
  hides on or talks by that flag.

  | Id | Quest | Accept flag | Unlock | Maps using the flag |
  |---|---|---|---|---|
  | 1 | InnQuest | 3 | area 1 visited | BugariaMainPlaza, FactoryProcessingPump |
  | 2 | ChuckQuest | 44 | area 1 visited | ChucksAbode |
  | 3 | TheaterQuest | 47 | flags 130 | BugariaTheater |
  | 4 | ToyQuest | 50 | area 1 visited | BugariaMainPlaza |
  | 5 | LadybugQuest | 53 | flags 348 | BugariaOutskirtsOutsideCity |
  | 6 | UndergroundBar | 131 | flags 130 | BugariaCommercial, RubberPrisonGiantLairBridge |
  | 7 | CableCar | 182 | flags 76 | GoldenHillsCableCar, GoldenHillsPath2 |
  | 8 | SeedlingKing | none | area 1 visited | - |
  | 9 | FalseMonarch | none | area 1 visited | - |
  | 10 | MotherChomper | none | area 1 visited | - |
  | 18 | Crisbee | 187 | area 10 visited | - |
  | 19 | Kut | 186 | area 10 visited | - |
  | 20 | Fry | 197 | flags 191 + 192 | BugariaCommercial |
  | 21 | Sandwyrm | none | area 1 visited | - |
  | 22 | Butomo | 320 | flags 18 | DefiantRoot3 |
  | 23 | PeacockSpider | 146 | area 1 visited | - |
  | 24 | Tanjerin | 272 | flags 18 + 139 | GoldenSettlement3 |
  | 25 | ZaspDoll | 188 | flags 298 | DefiantRoot3 |
  | 26 | Leif | none | a dialogue adds it | - |
  | 27 | LibraryantRed | none | a dialogue adds it | - |
  | 28 | Madeleine1 | 190 | flags 299 | GoldenHillsDungeonEntrance |
  | 29 | Venus | 184 | flags 191 + 192 + 278 | GoldenSettlement1 |
  | 30 | Bee | none | a dialogue adds it | - |
  | 31 | PowerPlant | 226 | flags 225 | GoldenSettlement2 |
  | 32 | CardGame | none | a dialogue adds it | - |
  | 33 | CicadaBook | 240 | area 1 visited | BugariaResidential |
  | 34 | Vivi | 256 | area 10 visited | AntTunnels, AntMinesBreakRoom |
  | 35 | MenderQuest | 324 | flags 348 | HoneyFactoryEntrance, HoneyFactoryWorkerRooms, FactoryProcessingMalbee |
  | 36 | Kali | 265 | flags 18 | DefiantRoot3 |
  | 37 | Isau | 266 | flags 19 | DefiantRoot1 |
  | 38 | Bomby | 307 | flags 86 | BugariaResidential |
  | 39 | Madeleine2 | 375 | flags 200 + 347 | FGOutsideSwamplands |
  | 40 | GenEri | 423 | flags 300 + 18 | BugariaCommercial, RubberPrisonGiantLairBridge |
  | 41 | Mun | 422 | flags 345 + 427 | BugariaResidential |
  | 42 | BanditHunt | 424 | flags 300 | DefiantRoot3 |
  | 43 | ArtBee | 425 | flags 384 | BeehiveMainArea |
  | 44 | TermiteLunch | 426 | flags 409 | TermiteIndustrial |
  | 45 | Alex | 428 | flags 379 | BugariaResidential |
  | 46 | Mayor | 556 | flags 276 + 348 | DefiantRoot1 |
  | 47 | SeedlingHunt | 473 | flags 409 | TermiteIndustrial |
  | 48 | Layna | 464 | flags 409 | TermiteIndustrial |
  | 49 | Eetl | 479 | area 1 visited | BugariaOutskirtsOutsideCity |
  | 50 | Farmer | 482 | flags 86 | GoldenSettlement2 |
  | 51 | RizSis | 510 | a dialogue adds it | FishingVillage |
  | 52 | Wizard | none | a dialogue adds it | - |
  | 53 | Eremi | 570 | flags 300 | DefiantRoot1 |
  | 54 | MoleCricket | none | a dialogue adds it | - |
  | 55 | Maki | 607 | flags 555 | AntBridge |
  | 56 | BadBook | 617 | area 1 visited | AntPalaceLibrary |
  | 57 | WaspTwins | 637 | flags 347 | MetalIsland1 |
  | 58 | BlacksmithGuy | 634 | flags 39 | DefiantRoot3 |
  | 59 | WorkerTermite | 624 | flags 409 | TermiteMainPlaza |
  | 60 | Beetle | none | a dialogue adds it | - |
  | 61 | Rebecca | 700 | flags 555 + 454 + 136 | AntPalaceWarRoom |
  | 62 | Roach | 702 | flags 555 | WaspKingdomThrone |
  | 63 | StratosDelilah | 705 | flags 555 + 612 + 231 | UndergroundBar |

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
  hideout and gives it back); Kabbu (beetle) uses the horn, a knock-up and melee hit that also cuts grass (always on);
  Leif (moth) freezes, droplets included (always on, once he has joined). Vi and Kabbu are in the party from a new game,
  so while the party is vanilla only Leif gates anything.
- **Where a move is needed** (the user, 2026-09-25, most of it playing Leif alone; the maps from the Detector's log):
  - **Kabbu's horn (grass):** the way down to Shades's shop; the Strong Start medal's spot in `DesertBeforeGH` (flag
    415: grass, then something to hit; whether another member's move does the hit is untested); the way to Snakemouth
    Den, both `BugariaOutskirtsSnakemouthCorridor2` (after the grass tutorial) and `OutsideSnakemouth`. On
    `OutsideSnakemouth` seven `BeetleGrass` patches (x -2 to -18.5) split the corridor side (right) from the cave side
    (left); the crystal berry (location 19, x -9.9) and the dig spot `Mound` (x -24) are on the cave side, reachable
    from the den without the horn.
  - **Kabbu's horn (puzzles):** `SnakemouthDoorRoom` from the bridge side is a chain of horn steps: cut grass to reach a
    trampoline, knock a rock down onto a vine, push two rocks onto switches, which starts the trapdoor scene (its
    starter, `MushroomItem`, requires flag 13, presumably the switches' flag: not measured). Coming up from the trapdoor
    without the horn is presumably one-way for the same reason (the user's reading, not tried). The bridge room's
    hidden-spot discovery (discovery 2, location 30) is behind grass too.
  - **Vi's beemerang (range):** `SnakemouthBridgeRoom`'s bridge comes down when its rope is hit; from the right only the
    beemerang reaches it, from the left Leif's move hit it (so presumably any member's; Kabbu's not tried). The room's
    Tattle tutorial (Event2) ran with stand-ins and finished (flag 10); its hint (Event0) is skipped by Skip cutscenes.
  - **Vi's beemerang (enemies in the air):** the lake's fight, in Leif's joining scene (Event14 on `SnakemouthLake`:
    `ChangeParty({0, 1, 2})`, flag 16, then two of enemy 1 that can't be fled, `EventControl.cs:3462-3466`); in chapter 1
    only the beemerang hits them. Whether Kabbu or Leif learn such a move later is unknown. Moot while the mod skips
    that scene (since 2026-09-25).
  - **Any member's attack:** Snakemouth's switch-room switches (`Big Switch`, Event23, flags 33/34) take Leif's ice as
    well as the beemerang or the horn (in `SnakemouthUndergroundLeftB`).
- **A blocked walk-in ends in a teleport** (the user, 2026-09-25, the game's own behaviour): entering
  `SnakemouthUndergroundRightB` the "wrong", one-way way, the gate blocked the walk-in, the party stood still for a
  moment, then was put past the gate; after that the switch could be hit and the way back used. A forced walk
  (`MoveTowards`) has a timer (500 frames for the player, 0.75 of it in a scene, `EntityControl.cs:4951`); when it runs
  out the character is moved straight to the target, with smoke (`EntityControl.cs:3692-3701`). So a door whose walk-in
  point is behind a barrier can still be entered. The logic doesn't count on it (more cautious than the game is allowed).
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
  room. **The switches in the data** (event log and EntityDump, 2026-09-24): each switch room has a `Big Switch`
  (`data` 1 23) that starts **Event23**, which sets the switch's own `activationflag` (`EventControl.cs`, Event23:
  `flags[call.activationflag] = true`): **flag 33** in `SnakemouthUndergroundLeftB`, **flag 34** in
  `SnakemouthUndergroundRightB`. On `SnakemouthUndergrondDoor`, the `DoorEvent` trigger requires **both 33 and 34**
  and is hidden by **35**, presumably the middle door opened (not yet seen). The log confirmed Event23 started by
  RightB's Big Switch when the user hit it. Past the lowered barrier, the path leads back and **drops down into the big-door room** (the user): a
  one-way way back, for the room-level graph. The ledge above the drop can't be climbed to from the big-door
  room: it's reached only from the switch room, through the lowered barrier (the user). The left and right
  switches can be done in either order; both are needed to open the middle door, which leads on to the first boss.
  The Crunchy Leaf behind the pillar needs nothing once you're in its room (the user).

## The door graph (2026-09-25, `dev-scripts/door-graph.py` on the EntityDump)

- **567 doors between maps; 15 have no door leading back.** A door sends the party to the map in its `data[0]`
  (`NPCControl` trigger -> `MainManager.TransferMap(data[0], vectordata...)`).
- **Snakemouth Den: 31 doors, all paired except `SnakemouthEmpty`'s `WarpOut`** (to the door room).
  `SnakemouthEmpty` holds nothing but that exit and no door leads in: an unused room, left out of the graph (the
  user, warped there 2026-09-25; walking out led to the door room).
- **The door room's `DoorLoadZone` leads to Upper Snakemouth (`UpperSnekEntrance`) and requires only flag 41** in
  the data. The user, on a file past the first boss (chapter 2 started), walked through into the later area
  (2026-09-25). **What really gates it** (the user, 2026-09-25): in normal play the way back to the cave is
  closed after the first boss (Eetl's blocker outside the city until flag 67; from 67 a `guard` and a `sign` on
  `NearSnakemouth`, which no flag removes; seen closed after the first boss, the user, 2026-09-25, playing Leif alone). **And past the
  door, a slot needs a key item even with the door open:** `UpperSnekEntrance`'s `slot` is a `LockedDoor`
  (hidden by 517) whose `dialogues[0].y` is 11, and Event59's key list at index 11 is **key item 116, the Peculiar
  Gem** (`SnakemouthKey`; names dump), given in code by **Event117** (`EventControl.cs:19958`, chapter 4 by the
  event-number rule). The user saw the slot refuse them (Event59 twice in the log). So Upper Snakemouth's locations
  need the Peculiar Gem: a key-item rule once key items are shuffled. Its other locked doors (Event59 list):
  `keycard1`/`keycard2` on `UpperSnekMiddleRoom` index 12 (item 160, the Lab Card), and the gear slots on
  `UpperSnekBeforeBoss` indices 13-15 (items 157-159; 157 is the Small Gear).
- **Paired on paper, one-way in play:** the big-door room's `WarpRightUp` <-> `SnakemouthUndergroundRightB`'s
  `DoorMainRoom`. Leaving Right B puts the party on the ledge above the big-door room; the user dropped down and
  can't climb back to `WarpRightUp` (see "Respawning pickups, seen in play"). The left side has the same shape
  (`WarpLeftUp` <-> `SnakemouthUndergroundLeftB`), where the Mushroom spot and crystal berry #2 are (upper left).
  So the dump gives the doors, and play decides which way each can be crossed.
- **Story-gated doors:** the door room -> fall room door (`LoadZoneFallRoom`) requires flag 41 (the first boss); in
  the story the fall room is first reached by the trapdoor (Event5), which is no door at all. The way back
  (`SnakemouthFallRoom`'s `LoadingZoneDoorRoom`) requires 41 too. **So before the first boss the trapdoor is a
  one-way drop** into the fall room, and after it the rooms are joined both ways (the user remembers it as one-way;
  from the data, 2026-09-25). **Confirmed after the boss** (the user, 2026-09-25): a green bounce mushroom leads back
  up. It's `SnakemouthFallRoom`'s `JumpShroom`, which requires 41, next to the door back (requires 41); before the
  boss the room has a `blocker` instead (Event12, hidden by 41). So the trapdoor is one-way until flag 41, two-way
  after: a connection whose direction depends on a story flag.
- **Scenery switched by flags** (2026-09-25, the map dump's new `bugfablesap-mapflags.tsv`: `ConditionChecker`
  hides or moves an object by its own `requires`/`limit`, `FlagAnimation` plays an animation by flags; 328 such
  objects in all maps). In `SnakemouthDoorRoom` the big door's closed halves (`Base/Door`, `Door (1)`) and the
  trapdoor models are hidden from **flag 14** (the trapdoor fall), and the open halves (`Door (2)`, `Door (3)`) shown
  from 14 (seen, the user, 2026-09-25: the trapdoor scene opens the trapdoor and the big door). So the door looks open from 14, while its load zone (`DoorLoadZone`) waits for 41. On
  `UpperSnekEntrance`, the round door (`Base/CircleDoor`) is hidden and the Gem shown in the slot from **517**.
  In `SnakemouthUndergrondDoor` the middle door's models switch at **35**; the switch rooms' `Gate`s at 33 / 34.

## Doors paired with their way back (2026-09-25, a new EntityDump with positions, `door-graph.py`)

- **A door's way back is the door the party arrives next to**: on the target map, the door leading back whose start
  position (entity fields 6-8) is nearest, in 3D, to the arrival point (`vectordata[1]`), within 10 units.
- **531 of 567 doors pair mutually** (each is the other's pair). **8 pair one way only, 28 don't pair.**
- **Story variants are one door:** doors on one map within 1 unit of each other (Golden Settlement's day and night
  copies, flags 85/86; the night one leads to the night map). **Two doors can share a name** on one map
  (`WaspKingdomOutside`'s `loadzoneinside`, to `WaspKingdom1` hidden by 370, to `WaspKingdomMainHall` from 555), so
  doors are told apart by entity index.
- **Door `data`** (`MainManager.TransferMap`, `MainManager.cs:17467-17620`): `[0]` target map; `[1..3]` = 1 switch
  the camera offset, angle and limits on arrival to `vectordata[3..6]`; `[4]` = 1 skips the walk into the door
  (9 doors: `SnakemouthDoorRoom`/`SnakemouthFallRoom` fall-room doors, `UndergroundBar`'s exit, `DefiantRoot1`'s well
  and back, `FarGrasslandsWizard`'s basement, `GiantLairBeforeBoss`/`2`'s ladders). The arrival jump is the door
  entity's `emoticonoffset.x` (field 175).
- **To check in play (the user: later, like `SnakemouthEmpty`):** `GoldenSettlement2`'s `Neo`, `beeguard`, `sign`,
  `sign - Duplicate`, `farmer ant outside` (all lead to `GoldenSettlement1`'s farm door: story blockers that turn you
  back?); `TermiteIndustrial`'s `NEARloadzoneback` (doors into their own map); `GiantLairBeforeBoss2`'s two ladders
  down, one ladder up; the Barren Lands `return...` zones (arrival 25-75 units from any door: one-way, a maze's
  "wrong way"?) and those leading into their own map; `SandCastleBasement` <-> `SandCastleMainRoom`'s basement doors
  (no door within 10 units of the arrival).

## Transfers that aren't doors (2026-09-25, ScriptDump's transfer column, `dev-scripts/event-transfers.py`)

- **Dialogue lines:** 7 lines move the party with `|warp,<map>[,x,y,z]|` or `|loadmap|` (`MainManager.cs:13262-13280`):
  `BOLostSandsEntrance` 10, `DefiantRoot2` 38, `FarGrasslandsOutsideCave` 3, `Swamplands8` 4 and 7, `TermiteMainPlaza`
  65, `BarrenLandsPinkSpider` 18. None uses `|transfer|`.
- **Story events:** 88 `LoadMap` calls in 63 of `EventControl`'s event methods; 20-odd reload the current map. What
  starts each: `event-triggers.py` on the listed events. Among them: **Event61, the bar's hatch** (to `UndergroundBar`,
  started by `BugariaCommercial` line 32, the hatch examined); **Events 108 and 109, to `HideoutCell`** (108 is the
  garden guards catching the party, the user's trip; 109 is the story's first capture, which takes the beemerang,
  flag 11, and in the cell gives dig, flag 18, `EventControl.cs` Event109; leaving the cell needs dig, the user; the cell also holds a `Dropplet` with no flags, which the user thinks is
  cosmetic, not needed to leave: unconfirmed, so the rule is dig only); **Event153, the boat** (seven harbours); **Event68**, three map
  pairs chosen by an `entrance` flag (elevators, to read); Event196, a destination from a list chosen in a menu.
- Not yet sorted into "chosen by the player" and "the game sends you" (the decision: `apimplementation.md`, build
  step 12, item 8).

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
- **Crystal berries are spent only at Shades's shop** (2026-09-25, a full script scan with `setvar`/`checkvar` added):
  her buy line (`UndergroundBar` line 3) is `checkvar,atleast,14,var10,5 setvar,sub,14,var,10 removebadgeshop,1,var,0
  kill,caller giveitem,2,var,0,6`: it checks and subtracts the price (`flagvar[10]`) from the counter (`flagvar[14]`).
  Nothing else in the game's code or scripts lowers `flagvar[14]`. **Prices** (medal table column 7, read in game with
  the console's `prices`): starting stock 19 (4), 6 (5), 9 (4), 43 (3), 42 (2) = 18; chapter 3's end 0 (2), 49 (5) =
  7; chapter 4's end 76 (2) = 2; chapter 5's start 44 (3), 6 (5), 50 (5) = 13; chapter 6's start 57 (5), 35 (5) = 10.
  **All of it costs exactly 50, every crystal berry in the game.** Berry prices (column 5) for the record: Merab's
  starting stock 0 (45), 1 (55), 7 (35), 12, 30, 86, 84, 87, 88 (30 each), 81 (45).
- **The way down to Shades's shop (the underground bar, map 30)** (2026-09-25, entity dump, ScriptDump, code): no door;
  `HideoutEntrance` on `BugariaCommercial` is examined (Check). Its lines: default 27 (sets flag 8), with flag 8 line
  30, with flag 135 line 32, which starts Event61, a plain `LoadMap(30)` with no party lookups. Flag 135 is set by a story
  scene (`EventControl.cs:12698`). The user asked for it set on a test file to reach the shop.
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

## What the mod's code relies on (code read 2026-09-24 and 2026-09-25; moved here from code comments 2026-09-25)

Facts the mod's hooks depend on, with their place in the decompiled source. Each names the file that uses it.

### Shops, the Quality of life page, dialogue

- A medal shop slot is an NPCControl with interacttype Shop, whose entity has animid 2 (medal) and animstate = the medal id, made by the shopkeeper's SetBadgeShop from avaliablebadgepool; the shopkeeper's dialogues[9].x is its badgeshops index, and its private field `shopitems` (EntityControl[]) holds the shelf's slot entities (NPCControl.cs:1504-1580). Used by `ShopSwap.cs`.
- A shop slot's description box, CreateDescWindow(shop), reads the medal's name and description from badgedata[id, 0] and [id, 1]; NPCControl.Interact copies the name into the buy prompt's text and the price into flagvar[1] (NPCControl.cs:4183-4228, 4360-4372). Used by `ShopSwap.cs`.
- UpdateShops rebuilds the shelf pool from badgeshops on every map start and after each purchase (MainManager.cs:4087, MapControl.cs:343, NPCControl.cs:1528); the game's own shoppool command writes badgeshops (MainManager.cs:11638-11657); the money command clamps to 0-999 (MainManager.cs:12580-12590). Used by `ShopSwap.cs`.
- Map entity table rows (Data/EntityData/<map id>) are fields split by '}': field 1 the entity type (DoorOtherMap), field 60 the data count followed by the data values (61 = the target map for a door), field 71 the vectordata count followed by x,y,z triples from 72 (MapControl.cs:1540-1566). Used by `QualityOfLife.cs`.
- MainManager's private static `currentdialogue` and `diagstring` (List<string>) are the line being shown and the lines so far; holding skip only works when they match (on the newest line), a box is open, no prompt/list, not |noskip| (MainManager.cs:2749, :2799, :5125-5140). Used by `QualityOfLife.cs`.
- The intro slides' fades are per-frame lerps scaled by Time.smoothDeltaTime (MainManager.TieFramerate, MainManager.cs:9567), so Time.timeScale speeds them; the game itself uses timeScale 2.5 for cooking (MainManager.cs:5546), and EndEvent resets timeScale to 1 (EventControl.cs:184). Used by `QualityOfLife.cs`.
- A hold-up's Giveitem shows its follow-up line via GetDialogueText(redirect) (MainManager.cs:11592); |end| sets `end`, which skips the final wait for a press (MainManager.cs:11909-11910, :14171); a negative id reads commondialogue (MainManager.cs:10186); each slide line waits for a press at its end (MainManager.cs:14169-14174). Used by `QualityOfLife.cs`.
- Event8 (new game): its first step after the slides is ChangeParty({1}, fromscratch: true, destroyoldentity: false), Kabbu alone (EventControl.cs:2740-2755); its end is HUD back, ResetCamera, the building's music, EndEvent, fade-in (EventControl.cs:2858-2866); after its slides it stands Kabbu 2.5 left of entity 4 (EventControl.cs:2770); the slides' backdrop NewSolidColor("back") is made at EventControl.cs:2655 and lives through 2656-2735. Used by `QualityOfLife.cs`.
- Event16's end points the camera at the new leader (EventControl.cs:3795); the starting house's exit only hands the camera back to the player for insides that centre on themselves (MapControl.cs:1373). Used by `QualityOfLife.cs`.
- Bridge scenes: Event0 (bridge message) is party/camera moves, three lines, flag 11, and flag 11 hides its trigger BridgeMessage (limit 11) (EventControl.cs:274-333); Event1 (rope) plays the bridge's Fall animation, fixes it fallen, then flags 7 and 11 (EventControl.cs:334-407); Event83 barkeeper's first talk sets flag 158, and its else branch handles bounties (EventControl.cs:13052-13080). Used by `QualityOfLife.cs`.
- Shopkeeper prompts are |prompt,map,Y,N,target1..targetN,text1..textN| (MainManager.cs:12213-12222). Used by `QualityOfLife.cs`.
- Holding skip: inputcooldown is 16 after a box, 10 when a new dialogue opens (MainManager.cs:5147, :10738), 4 while a box is typing (:5151), counted down one per frame (:7298). Used by `QualityOfLife.cs`.

### The connection, the dev console, the party, the menus

- The game's berry sprite for a money giveitem, by amount: itemsprites[0, 186] for 20 or more, [0, 6] under 5, else [0, 7] (MainManager.cs:11506). Used by `ItemIds.cs`.
- The game's money reward code adds berries clamped to 0..999 and sets showmoney = 1 to show the counter (MainManager.cs:11534); the money script command does the same (MainManager.cs:12580-12590). Used by `DevCheats.cs`, `DevConsole.cs`.
- A pickup's touch starts in NPCControl.OnTriggerEnter, an Enter trigger: standing on an item doesn't take it again, stepping off and back on does (NPCControl.cs:4516). Used by `DevConsole.cs`.
- A pickup's touchcooldown is waited out by CheckItem (NPCControl.cs:5608) and counted down each frame (NPCControl.cs:2802); 90 holds it about 1.5 s. Used by `DevConsole.cs`.
- Every hit's damage ends in BattleControl.DoDamage(attacker, ref target, amount, property, overrides, block); the other overloads lead there. The game tells the party from enemies by the target's "Player" tag (BattleControl.cs:7283, :7295). Used by `DevConsole.cs`.
- EntityControl.Jump is the normal jump (height and sound, EntityControl.cs:4598, via PlayerControl.DoJump); the player's own jump only fires on the ground (PlayerControl.cs:372); a jump sets jumpcooldown to 30 frames, longer than the whole jump (measured in the log 2026-09-24). Used by `DevConsole.cs`.
- Map entity table Data/EntityData/<map id>: rows split on '}', fields 6-8 the start position, field 194 the activationflag, as MapControl.CreateEntities reads them (MapControl.cs:1477-1640, :1661). Used by `DevConsole.cs`, `WarpButton.cs`.
- MainManager.TransferMap ends by walking the party to its target and waits for that walk (MainManager.cs:17610-17624): a target over water is never reached, the transition never ends, and the game keeps respawning the party there (seen at SnakemouthLake, 2026-09-24). Used by `DevConsole.cs`.
- Water raycasts as ground; water, spikes and pits carry the game's Hazards component. Used by `DevConsole.cs`.
- A map's auto-start cutscenes (MapControl.autoevent, pairs of (flag, event)) run on arrival while their flag is off (MapControl.cs:874-883); arriving out of story order, Event21 on SnakemouthUndergrondDoor crashed and left the game "in an event" (2026-09-24). Used by `DevConsole.cs`.
- Resets for what a dead cutscene leaves: MainManager.ResetCamera (MainManager.cs:7398), MapControl.RestoreLimit (MapControl.cs:1432), MainManager.ChangeMusic() for the map's own music (MainManager.cs:4873); Event31 left all three broken (2026-09-24). Used by `DevConsole.cs`.
- PlayTransition 4 ends on a black dimmer (MainManager.cs, Transition case 4 -> 0); PlayTransition 1 fades in and removes it. The boat scene (Event107) parents the party to the boat with LockRigid(true) and undoes both at its end (unparent, LockRigid(false), fade in). Used by `DevConsole.cs`.
- The trapdoor scene turns the party's gravity off and forces an animation (EventControl.cs:1334-1335). Used by `DevConsole.cs`.
- The game's dialogue end turns off message and the waits, and shrinks and removes the box (MainManager.cs:14185-14204). The private field `textbox` holds only the letters ("Text: ...", MainManager.cs:10677, :10814); the speech box is the Textbox prefab kept in maintextbox (MainManager.cs:10781), and an orphan "Textbox(Clone)" can stay under the GUI camera after dialogue ends. Used by `DevConsole.cs`.
- MainManager.SetPlayers(positions) places member j at newentitypos[j] (MainManager.cs:9416-9439), so a list shorter than the party throws IndexOutOfRange. Used by `PartyFit.cs`.
- A scene that reloads the map with recreateplayers (Event45's throne room, LoadMap) remakes the party characters, so references to the old ones go null. Used by `PartyFit.cs`.
- EntityControl.LateUpdate (EntityControl.cs:3672) runs after the scene's step and the entity's own updates, just before drawing: the place to force a renderer off. Used by `PartyFit.cs`.
- Every EntityControl.MoveTowards overload ends in MoveTowards(Vector3, float, int, int, bool) (EntityControl.cs:4911-4960). Used by `PartyFit.cs`.
- Scenes take the party as a list and use fixed slots p[0]..p[2] (about 110 lookups; Event83, the barkeeper's first talk, reads p[2], EventControl.cs:13055-13058); GetPartyEntities(true) returns the party in id order (MainManager.cs:9483-9503). Used by `PartyFit.cs`.
- The horn tutorial (Event10) waits while entities[0].forcemove (EventControl.cs:2935); the trapdoor's end puts member m at the m-th scene character's position (EventControl.cs:1476-1484); the spider fight's end (Event6, EventControl.cs:2272-2289) sets entities[2].following = entities[1]; the droplet scene's end (Event21, EventControl.cs:4112-4114) walks GetEntity(-2) and (-3) to the player. Used by `PartyFit.cs`.
- MainManager.GetEntity: -2 and -3 are the second and third member by position (MainManager.cs:18526-18537), -4/-5/-6 are Vi/Kabbu/Leif by name (MainManager.cs:18538-18570), 1000 + n reads map.tempfollowers[n] (MainManager.cs:18512-18515) and throws ArgumentOutOfRange when nobody is there; no caller null-checks the result. Used by `PartyFit.cs`.
- The main menu's confirm sound: StartMenu.Update plays "Confirm" for every main-menu choice (menuid 1) before acting on it. Used by `MenuToggle.cs`.
- On the file select (menuid 2, submenu 0), confirm on file 0-2 is StartMenu.Update's load or new-game branch (StartMenu.cs:512-535, Event22 or Event8); the save slots' boxes sort at -20 to -60 and their text at 10 (StartMenu.ShowSaves). Used by `MenuToggle.cs`.
- Closing the game's Settings from the title resets maxoptions to 3 (PauseMenu.cs:1811). Used by `MenuToggle.cs`.
- MainManager.Create9Box box type 1 is the game's orange box; ButtonSprite draws its label with no sort of its own, so the label text must carry |sort,N| to show over a box. Used by `MenuToggle.cs`.
- Pause menu window 0: maxoptions icons (4, or 2 in battle) built in BuildWindow as sprites[13 + n] with guisprites[74 + n] via NewUIObject (PauseMenu.cs:2378-2497, :2493-2497); window 0's sprites array is 19 long (:2404), other pages' 8 to 12 (PauseMenu.cs:2235-2678); confirm opens window option + 1 (:374-380); labels are menutext[10 + option] and [50 + option] (UpdateText); IconAnim gets {13, 14, 15, 16} and indexes by option (PauseMenu.cs:351); PrepareExit shrinks the boxes and DestroyPause follows 0.25 s later (PauseMenu.cs:1839). Used by `WarpButton.cs`.
- The game's menu cursor sprite is MainManager.cursorsprite[0], set up as at MainManager.cs:14822 (sort, layer 5, SpriteBounce.MessageBounce). Used by `WarpButton.cs`.
- A new game begins on the Outskirts: Event8 loads map 16 (EventControl.cs:2636). Used by `WarpButton.cs`.
- guisprites[34] is a round blue map icon in the pause-menu icon style (from SpriteDump's sheet). Used by `WarpButton.cs`.
- MultiClient.Net 6.7.1 net40: every send first checks websocket-sharp's IsAlive, which pings and blocks up to 5 s for the pong (WebSocket.ping, WaitTime); websocket-sharp's Close sends a close frame and waits up to 5 s for the answer. Used by `ApConnection.cs`.
- MultiClient.Net 6.7.1 keeps every location check the server hasn't confirmed and resends them with the next send: each `LocationChecks` packet is every checked location except `serverConfirmedChecks`, which fills from the server's `Connected` and `RoomUpdate` packets (`Helpers/LocationCheckHelper.cs` at tag v6.7.1, `GetLocationChecksPacket`; read 2026-09-25). Used by `ApConnection.cs`.
- Scouting with HintCreationPolicy.None creates no hints; a hint-creating scout would announce the seed's placements. Used by `ApConnection.cs`.
- ArchipelagoSocketHelper tries wss:// first for a bare address and falls back to ws://. Used by `ApConnection.cs`.
- slot_data location_shops: {location id: {shop, medal}}, one location per copy a medal shop ever stocks, done when the save marks that copy bought (ShopSwap). Used by `ApConnection.cs`.

### Map data, doors, dumps and probes, hot reload

- Interacting with an item shop slot (`Fixedshop<n>`, an item entity: animid 0, animstate the item id) puts the price in `flagvar[1]` and the item's name in `flagstring[0]`, then opens the shopkeeper's buy talk; looking at a slot opens its description box from `itemdata[0, id, 0]` and `[.., 2]`. The `additem` command only adds to the bag list, with no item-get box (NPCControl.cs:4374-4378, NPCControl.cs:4220-4226, MainManager.cs:12570-12571). Used by `ItemShops.cs`.
- A map's entity table (`Data/EntityData/<map id>`, one line per entity, fields split by `}`) holds an entity's `data` count at field 60 (values from 61) and its `vectordata` count at field 71 (each vector three fields from 72); names are in `Data/EntityData/Names/<map id>names`, one per line in the same order (MapControl.CreateEntities, MapControl.cs:1540-1566, MapControl.cs:1454). Used by `DoorShuffle.cs`.
- A door's `data[4] == 1` means TransferMap skips the walk into the door (`vectordata[0]`): a hole or a ladder. Used by `DoorShuffle.cs`.
- Grass that drops an item picks one `vectordata` entry at random and drops item x of it, so a grass entity's `vectordata` is its item list (NPCControl.cs:5976-5983). Used by `EntityDump.cs`.
- `GlowTrigger` components are the electric triggers, which the bubble shield also blocks (GlowTrigger.cs:189); `Hazards` type `WalkableSpike` is what the bubble shield walks over (Hazards.cs:207). Map prefabs load from Resources `Prefabs/Maps/<map>` (MainManager.cs:9652); dialogue tables from `Data/Dialogues<lang>/Maps/<map>` (MainManager.cs:2981) . Used by `MapDump.cs`, `ScriptDump.cs`.
- Dialogue commands that move the party to another map: `transfer` and `warp` take a map id (or varN) and an optional position; `loadmap` reloads a map (MainManager.cs:13262-13280). Used by `ScriptDump.cs`.
- `MapControl.autoevent` holds (flag, event) pairs: the map starts the event once while the flag is off, then sets the flag; these are story steps no entity or dialogue starts (MapControl.cs:874-883). Used by `MapDump.cs`.
- Loading a save allocates new `flags`/`regionalflags`/`crystalbflags` arrays of the same length, so a watcher must compare array identity, not length, or a load reads as mass flag flips (MainManager.cs:17274). Used by `GrantProbe.cs`.
- ScriptEngine's FileSystemWatcher can't run in this game: its Mono throws NotImplementedException from `new FileSystemWatcher(path)` inside ScriptEngine.Awake (2026-09-24), so DevReload polls the DLL and sets ScriptEngine's private `shouldReload` (with `autoReloadTimer`), field names read from ScriptEngine.dll r11.1 with ilspycmd. Used by `DevReload.cs`.
- A hot reload during a scene, conversation or battle orphaned the stand-ins the old plugin made for a running scene (the spider fight, 2026-09-25), so DevReload waits for a free moment. Used by `DevReload.cs`.
- World pickups pass the item id to SetText as `var,0`: NPCControl.CheckItem puts it in `flagvar[0]` first. Used by `TextProbe.cs`.
- The plugin is built with a Windows ("full") pdb: ScriptEngine reads the plugin through Mono.Cecil with symbols and can't read a portable pdb, so the plugin would silently never load (measured in the author's other project, 2026-08-28). Used by `BugFablesAP.csproj`.
- A dig spot (DigSpot) starts an event only when data[0] >= 2; data[0] = 0 buries an item, 1 a crystal berry (NPCControl.cs:5396-5420). Used by `dev-scripts/event-triggers.py`.

### The world's locations, from the data file's notes

- Event10 (the horn tutorial near Snakemouth, which sets flag 17) is started by the `Woodboring` EventTrigger on `NearSnakemouth` (EventControl.cs:2915); its 10 berries are `giveitem,-1,10,6` written in the event's code (EventControl.cs:3040) (location id 2). Used by `data/locations.json`.
- Cut grass that drops an item copies its own one-time flag onto the drop (NPCControl.cs:5981-5983), so a grass drop is an ordinary pickup location (location id 12). Used by `data/locations.json`.
- The Lore Book behind the Ant Palace library bookshelf is flag 71 (play-through log, 2026-09-24) (location id 15). Used by `data/locations.json`.
- A ground crystal berry's map data holds its index in data[3], copied to data[0] at load (NPCControl.cs:938); a berry dropped from cut grass has the index in the grass's data[1], carried by the drop in data[0] (NPCControl.cs:5967-5972) (location ids 19, 21). Used by `data/locations.json`.
- Flag 281 (one of the three respawning Snakemouth pickups' hiding flags) is set by nothing found in the code, the map scripts or the entities (2026-09-24); MEASURED.md only records it reading False in play (location ids 22, 23, 24). Used by `data/locations.json`.
- Entities behind pickup locations: `SnakemouthUndergrondDoor` entity 6 (HoneyDrop, id 22) and entity 20 (`CrunchyLeaf - Duplicate`, holding a Mushroom, id 23); `SnakemouthUndergroundRightB` entity 11 (CrunchyLeaf, id 24); `BugariaOutskirtsEast1` entity 38 (a Drowsy Cake under a stone, flag 735, id 25); `BugariaResidential` entity 39 (`badbook`, id 32) and entity 10 (`BugMeNot - Duplicate`, flag 59, id 33); Madeleine's house (inside 2) entity 71 (`tea`, Burly Tea, x 36, id 44) and entity 54 (`lorebookmadeleine`, Lore Book, x 33.6, activationflag 392, id 45) (EntityData / entity dump; locations.json)
- The pier statue's dialogue line 63 runs `|discovery,49|` (ScriptDump; `BugariaPier`'s own discovery list is 49, MapDump); examining it set flag 654 in the play log (2026-09-25) (location id 27). Used by `data/locations.json`.
- Discovery sources: Event11 (arrival outside Snakemouth) is `OutsideSnakemouth`'s autoevent 22:11 (MapDump) and records discovery 0 (EventControl.cs:3095); Event6 (fall room EventTrigger, data 6, limit 27) records discovery 1 at its end (EventControl.cs:2293); Event13 (entity `HiddenEvent` on `SnakemouthBridgeRoom`) records discovery 2 (EventControl.cs:3278); Event27, started by cutting the grass entity `AncientHouseDiscovery` (BeetleGrass) on `SnakemouthUndergrondDoor`, records discovery 3 (EventControl.cs:5001) (location ids 28-31). Used by `data/locations.json`.
- Event38 (the plaza statue discovery) asks for party members by name and sets no story flag (kept_present StatueDesc). Used by `data/locations.json`.
- Merab's buy is her line 39, `giveitem,2,var,0`; the later stock entries are added at EventControl.cs:11960-11962 (Event73), :16952-16954 (Event99), :20562-20563 (Event120), :24279-24281 (Event142), and We Owe Ya! by `MapControl.HelperMedalCheck` (flag 716, MapControl.cs:355-361). Entry 18 is the second TP Plus copy (after entry 2), entry 19 the second Ambusher (after entry 6) (location ids 34-57). Used by `data/locations.json`.
- Madame Butterfly is `ButterflyShopkeeper`, entity 10 on `BugariaCommercial`; the caravan's keeper is `Crickerly2`, entity 33 on `BugariaOutskirtsOutsideCity`; each stock entry is a `Fixedshop<n>` slot (the keeper's data, entity dump) (location ids 58-65). Used by `data/locations.json`.
- After the first boss, a ladybug girl outside the city starts the lost-kid quest (her flag 54); the kid then waits at the lake (story event First Boss Beaten). Used by `data/locations.json`.
- Event12 (the "turn back" blockers) only walks the player and sets no flags (kept_open eetlblocker1 - Duplicate, MM). Used by `data/locations.json`.
- The town's arrival-scene trigger is `DoorBugaria - Duplicate` on `BugariaOutskirtsOutsideCity` (an EventTrigger starting Event60, hidden by 107); the real door `DoorBugaria` is a DoorOtherMap to map 9 requiring 107 (kept_open / kept_present). Used by `data/locations.json`.
- On `BugariaOutskirtsOutsideCity`, `MiningAnt` and `MinerAntWalk` (miners at the rocks), `Crickerly1` (talk only) and `FuzzyMoth` all have limit 41; `LaydbugGirl` and `LaydbugBoy` require 41, with everyday lines 100 ("Dib, please don't do anything reckless") and 101 ("I'm not a kid anymore, Leby!"); their other lines answer to the lost-brother quest's flags (kept_open / kept_present). Used by `data/locations.json`.
- `SnakemouthFallRoom`'s `JumpShroom` (the bounce mushroom up to the pitfall room) requires 41 like `LoadingZoneDoorRoom` (kept_present). Used by `data/locations.json`.
- The palace's own blockers `makiblocker1` and `makiblocker2` stay in place: the story goes on there (kept_open MM). Used by `data/locations.json`.
- The Outskirts rocks' removal leaves `LoadZoneGoldenPath` still waiting for flag 41 on its own (scenery_hidden Base/BlockingRocks). Used by `data/locations.json`.

## Battles, for enemy shuffle (2026-09-26, code read; nothing seen in game) — SPOILERS: boss ids

- **One entry point:** every fight goes through `BattleControl.StartBattle(int[] enemyids, int stageid, int adv,
  string music, NPCControl calledfrom, bool canescape)` (`BattleControl.cs:718`). A map enemy passes itself as
  `calledfrom` with its `battleids` (`NPCControl.cs:5947`, `canescape: true`). A story fight passes `calledfrom:
  null` with a literal id array from its event. The game's own random swap (`EnemyCheck`, `:703-716`) runs only for
  map fights (`calledfrom == null || calledfrom.eventid <= 0`), then `StartData` snapshots the ids for a retry.
- **The running event's id** is `MainManager.lastevent`: set first thing in `EventControl.StartEvent`, -1 in
  `EndEvent` (`EventControl.cs:76`, `:177`).
- **Scripted fights: 69 `StartBattle` calls in `EventControl.cs`** (listed with `grep -n "StartBattle("`). One event
  can start several (Event40 three, Event163 four, Event200 two, Event124 five bounties), so a scripted fight is
  identified by **its event and its original id array**, not the event alone. Event173 starts `{23, 51}` twice
  (the same fight both times).
- **A boss's reward doesn't depend on the enemy beaten.** The event waits `while (MainManager.battle != null)`, then
  sets its flags and pays its prize by slot (`AddPrizeMedal(id)` or the `addprize` dialogue command,
  `MainManager.cs:3981`, `:13751`). `prizeenemyids` is read only for the name in Artis's line
  (`EventControl.cs:5739-5745`): with a swapped boss, Artis names the vanilla one (cosmetic).
- **The game's own boss lists** (`EventControl.cs:24-42`): `bosslist` {2, 24, 46, 54, 69, 95, 41, 36, 35, 50, 55,
  77, 98, 96, 90, 91, 92, -2}, `minibosslist` {21, 42, 40, 49, 31, 51, 34, 97, 85, 72, -1}, `minibosscard` (22 ids,
  used for the card game and the bestiary). Used by the rematch machine below and by `GetRandomEnemy` (`:64-71`),
  which excludes them and `excludeids`.
- **The rematch machine (Event85) is the game's proof that each listed boss works as its own fight**
  (`EventControl.cs:13598-13935`). Every boss in those lists starts from the same event, on **stage 16** (a neutral
  stage, not the boss's own), with `canescape: true`. Its only per-boss setup is a switch: the id group (2 → {13};
  41 → {20, 20, 41}; 72 → {27, 26, 72}; -1 → {3, 15}; -2 → {113, 114, 115}; 23/51 and 85/86 as pairs) and the music,
  plus one flag reset for the first boss (`flagvar[11] = 0`, `flags[37] = false`). It runs with `flags[162]`
  (hologram mode), which changes only the look, EXP and fleeing money (`BattleControl.cs:974`, `:6487`, `:30333`,
  `:30411`; `MainManager.cs:6294`), not any enemy's actions.
- **Events that reach into the running fight** (a swap there needs care):
  - Event137 (id 69) adds `SurviveWith10` to `enemydata[0]` (`EventControl.cs:23137`). Only enemy 69's own action
    removes it (`BattleControl.cs:18361-18470`), so another enemy in that slot could never die. **Not swappable.**
  - Event182 (id 96) freezes `playerdata[2]` with `EventStop` and calls `SetLastTurns()` (`:30665-30671`), a
    scripted fight. **Not swappable until read.**
  - Event40 (three fights) and Event224 reach into the stage (`battlemap.transform.GetChild(...)`, `:6313`,
    `:37599`); keeping the event's own stage keeps that safe.
  - Event3 and Event6 (tutorial fights) move entities into the stage and set `tempdata` / `disablespy`.
  - Event173 calls `StartData({23, 51}, ...)` itself (`:29004`, `:29251`), overwriting the retry snapshot.
  - Events that test `battleresult` for a scripted loss or a retry: 30, 40, 90, 120, 156, 163, 192, 207, 210, 224.
- **Code tied to an enemy's id** (from the survey, not each read): `eventondeath` (column 26) sends a defeat into
  `EventDialogue` (`BattleControl.cs:1972`, `:30719-30731`); setup by id at `:976+` (VenusBoss's extra entity,
  fixed positions for BeeBoss, SandWyrmTail, Pitcher); `GetEnemyData` swaps some ids' data (`MainManager.cs:
  6157-6191`); `NPCControl.StartBattle` forces "Battle3" music for ids 25-28 (`NPCControl.cs:5932`).
- **Map enemies' encounters** (2026-09-26, EntityDump with its new `battleids` column, run at the title screen): 327
  `Enemy` entities on 124 maps, every one with an encounter. Sizes: 74 of one enemy, 183 of two, 66 of three, 4 of
  four. 59 distinct enemy ids, **none from `bosslist` or `minibosslist`**. NPC and Object rows also fill
  `battleids`, for other uses (an event id, a stealth size), and aren't encounters. Odd ones: TestRoom's
  {113, 114, 115} (the holo party, a test room), and one {108} (IceKrawler).
- **An enemy's `eventid` is a respawn timer, not an event** (`NPCControl.cs:1236-1239`, `:4024-4035`): the enemy
  respawns that many frames after its death. 20 map enemies have one, all puzzle enemies (a flower to freeze, a
  pressure plate, a crank). The game's own `EnemyCheck` swap skips them (`calledfrom.eventid <= 0`). Their fight can
  be shuffled; their map model can't be changed without breaking the puzzle.
- **Who can hit what** (2026-09-26, code read). An enemy starts at the position in its data's column 19
  (`BattlePosition`: Ground, Flying, OutOfReach, Random, Underground; `MainManager.cs:6198`). During a fight,
  `RefreshEnemyPos` moves it between Ground and Flying by height (unless `cantfall`, column 29). The base attack's
  targets come from `SetTargets` (`BattleControl.cs:3091-3107`) and `UndergroundCheck` (`:4784`):
  - Vi (-1): anything but OutOfReach, and not Underground.
  - Kabbu (-2): Ground only, and only the front enemy. Underground doesn't count.
  - Leif (-3): Ground and Underground.

  Thrown items target Ground only (`:4934`). Skills have their own targeting: `skilldata` columns 7 (ground only)
  and 8 (front only), and columns 4-6 say which members a skill needs (`CanSkill`, `:30472`). Who knows which skill
  depends on story flags, party level and equipped medals (`MainManager.RefreshSkills`, `:8395`). The tables
  themselves are game data (`Data/EnemyData`, `Data/SkillData`), not in the code, so not dumped yet.
- **Each enemy's start position** (2026-09-26, EntityDump's new `bugfablesap-enemies.tsv`, the enemy table read at
  the title screen, 117 rows): 96 Ground, 16 Flying, 3 Random, 1 Underground, one blank row.
  - **Flying:** Thief 6, FlyingSeedling 10, WaspHealer 28, Midge 29, Flowering 38, CursedSkull 61, Mothfly 78,
    MothflyCluster 79, DeadLanderB 88 (can't fall), IceWarden 109 (all on maps); MidgeBroodmother 36, BeeBoss 46,
    EverlastingKing 91 (bosses); KeyR 101, KeyL 102, FireWarden 106.
  - **Underground:** Sandworm 33 (on maps).
  - **Random:** Mushroom 1, BeeBot 43, Mantidfly 71 (on maps).
  - Every other boss starts on the Ground.
  - Bosses with an event on defeat (column 26): VenusBoss 24, UltimaxTank 95, SandWyrm 50, Pitcher 98, WaspKing 90,
    EverlastingKing 91, Acolyte 21, Scarlet 31, Kali 51, Cenn 85 and Pisci 86. The rematch machine runs them too.
  - So for the base attacks: a flier needs Vi, the Sandworm needs Leif, and a Random one needs whichever position it
    takes.
- **Where enemy stats are shown** (2026-09-26, code read): in a fight, the bar over a spied enemy (or with the scope
  medal) shows its live `hp`, `maxhp` and defence (`TrueDef`) (`BattleControl.cs:3148-3163`), so changed numbers show
  there as they are. The pause menu's bestiary page works out HP and defence from the raw `enemydata` row plus the
  Hard/HARDEST bonuses, not from `GetEnemyData` (`PauseMenu.cs:1993-2004`), and shows times seen and defeated.
  Attack is shown nowhere. Whether the Spy text itself names numbers is game data, not checked.
- **"Invalid Layer Index '-1'"** (2026-09-26, the log): always paired with `Animator.GotoState: State could not be
  found`. Unity's warning when a character is asked for an animation state its controller lacks (layer -1 = any).
  Harmless: nothing plays. Seen in clusters of 18-21 during the map look tests, where a boss look kept the Underling's
  map AI and was asked for its dig animations; also from a lone Leif acting other members' parts (build step 13).
  Whether vanilla shows it too: not checked (a run with the mod disabled would tell).
- **An enemy's "outgrown" level, from its EXP** (2026-09-26, the enemy table dump and `MainManager.GetEXP`, for
  enemy scaling): a fight's EXP per enemy is `base - (level - 1) * 2.5`, clamped, so each enemy stops giving EXP near
  level `base / 2.5 + 1`. Ordinary enemies climb steadily through the game: Seedling 3.8, Cordyceps Ant 4.6, Underling
  7, Pseudoscorpion 10.6, Cactus 11.4, Wasp Trooper 11, Abomihoney 13.4, Krawler 16.6, Jumping Spider 19, Zombee 21,
  Ironclad 24.6, Ruffian 26.6, Dead Landers 29.4-31 (the level cap is 27). **Not usable:** bosses (flat EXP, most
  20-25, the Wasp King 0), the Flying Seedling (2, deliberately low) and ids whose data the game swaps (IceKrawler 2).
- **Which fights can be fled:** every map fight (`NPCControl.StartBattle`, `canescape: true`). Almost every scripted
  fight can't be (`canescape: false`); the exceptions are Event30, Event42, the rematch machine (Event85), Event156,
  Event207 and Event224 (the list above).
- **EXP from a fight** (for the EXP multiplier): each enemy's share is `BattleControl.GetEXP(amount, fixedexp,
  enemy)` (`BattleControl.cs:30844`; 0 at level 27 or with flag 613; +15% on Hard Mode; +50% with medal 42), then
  clamped per enemy to `neededexp` (`:30715`), so one enemy never gives more than the rest of a level. The EXP shown
  on a map enemy is `MainManager.GetEXP` (`NPCControl.cs:5846`), a separate function. A level-up's rewards come
  from a table (`MainManager.LevelUpMessage`, `:9315`): TP or MP (`maxtp`, `maxbp`), and attack, defence or HP
  bonuses per member (`AddStatBonus`).
- **Berries picked up** (for the berry multiplier): touching a berry adds 1, 5 or 20 (`MoneySmall` / `MoneyMedium` /
  `MoneyBig`, `NPCControl.cs:5741-5753`), then clamps the wallet to 999. Berries an enemy drops after a fight are
  the same pickups (`EntityControl.spitmoney` creates them, `EntityControl.cs:5353-5361`). Items from the server
  and dialogue rewards add money elsewhere (`MainManager.cs:12583-12590`), so they aren't touched by this path.
- **Still to measure:** each scripted event's fight, one by one (safe to swap in, safe to swap out); what a map
  enemy's `battleids` hold across the EntityDump (group sizes); which enemies a one-member party can't hit.

## The round pause-menu icons' colours (2026-09-26, sampled from the SpriteDump sheet)

Every round icon (`guisprites` 30-34, 74-77) is one hue in two tones: the **ring at full saturation and brightness
0.51**, the **fill at saturation 0.34 and full brightness** (one fill measured 0.38 / 0.85, one 0.40 / 0.84), the fill's
hue about 0.01 below the ring's. Hues: red 0.99, gold 0.14, amber 0.11, orange 0.05, purple 0.75-0.77, green
0.43-0.46, blue 0.59. Sprite 31 is the game's own orange (ring 129, 40, 0; fill 255, 189, 169). Used by
`WarpButton.cs` for the Warp button's drawn backdrop.

## Visited areas and the pause-menu map (2026-09-26, code read and the mod's diagnostic)

- **An area counts as visited** when `librarystuff[4, area]` is set, which only `MainManager.UpdateArea` does, and a map
  calls it only when its area differs from the current one (`MapControl.cs:279-282`). A new file starts in area 0,
  the Outskirts, so the start is **never marked visited** until the party leaves and comes back. In vanilla the map
  item comes much later, so it never shows.
- **The map window** (6): `BuildWindow` sets it up 0.25 s later (`MapSetup`, `PauseMenu.cs:2776`) with a new
  `sprites` array (0 the cursor, `area + 1` a marker for each visited area, the rest null), **one** box, and
  `option` set to the current area only if that area is visited. Its `Update` draws the cursor toward
  `sprites[option + 1]` every frame while `option >= 0`; `-1` means none chosen yet. Opened with an `option` left over
  from another window and no marker there, it threw a NullReferenceException every frame (the diagnostic: window 6,
  option 5, markers 1-25 all null).

## EXP and berries picked up (2026-09-26, code read)

- **A battle's EXP** is summed per defeated enemy: `num = Clamp(GetEXP(exp, fixedexp, animid), 0, hologram ? 5 : neededexp)`,
  then `expreward = Clamp(expreward + num, 0, neededexp)`: one battle never gives more than a level's worth.
  `BattleControl.GetEXP(int, bool, Enemies)` (private) returns 0 at level 27 or with flag 613, adds 15% for Hard Mode
  (medal 11 or flag 614) and 50% for medal 42, returns at most 5 with flag 166 (hologram fights), the amount itself
  when `fixedexp`, else caps it at 20 (Chomper Brute, Toe Biter and enemies 87-89) or 15.
  `EndBattleWon(addexp)` adds the raw `exp` of enemies still standing, without `GetEXP`.
- **A berry picked up in the world** (`NPCControl.CheckItem`, items MoneySmall, MoneyMedium, MoneyBig) adds 1, 5 or 20,
  then calls `StartCoroutine(BerryBounce())` (its only caller) and clamps money to 0-999. Berries dropped after a fight
  are the same pickups (`EntityControl` spits them from `spitmoney`). Used by `Multipliers.cs`.
- **The volume rows' bar** (`MainManager.ShowItemList`, type 17, `settingsindex` 33, 34 and 160): ten `pip` objects from
  x 4.45, 0.4 apart, between arrows at 3.75 and 8.75; empty `guisprites[59]` at 1/4 scale, lit `guisprites[42]` at 1/3,
  yellow; sorting 10 + index. Used by `ApMenu.cs`.

## The text letter pool (2026-09-26, code read; the symptom seen by the user)

- **Every drawn letter comes from one pool of 500** `TextMesh`es (`MainManager.letterpool`, made at start-up).
  `GetEmptyLetter` hands out the first whose text is `""`, or **null when none is free**: `SetText` then skips that
  letter silently, so the text just ends early.
- **`DestroyText(parent)` frees only every other letter at once.** For each `Text` holder it steps forward through the
  holder's children and moves each freed letter back under `MainManager.instance`, which shifts the next child into the
  index it just left. The skipped letters keep their text until the holder is destroyed at the frame's end. A redraw
  in the same frame therefore needs about half the old letters plus all the new ones.
- **Seen:** the Quality of life page opened from Settings (the Settings list stays drawn behind it) with the Reset
  question's Yes / No box: the first draw was whole, and after a left / right redraw it showed "Ye" and no "No". The
  shorter Disable all question fitted. Used by `TextPool.cs`.

## The item table's fields (2026-09-26, code read)

`MainManager.itemdata` is `string[1, 256, 7]` (`MainManager.cs:3431`): 256 slots, about 188 used, so ids after the last
are free for the mod's own items. Per id, fields 0-3 come from the language file `Data/Dialogues<lang>/Items` (split on
`@`), 4-6 from `Data/ItemData`: **0 the name, 2 the description the menus and the item-get box show**
(`PauseMenu.cs:1868`, `:2274`; `NPCControl.CreateDescWindow` reads `itemdata[type, id, 2]`), 3 the article, 4 the price.
**Field 1 is not the description**: every key item holds "Desc" there, the Explorer Permit nothing. Medals keep theirs
in `badgedata[id, 1]`. Used by `ItemSwap.cs` (fixed 2026-09-26: it showed field 1) and `EntityDump.cs`.

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
