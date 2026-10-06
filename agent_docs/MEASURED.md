# Measured facts about Bug Fables

Each entry names our evidence and its date. The game can update without this repo changing, so a dated
entry is true as of that date. **Nothing here is "verified"**: that word is reserved for what the tester
confirms on screen. **Seen** (or *seen in play*) means the tester saw it on screen; everything else is a code
read, a log or a probe.

## Contents

- [The build](#the-build-2026-09-24-read-from-a-steam-install-game-not-run)
- [How the game grants
  items](#how-the-game-grants-items-2026-09-24-read-from-assembly-csharpdll-decompiled-with-ilspycmd-1011)
- [Observed in the running game](#observed-in-the-running-game-2026-09-24-grantprobe-a-new-game-played-through)
- [Save files](#save-files-2026-09-24-decompiled-inputiomanagerinputiocs)
- [Free save slots for the mod](#free-save-slots-for-the-mod-2026-09-24)
- [The main menu](#the-main-menu-2026-09-24-decompiled-startmenucs)
- [The quest board](#the-quest-board-2026-09-24)
- [Input](#input-2026-09-24)
- [Key-item grant sources, raw — SPOILERS for the whole
  game](#key-item-grant-sources-raw-2026-09-24--spoilers-for-the-whole-game)
- [World pickups and their gates](#world-pickups-and-their-gates-2026-09-24-entitydump)
- [Hard Mode boss prize medals](#hard-mode-boss-prize-medals-2026-09-24-code-read-the-missed-prize-path-seen-in-play)
- [Chapters — SPOILERS: map names](#chapters-2026-09-24-code-read-and-entitydump--spoilers-map-names)
- [What starts the gate events —
  SPOILERS](#what-starts-the-gate-events-2026-09-24-entitydump-scriptdump-with-event-lines-mapdump--spoilers)
- [Lore Books at the library](#lore-books-at-the-library-2026-09-24-seen-in-a-play-through)
- [All crystal berries](#all-crystal-berries-2026-09-24-entity-dump-and-scriptdump-matched-to-the-bug-fables-wiki)
- [Respawning pickups, seen in play](#respawning-pickups-seen-in-play-2026-09-24-with-the-dev-log)
- [The door graph](#the-door-graph-2026-09-25-dev-scriptsdoor-graphpy-on-the-entitydump)
- [Doors paired with their way
  back](#doors-paired-with-their-way-back-2026-09-25-a-new-entitydump-with-positions-door-graphpy)
- [The Forsaken Lands' fog maze, and the other one-way
  doors](#the-forsaken-lands-fog-maze-and-the-other-one-way-doors-2026-10-02-entitydump-and-code-read)
- [Transfers that aren't
  doors](#transfers-that-arent-doors-2026-09-25-scriptdumps-transfer-column-dev-scriptsevent-transferspy)
- [What the Explorer Permit
  opens](#what-the-explorer-permit-opens-2026-09-24-code-read-and-scriptdump-the-wiki-lists-four-uses)
- [All medals by
  source](#all-medals-by-source-2026-09-24-entity-dump-scriptdump-code-read-matched-to-the-bug-fables-wiki)
- [We Owe Ya!'s helpers](#we-owe-yas-helpers-2026-09-27-code-read-a-testers-report)
- [What the mod's code relies
  on](#what-the-mods-code-relies-on-code-read-2026-09-24-and-2026-09-25-moved-here-from-code-comments-2026-09-25)
- [Battles, for enemy shuffle — SPOILERS: boss
  ids](#battles-for-enemy-shuffle-2026-09-26-code-read-a-swapped-fight-seen-in-play--spoilers-boss-ids)
- [How the game draws a frame](#how-the-game-draws-a-frame-2026-09-28-measured-in-the-running-game-and-code-read)
- [The round pause-menu icons'
  colours](#the-round-pause-menu-icons-colours-2026-09-26-sampled-from-the-spritedump-sheet)
- [Visited areas and the pause-menu
  map](#visited-areas-and-the-pause-menu-map-2026-09-26-code-read-and-the-mods-diagnostic)
- [Fades, and a light's glow colour](#fades-and-a-lights-glow-colour-2026-09-26-code-read-both-seen-in-the-log)
- [Enemy-only walls](#enemy-only-walls-2026-09-26-code-read-and-the-consoles-solids-the-symptom-seen-in-play)
- [EXP and berries picked up](#exp-and-berries-picked-up-2026-09-26-code-read)
- [The text letter pool](#the-text-letter-pool-2026-09-26-code-read-the-symptom-seen-in-play)
- [The item table's fields](#the-item-tables-fields-2026-09-26-code-read)
- [Frame rate](#frame-rate-2026-09-27-code-read-the-consoles-display-on-the-test-machine)
- [Save crystals, saving, Game Over and room
  transfers](#save-crystals-saving-game-over-and-room-transfers-2026-09-28-code-read-the-crystals-range-seen-2026-09-29)
- [Upper Snakemouth's boss: Leif out until its
  beam](#upper-snakemouths-boss-leif-out-until-its-beam-2026-09-28-code-read-seen-in-play)
- [A dimmer fade-out never finishes
  early](#a-dimmer-fade-out-never-finishes-early-2026-09-28-code-read-the-delay-seen-in-play)
- [Text commands inside a substituted string, and the save's
  separators](#text-commands-inside-a-substituted-string-and-the-saves-separators-2026-09-29-code-read-not-seen-in-game)
- [Music and jingles](#music-and-jingles-2026-09-30-code-read-nothing-seen-in-game)
- [An item entity's item](#an-item-entitys-item-2026-09-30-code-read-nothing-seen-in-game)
- [Fixed numbers in the enemies'
  scripts](#fixed-numbers-in-the-enemies-scripts-2026-09-30-code-read-nothing-seen-in-game)
- [The submarine](#the-submarine-2026-09-30-code-read-the-dumps-and-the-games-text-nothing-seen-in-game)
- [Spy Specs](#spy-specs-2026-09-30-code-read-nothing-seen-in-game)
- [The Settings list's
  arrows](#the-settings-lists-arrows-2026-09-30-code-read-the-games-screen-in-the-users-screenshots)
- [Rooms seen on screen](#rooms-seen-on-screen-2026-10-05-the-user-every-ability-in-hand)
- [Still to measure](#still-to-measure)

## The build (2026-09-24, read from a Steam install, game not run)

- **Unity 2018.4.12f1**, read from the header of `Bug Fables_Data\data.unity3d`.
- **Mono, not IL2CPP:** `Bug Fables_Data\Managed\Assembly-CSharp.dll` (2,196,992 bytes) has a CLR header, a
  `MonoBleedingEdge` runtime folder exists, and there is no `GameAssembly.dll` or `global-metadata.dat`.
- **x64:** `UnityCrashHandler64.exe`.
- **The game's own `version.txt` says `1.2`.**
- **`Managed\` ships `netstandard.dll`**, so a `netstandard2.0` plugin loads: the mod targets it and has run in the
  game since 2026-09-24.
- **BepInEx 5.4.23.5 loads in this game.** Measured 2026-09-24 from `BepInEx/LogOutput.log` after the tester
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
- **Items and key items share one id space: the `MainManager.Items` enum, `None` = -1 then 0 to 186**
  (`MainManager.cs:1002`). An item is a key item because it was added to `items[1]`, not because of its id. Examples of
  names: `ExplorerPermit` 27, `FlowerKey` 54, `DesertKey` 92, `YinKey` 105, `YangKey` 106, `SandCastleBossKey` 115
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
    becomes `GetRandomMedal()`. The item name goes to `flagstring[0]` for the "You got" box (`menutext[106]`). The type
    is -1 money, 0 item, 1 key item, 2 medal (`badges`), or 3 crystal berry (`crystalbflags`). Adds
    with `items[type].Add(id)` when the type is below 2.
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
    (`items[0].Count + 1 > maxitems`); the mod puts it in storage (`items[2]`) while `items[2].Count < maxstorage`,
    the game's own storage-full test (`Checkinvqtd`, `:12503`; `Additem` itself adds with no cap), else holds it
    back.
  - money: `showmoney = 1`, `money = Clamp(money + n, 0, 999)`: the same two lines.
  - medal: `badges.Add({id, -2})`, which is the game's `AddBadge` (`:16974`); the mod calls `AddBadge`.
  - crystal berry: `flagvar[14]++` (the shop currency) **and** `crystalbflags[n] = true`. The mod does only the
    first. `crystalbflags[n]` marks berry location n as found (the pickup's presence, `NPCControl.cs:940`, `:1378`;
    a beetle grass hiding one, `:818`, `:1349`), so a received berry must not set it. **Consequence:** the game's own
    berry total, `CrystalBerryAmmount()` (`:10212`, counts `crystalbflags`), counts berry *locations checked*, not
    berries received. It is shown by the `|cberrytotal|` text command (`:12744`) and unlocks the "all 50 berries"
    logbook entry (`:4343`).
  - flags: the game's `|flag,n,v|` command is a plain `flags[n] = v` (`:12462`), and `EventControl` alone
    sets a literal flag directly 338 times.
- **Open question for location identity:** most key-item grants live in the dialogue text assets, not the
  code. A hook on the three commands catches every grant, but naming *which* location fired needs context:
  the calling NPC, the map, and the flag set in the same text. That's the next thing to measure.

## Observed in the running game (2026-09-24, GrantProbe, a new game played through)

Instrument: `mod/BugFablesAP/Dev/GrantProbe.cs`, which is read-only, logged to `BepInEx/LogOutput.log`, and
throttled to changes.

- **At the file select**, before any map (`map=none`): `flag[691]`, `flag[694]` and `flag[715]` go
  False -> True. `Event8`, the new game started from the file select, sets all three (`EventControl.cs:2599-2601`);
  `Event22`, a load, sets 694 and 715 if unset. Used by `GameSlots.cs` (691, every new file),
  `logic/bugaria_city.py`, `logic/outskirts.py`.
- **The first key item: `id=27 (ExplorerPermit)` added to `items[1]` at frame 13844**, on
  `BugariaOutskirtsOutsideCity/BugariaOutskirts`, with `inevent=True` and `message=True`. It came during an
  event's dialogue.
- **`flag[15]` went False -> True at frame 16679 on the same map**, about 47 s later, and no other flag
  changed in between. So the event that grants the item sets flag 15 as it wraps up. **A candidate location
  identity: "the event whose completion flag is 15".** Whether flag 15 belongs to this grant alone, and
  whether the Giveitem call sits in that event's dialogue text, still has to be confirmed.
- **Then `flag[31]` (frame 18896), `flag[32]` (19202) and `flag[30]` (19979)**, all on the same map. Flag 31
  is the one `NPCControl.CheckItem` sets the first time a medal is picked up (`|flag,31,true|` when
  `animid == 2`), so the "first item" seen in play was probably a medal. GrantProbe doesn't watch `badges`, so no
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
    separate moments:** `Event16`'s code appends the `giveitem` to dialogue line 12 (`EventControl.cs:3687`), and
    sets flag 15 when it ends (`:3818`).
    The local grant happened because sending checks doesn't exist yet. Log kept only in that session's
    scratchpad.
- **The first medal, captured (2026-09-24, same run):** Artis's dialogue (`caller=ShwEmArtys`) on
  `BugariaOutskirtsOutsideCity/BugariaOutskirts` ends `|giveitem,2,11,45|`: type 2 medal, id 11, then dialogue
  line 45. An NPC talk started it, not an event. Right after, `flag[31]` flipped (frame 40171), then `flag[32]`
  (40278, about 107 frames later). **`flag[30]` flipped before this talk** (frame 38697), so it isn't part of
  the medal. In the first run it came after 31 and 32, so 30 belongs to something else nearby. Flag 31 fits
  "first medal ever" (see above). **Flag 32 is Artis's medal:** confirmed 2026-09-24 by reloading a save from before him
  and talking to him again. Flag 31 flipped (frame 2279), then flag 32 (2419), the same order as both first runs, and
  the mod sent location 7720003 from flag 32.
  GrantProbe doesn't watch `badges`, so no item line appeared.
  - **`Event17` (`:3824`) is the gate the permit opens.** It sets `flags[28] = true`. Observed live: when the
    permit was shown (frame 21527), `flag[28]` flipped, **and the permit stayed in `items[1]`**. It
    is shown, not consumed. Seen on screen: a gate opened.
  - **`flag[26]` and `flag[92]`** flipped earlier on the same map, *before* the permit was shown (the tester
    confirmed it hadn't been used yet), so they belong to something else there.
- **An NPC reward, first TextProbe capture:** on `NearSnakemouth`, the reward script ended
  `|giveitem,-1,10,6|`: type -1 (money), amount 10, then dialogue line 6 (`redirect`, `temp[3]`); no entity
  argument (the entity is a 5th, `id3`, read only when present, `MainManager.cs:11484-11487`). `caller=none`, so an
  event started it. **`flag[17]` flipped right after** on the same map. Same shape as the permit: a grant plus a
  completion flag.
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
  `regionalflag[16]` flipped in the same frame (47273). The spawn check (`MainManager.CheckIfCanExist`) hides an
  object while its hiding flags or its regional flag are set, and the global flag is never wiped. **How it reads the
  lists** (corrected 2026-09-28 after an audit): a `limit` entry below -1 hides the object if that one flag (its
  absolute value) is set; the other `limit` entries hide it only if **all** are set; `requires` needs all its flags;
  and either list is ignored when its first remaining entry is -1 or below. In the entity dump no door and no pickup
  uses a negative or a second positive `limit` entry: 14 NPCs and one decoration do (2026-09-28, the dump read by
  a script). **The rule,
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
- **The treasure after the boss leaves no trace of its own** (picked up in play, 2026-09-24). Nothing was
  logged at the pickup. `SaveDiff` of the tester's save before the boss (`save2backup.dat`, 02:37) against after
  the treasure (`save2.dat`, 02:55), decoded in-game with `InputIO.Encrypt`, found:
  - line 11 (the 750 global flags): only `[41]` changed;
  - line 6 (the items, `items[0]@items[1]@items[2]`): the key items are still just `27`;
  - line 14 (regional flags): the treasure room's wipe;
  - line 10 (a 5-row true/false table): only `[4,0]`. That's `librarystuff[4, area]`, which `UpdateArea` sets
    on entering an area (`MainManager.cs:4084`).

  **So the treasure is a story moment, and flag 41 carries it.**
- **The artifacts are a count of story flags, not items** (the first one seen on screen in the pause menu and
  on the save file, 2026-09-24). `MainManager.SaveProgressIcons()` counts the set flags among
  **41, 88, 299, 345, 347, 346, 555**, one artifact each (7 in all, `StartMenu.psprite` has 7 icons). The pause
  menu draws that many (`PauseMenu.cs:2398`). The save stores the count as `LoadData.progression`
  (`MainManager.cs:17167`, field 15 of its line), and the file select draws that many icons
  (`StartMenu.cs:792`). **Having an artifact = its flag being set**: usable as checks, or as a "collect N"
  goal.
- **The save file's layout, as far as seen:** 18 lines, where line 6 is the three item lists joined by `@`,
  line 10 is `librarystuff` (5 rows), line 11 the 750 `flags`, and line 14 the 100 `regionalflags`. Other
  lines changed with ordinary play (position, stats, counters) and aren't identified yet.
- **A two-part door, no item involved:** `flag[33]` on `SnakemouthUndergroundLeftB` (frame 25491), then
  `flag[34]` on `SnakemouthUndergroundRightB` (38629), then `flag[35]` on `SnakemouthUndergrondDoor`
  (39302). **Seen on screen:** the left side done first, then the right, and the door opened. So in the
  logic that door is "both sides done", with no key item.
- **A crystal berry given by a character** (for handing over the first artifact, seen in play): on
  `AntPalace2`, `caller=none`, the script was `…|giveitem,3,5,16,-4|` (type 3 crystal berry, berry **5**, then
  line 16, over entity -4), and `crystalbflag[5]` flipped (frame 46414). **Found or given, a crystal berry's identity is
  its `crystalbflags` index, and a `giveitem,3,<n>` names that index directly**, so the full berry list can come
  from the dialogue dump plus the code.
- **The second key item: `id=41 (Map)`** added to `items[1]` on `AntPalace2` (frame 52686, during an event).
  It matches the dialogue dump's `AntPalace2` line 14 → `giveitem,1,41`, so the dump named it before it happened.
  **`flag[67]` followed (frame 54157), set at the end of `Event45`** (`EventControl.cs:7181`, flag at `:7505`):
  the Map's location is Event45 / flag 67, the same shape as the permit. `flag[68]` (frame 55406, on
  `AntPalace1`) is `Event46` (`:7511`), the next story step; on screen, it was walking out of the throne room.
  **TextProbe logged nothing for this grant**: the event seems to pass `SetText` a reference to the map's
  dialogue line, not the text, so the probe never sees the `giveitem`. The dump and GrantProbe covered it.
- **A pickup with no flag at all: the inn's item.** A Honey Drop (id 1) with `caller=Fixedtempitem` and the
  script `|additemtoss,0,var,0|`: no global flag, no regional flag. **Seen on screen:** it appears when
  the inn is paid for and used, not otherwise. So it's a repeatable reward spawned on the spot, recorded
  nowhere, and **not a location**. Three kinds of world pickup so far: a global flag (one-time, can be a
  location), a regional flag (respawns, not one), and no flag (repeatable, not one).
- **A key item lying in the world, with a global flag:** on `BugariaResidential`, `caller=badbook`, the script
  `|flag,621,true||additemtoss,1,var,0|` (type 1, key item), and `KEYITEM +1 id=174` plus `flag[621]` in the
  same frame (17532). The first grant all three probes caught together. **A clean key-item location: flag
  621.** Also `flag[43]` flipped (frame 14950): Samira's first talk, set in `Event28`, her music shop
  (`EventControl.cs:5062`).
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
- **A key item from a conversation:** `KEYITEM +1 id=25` on `BugariaTheater` (`GBugRangerPlushie`: the enum literal in
  the IL is 25, checked 2026-09-24; an earlier note called it "the doll", and `MothivaDoll` is 57) (frame
  719, `message=True`), then `flag[58]` when the dialogue was confirmed (frame 2157). The dump predicted it
  (`BugariaTheater` line 7 → `giveitem,1,25`). **Location: flag 58.**
- **A multi-step quest, measured step by step:** the quest book (id 93) was handed over with `flag[241]`, then
  delivered in `AntPalaceLibrary` with `flag[242]` and `KEYITEM -1 id=93` in the same frame (9030). The quest was
  still not done (seen in play; its id hadn't reached `boardquests[2]`). So one quest can be several locations,
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
  (`|giveitem,-1,20,-11|`, `caller=none`); the NPC spoke of 5 discoveries both times (seen in play). `Event156`'s
  `10 × (thisdecimal + 1)` payout (`EventControl.cs:26186`) was first taken for it; it belongs to a hologram minigame.
  **The payer, found (2026-09-27, code read; the event seen running in the event log 2026-09-24): `Event189`**
  (`EventControl.cs:31243`), the librarian in `AntPalaceLibrary`. The first talk only introduces it (sets `flag[579]`).
  Each later talk counts the set Discovery bools (`librarylimit[0]` = 50) and pays every milestone not yet paid:
  milestone j (1-10) at 5 × j discoveries, while `flagvar[53]` < j, then `flagvar[53]++`. So **`flagvar[53]` =
  milestones claimed (0-10)**. Rewards in order (`rewards` array; below 0 berries, 1000s crystal berry, 2000s key item,
  else medal): 20 berries, crystal berry 43, crystal berry 44, 25 berries, medal 69, 30 berries, crystal berries 45, 46,
  47, key item 83 (game names not yet read). Paid through `EventControl.GiveItem`. No such track for the Bestiary or
  Recipes: their completion only unlocks Logbook entries (below).
  **Measured:** the journal is `librarystuff`, 5 rows (`MainManager.Library`: Discovery, Bestiary, Recipes,
  Logbook, Map). Completing Discovery / Bestiary / Recipes unlocks Logbook entries 10 / 9 / 8, and nearly
  finishing the Logbook sets `flag[63]` (`MainManager.cs:4294–4371`, counts via `HowManyTrue(GetLibraryBools(n))`
  against `librarylimit[n]`). The library's dialogue also takes Lore Books (`removeitem,1,52`, line 7) and has
  crystal berry 25 (`giveitem,3,25`, line 27).
- **Turning in a Lore Book** (seen in play, `AntPalaceLibrary`): `KEYITEM -1 id=52` (frame 40180), **no global flag and
  no item script**. On screen, the book was placed on a shelf and became readable. So placed books are
  recorded outside the global flags. **Measured in code: a counter, `flagvar[15]`.** The dialogue command
  `Librarybook` (`MainManager.cs:11032`) does `flagvar[15]++` and refreshes `LibraryShelf`, which draws that many
  books left to right, 14 per row, then a second row (`LibraryShelf.cs`, `breakpoint = 14`). Only the count is
  kept, not which book, so Lore Books are interchangeable and any milestone is count-based.
- **A second crystal berry:** on `SnakemouthLake`, `crystalbflag[1]` flipped (frame 111883), with no
  tutorial flag this time. The script was `|additemtoss,3,var,0|` with `caller=tempitem`, and `flagvar[0]`
  read 1 (HoneyDrop): `CheckItem` writes the pickup's `animstate` there for every item, berries too
  (`NPCControl.cs:5645`), but `additemtoss` adds nothing for type 3. `flagvar[0]` means nothing for crystal
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
  **Seen on screen (2026-09-24):** picking it up drops the party through a trapdoor, which stays open
  afterwards, so the item can't appear again. That's consistent with a one-time location identified by the
  event's flag.
- **After the trapdoor and the third party member joining** (seen in play), `flag[14]` flipped on
  `SnakemouthDoorRoom` (frame 24876) and `flag[27]` on `SnakemouthFallRoom` (frame 38710). No item was
  involved, so joining is story flags only. **Open for the logic:** party members bring field abilities that
  gate areas, so the apworld will need abilities as requirements (fixed, or shuffled).
- **The third party member joining for good** (seen in play): `flag[29]`, `flag[16]` and `flag[24]` on
  `SnakemouthLake` (frames 117036–119390); 24 is Leif's first-battle line (set at `BattleControl.cs:2073`, read at
  `:2811`; used by `PartyMembers.cs`). The earlier `flag[27]` in `SnakemouthFallRoom` was likely the
  first meeting.
- **Loose berries (money pickups) leave no flag.** `CheckItem` takes its `ismoney` path (anim states 6, 7 and
  186), and no flag flipped when one was picked up in play. They can't be recovered from the save, which is fine:
  they're out of scope.
- **A working model for locations:** a story-event grant is a location identified by its event number and
  the flag it sets. A world pickup is identified by its object's `activationflag` or `regionalflag`. A gate
  in the logic is "has item X", when the game shows the item rather than consuming it (true for the
  permit's gate).
- **Saves live in the game folder as `save<slot>.dat`**, numbered from 0, with `save<slot>backup.dat` written
  at the same moment. The tester's slot 3 save is `save2.dat` (29,264 bytes, 01:11). These are the player's own
  files; nothing we build ever touches them directly.
- **Starting a new game did not replace `flags` or `items[1]`.** The probe re-baselines when either
  array is replaced, and it didn't. A load replaces `flags`, `regionalflags` and `crystalbflags` with new arrays
  (`MainManager.cs:17274`; below); first seen 2026-09-24 (Artis, above).

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
  handling. Everything else goes through those. Used by `SaveRedirect.cs`.
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
- **Chosen:** **`flagvar[60]`** for the received-item count, and **`flagstring[5]`** for the seed's name. Used by
  `ItemReceiver.cs`.
- **Chosen (2026-09-25):** **`flagvar[7]`** and **`[8]`** for the bits of the copies bought from Merab's and
  Shades's medal shops (a copy per bit, in location id order). Both free by the same two scans; `flagvar[69]` is the
  last one left. Used by `ShopSwap.cs`.
- **Chosen (2026-09-25):** **`flagvar[69]`**, that last one, for the crystal berries received (`CrystalBerryTotal.cs`).
  No `flagvar` slot is left free by the two scans.
- **A battle retry rolls `flagvar` back:** `BattleControl` snapshots `flags`, `flagvar`, `items[0]` and `items[1]`
  at battle start (`BattleControl.cs:614-625`); `GameOver`'s setup restores `flags` and `flagvar` (`SetFlags`,
  `:3437`), Retry the bag and the key items (`ReloadInitialData`, `:3583-3597`); medals, money and `crystalbflags`
  stay. So the mod must never give an item during a battle. Then the count and the inventory stay consistent.
- **Correction:** `flagstring` IS saved. The item swap's use of `flagstring[0]` is harmless, because the
  game writes that slot itself for every item-get.

## The main menu (2026-09-24, decompiled `StartMenu.cs`)

- **Three fixed options.** `selections = new Transform[3]` in `Intro` (`StartMenu.cs:117`). `SetMenuText`
  (`:301`) draws them from `menutext` ids `{123, 13, 124}`, one line each at `y = -0.5 - i`, then sets
  `MainManager.instance.maxoptions = selections.Length` and `menuid = 1`.
- **Choosing an option** is handled in `Update` (`:357`) under `menuid == 1`, branching on
  `MainManager.instance.option == 0 / 1 / 2`.
- **The mod adds a fourth entry:** a `SetMenuText` prefix trims `selections` to 3, a postfix enlarges it to 4,
  draws the line (our own text, not a `menutext` id) and sets `maxoptions`; `option == 3` is taken in `Update`'s
  `menuid == 1` branch. Seen on screen 2026-09-24. Used by `MenuToggle.cs`.

## The quest board (2026-09-24)

- **Flags 1 and 2 are board UI state, not quest progress.** `flag[2]` is set by `OpenQuestBoard`
  (`MainManager.cs:17827`) and cleared by `ChangeBoardQuest` when a new quest arrives (`:17953`): a "seen"
  marker. `flag[1]` isn't set in code (a dialogue script sets it, likely the board's first-time talk). Both
  flipped on `BugariaMainPlaza` (frames 58894 and 59211).
- **Quests are `MainManager.instance.boardquests`, 3 lists of quest ids** (`MainManager.cs:2219`, allocated
  `:3576`; the `BoardQuests` enum at `:535`; data from `Data/Dialogues<lang>/BoardQuests`). `ChangeBoardQuest`
  moves an id into a list (`:17945`), and taking a quest can also set a flag named in its data
  (`boardquestdata[id, 3]`, `:13906`). After the tester took
  several quests: `[0]` = 8,9,10,21,23; `[1]` = 12,1,2,4,33,49,56; `[2]` = 11,0. GrantProbe logs every
  change, so finishing one quest will show it.
- **Taking quests set a burst of flags:** 3, 64, 44, 50, 240, 479, 617 on `BugariaMainPlaza` (frames
  60502–61679; seen in play), consistent with each taken quest setting its `boardquestdata[id, 3]` flag.
  **Hypothesis, unmeasured:** `boardquests[1]` (7 ids) holds the taken quests. Those flags mark "taken", not
  "done"; finishing one will show which list completion moves an id to, and what the reward sets.
- **Quest completion measured** (a quest completed in play, 2026-09-24): at frame 9844, quest **1** moved
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
  number, `MainManager.CheckQuests`, `:4027`), or by a dialogue command (`|addquest|`, `|addboard|`, `:13714`,
  `:13840`).
- **Every board quest, dumped** (2026-09-25, `QuestDump` in the running game, joined with EntityDump and the code).
  `BoardData` column 3 is the flag taking the quest sets (none for four of the five bounties and a few others, whose
  NPCs check the quest lists instead; the fifth, 23, has 146, which no code reads); column 5 is its difficulty, 1 to 3.
  The unlock is its `QuestChecks` row (negative: a visited area; 0: never automatic, a dialogue adds it). **No accept
  flag does anything outside its own quest:** most are read only by the quest's NPCs; the code's five (131, 186, 187,
  197, 423) are the quest's own state, checked while it runs (the chefs' dish checks, `EventControl.cs:574-597`) and
  cleared when it ends. Maps are where an entity requires, hides on or talks by that flag.

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
  9 to `[5]`. A first read of `joykeys[4]/[5]` as confirm/cancel was wrong; on screen, Start acted as "done".
- **The game never reads typed text** (no `Input.inputString` anywhere); its name entry is a letter grid.
- **Action 9 (Enter; Back on a gamepad) does six things** (2026-09-29, code read; the first seen by the user, the
  rest not): in the field, the "help" (`PlayerControl.GetInput`, `PlayerControl.cs:341`: a party member talks about
  what's in front, its `tattleid`, when flag 10 is set and Kabbu is in the party); in the pause menu, it opens window
  6 (`PauseMenu.cs:383`), acts on the medal list on page 0 (`:687`), leaves the Settings list (`:895`) and toggles the
  map's icons with action 7 (`:1399`); in the start menu, on an empty save slot with two or more secrets unlocked,
  it starts a new file through menu 3 (`StartMenu.cs:631`). All through `MainManager.GetKey(9)`, so rebinding action
  9 moves all six. For the in-game text client's key (`documentation.md`, step 2).

## Key-item grant sources, raw (2026-09-24) — SPOILERS for the whole game

Two instruments, both read-only. **Code:** `giveitem,1,<id>` literals and `items[1].Add(...)` in the
decompiled `EventControl.cs`/`BattleControl.cs`, each with its enclosing `Event<N>`. **Data:** `ScriptDump`
(`mod/BugFablesAP/Dev/ScriptDump.cs`), which loads every map's dialogue table in the running game and keeps only
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

**Missed by the search above** (an audit, 2026-09-28): `EventControl.GiveItem(type, id[, playerid])` builds the
`|giveitem,…|` command at runtime, so a literal search never sees it. `grep "GiveItem(1,"` in `EventControl.cs`:
20883–21306 Event124: 151, 150, 118, 146, 29 · 31319 Event189: `Abs(2000 - rewards[j])` (83) · 35637/35643 Event207:
4, 5 · 36130 Event210: 184. Also `items[1].AddRange(...)` at 31506 and grants whose type is a variable (13123, 20667,
26249), not yet read. **A grant search needs three patterns:** the literal, `GiveItem(`, and `items[1].Add`.

**Data** (map, dialogue line: id [flags set on the same line])
AntTunnels 8: 37 · BugariaMainPlaza 82: 142 [flag 442] · BugariaCommercial 92/112/143: 110 ×3 ·
BugariaOutskirtsOutsideCity 114: 149 [flag 480], 128: 167 · BugariaTheater 7: 25 · BugariaResidential 26: 93,
31: 52, 82: 176 [flag 630] · UndergroundBar 78: 138 · AntPalace2 14: 41, 71: 109 · GoldenSettlement2 45: 55,
67/77: 56 ×2, 139: 140 [flag 444] · DefiantRoot1 24: 89 [event, flags 157 and 150], 28: 89 [flag 150] ·
DefiantRootWell 3: 111 [flag 239] · DefiantRoot3 126: 83, 163: 141 [flag 443] · GoldenSettlement3 46: 52
[flag 603] · BeehiveMainArea 48: 99 [flag 251], 54: 94 [flag 252] · BeehiveBalcony 21: 54 ·
DesertRoachVillage 1: 105 · TermiteIndustrial 31: 139, 46: 145

## World pickups and their gates (2026-09-24, EntityDump)

`EntityDump` (`mod/BugFablesAP/Dev/EntityDump.cs`) read every map's entity table in the running game, at the
same field positions as `MapControl.CreateEntities` (`MapControl.cs:1446-1640`): **4072 entities from all
246 maps, none unreadable**; names for **187 items and 91 medals** (`itemdata[0,id,0]`, `badgedata[id,0]`).
The output stays in the BepInEx folder.

- **Floor pickups** (`objecttype == Item`; `data[0]` is the kind, `animid` the id): 55 key items, 24 medals,
  43 ordinary items, 21 crystal berries.
- **One-time:** 54 of 55 key items, 23 of 24 medals and 30 ordinary items have an `activationflag`, set when
  picked up (`NPCControl.cs:5714`). Berries are tracked by `crystalbflags` instead (1 has a flag too).
- **Hiding flags (`limit`):** every one-time pickup lists its own `activationflag` there, which is how it
  stays gone once collected. **Only 5 pickups, all ordinary items, are hidden by some other flag** (each has
  no flag of its own and one other `limit` flag, 14 or 281, so that flag alone hides it): the only floor missables. No
  floor key item or medal is missable.
- **Required flags (`requires`):** only 10 pickups have any (4 key items, 1 medal, 4 items, 1 berry). Most
  pickups are gated by the map they lie on, not by a flag of their own.
- **Indoor pickups (seen on screen, 2026-09-24):** the pickup with flag 686 on
  `BugariaOutskirtsOutsideCity` is inside a building (an *inside*) that isn't open in chapter 1, next to a
  second item; seen after the dev console's `loc` put the party by it. It couldn't be picked up, since the warp
  hadn't entered the inside the way its door does. **An entity's `insideid` (field 178, `MapControl.cs:1631`)
  says which inside it's in; -1 is outdoors.** EntityDump now writes it. An indoor pickup is gated by its
  inside's door (`DoorSameMap`), not only by its map.
- **The ladybug siblings are Leby (the sister) and Dib (the lost kid at the lake)** (seen in play, 2026-09-24).
- **Crystal berries around Snakemouth** (2026-09-24, seen in play after dev warps): #2 in the underground door room is
  reachable in chapter 1 from the room's upper-left entrance with nothing, from below only with Leif (a droplet);
  #32 (`VinedItem`, bridge room) sits up on the vines at the far side and only exists after the first boss
  (requires flag 41). Seen 2026-10-06 with flag 41 set: Bee Fly (hover) over the pillars and the Beemerang on the
  vine, no Jump, from the room's right side: location 94, kept present from the start. The vine also lets go for the
  horn, the Dash and Icicle (`NPCControl.cs`, CoiledObject), but it hangs too far away for them: only the Beemerang
  reaches it (the user, seen).
  #3 (`ChucksAbode`) lies behind the house, past a big rock that only the Horn Dash breaks (seen 2026-10-05):
  location 82.
- **Houses outside the city** (`BugariaOutskirtsOutsideCity`, 2026-09-24): the ladybug siblings' house
  (`DoorLadybug`, inside 1) has no gate flags; its Mistake (flag 679) was found in play after the first boss, and
  the tester remembers it locked earlier (to check on an earlier save). The other house (`doormadeleine`, inside 2, with
  a Lore Book and Burly Tea) needs flag 390, set by dialogue on `Swamplands8` line 4; a `lockeddoor` character
  stands there until then. Seen on screen after the first boss: that house is locked, with a pop-up saying so.
- **Doors:** 567 `DoorOtherMap` entities, 59 of them with required or hiding flags. Those are the map graph
  and its story gates, for the regions.
- **Not in this dump:** the one key item and one medal without an `activationflag` still need judging.
  Items given by NPCs and events are in the `ScriptDump` and code lists above.

**Dig spots that bury a one-time item** (2026-10-05, EntityDump; `data` = 0, type, item; the flag is copied onto the
item, `NPCControl`, `DigSpot`): `OutsideSnakemouth` `Mound - Duplicate` (0 2, flag 683, location 79);
`BugariaCommercial` `MoundHidden` (1 52, 388); `BugariaOutskirtsOutsideCity` `digspotinhideout` (0 64, 487) and
`blackcherryspot` (0 121, 642); `GoldenHillsCableCar` `digmound` (0 121, 397); `GoldenPathTunnel` `digspotinhideout -
Duplicate` (1 52, 488); `GoldenHillsPath3` `diggablespot` (1 52, 380); `BugariaOutskirtsEast1` `DarkCherry - Duplicate`
(0 121, 633); `BugariaOutskirtsEast2` `TangyBerry` (0 77, 669); `DesertDREastEntrance` `digspot` (0 121, 398);
`FarGrasslandsLake` `DarkCherry` (0 121, 632); `Swamplands2` `Mound` (0 31, 737); `WaspKingdomPrison` `digspot` (0 121,
369); `AbandonedCity` `lorebookspot` (1 52, 499); `MysteryIsland` `digspot` (0 121, 653). With no flag (repeat every
visit, so no location): `Swamplands8`, `WaspKingdomDrillRoom`, `GiantLairBeforeBoss`, and `TestRoom`'s test mound.

## Hard Mode boss prize medals (2026-09-24, code read; the missed-prize path seen in play)

- **Hard Mode is on** when medal 11 is equipped (`BadgeIsEquipped(11)`, Artis's medal) or flag 614 is set
  (the new-game code, `EventControl.cs:2432-2554`).
- **Two levels, not one** (code read, 2026-09-24): nearly every check is `BadgeIsEquipped(11) || flags[614]`,
  so the medal and the HARDEST code share the Hard Mode effects. The code adds more on top, only with flag
  614: enemy HP x1.15 more and +1 defence after flag 300 (`PauseMenu.cs:2001-2002`, the enemy info shown),
  and more in `MainManager.cs:6273` and `PauseMenu.cs:1819/2452`. The one check needing both
  (`EntityControl.cs:3645`) is cosmetic, a model swap, not difficulty.
- **23 prize slots:** `prizeflags` (flagvar indices), `prizeids` (the medal) and `prizeenemyids` (the enemy),
  parallel arrays of 23 (`MainManager.cs:3401-3418`). One slot's enemy is -1 (no single enemy).
- **Beating the boss writes the slot** (`AddPrizeMedal(id)`, `MainManager.cs:3981`; 22 calls in `EventControl.cs`
  plus the dialogue command `addprize`, corrected 2026-09-28 from "23 story events"): **1** with Hard Mode on (and
  `flags[56]`, "a prize waits"), **2** without. `flagvar[55]` counts. **Not every slot is a boss prize** (an audit,
  2026-09-28): slot 1 (`flagvar[17]`, medal 24) is written 2 by `Event58` when the dialogue gift of flag 102 was
  skipped (`EventControl.cs:9531`), so the game sells it at the caravan; slot 15 has no event call (dialogue
  `addprize` only). Used by `MedalAssist.cs` (`PayPrizes` skips slot 1).
- **Value 1:** `Event33` hands every waiting prize over with `giveitem,2,<medal>` from an NPC
  (`EventControl.cs:5724-5752`), then sets the slot to **3**. **Event33 is started by talking to Artis**
  (`ShwEmArtys`, outside the city; seen in the event log, 2026-09-24).
- **Seen in play (2026-09-24):** the first boss beaten on Normal wrote its slot as missed; talking to Artis
  then gave nothing, and the caravan (open after flag 41) offered a medal: **Quick Flea, medal 5 = `prizeids[0]`**,
  which the tester bought. Confirmed: a missed prize is sold at the caravan. Event26 writes a Normal kill's slot
  directly (`flagvar[13] = 2`, `EventControl.cs:4964`), not through `AddPrizeMedal`. It is the only boss event that
  does: the other `BadgeIsEquipped(11) || flags[614]` tests in `EventControl.cs` only add a Logbook entry
  (corrected 2026-09-28 from "eight boss events test Hard Mode themselves like this"; searched: every
  `BadgeIsEquipped(11)` in `EventControl.cs`).
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
- **Leif's joining chain, played through with the event log on** (2026-09-24): Event4 on
  `SnakemouthDoorRoom` (the trapdoor, started by the map: a rock-and-pressure-plate puzzle's AND gate; flag 13) →
  Event5, started by picking up the Mushroom the trapdoor scene creates (`tempitem`, data {0,5,1}; flag 14) →
  Event6, the `SnakemouthFallRoom` trigger (the first spider fight, scripted so damage can't win it,
  `EventControl.cs:1702`; flag 27: Leif follows, not yet in the party) → Event18 on `SnakemouthLake`, a switch (flag 29)
  → Event14, the lake's `MothEvent` trigger (flag 16: Leif joins the party; then flag 24). Seen on screen as a full
  member: in the pause menu and usable in battle. Each step expects the one before: a file that skipped part of the
  chain crashes entering its middle. **One exception seen** (2026-09-26, Vi and Kabbu, a new file): a dev warp
  into `SnakemouthFallRoom` through the trapdoor's way down, with flags 13 and 14 still off, played Event6 to its end
  with no error, and Leif joined (the mod's join after flag 27). **The spider scene can be entered from the lake side
  too** (seen the same day): the web holding Leif has no collision, and walking far enough right from
  `DoorLakeRoom` starts Event6 as usual (its trigger, entity 2, sits between the lake door and the trapdoor's landing).
- **The party's basic moves** (seen in play, 2026-09-24, matching `PlayerControl.cs`): Vi (bee) throws the
  beemerang, which hits and grabs at range (flag 11, on from the start; Event109 takes it away in the bandit
  hideout and gives it back); Kabbu (beetle) uses the horn, a knock-up and melee hit that also cuts grass (always on);
  Leif (moth) freezes, droplets included (always on, once he has joined). Vi and Kabbu are in the party from a new game,
  so while the party is vanilla only Leif gates anything.
- **Where a move is needed** (seen in play, 2026-09-25, most of it playing Leif alone; the maps from the Detector's
  log):
  - **Kabbu's horn (grass):** the way down to Shades's shop; the Strong Start medal's spot in `DesertBeforeGH` (flag
    415: grass, then something to hit; whether another member's move does the hit is untested); the way to Snakemouth
    Den, both `BugariaOutskirtsSnakemouthCorridor2` (after the grass tutorial) and `OutsideSnakemouth`. On
    `OutsideSnakemouth` seven `BeetleGrass` patches (x -2 to -18.5) split the corridor side (right) from the cave side
    (left); the crystal berry (location 19, x -9.9) and the dig spot `Mound` (x -24) are on the cave side, reachable
    from the den without the horn.
  - **Kabbu's horn (puzzles):** `SnakemouthDoorRoom` from the bridge side is a chain of horn steps: cut grass to reach a
    trampoline, knock a rock down onto a vine, push two rocks onto switches, which starts Event4: it sets flag 13
    itself (`EventControl.cs:1215`) and drops the Mushroom whose pickup (`MushroomItem`, which requires flag 13)
    starts the trapdoor scene, Event5. Coming up from the trapdoor without the horn is presumably one-way for the same
    reason (the tester's reading, not tried). The bridge room's hidden-spot discovery (discovery 2, location 30) is
    behind grass too.
    **Mapped on screen (2026-10-06):** the puzzle takes Jump and the horn (grass cut, Jump onto the stone hung on the
    vine, hit it down, both stones knocked onto the plates). The bridge-room door works both ways with nothing. The big
    door (to `UpperSnekEntrance`) is scenery shut until flag 14 (`Base/Door`, `(1)`; open `(2)`, `(3)` from 14):
    arriving through it the party stood behind it until moving a little, then got through. Kept open from the start
    (the user); with it open, Event4's drop and Event5's fall still played. The hole down is shut until flag 14 too.
    The high door to `SnakemouthTop` takes the horn (grass) and Bee Fly (across) to reach; dropped down from freely.
    **`SnakemouthFallRoom` (2026-10-06, seen):** the spider scene starts from either side; its fight needs nothing
    particular, every member can hit the web. The door room's door sits on the bounce mushroom's ledge: Jump to get
    back up there, a free drop down from it.
    **`SnakemouthLake` (2026-10-06, seen):** its top holds only the door to `SnakemouthUndergrondDoor` and Leif's scene;
    up from below takes the Beemerang (a switch on a pillar, flag 29) and Jump (platforms), down from it Jump, a
    one-way once down. The medal on its pillar (location 7): the Beemerang, too far for anything else. The berry bush
    by the tablet (location 21): Jump and the horn. Three grass patches drop a random item from their list (one
    Aphid Egg among them), and any grass may drop berries: no locations.
    **`SnakemouthUndergrondDoor` (2026-10-06, seen):** the bottom (lake door, statue: nothing) is below the middle
    (the right door, the broken house's medal, location 5, the save points, the big door): up by the droplets, Jump
    and Freeze; down a drop. The pillar's Honey Drop (location 22) the same. The left (its door, both items under the
    glowing cap) is up from the middle by Jump and Freeze, a drop back. The two top doors can't be reached inside the
    room: top left drops by the left door, top right by the right door. The big door (`DoorEvent`, Event24) opens on
    flags 33 and 34, the two big switches of `UndergroundLeftB` and `UndergroundRightB` (EntityDump), and sets 35;
    behind it, the mushroom pit's door. Arriving from the pit while it's shut, the game pushes the party past it,
    every time (three tries). The broken house fades as the party comes near (`FaderRange`: its renderers switched to
    `Fade3D`, see-through); a ground item draws with "Sprites/Bumped Diffuse with Shadows" in queue 2450 (dev
    `iteminfo`, 2026-10-06), so it shows through a faded wall. Used by `ItemSwap.Looks.cs`.
    **`SnakemouthMushroomPit` (2026-10-06, seen):** from the bottom door (to `SnakemouthUndergrondDoor`) nothing is
    reached without Jump: up the bounce mushrooms (six `JumpSpring`s, EntityDump) takes it. From the top door (to
    `SnakemouthTreasureRoom`) the medal on its mushroom ledge (location 8) needs nothing, and the bottom door is a
    free way down. The item by the droplet (location 9): Jump and Freeze.
    **`SnakemouthTreasureRoom` (2026-10-06, the user):** in and out need nothing; past flag 41 an empty dead end. The
    Spider fight (`MaskEvent`, limit 41, Event26) needs Vi, the one who hits it in the air. The scene ends on
    `BOGoldenPath` (`event-transfers.py`), not yet in the logic.
    **`SnakemouthUndergroundRightA` (2026-10-06, the user):** from the left door to `UndergroundRightB`'s door, the
    same both ways: switches (an attack) and moving platforms (Jump), two small ledges (Jump), then a switch (an
    attack) and the rotating platform (Jump). No locations; the grass by the sign hides nothing (EntityDump).
    **`SnakemouthUndergroundRightB` (2026-10-06, the user):** from the bottom door the Crunchy Leaf behind the pillar
    (location 24) needs nothing; up to the top (the big switch, flag 34, and the high door) takes a switch on the
    rotating bridge (an attack), Jump, Freeze and the horn; down from the top is free. The high door has a gate the big
    switch opens: arriving through it while shut, the game pushes the party past it, so leaving that way needs the
    switch (an attack).
    **`SnakemouthUndergroundLeftA` (2026-10-06, the user):** from the right door (to `SnakemouthUndergrondDoor`) up to
    the left door (to `UndergroundLeftB`) takes Jump, Freeze and the horn; down from it a free drop. No items.
    **`SnakemouthUndergroundLeftB` (2026-10-06, seen):** from the low door (to `UndergroundLeftA`) up to the top (the big
    switch, flag 33, and the high door) takes Jump, Freeze and the horn; down is a free drop. The high door's gate
    opens on the big switch (an attack): arriving through it with flag 33 cleared, the party was pushed past the gate.
    **`SnakemouthTop` (2026-10-06, the user):** one door, to the door room's high door; in, out and the Sophie Petal
    (key item 127, gone after flag 421, EntityDump) need nothing. The petal is Doctor Isau's request on `DefiantRoot1`
    ("Have you found the Sophie Petal in Snakemouth Den?", picked from key items; dev `textsearch sophie`).
    **`UpperSnekEntrance` (2026-10-06, the user):** the bottom door (the door room's big door) and the room itself need
    nothing; the gem slot takes Jump to use. The top door (to `UpperSnekTransition`) is shut until the gem is placed
    (flag 517): arriving through it, the party was pushed past it, so leaving that way needs the gem placed.
    **`AntTunnels` (2026-10-06, the user):** nothing to cross between the palace door and the break room door; each
    tunnel entrance (EventTriggers, flags 75-80, EntityDump) only needs its flag to appear. No items.
    **`BugariaMainPlaza` (2026-10-06, the user):** its five doors are open to each other; the statue's and the inn
    portrait's discoveries and the quest board need nothing. The red house takes the Flower Key; its roof's Charge
    Up (medal 52, flag 230) takes only that, the bounce pads working without Jump. The mound (crystal berry #29,
    `DigSpot` data `1 29`) takes only Beetle Dig. Sleeping at the inn needs nothing; the Honey Drop that appears
    each time (Jump to reach) is no location.
    **`BugariaCommercial` (2026-10-06, the user):** left to right and every shop need nothing. The corner a bit above
    the plaza door is behind grass (the horn), reached freely: the NPC down to the underground bar and a dig spot are
    behind it, and the bar's door comes back up there, the dig spot free from it, the rest of the room the horn. The
    arcade (door, sign, games, helper, exchanger) is made only from flag 350, two miners there until it (EntityDump);
    its building is scenery, `Model/TermiteArcade` from 350, `Model/Base/EmptyLotFence` until it (MapDump). The
    greeter (`termiteoutside`, lines 92-93) gives 15 tokens once (`var,1,15`, `giveitem,1,110`, flag 351, discovery 42);
    the token count is `flagvar[27]` (`Showtokens`, the prize stand spends it, Event at `EventControl.cs:20662`).
    **`BugariaTheater` (2026-10-06, the user):** in and out free; the moth and the spinner need nothing to reach,
    the stage (Chubee) needs Jump. A moth sells the G-Bug Ranger Plushie for 40 berries (line 7, `giveitem,1,25`,
    flag 58). Crystal berry #13 (the one no data or literal grant placed) comes from a `MusicSpinner`, scenery on
    the right: each `BeetleHorn` hit spins it, and past its limit it spits out its item (`itemtype` 3, `flag` the
    berry), only while that berry isn't taken; the hidden-item medal (badge 2) counts it. Seen: picked up,
    `crystalbflags[13]` then true (dev `berry 13`).
    **`BugariaResidential` (2026-10-06, the user):** free between its two doors and to the cicada's house; the Bad
    Book's rooftop (location 32) takes the horn (grass) and no Jump; the fountain rooftop (location 33) takes Jump and
    Freeze. The rest (the moth house from flag 130, the quests) waits for the quest pass.
    **`UndergroundBar` (2026-10-06, the user):** everything reachable and in and out free: the bounce pad reaches the
    high door without Jump. Walking in records a discovery, in Shades' scene (Event80, the `shades event` trigger
    until flag 141).
    **`AntPalace1` (2026-10-06, the user):** nothing needed anywhere in the hall, between its five doors.
    **`AntPalace2` (2026-10-06, the user):** one door, free in and out, no items in the room; up two ledges to the
    guard and the queen takes Jump. Its two crystal berries are story rewards
    (dev `textsearch`): #5 at line 15 ("Here, you've earned this.", a chapter scene) and #12 at line 48 (the Royal
    Guard after flag 130); its discoveries (ant, bee; termite and wasp from 370) are checked.
    **`AntBridge` (2026-10-06, the user):** nothing needed across, both ways; an NPC there gives a discovery when
    talked to (discovery 11, line 1). No items; Maki's and Kina's quest NPCs stand here.
    **`AntPalaceLibrary` (2026-10-06, the user):** nothing needed for anything, the Lore Book behind the bookshelf
    (location 15) included. The librarian's turn-ins (Lore Books, crystal berry #25 at line 27; Bad Books, 35 berries
    each) and the discoveries are for the later passes.
    **`AntPalaceWarRoom` (2026-10-06, the user):** in and out and its NPCs free; the Royal Calling on the table
    (location 77) takes Jump.
    **`AntMinesBreakRoom` (2026-10-06, the user):** nothing needed in or out or inside; its dig spot (crystal berry
    #34, `DigSpot` data `1 34`) takes only Beetle Dig.
    **`BugariaPlazaAttack` (2026-10-06, the user):** nothing needed to cross between its two doors; no items. Its
    discoveries 4 and 5 (MapDump) are the statue and the inn portrait, as in every copy of the plaza (the main and
    the ending's list the same two): never missable, the main plaza always has them.
    **`BugariaBridgeAttack` (2026-10-06, the user):** nothing needed to cross; no items, no discoveries.
    **`BugariaCastleAttack` (2026-10-06, the user):** nothing needed to cross from its door to the trigger (Event120,
    which ends in `AntPalace2`); no items, no discoveries.
    **`BugariaEndPlaza`, `BugariaEndBridge`, `BugariaEndThrone` (2026-10-06, the user):** the ending played through
    from the plaza: nothing needed in the plaza or on the bridge; the throne room is the ending's scene and the credits.
    No items in any of the three (EntityDump).
  - **A full bag's toss reuses the pickup** (`MainManager.SetText`, the item list's -2 answer, read 2026-10-06): taking
    an item with a full bag and throwing a bag item out in exchange puts the thrown item into the same floor entity
    (`animstate`, `itemstate`, `basestate`), marks it `tossed` and lets it bounce out; it isn't destroyed. Seen as a
    Crunchy Leaf spot "holding" an Aphid Egg. Used by `ShopInventories.cs`.
  - **Vi's beemerang (range):** `SnakemouthBridgeRoom`'s bridge comes down when its rope is hit; from the right only the
    beemerang reaches it, from the left Leif's move hit it (so presumably any member's; Kabbu's not tried). The room's
    Tattle tutorial (Event2) ran with stand-ins and finished (flag 10); its hint (Event0) is skipped by Skip cutscenes.
    **Mapped on screen (2026-10-06):** the rope is up a ledge on each bank: Jump and the Beemerang from the right, Jump
    and any basic attack from the left (close enough there); Event1 then sets flag 7, the bridge stays down, and a hit
    from the left moves the party to the right bank. Getting onto and across the bridge takes Jump. Both bounce pads
    work without Jump, so the right bank never traps; the pillar's Mushroom (location 6) is grabbed with the Beemerang
    from there. The hidden sign (discovery 2) is behind grass on the left bank: the horn.
  - **Not needed: the spider scene's second fight** (Event6, enemies 2 and 12; seen 2026-09-27): it is won by
    beating Leif in the web (enemy 12), whom ground attacks reach; only the spider is in the air. So the fall room's
    spots (locations 29, 67) need no Beemerang. With Kabbu and Leif and no Vi, the mod gives Vi's place to Leif
    (`PartyMembers`), so Leif fights beside Kabbu against the Leif in the web: not yet played.
  - **Vi's beemerang (enemies in the air):** the lake's fight, in Leif's joining scene (Event14 on `SnakemouthLake`:
    `ChangeParty({0, 1, 2})`, flag 16, then two of enemy 1 that can't be fled, `EventControl.cs:3462-3466`); in chapter
    1 only the beemerang hits them. Whether Kabbu or Leif learn such a move later is unknown. Moot while the mod skips
    that scene (since 2026-09-25).
    **The first boss too** (seen 2026-09-26, Leif alone, OneHit off): in the spider boss fight (Event26, battle 13)
    Leif couldn't hit the enemies in the air and lost. **The spider itself goes up into the air during the fight, and
    Kabbu can't hit it there either** (seen the same day): so it counts as an air enemy even though it starts on the
    ground, and the boss needs Vi in chapter 1 (the beemerang). The Leif-alone run of 2026-09-25 got past this boss; how
    is not recorded.
  - **Any member's attack:** Snakemouth's switch-room switches (`Big Switch`, Event23, flags 33/34) take Leif's ice as
    well as the beemerang or the horn (in `SnakemouthUndergroundLeftB`).
- **A blocked walk-in ends in a teleport** (seen 2026-09-25, the game's own behaviour): entering
  `SnakemouthUndergroundRightB` the "wrong", one-way way, the gate blocked the walk-in, the party stood still for a
  moment, then was put past the gate; after that the switch could be hit and the way back used. A forced walk
  (`MoveTowards`) has a timer (250 frames for the player, 375 in a scene, `EntityControl.cs:4951`); when it runs
  out the character is moved straight to the target, with smoke (`EntityControl.cs:3692-3701`). So a door whose walk-in
  point is behind a barrier can still be entered. The logic doesn't count on it (more cautious than the game is
  allowed).
- **Ability flags, confirmed as reads in `PlayerControl.cs`:** 11 (beemerang, with `!flags[41]`), 699
  (Dash), 39 (Horn Dash, the Dash's upgrade: its hitbox breaks rocks), 171 (big icicle), 19 (hover), 18 (dig), 20
  (bubble shield). **The game's names** (its text, the console's `textsearch`, 2026-09-27): *Dash* (`Skills` 49, "Press
  twice for Kabbu to dash, letting you move faster!"; learned at `BOLostSandsEntrance`, "Kabbu can now Dash!") and
  *Horn Dash* (`Skills` 38, "a strong move which can break some objects!"; learned at `SwamplandsBridge`, "Kabbu's Dash
  is now the Horn Dash! ... a rock destroying dash!"). *Horn Slash* is the attack, apart from both (seen in play).
- **Every field ability** (2026-09-27: names and inputs from the game's `Skills` text, lines 34-42 and 49, the console's
  `textsearch`; flags from `PlayerControl.cs` and the Beemerang's `NPCControl` case; setters from `EventControl.cs`):

  | Member | Ability (the game's name) | Input | Unlocked by | Set in |
  |---|---|---|---|---|
  | Vi | Beemerang Toss | press | none of its own: allowed before flag 41, after it with flag 11 (set early) | Event0, Event1, Event109 |
  | Vi | Beemerang Halt | toss, then hold | flag 21 (checked on the thrown Beemerang) | Event55 |
  | Vi | Bee Fly | hold | flag 19 (without it, a hold tosses) | Event150 |
  | Kabbu | Horn Slash | press | none: always | |
  | Kabbu | Dash | press twice (the second within the slash's 15 frames) | flag 699 | Event22, Event137, Event221 |
  | Kabbu | Horn Dash | as the Dash | flag 39 (the Dash's hitbox then `BeetleDash`, breaking rocks) | Event131 (after the swamp bridge) |
  | Kabbu | Beetle Dig | hold | flag 18 | Event109 |
  | Leif | Freeze | press | none: always | |
  | Leif | Icicle | press twice | flag 171 | Event180 |
  | Leif | Shield | hold | flag 20 | Event95 |

  Where each scene runs: "The unlock scenes, read", below (first from the game's own text: the Dash learned
  at `BOLostSandsEntrance`, the Horn Dash at `SwamplandsBridge`). **Beemerang Halt in play** (seen 2026-09-27): holding
  the action button keeps the Beemerang in place to spin things, which is how some bridges are activated; so a spot
  behind such a mechanism needs the Halt, not just the Toss. **What each opens, for the logic** (the game's text, and
  seen in play, 2026-09-27): Shield, walking on hazardous terrain (its deflecting attacks is comfort only); Beetle Dig,
  going under some roadblocks and the dig spots; Freeze, freezing droplets and water fountains ("freeze enemies and
  liquids"; the droplet rooms already need it). The game has exactly two freezable objects, `ObjectTypes.Dropplet` and
  `ObjectTypes.Geizer` (the fountains), reacting to Freeze's hitbox (tag `Icecle`); the Geizer also to the Icicle
  (`Icefall`) (`NPCControl.cs:4779, 4829`; seen in play: droplets in Snakemouth Den, a fountain in town). Bee Fly,
  crossing large gaps (not yet unlocked in play); Icicle, platforms on water, dropped by a second tap during the Freeze
  (`PlayerControl.cs:1164`), so chained on Freeze as the Dash is on the Horn Slash. Used by `abilities.py`
  and `Abilities.cs` (build step 23).
- **The unlock scenes, read** (2026-09-27, code read with the entity, script and map dumps; nothing seen in game):
  - *Beemerang Halt*, flag 21, `Event55` (`EventControl.cs:8989`, its only setter): the end of the Wacka Worm
    minigame at the festival on `GoldenSettlement2`, only when won (`flagvar[1] >= 15`; the win also sets flag 96,
    `:8691`); the replays on `GoldenSMinigame` never set it. Chapter 2.
  - *Dash*, flag 699: the scene is `Event221` on `BOLostSandsEntrance` (trigger `dashevent`, req flags 138 and 88,
    set by `Event78` and `Event73`), so **chapter 3**, not 1. `Event137` (the swamp boss, `SwamplandsBoss`, marker flag
    359) also sets it as a fallback, and `Event22`, run on **every save load** (`StartMenu.cs:526`, `ReloadSave`), turns
    699 on when 39 or 359 is set (`EventControl.cs:4167-4185`).
  - *Shield*, flag 20, `Event95` (`:15962`): started by the switch in `FactoryProcessingFirstRoom` (Switch data `1 95`,
    `NPCControl.cs:4701-4705`; `event-triggers.py` finds it since 2026-09-28). The switch counts as hit on load once
    20 is set (`NPCControl.cs:1022-1033`). Chapter 3.
  - *Beetle Dig*, flag 18, `Event109`'s `HideoutCell` branch (`:18554`). 18 also gates the hideout's story: its
    capture scene (`HideoutEntrance/eventcheck`, lim 18), the door below and the Astotheles fight (req 18); the cell is
    left by digging. Chapter 4.
  - *Horn Dash*, flag 39, `Event131` on `SwamplandsBridge` (`:22344`; trigger `eventtrigger2`); 39 also removes that
    map's rock and the `GoldenPathTunnel` mushroom spring (lim 39). Chapter 5.
  - *Bee Fly*, flag 19, `Event150` on `BarrenLandsBeefly` (`:25049`; trigger `beeflyevent`, req 347 from `Event142`).
    Chapter 6.
  - *Icicle*, flag 171, `Event180` on `UpperSnekTransition` (`:30509`), with a miniboss (enemies 52, 53) that 171
    removes. Chapter 6.
  - None of these scenes sets another flag that marks it done, apart from `Event55` (96) and `Event137` (359): the
    ability flag is the scene's own marker.
  - **The same flags give battle skills** (`MainManager.RefreshSkills`, `MainManager.cs:8395-8577`): 21 Vi's skill 18;
    19 Vi's 5 and Kabbu's 5; 699 Kabbu's 10; 18 Kabbu's 6; 20 Leif's 7; 171 Leif's 25 (plus each field skill's menu
    entry). **In the game's IL** (an ILSpy IL dump of `Assembly-CSharp.dll`, 2026-09-27) the reads as `ldfld flags;
    ldc.i4 n; ldelem.u1`: `PlayerControl` 8 (`DashBehavior` 39; `DoActionHold` 18 x2, 19, 20; `DoActionTap`'s coroutine
    39, 171, 699), `NPCControl.Update` 2 (21), `MainManager.RefreshSkills` 15 (39 twice). Used by `Abilities.cs`.
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
  `UpperSnekTransition` have no door back. Seen on screen: a door blocked the way back into a room while its
  pickups were reachable from either side (2026-09-24). **Room-level regions need drops and scripted moves as
  connections of their own**, found from events, not doors.
- **Not in this table:** gates that aren't doors (objects only an ability passes, characters that block a
  path), and `CheckIfCanExist` on non-door entities.

## What starts the gate events (2026-09-24, EntityDump, ScriptDump with event lines, MapDump) — SPOILERS

`dev-scripts/event-triggers.py` looks in the places below (corrected 2026-09-28 after an audit: the first version
missed switches, AND gates and pressure plates; searched: every `StartEvent(` in `NPCControl.cs`). **How events
start:**
- talking to or touching an entity whose `eventid` is the event (`NPCControl.cs:4358`);
- an `EventTrigger` object, `data[0]` (`NPCControl.cs:5525`);
- a dig spot with `data[0] >= 2`, `data[1]` (`:5530`);
- a pickup chain, `data[1]` (`NPCControl.cs` ~5704-5710; `MainManager.cs:12543` is the full-bag toss path);
- a `Switch`, `StencilSwitch` or `WaterSwitch` hit while `data[0] == 1`, `data[1]` (`NPCControl.cs:4705`);
- an `ANDGate`, `data[0]` (`:1856`); a `PressurePlate`, `data[2]` (`:5438`);
- touching any entity while `entitytouchevent` is set (`:5903`; only `Event102` sets it, to 102);
- a stealth guard (`StealthAI`) spotting the party, `battleids[0]` (`:3362`): not in the script, the entity dump
  has no behaviours;
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
  (flag 20, the bubble shield): found since, a switch in `FactoryProcessingFirstRoom` (above). Rerun on
  2026-09-28 with every starter: 57 more found, and only one for a gate event (Event109, a switch hidden by flag 11,
  which is on from the start, so it never appears). No gate changed.

**Dig spots bury things** (`NPCControl.cs:5396-5420`): `data[0]` 0 = an item (kind `data[1]`, id `data[2]`,
with its own `activationflag`), 1 = a crystal berry (index `data[1]`), 2 or more = an event (`data[1]`).
Outside `TestRoom`: 14 ordinary items, 4 key items, 12 berries and 1 event; 15 buried items have a one-time
flag. **Buried items are locations the floor-pickup count missed, and every one of them needs dig.**

**Hazards (MapDump):** `WalkableSpike`, what the bubble shield crosses, is on 12 maps (e.g. `GoldenHillsPath3`,
the desert maps, `SandCastleRockRoom`, `FarGrasslands4`, `RubberPrisonSpikeRoom`). `Hole` hazards (pits) are
on many maps from the first dungeon on, so a pit doesn't mean hover.

## Lore Books at the library (2026-09-24, seen in a play-through)

- **Placing Lore Books uses them up and gives only reading**: two placed at once logged `KEYITEM -1 id=52` twice
  on `AntPalaceLibrary`, then the tester could choose which to read; no item came back (Event189 ran there first,
  started by `LibrayantDiscovery`). The count, `flagvar[15]`, is used only by the shelf's display
  (`LibraryShelf.cs:27`) and the reading list (`MainManager.cs:15372`), and by no game text (VarDump). **No count
  reward: the Lore Book is useful, not progression.**
- **A delivery quest's reward is a Lore Book**: on `BugariaResidential` a cicada ("Oh, you delivered it!") gave
  `giveitem` of item 52, then flag 243 (quest 33 done; see "Observed in the running game" above for flags 241-243).

## All crystal berries (2026-09-24, entity dump and ScriptDump, matched to the Bug Fables wiki)

**39 of the 50 are placed by data** (ground pickups: index in `data[3]`; dig spots with `data[0] = 1`: index in
`data[1]`; cut grass with `data[1] > -1`; dialogue `giveitem,3,N`). Code gives #15 (also in data), #18 and #39
(`giveitem,3` in Events 93, 105 and 71), #11 and #19 (`GiveItem(3, …)` in Events 210 and 217) and #43-47 (Event189's
discovery rewards). #13 (the theater's `MusicSpinner`, found 2026-10-06) and #38: no literal grant found by either pattern (corrected 2026-09-30 from "41 by data, 9
from code with computed values, per the wiki"). By index: #0 OutsideSnakemouth
(ground), #1 SnakemouthLake (grass), #2 SnakemouthUndergrondDoor, #3 ChucksAbode, #4 GoldenSettlement2, #5
AntPalace2 (gift, dialogue line 15), #6 BOGoldenPath (dig), #7 GoldenSettlement2 (dig), #8 GoldenHillsCableCar, #9
GoldenHillsDungeonLeftMain, #10 BugariaPier, #12 AntPalace2 (gift, line 48), #14 DesertCaravanMap (dig), #15
DefiantRoot1 and a code gift, #16 FactoryProcessingPuzzle3, #17 FactoryStorageMaze, #18 code gift, #20 HideoutRightA
(dig), #21 DesertRoachVillage (dig), #22 GoldenSettlementEntrance (dig), #23 SandCastleBasement, #24
SandCastleRockRoom, #25 AntPalaceLibrary (gift, line 27), #26 FarGrasslands2, #27 Swamplands5 (dig), #28
TermitePier, #29 BugariaMainPlaza (dig), #30 BugariaOutskirtsOutsideCity (dig), #31 FarGrasslands1 (dig), #32
SnakemouthBridgeRoom (requires flag 41), #33 TermiteRoyalChamber, #34 AntMinesBreakRoom (dig), #35 BeehiveMainArea
(gift, line 64), #36 MetalIsland1, #37 WizardTowerBasement, #39 code gift, #40 GoldenPitcher2, #41 FishingVillage (gift,
line 11), #42 UpperSnekPressurePlateRoom, #48 GiantLairFridgeInside, #49 GiantLairDeadLands1 (dig).

**Chapter 1 per the wiki, matched:** #0 (behind a bush outside the cave), #1 (cut the bush by the sign, lake room's far
left), #2 (behind the large mushroom; seen in play: upper-left entrance free, from below Leif), #5 (the Queen, for the
Ancient Mask). **Later:** #3 behind Chuck's house needs a large boulder smashed (chapter 5; the Horn Dash, to
confirm); #32 in the bridge room needs Vi's fly (hover) over two pillars and the beemerang on a vine (chapter 6), as the
tester guessed. The wiki is a lead, not proof: each entry is checked against the data or on screen before it's logic.

## Respawning pickups, seen in play (2026-09-24, with the dev log)

- **The respawn cycle works as designed:** on `SnakemouthUndergrondDoor`, the Honey Drop (regional flag 24) and
  the Mushroom (29) each showed the seed's item and sent their check on the first pickup (server confirmed
  7720022, 7720023). After a trip outside (area change to the Outskirts; the probe logged `regionalflag[29]`
  True -> False), both were back, and the Honey Drop gave a real Honey Drop with no check (`check already done:
  vanilla item`). The Crunchy Leaf on `SnakemouthUndergroundRightB` (28) showed Mushroom Gummies and sent 7720024.
- **Where they are:** the Honey Drop sits on top of a pillar, reached with Leif's ice. The Mushroom is reached with
  nothing from the room's left side, with ice from the bottom or right, like crystal berry #2. The Crunchy Leaf is
  behind a pillar, out of sight: from the room's bottom-middle entrance, walk right and behind it.
- **Many items may be hidden behind walls or pillars** (seen in play): the camera never shows them. The dev console's
  `items` lists every pickup with its position, and `nudge` moves the party by an exact amount.
- **Flag 281** (in all three pickups' hiding flags) read False in play.
- **The underground's door layout** (seen 2026-09-24), for room-level regions later: the big-door room has a
  switch room on each side (`SnakemouthUndergroundRightB` is one, with its switches and rotating bridge). Hitting
  a room's switch also opens a small gate back toward the big-door room, between the two. In
  `SnakemouthUndergroundRightB` (a screenshot from play, 2026-09-24): the switch is the pentagon crystal on a
  pedestal at the top left of the room; hitting it lowers a pillar barrier beside it, the way back to the big-door
  room. **The switches in the data** (event log and EntityDump, 2026-09-24): each switch room has a `Big Switch`
  (`data` 1 23) that starts **Event23**, which sets the switch's own `activationflag` (`EventControl.cs`, Event23:
  `flags[call.activationflag] = true`): **flag 33** in `SnakemouthUndergroundLeftB`, **flag 34** in
  `SnakemouthUndergroundRightB`. On `SnakemouthUndergrondDoor`, the `DoorEvent` trigger requires **both 33 and 34**
  and is hidden by **35**, the middle door opened (seen 2026-09-24, "Observed in the running game"). The log confirmed
  Event23 started by RightB's Big Switch when it was hit in play. Past the lowered barrier, the path leads back
  and **drops down into the big-door room** (seen in play): a one-way way back, for the room-level graph. The ledge
  above the drop can't be climbed to from the big-door room: it's reached only from the switch room, through the lowered
  barrier (seen in play). The left and right switches can be done in either order; both are needed to open the middle
  door, which leads on to the first boss. The Crunchy Leaf behind the pillar needs nothing once you're in its room (seen
  in play).

## The door graph (2026-09-25, `dev-scripts/door-graph.py` on the EntityDump)

- **567 doors between maps; 15 have no door leading back.** A door sends the party to the map in its `data[0]`
  (`NPCControl` trigger -> `MainManager.TransferMap(data[0], vectordata...)`).
- **Snakemouth Den: 31 doors, all paired except `SnakemouthEmpty`'s `WarpOut`** (to the door room).
  `SnakemouthEmpty` holds nothing but that exit and no door leads in: an unused room, left out of the graph (seen
  in play, warped there 2026-09-25; walking out led to the door room).
- **The door room's `DoorLoadZone` leads to Upper Snakemouth (`UpperSnekEntrance`) and requires only flag 41** in
  the data. Seen in play, on a file past the first boss (chapter 2 started): the party walked through into the later
  area (2026-09-25). **What really gates it** (seen 2026-09-25): in normal play the way back to the cave is
  closed after the first boss (Eetl's blocker outside the city until flag 67; from 67 a `guard` and a `sign` on
  `NearSnakemouth`, which no flag removes; seen closed after the first boss, 2026-09-25, playing Leif alone). **And past
  the door, a slot needs a key item even with the door open:** `UpperSnekEntrance`'s `slot` is a `LockedDoor`
  (hidden by 517) whose `dialogues[0].y` is 11, and Event59's key list at index 11 is **key item 116, the Peculiar
  Gem** (`SnakemouthKey`; names dump), given in code by **Event117** (`EventControl.cs:19958`, chapter 4 by the
  event-number rule). Seen on screen: the slot refused them (Event59 twice in the log). So Upper Snakemouth's locations
  need the Peculiar Gem: a key-item rule once key items are shuffled. Its other locked doors (Event59 list):
  `keycard1`/`keycard2` on `UpperSnekMiddleRoom` index 12 (item 160, the Lab Card), and the gear slots on
  `UpperSnekBeforeBoss` indices 13-15 (items 157-159; 157 is the Small Gear).
- **Paired on paper, one-way in play:** the big-door room's `WarpRightUp` <-> `SnakemouthUndergroundRightB`'s
  `DoorMainRoom`. Leaving Right B puts the party on the ledge above the big-door room; the party dropped down in play
  and can't climb back to `WarpRightUp` (see "Respawning pickups, seen in play"). The left side has the same shape
  (`WarpLeftUp` <-> `SnakemouthUndergroundLeftB`), where the Mushroom spot and crystal berry #2 are (upper left).
  So the dump gives the doors, and play decides which way each can be crossed.
- **Story-gated doors:** the door room -> fall room door (`LoadZoneFallRoom`) requires flag 41 (the first boss); in
  the story the fall room is first reached by the trapdoor (Event5), which is no door at all. The way back
  (`SnakemouthFallRoom`'s `LoadingZoneDoorRoom`) requires 41 too. **So before the first boss the trapdoor is a
  one-way drop** into the fall room, and after it the rooms are joined both ways (the tester remembers it as one-way;
  from the data, 2026-09-25). **Confirmed after the boss** (seen 2026-09-25): a green bounce mushroom leads back
  up. It's `SnakemouthFallRoom`'s `JumpShroom`, which requires 41, next to the door back (requires 41); before the
  boss the room has a `blocker` instead (Event12, hidden by 41). So the trapdoor is one-way until flag 41, two-way
  after: a connection whose direction depends on a story flag.
- **Scenery switched by flags** (2026-09-25, the map dump's new `bugfablesap-mapflags.tsv`: `ConditionChecker`
  hides or moves an object by its own `requires`/`limit`, `FlagAnimation` plays an animation by flags; 328 such
  objects in all maps). In `SnakemouthDoorRoom` the big door's closed halves (`Base/Door`, `Door (1)`) and the
  trapdoor models are hidden from **flag 14** (the trapdoor fall), and the open halves (`Door (2)`, `Door (3)`) shown
  from 14 (seen 2026-09-25: the trapdoor scene opens the trapdoor and the big door). So the door looks open from 14,
  while its load zone (`DoorLoadZone`) waits for 41. On `UpperSnekEntrance`, the round door (`Base/CircleDoor`) is
  hidden and the Gem shown in the slot from **517**. In `SnakemouthUndergrondDoor` the middle door's models switch
  at **35**; the switch rooms' `Gate`s at 33 / 34.

## Doors paired with their way back (2026-09-25, a new EntityDump with positions, `door-graph.py`)

- **A door's way back is the door the party arrives next to**: on the target map, the door leading back whose start
  position (entity fields 6-8) is nearest, in 3D, to the arrival point (`vectordata[1]`), within 10 units (12 since
  2026-10-02: `GiantLairBeforeBoss2`'s right ladder lands 11.3 below the ladder back up; 12 adds exactly that pair).
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
- **To check in play (later, like `SnakemouthEmpty`):**
  - `GoldenSettlement2`'s `Neo`, `beeguard`, `sign`, `sign - Duplicate`, `farmer ant outside`: all lead to
    `GoldenSettlement1`'s farm door. Story blockers that turn you back?
  - `TermiteIndustrial`'s `NEARloadzoneback`: doors into their own map.
  - `SandCastleBasement` <-> `SandCastleMainRoom`'s right-hand basement doors: both at height 99 (below). Can they be
    reached at all?

  Settled on 2026-10-02 (next section, and the next one): the Barren Lands `return...` zones are the fog maze's wrong
  turns, and of `GiantLairBeforeBoss2`'s ladders, the right one pairs with the ladder up while the left one is one-way.
- **Doors made only from a story flag** (2026-10-04, the EntityDump's `requires`; `door-graph.py --export` lists them in
  `doors.json`'s `gated`): 35 of the shuffled doors, absent on a new file until their flag. Arriving through one's pair
  before then lands where the door would be, over nothing. Seen at the desert border: `DesertFGBorder`'s `loadzonefg`
  (to `FarGrasslands1`) requires 348 (chapter 5); the same flag swaps its scenery, `Base/Gate` (limit 348) for
  `Base/GateBroken` (requires 348), and brings chapter 5's scene there (Maki, Kina, Yin, `makievent`, requires 348,
  limit 322); the guard `bulkbee1` and `futes - Duplicate` stand there only before it. Used by `data_tables.py`
  (`ROOM_STARTS`) and `logic/lost_sands.py`.

## The Forsaken Lands' fog maze, and the other one-way doors (2026-10-02, EntityDump and code read)

The game's name for the `BarrenLands*` maps is the Forsaken Lands (the names dump: the Squash, "native to the
Forsaken Lands"). Used by `door-graph.py` and `entrances.py`.

- **What the user sees:** "the fog maze just sends you back every now and then unless you walk the right path".
- **Each wrong turn is a `DoorOtherMap` at a room's edge, named `return...`, with no door back near where it lands.**
  There are 12 in 8 maps.
  - **Four lead into their own map**, landing at that room's own entrance: `BarrenLandsEntrance`'s right
    edge, `BarrenLandsCD`'s and `BarrenLandsCloud`'s bottom edges, `BarrenLandsRock`'s south edge.
  - **The others send you to an earlier room**, landing next to one of its doors: for example, `BarrenLandsTanks`'
    bottom edge to the top of `BarrenLandsCloud`, and `BarrenLandsSideGPT`'s two edges to `BarrenLandsCD`.
  - **The right path is ordinary doors in pairs.**
- **`BarrenLandsCD`'s left edge is two copies at one spot:**
  - `returnloadzoneleft`, hidden by flag 384, back to `BarrenLandsEntrance`'s right edge;
  - `returnloadzoneleft - Duplicate`, which needs 384, a shortcut to `BarrenLandsCloud`'s right side.
  - 384 is set when the Termite gate is first opened from outside (Event149).
- **The other one-way doors:**
  - **The pink spider's room:** in from `BarrenLandsMushrooms`. Out through `loadzonepumpkin - Duplicate`, which lands
    in `BarrenLandsPumpkins` next to its door to `BarrenLandsMushrooms`, as if you'd come that way.
  - **The underground bar's exit:** lands in `BugariaCommercial`, 21.8 from the nearest door. Its way in is the
    hatch, a transfer.
  - **The wizard's basement:** `FarGrasslandsWizard`'s `loadzonebasement`, flag 449, skips the walk (a drop). The
    basement's only door goes to `WizardTowerStairs`.
  - **`GiantLairBeforeBoss2`'s left ladder down:** lands 26 units from the one ladder up.
- **The ladder pair:** `GiantLairBeforeBoss`'s `loadzoneup` and `GiantLairBeforeBoss2`'s `loadzoneright`. Up lands 1.4
  from the right ladder; down lands 11.3 from the ladder up, the ladder's height. A pair, once the pairing reaches 12.
- **Parked load zones:** `SandCastleBasement`'s `loadzoneright` and `SandCastleMainRoom`'s `loadzonebasementright`
  sit at height 99, mirrors of the left-hand pair at 0.
  - They're the only load zones at 60 or above.
  - The game puts objects meant to stay out of reach at 99 to 9999 (AND gates, jump springs, a `dummy`).
  - Nothing in the decompiled code names either door or moves them.

## Transfers that aren't doors (2026-09-25, ScriptDump's transfer column, `dev-scripts/event-transfers.py`)

- **Dialogue lines:** 7 lines move the party with `|warp,<map>[,x,y,z]|` or `|loadmap|` (`MainManager.cs:13262-13280`):
  `BOLostSandsEntrance` 10, `DefiantRoot2` 38, `FarGrasslandsOutsideCave` 3, `Swamplands8` 4 and 7, `TermiteMainPlaza`
  65, `BarrenLandsPinkSpider` 18. None uses `|transfer|`.
- **Story events:** 87 `LoadMap` calls in 63 of `EventControl`'s event methods (and one in `ColiseumEnd`); 20-odd reload
  the current map. What starts each: `event-triggers.py` on the listed events. Among them: **Event61, the bar's hatch**
  (to `UndergroundBar`, started by `BugariaCommercial` line 32, the hatch examined); **Events 108 and 109,
  to `HideoutCell`** (108 is the garden guards catching the party, seen in play; 109 is the story's first capture, which
  takes the beemerang, flag 11, and in the cell gives dig, flag 18, `EventControl.cs` Event109; leaving the cell needs
  dig, seen in play; the cell also holds a `Dropplet` with no flags, which the tester thinks is cosmetic, not needed to
  leave: unconfirmed, so the rule is dig only); **Event153, the submarine's docks** (six; corrected 2026-09-30, it was
  written down as the boat, which is Event107: "The submarine"); **Event68**, three map pairs chosen by an `entrance`
  flag (elevators, to read); Event196, a destination from a list chosen in a menu.
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
  permit: cautious, never wrong. **The Cave of Trials, measured 2026-10-05: the permit is refused** ("the key does not
  fit", the user); its opening scene, Event156, sets the Mysterious Piece (item 109, `TrialKey`, from Neolith) into
  the altar, so that's its key. Line 12's `checkvar,0,27` is the permit's own refusal.

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
  Mode prizes (`:5747`, see above). Quest-board rewards not yet read (see "Still to measure", Quests).
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
  30, with flag 135 line 32, which starts Event61: `LoadMap(30)` with the party remade and placed, no fixed party slot
  read (`EventControl.cs:9865-9872`). Flag 135 is set when quest 6
  (UndergroundBar) is completed, in Event77 (`EventControl.cs:12698`). The tester asked for it set on a test file to
  reach the shop.
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

## We Owe Ya!'s helpers (2026-09-27, code read; a tester's report)

- **What it does:** at a battle's start, if no helper has joined already (`aiparty == null`) and medal 85 is equipped,
  the game builds a list from the flags below, picks one entry at random and adds it as a hologram ally
  (`BattleControl.cs:1202-1240`; `AddAI(id, animstate)` makes an entity with that id, `:1553`). **With none of the
  flags set the list is empty and the medal does nothing.** The game hides this: the medal joins Merab's stock only
  once one of the flags is set (`MapControl.HelperMedalCheck`, flag 716). A randomizer can hand it out before that; a
  tester received it early from another game and saw nothing happen (2026-09-27).
- **The list:** flag 514 → `AddAI(72, 5)` (set in Event177); 498 → `(47, 0)` (Event175; entity 47 also brings entity 48
  beside it, `:1242-1251`); 610 → `(46, 13)` (Event207); 135 → `(3, 0)` and `(49, 13)`, two entries (Event77); 709 →
  `(95, 13)` (Event222); 391 → `(76, 0)`; 298 and 189 together → `(20, 13)` (298 in Event111). The shop check also
  counts flag 704, which adds no entry. Where 391 and 189 are set, and which characters these entity ids are, isn't
  read yet. Not seen in game.

## What the mod's code relies on (code read 2026-09-24 and 2026-09-25; moved here from code comments 2026-09-25)

Facts the mod's hooks depend on, with their place in the decompiled source. Each names the file that uses it.

### Shops, the Quality of life page, dialogue

- A medal shop slot is an NPCControl with interacttype Shop, whose entity has animid 2 (medal) and animstate = the medal
  id, made by the shopkeeper's SetBadgeShop from avaliablebadgepool; the shopkeeper's dialogues[9].x is its badgeshops
  index, and its private field `shopitems` (EntityControl[]) holds the shelf's slot entities (NPCControl.cs:1504-1580).
  Used by `ShopSwap.cs`.
- A shop slot's description box, CreateDescWindow(shop), reads the medal's name and description from badgedata[id, 0]
  and [id, 1]; NPCControl.Interact copies the name into the buy prompt's text and the price into flagvar[1]
  (NPCControl.cs:4183-4228, 4360-4372). Used by `ShopSwap.cs`.
- UpdateShops rebuilds the shelf pool from badgeshops on every map start and after each purchase (MainManager.cs:4087,
  MapControl.cs:343, NPCControl.cs:1528); the game's own shoppool command writes badgeshops
  (MainManager.cs:11638-11657); the money command clamps to 0-999 (MainManager.cs:12580-12590). Used by `ShopSwap.cs`.
- Map entity table rows (`Data/EntityData/<map id>`) are fields split by '}': field 0 the entity type (`entitytype`,
  an `NPCType`), field 1 the object type (`objecttype`, e.g. DoorOtherMap), field 60 the data count followed by the data
  values (61 = the target map for a door), field 71 the vectordata count followed by x,y,z triples from 72
  (MapControl.cs:1473-1474, :1540-1566). Used by `QualityOfLife.Opening.cs`. A door's second vectordata triple is where
  it lands in the target map: `dev-scripts/door-graph.py` pairs each door with the door back within 12 units of it,
  which built `doors.json`. The PopTracker pack's room layout reads the same two points, the door's own position and
  that landing (its `tools/rooms.py`, 2026-10-04).
- MainManager's private static `currentdialogue` and `diagstring` (`List<string>`) are the line being shown and the
  lines so far; holding skip only works when they match (on the newest line), a box is open, no prompt/list,
  not |noskip| (the fields MainManager.cs:2749, :2747; the test :5125-5141). Used by `QualityOfLife.cs`.
- The intro slides' fades are per-frame lerps scaled by Time.smoothDeltaTime (MainManager.TieFramerate,
  MainManager.cs:9567), so Time.timeScale speeds them; the game itself uses timeScale 2.5 for cooking
  (MainManager.cs:5546), and EndEvent resets timeScale to 1 (EventControl.cs:184). Used by `QualityOfLife.cs`.
- A hold-up's Giveitem shows its follow-up line via GetDialogueText(redirect) (MainManager.cs:11592); |end| sets `end`,
  which skips the final wait for a press (MainManager.cs:11909-11910, :14171); a negative id reads commondialogue
  (MainManager.cs:10186); each slide line waits for a press at its end (MainManager.cs:14169-14174). Used
  by `QualityOfLife.Scenes.cs`.
- Event8 (new game): its first step after the slides is ChangeParty({1}, fromscratch: true, destroyoldentity: false),
  Kabbu alone (EventControl.cs:2740-2755); its end is HUD back, ResetCamera, the building's music, EndEvent
  (EventControl.cs:2857-2866), with no fade-in there: it fades in earlier (:2742, or :2780 when flagvar[0] is 0); after
  its slides it stands Kabbu 2.5 left of entity 4 (EventControl.cs:2770); the slides' backdrop NewSolidColor("back") is
  made at EventControl.cs:2655 and lives through 2656-2735. Used by `QualityOfLife.Opening.cs`.
- Event16's end points the camera at the new leader (EventControl.cs:3795); the starting house's exit only hands the
  camera back to the player for insides that centre on themselves (MapControl.cs:1373). Used
  by `QualityOfLife.Opening.cs`.
- Bridge scenes: Event0 (bridge message) is party/camera moves, three lines, flag 11, and flag 11 hides its trigger
  BridgeMessage (limit 11) (EventControl.cs:274-333); Event1 (rope) plays the bridge's Fall animation, fixes it fallen,
  then flags 7 and 11 (EventControl.cs:334-407); Event83 barkeeper's first talk sets flag 158, and its else branch
  handles bounties (EventControl.cs:13052-13080). Used by `QualityOfLife.cs`.
- Shopkeeper prompts are |prompt,map,Y,N,target1..targetN,text1..textN| (MainManager.cs:12213-12222). Used
  by `QualityOfLife.Opening.cs`.
- Holding skip: inputcooldown is 16 after a box, 10 when a new dialogue opens (MainManager.cs:5147, :10738), 4 while a
  box is typing (:5151), counted down one per frame (:7298). Used by `QualityOfLife.cs`.

### The connection, the dev console, the party, the menus

- The game's berry sprite for a money giveitem, by amount: itemsprites[0, 186] for 20 or more, [0, 6] under 5, else [0,
  7] (MainManager.cs:11506). Used by `ItemIds.cs`.
- The game's money reward code adds berries clamped to 0..999 and sets showmoney = 1 to show the counter
  (MainManager.cs:11534); the money script command does the same (MainManager.cs:12580-12590). Used
  by `DevCheats.cs`, `DevConsole.cs`.
- A pickup's touch starts in NPCControl.OnTriggerEnter, an Enter trigger: standing on an item doesn't take it again,
  stepping off and back on does (NPCControl.cs:4516). Used by `DevConsole.cs`.
- A pickup's touchcooldown is waited out by CheckItem (NPCControl.cs:5608) and counted down each frame
  (NPCControl.cs:2802); 90 holds it about 1.5 s. Used by `DevConsole.Warp.cs`.
- Every hit's damage ends in BattleControl.DoDamage(attacker, ref target, amount, property, overrides, block); the other
  overloads lead there. The game tells the party from enemies by the target's "Player" tag (BattleControl.cs:7283,
  :7295). Used by `DevConsole.cs`.
- EntityControl.Jump(float) is the normal jump: it sets the upward velocity, offgroundframes 20 and jumpcooldown 30
  (EntityControl.cs:4598-4607); the sound is PlayerControl.DoJump's (PlayerControl.cs:1555). The player's own jump fires
  on the ground, or within 3 frames of leaving it while jumpcooldown is 0 (PlayerControl.cs:372); jumpcooldown's 30
  frames outlast the whole jump (measured in the log 2026-09-24). Used by `DevConsole.cs`.
- Map entity table `Data/EntityData/<map id>`: rows split on '}', fields 6-8 the start position, field 194 the
  activationflag, as MapControl.CreateEntities reads them (MapControl.cs:1477-1640; the activationflag :1650, the start
  position :1661). Used by `DevConsole.Warp.cs`, `WarpButton.cs`.
- MainManager.TransferMap ends by walking the party to its target and waits for that walk (MainManager.cs:17610-17624):
  a target over water is never reached, the transition never ends, and the game keeps respawning the party there (seen
  at SnakemouthLake, 2026-09-24). Used by `DevConsole.Warp.cs`.
- Water raycasts as ground; water, spikes and pits carry the game's Hazards component. Used by `DevConsole.Warp.cs`.
- A map's auto-start cutscenes (MapControl.autoevent, pairs of (flag, event)) run on arrival while their flag is off
  (MapControl.cs:874-883); arriving out of story order, Event21 on SnakemouthUndergrondDoor crashed and left the game
  "in an event" (2026-09-24). Used by `DevConsole.Warp.cs`.
- Resets for what a dead cutscene leaves: MainManager.ResetCamera (MainManager.cs:7398), MapControl.RestoreLimit
  (MapControl.cs:1432), MainManager.ChangeMusic() for the map's own music (MainManager.cs:4873); Event31 left all three
  broken (2026-09-24). Used by `DevConsole.Warp.cs`.
- PlayTransition 4 ends on a black dimmer (MainManager.cs, Transition case 4 -> 0); PlayTransition 1 fades in and
  removes it. The boat scene (Event107) parents the party to the boat with LockRigid(true) and undoes both at its end
  (unparent, LockRigid(false), fade in). Used by `DevConsole.Warp.cs`.
- The trapdoor scene (Event5) turns gravity off and sets overrideanim and overrridejump for both its characters, Vi and
  Kabbu (EventControl.cs:1292-1296), then gives Vi her fall animation, 107 (:1334-1335). Used by `DevConsole.Warp.cs`.
- The game's dialogue end turns off message and the waits, and shrinks and removes the box (MainManager.cs:14185-14204).
  The private field `textbox` holds only the letters ("Text: ...", MainManager.cs:10677, :10814); the speech box is the
  Textbox prefab kept in maintextbox (MainManager.cs:10781), and an orphan "Textbox(Clone)" can stay under the GUI
  camera after dialogue ends. Used by `DevConsole.Warp.cs`.
- The found-item line (read in game, 2026-09-26, `articles`): Giveitem shows menutext[106], `You
  got |string,1| |color,1||string,0||color,0|!` (menutext[110] `You got |currency,var,0|!` for the other case,
  MainManager.cs:11528-11539, :11564); flagstring[1] is the article, menutext[125] `a` by default, else the item's
  itemdata[0, id, 3] with no trailing space (`a` for Crunchy Leaf, Mushroom, Danger Shroom, Bad Book; `the` for Explorer
  Permit and Overdue Book). So a blank article leaves two spaces unless the line's own space goes too. A world pickup's
  line is menutext[2], `You found |string,1| |color,1||string,0||color,1|!` (NPCControl.cs:5650-5693; its "!" is red),
  with the article flagstring[1] set from the item picked up. Used by `ItemSwap.cs`.
- How a line is laid out (code read 2026-09-30): Giveitem draws `|sort,1||center||halfline|` + menutext[106] with the
  three-argument SetText, which has no line width (MainManager.cs:11564, :10074), so a long line runs past the box (seen
  2026-09-30: "You got a Poison Resistance Medal from BugTester!" past both edges). A letter
  advances `GetLetterOffset(c, font, size.x)` (public, :9776), a space 0.3 x size.x, a `|command|` nothing
  (:13942-13946, :14025); at a Japanese, Russian or Korean letter the font turns 3, 4 or 5 and stays so
  (:13962-13979). `|line|` starts the next line at the left, 0.7 x size.y lower (:11738-11760); `|center|` moves the
  whole block by half its widest line, so the lines of a block start at the same x (:14027-14036). The engine's own
  shrink for a long substituted string is `|string,N,clamp,max,mult|`, which wraps it
  in `|sizemulti,mult,1|...|size,x,y|`, narrowing only (:12694-12697). The dialogue box wraps at `messagebreak` (9.75 in
  English, 10.5 otherwise, set each frame, :7236) with `OrganizeLines` (:9915-10018), which runs before `|string,N|` is
  filled in, so a name in a pickup's line is never measured (NPCControl.cs:5724). The game runs in the en-US culture
  (:2861), so a number in a command is written with a point. Used by `TextFit.cs`.
- Item entities' placement (read in game, 2026-09-26, `iteminfo` on the Caravan's shelf, Madeleine's and the Ladybugs'):
  an item sprite is pivoted at its centre and lifted by half its height (`spritetransform` y 0.5 for the
  usual 0.5-extent item sprites); shelf slots stand at one height. The pickup starburst `guisprites[85]` ("gui_85") is
  212 x 207, pivoted at its centre, extents 1.4. Used by `ItemSwap.cs`, `ItemSwap.Looks.cs`.
- Item sprites' outline (measured 2026-09-30, a script on the `SpriteDump` of `items0`, items 0-39, rows and columns at
  40, 50 and 60% of each sprite, the run of dark opaque pixels in from each edge): pure black, a median of 4 sheet
  pixels, quartiles 4 and 5, the Crunchy Leaf 6. The Crunchy Leaf's rect is 61 x 63, and the drawn Archipelago icon's
  128 pixels span its larger side, so an icon pixel is about half a sheet pixel: the icon's rim (share 0.07) is about
  2.1 sheet pixels and its lines between circles (0.05) about 1.5, half the game's. Used by `ApIcon.cs`.
- The GUI camera can be turned (read in game, 2026-09-26, `menuinfo` in `BugariaCommercial`, the shop building): at
  (-5.6, 3.0, 6.0), rotation (5, 90, 0), orthographic, near 0.3; outside it has no turn. The game's own letters carry
  the camera's rotation; an object attached with `.parent =` keeps its world rotation, so it must set `localEulerAngles`
  to zero or it's seen edge-on there. Used by `ApMenu.cs`, `WarpButton.cs`.
- The text palette (read in game, 2026-09-26, `palette`): `|color,n|` indexes MainManager.textcolors, which the scene
  sets to 10 colours, not the code's 7 (MainManager.cs:2318): 0 000000, 1 EE0B0B (item names), 2 00E700, 3 0000FF, 4
  FFFFFF, 5 A9F1FF, 6 FFD500, 7 7C7C7C, 8 00CC01, 9 FFA400. An index past the end throws IndexOutOfRange and the line
  stops there. Used by `HoldUps.cs`.
- A party member's icons, by member number (trueid 0 Vi, 1 Kabbu, 2 Leif): the pause menu's party row shows
  guisprites[94 + trueid] ("PlayerIcon", PauseMenu.cs:2486) and elsewhere guisprites[5 + trueid] (PauseMenu.cs:1906,
  :2541); each member's colour is charcolor[trueid], a Color[3] (MainManager.cs:2329). The party icon is about 2.7 times
  an item sprite's size (2.85 x 2.56 against the Crunchy Leaf); scaled down to it, seen on screen in a hold-up
  (2026-09-26). Used by `ItemSwap.Looks.cs`, `CustomItems.cs`.
- MainManager.SetPlayers(positions) places member j at newentitypos[j] (MainManager.cs:9416-9439), so a list shorter
  than the party throws IndexOutOfRange. Used by `PartyFit.cs`.
- A scene that reloads the map with recreateplayers (Event45's throne room, LoadMap) remakes the party characters, so
  references to the old ones go null. Used by `PartyFit.cs`.
- EntityControl.LateUpdate (EntityControl.cs:3672) runs after the scene's step and the entity's own updates, just before
  drawing: the place to force a renderer off. Used by `PartyFit.cs`.
- Every EntityControl.MoveTowards overload ends in MoveTowards(Vector3, float, int, int, bool)
  (EntityControl.cs:4911-4960). Used by `PartyFit.cs`.
- Scenes take the party as a list and use fixed slots p[0]..p[2] (about 110 lookups; Event83, the barkeeper's first
  talk, reads p[2], EventControl.cs:13055-13058); GetPartyEntities(true) returns the party in id order
  (MainManager.cs:9483-9503). Used by `PartyFit.cs`.
- The horn tutorial (Event10) waits while entities[0].forcemove (EventControl.cs:2935); the trapdoor's end puts member m
  at the m-th scene character's position (EventControl.cs:1476-1484; in the IL the only `ldelem Vector3` after
  the `SetPlayers` call in `<Event5>d__30.MoveNext`, read 2026-09-26); the spider fight's end (Event6,
  EventControl.cs:2272-2289) sets entities[2].following = entities[1]; the droplet scene's end (Event21,
  EventControl.cs:4112-4114) walks GetEntity(-2) and (-3) to the player. Used by `PartyFit.cs`.
- A battle's party slots and the leader (code read 2026-09-30, not measured): `playerdata` is in `ChangeParty` order,
  and `ChangeParty` sets `partyorder = ids` (MainManager.cs:3786); the field switch rotates each slot's `animid` and
  writes those member numbers into `partyorder` (PlayerControl.SwitchOrder, :1525-1532). Every battle resets
  `partypointer` (slots) to {0, 1, 2} (BattleControl.cs:772), and `MainManager.SwitchParty(battle: true)` rotates only
  the first `playerdata.Length` of them (MainManager.cs:18362-18410). The start then loops until `partypointer[0] ==
  partyorder[0]`, a slot against a member number (BattleControl.cs:1282-1289; in the IL of
  `<StartBattle>d__170.MoveNext`, the only `ldfld partyorder; ldc.i4.0; ldelem.i4`), and the dizzy first strike
  compares `playerdata[n].trueid` with `partypointer[0]` (:1336, the only `ldfld trueid` followed by `ldsfld battle;
  ldfld partypointer`). In vanilla slot k is always member k where these run. Used by `PartySlots.cs`.
- Other places a fight uses a member's number as a slot (code read 2026-09-30, not measured):
  - The eaten tick in `BattleControl.AdvanceTurnEntity(ref BattleData t, ref bool delay)` reads
    `playerdata[t.trueid].hp` twice (:3259, :3270). In the IL these are the only `ldarg.1; ldfld trueid; ldelema`; the
    method's other two `trueid` reads are `BadgeIsEquipped` arguments. Only enemy 98 (the Pitcher) eats (`Eat`, :25374).
  - `MainManager.GetPlayerData(int id, bool frombattleentity)` with `true` finds the member whose
    `battleentity.battleid == id`, and `battleid` is the slot (BattleControl.cs:1132), so a constant id there is a slot.
    `DoAction` has five: Heavy Strike's `GetPlayerData(1, true)` three times (:11545, :11566) and the Vi and Leif team
    attack's `(0, true)` and `(2, true)` (:12029-12030), each `ldc.i4.k; ldc.i4.1; call GetPlayerData(int32, bool)`.
    `GetPlayerAttack(id, …)` uses the one-argument `GetPlayerData(id)`, which finds a member by `trueid`.
  Used by `PartySlots.cs`.
- A fight's scripted lines (code read 2026-09-30, not measured): `BattleControl.CheckEvent` (:2764-2825) starts
  `EventDialogue(5)` in the spider's second fight (enemy 0 is the spider, `animid 2`; flag 27 unset; `flagvar[11] == 2`)
  on every even turn above 0; case 5 gives Kabbu a line if `playerdata[1].hp > 0` (:2101-2111). `EventDialogue`'s
  coroutine has 20 fixed slot reads (`ldfld playerdata; ldc.i4.k; ldelema BattleData`: 10 of slot 0, 7 of slot 1, 3
  of slot 2), and its parameter `id` is a field of the same name on the enumerator. An exception in it leaves
  `inevent` set, and the turn logic only runs while `!action && !inevent` (:2944). Used by `PartySlots.cs`.
- The scripted fights' fixed slots (code and IL read 2026-09-30, not measured):
  - `<DoAction>d__400.MoveNext` has 29 fixed `ldfld playerdata; ldc.i4.k; ldelema BattleData` reads (8 of slot 0, 13 of
    1, 8 of 2) and 2 by value (`ldelem`, the Beast's boost and Zommoth's unfreeze). The step's hoisted `entity` and
    `actionid` name the acting enemy (`enemydata[actionid].animid`, the enemy id).
  - The Beast's script (`case Centipede`, 69) holds its only `RevivePlayer(1, 5, false)`, BattleControl's only
    `GetSingleTarget(int)` call, followed by `playerdata[playertargetID].hp > 0`, its only `ClearStatus(ref
    playerdata[<hits>])`, and the two `StartDeath()` after a fixed read.
  - Six animation stores follow a fixed read: the Beast's slot 1 `overrideanim`/`animstate 9`, and Zommoth's (96) slot
    2 `overrideanim`/`animstate` 116 and 13.
  - The Everlasting King is 91, Zommoth 96 (`MainManager.Enemies`).
  - `Event137` reads slot 1 three times (`lockitems`, :23124, :23183, :23213); `Event182` reads slot 2 once
    (`SetCondition(EventStop, ref playerdata[2], 99999)`, :30669).
  - `SetCondition` for `EventStop` and `RemoveCondition` of a condition not held never touch the member's body
    (MainManager.cs:6527-6615, :6685-6710).
  Used by `PartySlots.cs`.
- Scenes reading a party slot's character directly (IL read 2026-09-30): `<Event52>` has 3 fixed reads of slots 1-2
  (`ldfld playerdata; ldc.i4.k; ldelema`), `<Event122>` 2, `<Event130>` 1, `<Event138>` 1 plus 4
  `playerdata[partyorder[k]]` (`ldfld partyorder; …; ldelem.i4; ldelema`). Their `.entity` is the k-th character in
  line: the field switch rotates `animid` across the slots and `RefreshEntities` gives each slot's character its
  `animid` (MainManager.cs:9083). Used by `PartySlots.cs`.
- A scripted end at 10 HP (code read 2026-09-30, not measured): with `SurviveWith10` every hit leaves the enemy at 10 HP
  or more (`BattleControl.cs:7491-7494`, except Kabbu's skill 9 used as a skill), and the Beast plays its script on
  its own turn at `hp <= 10` (:18361), the Everlasting King its phases at `hp <= 10` (:20826). An enemy's weaknesses
  are the enemy table's column 23, `N{Prop{Prop{` (MainManager.cs:6387-6398); the King's holds `SurviveWith10`, the
  Beast gets it from `Event137` after the fight starts (EventControl.cs:23137). The Beast's HP is 76 (seen in
  vanilla, `log.md`, 2026-09-28). Used by `EnemyScaling.cs`.
- MainManager.GetEntity: -2 and -3 are the second and third member by position (MainManager.cs:18526-18537), -4/-5/-6
  are Vi/Kabbu/Leif by name (MainManager.cs:18538-18570), 1000 + n reads map.tempfollowers[n]
  (MainManager.cs:18512-18515) and throws ArgumentOutOfRange when nobody is there; of about 420 callers, only a few
  null-check the result (GetPartyEntities, MainManager.cs:9498; Event1, EventControl.cs:336). Used by `PartyFit.cs`.
- The main menu's confirm sound: StartMenu.Update plays "Confirm" for every main-menu choice (menuid 1) before acting on
  it. Used by `MenuToggle.cs`.
- On the file select (menuid 2, submenu 0), confirm on file 0-2 is StartMenu.Update's load or new-game branch
  (StartMenu.cs:512-535, Event22 or Event8); the save slots' boxes sort at -20 to -60 and their text at 10
  (StartMenu.ShowSaves). Used by `MenuToggle.cs`.
- Closing the game's Settings from the title resets maxoptions to 3 (PauseMenu.cs:1811). Used by `MenuToggle.cs`.
- MainManager.Create9Box box type 1 is the game's orange box; ButtonSprite draws its label with no sort of its own, so
  the label text must carry |sort,N| to show over a box. Used by `MenuToggle.cs`.
- Pause menu window 0: maxoptions icons (4, or 2 in battle) built in BuildWindow as sprites[13 + n] with guisprites[74 +
  n] via NewUIObject (PauseMenu.cs:2378-2497, :2493-2497); window 0's sprites array is 19 long (:2404), window 1's 11,
  window 2's 20 (only 0-10 and 12-14 filled), window 3's 8, the controls page's 12 (PauseMenu.cs:2235-2678), and the
  map's (window 6) one per area plus one, a marker at area + 1 for each visited area (:2779); BuildWindow shrinks the
  old page's boxes for 0.2 s before it builds the new one, so for that long `sprites` is still the old page's
  (:2348-2368); confirm opens window option + 1 (:374-380); labels are menutext[10 + option] and [50 + option]
  (UpdateText); IconAnim gets {13, 14, 15, 16} and indexes by option (PauseMenu.cs:351); PrepareExit shrinks the boxes
  and DestroyPause follows 0.25 s later (PauseMenu.cs:1839). Used by `WarpButton.cs`.
- The game's menu cursor sprite is MainManager.cursorsprite[0], set up as at MainManager.cs:14822 (sort, layer 5,
  SpriteBounce.MessageBounce). Used by `WarpButton.cs`.
- A new game begins on the Outskirts: Event8 loads map 16 (EventControl.cs:2636). Used by `WarpButton.cs`.
- guisprites[34] is a round blue map icon in the pause-menu icon style (from SpriteDump's sheet). Used
  by `WarpButton.cs`.
- MultiClient.Net 6.7.1 net40: every send first checks websocket-sharp's IsAlive, which pings and blocks up to 5 s for
  the pong (WebSocket.ping, WaitTime); websocket-sharp's Close sends a close frame and waits up to 5 s for the answer.
  Used by `ApConnection.cs`.
- MultiClient.Net 6.7.1 keeps every location check the server hasn't confirmed and resends them with the next send:
  each `LocationChecks` packet is every checked location except `serverConfirmedChecks`, which fills from the
  server's `Connected` and `RoomUpdate` packets (`Helpers/LocationCheckHelper.cs` at tag
  v6.7.1, `GetLocationChecksPacket`; read 2026-09-25). Used by `ApConnection.cs`.
- Scouting with HintCreationPolicy.None creates no hints; a hint-creating scout would announce the seed's placements.
  Used by `ApConnection.cs`.
- ArchipelagoSocketHelper tries wss:// first for a bare address and falls back to ws://. Used by `ApConnection.cs`.
- MultiClient.Net 6.7.1 caches each game's data package
  at `<LocalApplicationData>\Archipelago\Cache\datapackage\<game>\<checksum>.json`
  (`DataPackage.FileSystemCheckSumDataPackageProvider`, an internal class). Its
  private `GetFileSystemSafeFileName(string gameName)` strips invalid characters into its parameter but returns the
  unchanged original; `TryGetDataPackage(string game, string checksum, out GameData)` builds its path with the raw
  checksum; in both, `Path.Combine` runs outside the `try`. Game and checksum are the server's: RoomInfo's `games`
  and `datapackage_checksums` for reading, the DataPackage reply's game keys and each `Checksum` for writing
  (`DataPackage.DataPackageCache`). Read in the shipped DLL, decompiled 2026-09-29. Used by `CachePaths.cs`.
- HarmonyX 2.7.0 (compiled against) and 2.9.0 (in `BepInEx/core`) both have `HarmonyPatch(string
  assemblyQualifiedDeclaringType, string methodName)`; 2.9.0 resolves it with `Type.GetType(name, true)`, which throws
  when the type is missing (`HarmonyMethod.GetDeclaringType` at tag v2.9.0; 2.7.0's constructors from its NuGet DLL;
  2026-09-29). Used by `CachePaths.cs`.
- slot_data location_shops: {location id: {shop, medal}}, one location per copy a medal shop ever stocks, done when the
  save marks that copy bought (ShopSwap). Used by `SeedData.cs` (parsed there; `ApConnection.cs` passes it on).

### Map data, doors, dumps and probes, hot reload

- Interacting with an item shop slot (`Fixedshop<n>`, an item entity: animid 0, animstate the item id) puts the price
  in `flagvar[1]` and the item's name in `flagstring[0]`, then opens the shopkeeper's buy talk; looking at a slot opens
  its description box from `itemdata[0, id, 0]` and `[.., 2]`. The `additem` command only adds to the bag list, with no
  item-get box (NPCControl.cs:4374-4378, NPCControl.cs:4220-4226, MainManager.cs:12570-12571). Used by `ItemShops.cs`.
- A map's entity table (`Data/EntityData/<map id>`, one line per entity, fields split by `}`) holds an entity's `data`
  count at field 60 (values from 61) and its `vectordata` count at field 71 (each vector three fields from 72); names
  are in `Data/EntityData/Names/<map id>names`, one per line in the same order (MapControl.CreateEntities,
  MapControl.cs:1540-1566, MapControl.cs:1454). Used by `DoorShuffle.cs`.
- A door's `data[4] == 1` means TransferMap skips the walk into the door (`vectordata[0]`): a hole or a ladder. Used
  by `DoorShuffle.cs`.
- Grass that drops an item picks one `vectordata` entry at random and drops item x of it, so a grass
  entity's `vectordata` is its item list (NPCControl.cs:5976-5983). Used by `EntityDump.cs`.
- `GlowTrigger` components are the electric triggers, which the bubble shield also blocks
  (GlowTrigger.cs:189); `Hazards` type `WalkableSpike` is what the bubble shield walks over (Hazards.cs:207). Map
  prefabs load from Resources `Prefabs/Maps/<map>` (MainManager.cs:9652); dialogue tables
  from `Data/Dialogues<lang>/Maps/<map>` (MainManager.cs:2981) . Used by `MapDump.cs`, `ScriptDump.cs`.
- Dialogue commands that move the party to another map: `transfer` and `warp` take a map id (or varN) and an optional
  position; `loadmap` reloads a map (MainManager.cs:13262-13280). Used by `ScriptDump.cs`.
- `MapControl.autoevent` holds (flag, event) pairs: the map starts the event once while the flag is off, then sets the
  flag; these are story steps no entity or dialogue starts (MapControl.cs:874-883). Used by `MapDump.cs`.
- Loading a save allocates new `flags`/`regionalflags`/`crystalbflags` arrays of the same length, so a watcher must
  compare array identity, not length, or a load reads as mass flag flips (MainManager.cs:17274). Used
  by `GrantProbe.cs`.
- ScriptEngine's FileSystemWatcher can't run in this game: its Mono throws NotImplementedException from `new
  FileSystemWatcher(path)` inside ScriptEngine.Awake (2026-09-24), so DevReload polls the DLL and sets ScriptEngine's
  private `shouldReload`, field names read from ScriptEngine.dll r11.1 with ilspycmd. Used by `DevReload.cs`.
- A hot reload during a scene, conversation or battle orphaned the stand-ins the old plugin made for a running scene
  (the spider fight, 2026-09-25), so DevReload waits for a free moment. Used by `DevReload.cs`.
- World pickups pass the item id to SetText as `var,0`: NPCControl.CheckItem puts it in `flagvar[0]` first. Used
  by `TextProbe.cs`.
- The plugin is built with a Windows ("full") pdb: ScriptEngine reads the plugin through Mono.Cecil with symbols and
  can't read a portable pdb, so the plugin would silently never load (measured in the author's other project,
  2026-08-28). Used by `BugFablesAP.csproj`.
- A dig spot (DigSpot) starts an event only when data[0] >= 2; data[0] = 0 buries an item, 1 a crystal berry
  (NPCControl.cs:5396-5420). Used by `dev-scripts/event-triggers.py`.

### The world's locations, from the data file's notes

- Event10 (the horn tutorial near Snakemouth, which sets flag 17) is started by the `Woodboring` EventTrigger
  on `NearSnakemouth` (EventControl.cs:2915); its 10 berries are `giveitem,-1,10,6` written in the event's code
  (EventControl.cs:3040) (location id 2). Used by `logic/outskirts.py`.
- Cut grass that drops an item copies its own one-time flag onto the drop (NPCControl.cs:5981-5983), so a grass drop is
  an ordinary pickup location (location id 12). Used by `logic/outskirts.py`.
- The Lore Book behind the Ant Palace library bookshelf is flag 71 (play-through log, 2026-09-24) (location id 15). Used
  by `logic/bugaria_city.py`.
- A ground crystal berry's map data holds its index in data[3], copied to data[0] at load (NPCControl.cs:938); a berry
  dropped from cut grass has the index in the grass's data[1], carried by the drop in data[0] (NPCControl.cs:5967-5972)
  (location ids 19, 21). Used by `logic/outskirts.py`, `logic/snakemouth_den.py`.
- Flag 281 (one of the three respawning Snakemouth pickups' hiding flags) is set by nothing found in the code, the map
  scripts or the entities (2026-09-24, again 2026-10-02) and read False in play, so each of the three is hidden only by
  its regional flag (24, 29, 28), which `logic/snakemouth_den.py` keys them on (location ids 22, 23, 24). Used
  by `logic/snakemouth_den.py`.
- Entities behind pickup locations: `SnakemouthUndergrondDoor` entity 6 (HoneyDrop, id 22) and entity 20 (`CrunchyLeaf -
  Duplicate`, holding a Mushroom, id 23); `SnakemouthUndergroundRightB` entity 11 (CrunchyLeaf,
  id 24); `BugariaOutskirtsEast1` entity 38 (a Drowsy Cake under a stone, flag 735, id 25); `BugariaResidential` entity
  39 (`badbook`, id 32) and entity 10 (`BugMeNot - Duplicate`, flag 59, id 33); Madeleine's house (inside 2) entity 71
  (`tea`, Burly Tea, x 36, id 44) and entity 54 (`lorebookmadeleine`, Lore Book, x 33.6, activationflag 392, id 45)
  (EntityData / entity dump). Used by `logic/snakemouth_den.py`, `logic/outskirts.py`, `logic/bugaria_city.py`.
- The pier statue's dialogue line 63 runs `|discovery,49|` (ScriptDump; `BugariaPier`'s own discovery list is 49,
  MapDump); examining it set flag 654 in the play log (2026-09-25) (location id 27). Used by `logic/outskirts.py`.
- Discovery sources: Event11 (arrival outside Snakemouth) is `OutsideSnakemouth`'s autoevent 22:11 (MapDump) and records
  discovery 0 (EventControl.cs:3095); Event6 (fall room EventTrigger, data 6, limit 27) records discovery 1 at its end
  (EventControl.cs:2293); Event13 (entity `HiddenEvent` on `SnakemouthBridgeRoom`) records discovery 2
  (EventControl.cs:3278); Event27, started by examining the old statue (entity `AncientHouseDiscovery`, object
  type BeetleGrass, interact Event, at 4:0:-3.04; corrected 2026-10-04: seen by the user, a piece of grass there
  only drops berries) on `SnakemouthUndergrondDoor`, records discovery 3 the first time, with the team's lines about
  the roaches' statue (map line 5; after, line 6 "An old statue.") (EventControl.cs:5001) (location ids 28-31). Used
  by `logic/outskirts.py`, `logic/snakemouth_den.py`.
- Event38 (the plaza statue discovery) asks for party members by name and sets no story flag (kept_present StatueDesc).
  Used by `logic/bugaria_city.py`.
- Merab's buy is her line 39, `giveitem,2,var,0`; the later stock entries are added at EventControl.cs:11960-11962
  (Event73), :16952-16954 (Event99), :20562-20563 (Event120), :24279-24281 (Event142), and We Owe Ya!
  by `MapControl.HelperMedalCheck` (flag 716, MapControl.cs:355-361). Entry 18 is the second TP Plus copy (after
  entry 2), entry 19 the second Ambusher (after entry 6) (location ids 34-43 and 46-57; 44 and 45 are Madeleine's
  house). Used by `logic/bugaria_city.py`.
- Madame Butterfly is `ButterflyShopkeeper`, entity 10 on `BugariaCommercial`; the caravan's keeper is `Crickerly2`,
  entity 33 on `BugariaOutskirtsOutsideCity`; each stock entry is a `Fixedshop<n>` slot (the keeper's data, entity dump)
  (location ids 58-65). Used by `logic/bugaria_city.py`, `logic/outskirts.py`.
- After the first boss, a ladybug girl outside the city starts the lost-kid quest (her flag 54); the kid then waits at
  the lake (story event First Boss Beaten). Used by `logic/snakemouth_den.py`.
- Event12 (the "turn back" blockers) only walks the player and sets no flags (kept_open eetlblocker1 - Duplicate, MM).
  Used by `logic/outskirts.py`, `logic/bugaria_city.py`.
- The town's arrival-scene trigger is `DoorBugaria - Duplicate` on `BugariaOutskirtsOutsideCity` (an EventTrigger
  starting Event60, hidden by 107); the real door `DoorBugaria` is a DoorOtherMap to map 9 requiring 107 (kept_open /
  kept_present). Used by `logic/outskirts.py`.
- On `BugariaOutskirtsOutsideCity`, `MiningAnt` and `MinerAntWalk` (miners at the rocks), `Crickerly1` (talk only)
  and `FuzzyMoth` all have limit 41; `LaydbugGirl` and `LaydbugBoy` require 41, with everyday lines 100 ("Dib, please
  don't do anything reckless") and 101 ("I'm not a kid anymore, Leby!"); their other lines answer to the lost-brother
  quest's flags (kept_open / kept_present). Used by `logic/outskirts.py`.
- The field attack is `PlayerControl.DoActionTap`, by the leader's `playerdata[0].animid` (0 Vi's beemerang, only
  while `!flags[41] || flags[11]`; 1 Kabbu's horn; 2 Leif's ice), started from a tap or from `DoActionHold`; the jump
  is `PlayerControl.DoJump`, called only by the jump button (PlayerControl.cs:372-392, 1008-1100, 1549); the game's
  refusal sound is `MainManager.PlayBuzzer()` ("Buzzer", used by the pause menu). Read 2026-09-27. **A Harmony prefix
  on `DoActionTap` itself never runs** (seen 2026-09-27: no refusal logged while attacks worked): it only builds the
  coroutine, and the runtime inlines it into its callers; its `MoveNext` is patched instead. `DoActionTap`
  clears `actionroutine` only at its end (PlayerControl.cs:1224), and `DoActionHold` starts Kabbu's or Leif's tap only
  while it is null (Vi's only while no beemerang is out, `:1272`). The game's items end at 186 (`MainManager.Items`), so
  200-204 are free for the mod's own. **The game's names for the three field attacks** (its `Skills` text, English, read
  with the dev `textsearch`, 2026-09-27): line 34 "Beemerang Toss" (Vi), 37 "Horn Slash" (Kabbu), 40 "Freeze" (Leif);
  the list has no entry for jumping. Used by `FieldMoves.cs`, `CustomItems.cs`, `data/items.json`.
- `SnakemouthFallRoom`'s `JumpShroom` (the bounce mushroom up to the pitfall room) requires 41
  like `LoadingZoneDoorRoom` (kept_present). Used by `logic/snakemouth_den.py`.
- `OutsideSnakemouth` (seen on screen, 2026-09-26): the arrival discovery (0) is reached from either side; the crystal
  berry (#0, location 19) and the dig spot (`Mound - Duplicate`, entity 12, hidden by flag 683) is reached from the
  cave's side, and from the Outskirts' side only by cutting the grass across the middle (entities 2-8, `BeetleGrass`).
  Both need the horn from the Outskirts' side, or the way round through the cave (the dig spot is no location yet). Used
  by `logic/outskirts.py`.
- The palace's own blockers `makiblocker1` and `makiblocker2` stay in place: the story goes on there (kept_open MM).
  Used by `logic/bugaria_city.py`.
- The Outskirts rocks' removal leaves `LoadZoneGoldenPath` still waiting for flag 41 on its own (scenery_hidden
  Base/BlockingRocks). Used by `logic/outskirts.py`.

## Battles, for enemy shuffle (2026-09-26, code read; a swapped fight seen in play) — SPOILERS: boss ids

Used by `EnemyShuffle.cs` (`StartBattle`, a map enemy's fight) and `EnemyScaling.cs` (the level an enemy outgrows,
flag 162). A map enemy's fight swapped through `StartBattle` was seen on 2026-09-26 (`apimplementation.md`, build
step 14).

- **One entry point:** every fight goes through `BattleControl.StartBattle(int[] enemyids, int stageid, int adv,
  string music, NPCControl calledfrom, bool canescape)` (`BattleControl.cs:718`). A map enemy passes itself as
  `calledfrom` with its `battleids` (`NPCControl.cs:5947`, `canescape: true`). A story fight passes `calledfrom:
  null` with a literal id array from its event. The game's own `EnemyCheck` (`:703-716`) runs for every fight but a
  respawning map enemy's (`calledfrom == null || calledfrom.eventid <= 0`, `:743`), story fights included: outside an
  event it swaps a few ids for 32 at random, and any fight with 50 or 99 becomes `{50, 99}`. Then `StartData`
  snapshots the ids for a retry.
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
  (hologram mode), which changes the look, EXP and fleeing money (`BattleControl.cs:974`, `:6487`, `:30333`,
  `:30411`; `MainManager.cs:6294`) and skips some bosses' story parts: Zommoth's scripted moves, revives and Leif's
  rejoin (`:23321-23563`), the Everlasting King's lines and revives at a phase (`:20828`, `:20857`), and the Wasp King's
  exit from the fight at its defeat, a win instead (`EventDialogue` 17, `:2618-2632`).
- **Events that reach into the running fight** (a swap there needs care):
  - Event137 (id 69) adds `SurviveWith10` to `enemydata[0]` (`EventControl.cs:23137`). Only an enemy's own script
    removes it, each from itself: the Beast's (`BattleControl.cs:18361-18470`) and the Everlasting King's at its last
    phase (`:20899`). So another enemy in that slot could never die. **Not swappable.**
  - Event182 (id 96) freezes `playerdata[2]` with `EventStop` and calls `SetLastTurns()` (`:30665-30671`), a
    scripted fight. **Not swappable until read.**
  - Event40 (three fights) and Event224 reach into the stage (`battlemap.transform.GetChild(...)`, `:6313`,
    `:37599`); keeping the event's own stage keeps that safe.
  - Event3, the cooking scene, starts one fight (two Abomihoneys, `{48, 48}`, `:893`) and puts a copy of the cook
    into its stage (`:899-902`); Event6 (the spider tutorial's two fights, `:1702`, `:1935`) sets `disablespy` in
    both and `tempdata` in the second.
  - Event173 calls `StartData({23, 51}, ...)` itself (`:29004`, `:29251`), overwriting the retry snapshot.
  - Events that test `battleresult` (a scripted loss, a retry, a prize): 30, 40, 85 (the rematch machine), 90, 156,
    163, 192, 207, 210, 224.
- **Code tied to an enemy's id** (from the survey, not each read): `eventondeath` (column 26) sends a defeat into
  `EventDialogue` (`BattleControl.cs:1972`, `:30719-30731`); setup by id at `:976+` (VenusBoss's extra entity,
  fixed positions for BeeBoss, SandWyrmTail, Pitcher); `GetEnemyData` swaps some ids' data (`MainManager.cs:
  6157-6191`: when it makes their entity, the fire and ice variants 105-109 read row 57, 61 or 58; each one's column 25
  points at the same row, so `animid` ends as the row read, `bugfablesap-enemies.tsv` of 2026-09-27, read 2026-10-02;
  used by `EnemyScaling.cs`); `NPCControl.StartBattle` forces "Battle3" music for ids 25-28 (`NPCControl.cs:5932`).
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
  - **Kabbu flips** (2026-09-27, code read; the tester's rule of thumb the same day: Leif hits the burrowed, Kabbu the
    ones to flip over, Vi the ones in the air). Kabbu's base attack carries `AttackProperty.Flip`
    (`BattleControl.cs:11551`, `:11570`; Heavy Strike's hits `:11545`, `:11566`). "Flip" is the tester's word for it:
    Kabbu's attack knocks the enemy over, and then its defence is reduced (in the code, a flipped enemy's defence is
    0: `TrueDef`, `:3131`). Which enemies it works on: those with `Flip` among their weaknesses (enemy data column
    23, `{`-separated after a count; EntityDump's `weakness` column, run 2026-09-27 on the test machine): only **five**,
    all Ground, all defence 2: Cactiling 4, Inichas 8, Acornling 16, Wasp Bomber 26 (also `ToppleFirst`) and
    Madesphy 68. The logic expects Kabbu for them even where the others could get through the defence
    (2026-09-27; `room-logic.md`, question 8). The code also has `ToppleFirst` (Wasp Bomber, Heavy Drone
    B-33 46), `ToppleAirOnly` (Venus' Guardian 24) and `FlyOnFlip` (The Everlasting King 91, TANGYBUG 110); what they
    change in play is not seen.
- **Where enemy stats are shown** (2026-09-26, code read): in a fight, the bar over a spied enemy (or with the scope
  medal) shows its live `hp`, `maxhp` and defence (`TrueDef`) (`BattleControl.cs:3148-3163`), so changed numbers show
  there as they are. The pause menu's bestiary page works out HP and defence from the raw `enemydata` row plus the
  Hard/HARDEST bonuses, not from `GetEnemyData` (`PauseMenu.cs:1993-2004`), and shows times seen and defeated.
  Attack is shown nowhere. Whether the Spy text itself names numbers is game data, not checked.
- **"Invalid Layer Index '-1'"** (2026-09-26, the log): always paired with `Animator.GotoState: State could not be
  found`. Unity's warning when a character is asked for an animation state its controller lacks (layer -1 = any).
  Harmless: nothing plays. Seen in clusters of 18-21 during the map look tests, where a boss look kept the Underling's
  map AI and was asked for its dig animations; also from a lone Leif acting other members' parts (build step 13).
  The game's own scenes show it too (2026-09-28, the log): Upper Snakemouth's boss scene (`Event182`) on a normal save,
  Archipelago off so `AnimGuard` off (no `[anim]` skips logged), two pairs each time the scene started; the mod animates
  nothing there. So vanilla warns as well. Again in the Barren Lands (2026-09-29, the log): a normal save with *Use on
  normal saves* on, the guard then off with Archipelago; about 70 pairs between Barren Lands CD, the Abandoned City
  and the rock. The guard now also runs with that row on; its `[anim]` lines will name the source.
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
- **The game's own per-hit bonuses for the party** (for the attack boost, code read 2026-09-28):
  `BattleControl.CalculateBaseDamage(attacker, ref target, basevalue, ...)` runs for each hit. A Raw hit returns
  first, unchanged. Then, when the attacker is a party member (tag `Player`) and not in the demo battle (`demomode`),
  it adds +1 for the member in front (`partypointer[0] == currentturn`), medal 6's count while poisoned and medal 3's
  count at 4 HP or less. Skills pass their damage in built from `playerdata[].atk`, most per hit (`atk + combo - 1`
  on each hit of one), so +1 attack and +1 per hit come to the same. **The medals screen's stats** (code read,
  2026-09-28): `PauseMenu.UpdateDynamicText`, run every frame from `PauseMenu.Update`, rewrites window 2's
  `dynamictext` (0 HP, 1 attack, 2 defence of `playerdata[option]`, 3 TP). Used by `AttackBoost.cs`.
- **Still to measure:** each scripted event's fight, one by one (safe to swap in, safe to swap out); what a map
  enemy's `battleids` hold across the EntityDump (group sizes); which enemies a one-member party can't hit.

## How the game draws a frame (2026-09-28, measured in the running game and code read)

- **Cameras** (the console's `cams`, on the test machine, 2026-09-28): `Main Camera` (the world; the game's `FXAA`
  component on it), `3DGUI` (a child of it, layer mask 32768, clears depth only) and `GUICamera` (the HUD, mask 32).
  All three forward rendering, MSAA allowed; the world camera has HDR on. `QualitySettings.antiAliasing` was 0: the
  game never sets it.
- **The game's render scale** (`MainManager.SetRenderTexture(index)`, `downsamples` 1, 0.9, 0.8, 0.75, 0.6, 0.5, 0.4):
  0 draws straight to the screen and hides `GUICamera`'s first child (a quad). Any other draws the world and 3DGUI
  cameras into a `RenderTexture` of 1920x1080 times the factor (whatever the screen's size), the world camera's `rect`
  shrunk to the factor, shown on that quad (bilinear). **The quad's shader is `Custom/CRT`** (the console's `cams`,
  2026-09-28): a CRT-TV look (curved, inset picture, vertical colour stripes, darker), the minigames' look; seen
  on screen at 200% when the mod first drew through that quad. Called with 0 on start and after a minigame, with 2 by
  some minigames. Settings save `downsample` and FXAA.
- **Screen positions are viewport-relative:** every conversion the game makes is `WorldToViewportPoint` (0 to 1),
  none in pixels, so a larger render texture moves nothing.
- **Resolutions:** the game's list runs 1024x576 to 3840x2160; fullscreen is a bool passed to `Screen.SetResolution`.
  The test machine on 2026-09-28: a 1920x1080 window on a 3840x2160, 240 Hz screen. Unity 2018.4.12.
- **Textures at full size** (the test machine, 2026-09-28): the game's low-texture setting off (`lowtexture` False,
  `QualitySettings.masterTextureLimit` 0), anisotropic filtering `Enable` (per texture).
- Measured for the render-scale and MSAA rows, since removed (mod guide, step 28).

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
- Used by `WarpButton.cs` (map travel).
- **Where each area's marker sits** (2026-10-03/04, code read and the user's screenshot): `MapSetup` places it at a
  fixed `(x, z)` on the map object for each `MainManager.Areas` value (`PauseMenu.cs:2786-2864`; e.g. the Outskirts
  `(0.92, 2.11)`, the Honey Factory `(5.32, -2.73)`). On screen **+x is to the left and +z down**, one scale for both:
  fitted to the user's screenshot of a save with 22 areas visited, every marker within a few pixels of
  `(999 - 112.4 x, 504 + 112 z)` at 2000 px wide. `SetMapLines` (`PauseMenu.cs:2913-3062`) draws 23 lines, each
  between two visited areas; none goes to the Fishing Village. Each map's area is its prefab's `MapControl.areaid`,
  a field set in the game's data, not in code (`MapControl.cs:62`), written by the mod's `MapDump` for all 246 maps
  (2026-09-26). Area names are `Data/Dialogues<language>/AreaNames` (`MainManager.cs:3399`), not read yet. Used by
  the PopTracker pack's world map (its `data/area_pips.json`, `data/map_areas.json`).

## Fades, and a light's glow colour (2026-09-26, code read; both seen in the log)

- **`MainManager.PlayTransition(id, ...)`** stops the running transition coroutine and, for ids 0, 2, 4 and 5 (a fade
  out), destroys every `transitionobj` before starting its own. `TransferMap` fades out, then waits on
  `transitionobj[0]`'s `SpriteRenderer` alpha; another fade started meanwhile leaves it reading a destroyed sprite
  (`NullReferenceException` in `SpriteRenderer.get_color`, seen 2026-09-26). Used by `QualityOfLife.cs`.
- **`MainManager.SetVariables`** runs as the game boots and whenever the title screen starts (`StartMenu`), and after
  a language pick: every file begins after it. Used by `QualityOfLife.cs`.
- **`GlowTrigger`** reads `material.GetColor("_Emission")` in `Start` (once, with `getactivecolorfromstart`) and twice
  in `LateUpdate`, and writes it back with `SetColor`. A material without the property makes Unity log "Material
  doesn't have a color property '_Emission'" (seen on arriving at Rubber Prison's cell block). Used by `GlowGuard.cs`.

## Enemy-only walls (2026-09-26, code read and the console's `solids`; the symptom seen in play)

- **Maps have walls only enemies bump into**: colliders tagged `EntityOnly`. `MapControl.SetPlayerColliders` (private,
  run by `Invoke("SetPlayerColliders", 0.2f)` as a map loads) gathers them into `map.entityonly` and calls
  `EntityControl.IgnoreColliders(member, wall, true)` for each party member's entity and each temporary follower:
  `Physics.IgnoreCollision` pairs, so they belong to those exact colliders.
- **A party changed after the map loaded meets them as walls**: `ChangeParty(..., destroyoldentity: true)` makes new
  characters with no ignore pairs. Seen on Outskirts East (map 55) after the console's `addmember 1`: `55/Cube (2)`, a
  bare `BoxCollider` (layer 13, 1 x 19.8 x 19.9 at x 33.4) across the map, blocked the way left and up with either
  leader; in play the party stood on top of it at height 15.5. Loading any map clears it. Used by `PartyMembers.cs`.

## EXP and berries picked up (2026-09-26, code read)

- **A battle's EXP** is summed per defeated enemy: `num = Clamp(GetEXP(exp, fixedexp, animid), 0, hologram ? 5 :
  neededexp)`, then `expreward = Clamp(expreward + num, 0, neededexp)`: one battle never gives more than a level's
  worth. `BattleControl.GetEXP(int, bool, Enemies)` (private) returns 0 at level 27 or with flag 613, adds 15% for Hard
  Mode (medal 11 or flag 614) and 50% for medal 42, returns at most 5 with flag 166 (the rematch machine's hard option,
  set inside a flag-162 hologram fight, `EventControl.cs:13702-13707`; corrected 2026-09-28 from "hologram fights"), the
  amount itself when `fixedexp`, else caps it at 20 (Chomper Brute, Toe Biter and enemies 87-89)
  or 15. `EndBattleWon(addexp)` adds the raw `exp` of enemies still standing, without `GetEXP`.
- **A berry picked up in the world** (`NPCControl.CheckItem`, items MoneySmall, MoneyMedium, MoneyBig) adds 1, 5 or 20,
  then calls `StartCoroutine(BerryBounce())` (its only caller) and clamps money to 0-999. Berries dropped after a fight
  are the same pickups (`EntityControl` spits them from `spitmoney`). A Harmony prefix on `BerryBounce()` itself never
  ran in game (2026-09-26, a 10x test in play): the stub is inlined. Its iterator class is `<BerryBounce>d__172`, with
  the compiler's standard `<>1__state` and `<>4__this` fields (read from the game's DLL). Used by `Multipliers.cs`.
- **The volume rows' bar** (`MainManager.ShowItemList`, type 17, `settingsindex` 33, 34 and 160): ten `pip` objects from
  x 4.45, 0.4 apart, between arrows at 3.75 and 8.75; empty `guisprites[59]` at 1/4 scale, lit `guisprites[42]` at 1/3,
  yellow; sorting 10 + index. Used by `ApMenu.cs`.

## The text letter pool (2026-09-26, code read; the symptom seen in play)

- **Every drawn letter comes from one pool of 500** `TextMesh`es (`MainManager.letterpool`, a private static array
  made in `LoadEssentials`, `MainManager.cs:3060-3064`, each by the private `NewLetter`, `:3290`).
  `GetEmptyLetter` (`:14630`) hands out the first whose text is `""`, **makes one with `NewLetter` for a slot that is
  null**, or returns **null when none is free**: `SetText` then skips that letter silently, so the text just ends
  early. So a longer array is filled by the game itself, one letter as each is needed.
- **Measured (2026-09-30, the console's `letters` with the Quality of life page open from the main menu's
  Settings):** 500 of 500 taken, about 300 by the page, 135 by the Settings screen's hidden boxes, bars and buttons, 46
  by the main menu's four options; the page's last line lost its end.
- **`DestroyText(parent)` frees only every other letter at once.** For each `Text` holder it steps forward through the
  holder's children and moves each freed letter back under `MainManager.instance`, which shifts the next child into the
  index it just left. The skipped letters keep their text until the holder is destroyed at the frame's end. A redraw
  in the same frame therefore needs about half the old letters plus all the new ones.
- **Seen:** the Quality of life page opened from Settings (the Settings list stays drawn behind it) with the Reset
  question's Yes / No box: the first draw was whole, and after a left / right redraw it showed "Ye" and no "No". The
  shorter Disable all question fitted. Used by `TextPool.cs`.
- **The game's fonts** (2026-09-29, code read): `MainManager.fonts`, loaded at start from `Resources/Fonts/`, one per
  entry of the private enum `Fonts` (`MainManager.cs:15-23, 3167-3176`): BubblegumSans, D3Streetism, UNUSED (never
  loaded), Uzura, BalsamiqSans, ONEMobilePOP. Before sizing a letter the game asks its font for it
  (`RequestCharactersInTexture`, then `GetCharacterInfo`, `MainManager.cs:9784-9787`); a character the font lacks has
  no info. For the in-game text client (`documentation.md`, step 2).
- **Fonts from the computer:** the game's `UnityEngine.TextRenderingModule.dll` (Unity 2018.4) has
  `Font.CreateDynamicFontFromOSFont` and `Font.GetOSInstalledFontNames` (2026-09-29, both names found in the DLL
  itself; not called yet). The text client's fallback for characters the game's fonts lack.

## The item table's fields (2026-09-26, code read)

`MainManager.itemdata` is `string[1, 256, 7]` (`MainManager.cs:3431`): 256 slots, about 188 used, so ids after the last
are free for the mod's own items. Per id, fields 0-3 come from the language file `Data/Dialogues<lang>/Items` (split on
`@`), 4-6 from `Data/ItemData`: **0 the name, 2 the description the menus and the item-get box show**
(`PauseMenu.cs:1868`, `:2274`; `NPCControl.CreateDescWindow` reads `itemdata[type, id, 2]`), 3 the article, 4 the price.
**Field 1 is not the description**: every key item holds "Desc" there, the Explorer Permit nothing. Medals keep theirs
in `badgedata[id, 1]`. Used by `ItemSwap.cs` (fixed 2026-09-26: it showed field 1; it now calls `CreateDescWindow`),
`CustomItems.cs` (writes field 2 as its items' description) and `EntityDump.cs`.

## Frame rate (2026-09-27, code read; the console's `display` on the test machine)

- **The game's settings:** `MainManager.fps` 0 = 30, 1 = 60, 2 = uncapped (`targetFrameRate = -1`), applied in
  MainManager next to `vSyncCount`. The settings menu (PauseMenu, option 80) cycles only 0 and 1; option 2 still has a
  label (menutext 107), so it was cut. The value is saved in the game's own settings file, read at `fps = ...` in
  MainManager's settings loader.
- **VSync on:** `vSyncCount = clamp(floor(refreshRate / 60), 1, 4)`, so 144 Hz runs at 72 and 240 Hz at 60.
- **Scales with time:** `MainManager.TieFramerate(v) = v * Time.smoothDeltaTime * 60`, used almost everywhere.
- **Does not:** about 32 `Time.frameCount % N` checks (EntityControl, NPCControl, FishAI, Fader, LightSorter,
  Hidder, BattleControl, ...), so they tick in proportion to the frame rate. `MainManager.FrameDifference` divides by
  the target rate or the refresh rate.
- **The tapping-key action command** (BattleControl, `TappingKey`/`RandomTappingBar`): each press adds
  `TieFramerate(data[1]) / 80 * (rate / 60)`, with rate = `targetFrameRate` (VSync off) or the monitor's refresh
  rate (VSync on). Right at any fixed cap with VSync off; negative, so the bar drains, at uncapped (-1); with VSync on
  it is off by refresh / actual fps (4x on a 240 Hz monitor at 60).
- **Read on the test machine with the console's `display` (VSync off, 60fps, windowed):** current resolution 3840x2160 @
  240 Hz, window 1280x720, `vSyncCount` 0, `targetFrameRate` 60, measured 60.6 fps.
- **Physics steps 50 times a second** (`Time.fixedDeltaTime` 0.02, the console's `display`, 2026-09-27). The camera
  follows in `MainManager.FixedUpdate` (`RefreshCamera`: the camera's parent position, the camera's local position and
  angles, all lerps with a fixed factor); characters move by rigidbody velocity. The player's rigidbody has no
  interpolation (`None`); the game's code never sets `interpolation` (none in the decompiled code, 2026-09-30).
  Used by `FrameRate.cs`.
- **Three cameras** (the console's `cams`, 2026-09-27): Main Camera (depth -1, parent MainCam), 3DGUI (depth 0, a child
  of Main Camera, culling mask 32768 = layer 15, where emoticons such as the "!" over NPCs draw) and GUICamera (depth 1,
  a child of Main Camera, layer 5, the HUD). The children draw after the main camera, from wherever it is. Used by
  `FrameRate.cs`.
- **`MainManager.ApplySettings()`** (static, no arguments) applies FPS and VSync with the rest of the settings; the
  settings screen calls it. It first sets the volume of every `music` and `sounds` audio source, null-checking the
  arrays but not their members, so it throws once those are destroyed: the game's log showed a
  `NullReferenceException` in it from the plugin's unloading as the game closed (2026-10-03, code read 2026-10-04).
  Used by `FrameRate.cs`.
- **The game forces a collection every 5 seconds:** `MainManager.DoClock` (the play-time clock, once a second) calls
  `Resources.UnloadUnusedAssets()` then `GC.Collect()` when `clocksec % 5 == 0` and no room transition is on. Leaving a
  map does the same when no event is running. Measured cost on the test machine: two slow frames, about 45 and 66 ms,
  exactly 5.00 s apart (the console's `frames`, 2026-09-27). Used by `ClockCleanup.cs`.
- **Walking up versus jumping, two calibration points** (seen in play, 2026-09-27; heights not measured yet):
  the rock up to Madeleine's house (`BugariaOutskirtsOutsideCity`) can't be walked up, though it looks barely above the
  ground, so it needs Jump; the side of the stump inside the ladybug siblings' house can be walked up. The walkable
  limit lies between them; which of the two is a step and which a slope is still to read (a step limit and a slope
  limit may differ). For the planned Jump draft (`room-logic.md`).
- **A frozen enemy's slide** (2026-09-27, code read; both symptoms seen in play at 240): the knock
  (`NPCControl.Dizzy`, `:5107`) sets `rigid.velocity` and `icevel` to the push with no vertical part, then hops the
  enemy on the next frame; `NPCControl.Update`'s frozen branch (`:1648-1657`, enemies with `freezecooldown > 0`) and
  `PushRockStuff` (`:2862-2877`) cancel the slide (`icevel = 0`) whenever the vertical speed reads near zero, and the
  frozen branch writes `transform.position = LimitRadius(...)` every frame. Used by `FrameRate.cs`.
- **Platforms carry by parenting** (2026-09-27, code read; the symptom seen in play): `GroundDetector.OnTriggerStay`
  makes the entity a child of a collider tagged `Platform` or `PlatformNoClock` and sets its `platform`;
  `OnTriggerExit` un-parents it (the player and followers to no parent, others to the map) and clears `platform`
  (`GroundDetector.cs:59-66, 102-116`). Walking sets `rigid.velocity` (`EntityControl.Move`). With rigidbody
  interpolation on, the carried body was held back ("walking in mud" at 240 fps); off, it moved freely. Used by
  `FrameRate.cs`.
- **What moves a character, and when** (2026-09-30, code read; the conveyor measured with the console's `bodytrace`):
  - `PathPlatform` and `RotatingPlatform` (`NPCControl` objects, kinematic) set their position or angles in
    `NPCControl.Update`, every frame (`TieFramerate`).
  - A conveyor moves the leader in `PlayerControl.OnTriggerStay` (`transform.position += conveyor * framestep`),
    inside the physics step, which Unity's interpolation doesn't smooth. Measured on one in Termite Industrial (collider
    `Plane_001 (1)`, tag `Conveyor`, layer 13), interpolation on: the leader's on-screen x jumped about 22 px each
    physics step. The leader's own walking is velocity set in `PlayerControl.LateUpdate` (`Movement`).
  - `EntityControl.Follow` runs in `EntityControl.LateUpdate` unless paused. While Vi flies it puts Kabbu
    (`animid` 1) at her position plus 0.2 along the camera's forward, every frame; a temporary follower at the last
    party member's position in flight, and at the leader's while digging. Leif in flight lerps toward Vi in
    `EntityControl.FixedUpdate` (`leiffly`). Followers otherwise walk by velocity (`DoFollow`, `MoveTowards`).
  - An entity's emoticon (the "!") is a child of its `rotater` (`EntityControl`, where it's created).
- **Scenery swung or bobbed inside physics steps** (2026-10-01, code read; the swing found with the console's `solids`):
  `StaticModelAnim.FixedUpdate` writes the swing (`transform.eulerAngles = startangle + sin(bobspeed * t) * bobfreq *
  bobangle`, per axis) or the bob (`transform.position`), `t` being `Time.time`, when `!nomove`, `bobspeed` isn't zero
  (the game tests it twice) and `bobangle` isn't zero or `stopbob` is off; its texture scroll (`SetTextureOffset`) is
  applied there too. `KeepAngle.LateUpdate` writes its object's world `eulerAngles`. The swinging platforms on
  `RubberPrisonPier`: `218/Base/swingingplatform` (the `StaticModelAnim`), its child `CranePlatform` (tag
  `PlatformNoClock`, a `KeepAngle`: it hangs level), which the party stands on as its children. Other classes that move
  things in `FixedUpdate`: `Wind` (its streaks), `GlowingAura` (halos), `DummyControl` (spin, scale), `SpriteBounce`
  (scale), `DialogueAnim`, `PromptAnim` and `FaceCamera`. Used by `FrameRate.Scenery.cs`.
- **Unity's `Quaternion ==` is approximate** (the game's `UnityEngine.CoreModule`, decompiled 2026-10-01): equal when
  `Dot(a, b) > 0.999999f`, rotations under about 0.162 degrees apart; `Equals` compares each component exactly. The
  Rubber Prison's swing turns about that much in a physics step. Used by `FrameRate.Scenery.cs`.
  - `EntityControl.DoFollow` returns early when `Time.frameCount % 2 == 0` (or `usebuffer`), so at 60 fps a follower
    decides walk or brake 30 times a second. It walks with `MoveTowards` (sets `forcemove`; `FixedUpdate` then sets the
    velocity, scaled by distance) and brakes with `StopForceMove(basestate, smooth: true)`, which halves the
    horizontal velocity per call. That is the game's only smooth `StopForceMove` call. Every other `Time.frameCount`
    test outside `EventControl` does its work on the divided count or returns on `!= 0` (all read in context).
    Measured on a conveyor at 240 with the row's 60 Hz count: before, the follower's velocity changed every frame;
    after the fix, every 8th frame, halving each time (2.5, 1.25, 0.63), a walk-brake rhythm about every 0.1 s.
  Used by `FrameRate.cs` and `FrameSites.cs`.
- **Unity's side** (2026-09-30):
  - This game runs with `Physics.autoSyncTransforms` True (the console's `display`).
  - Unity 2018.4's docs: "All physics calculations and updates occur immediately after FixedUpdate" (manual,
    Order of Execution). With autoSyncTransforms false, syncing "only occurs prior to the physics simulation step"
    (`Physics.autoSyncTransforms`). The same page's flowchart (`monobehaviour_flowchart.svg`) places OnTriggerXXX,
    OnCollisionXXX, then `yield WaitForFixedUpdate` after the internal physics update, before Update.
  - Checked in play: a `WaitForFixedUpdate` coroutine sees the conveyor's push and the walking of the step it follows
    (the trace's move per step, 0.03 to 0.16 units while speeding up).
  Used by `FrameRate.cs` and `Plugin.cs`.
- **Text effects per frame** (2026-09-27, code read; seen, below): `FontEffects.Update` moves a *shaky* letter to a new
  random offset (up to 0.025) every frame, and a *glitchy* letter rolls its swap chance every frame, so both run 4x as
  often at 240 FPS as at 60. *Wavy* follows `Time.time` and doesn't change with the frame rate. Nothing in the mod
  handles `FontEffects` yet. The same per-frame re-roll: `MainManager.ShakeObject` (a coroutine, one random offset per
  frame, its length in frames via `TieFramerate`; the leaf gang's ambush shakes two bushes with it, `Event128`,
  `EventControl.cs:21712-21714`) and `EntityControl.ShakeSprite` (its length via `framestep`), both called on hits and
  in scenes; the camera's `screenshake` is rolled in `RefreshCamera`, run from `MainManager.FixedUpdate` (50 a second).
  Seen on screen: the text blurry at 240 and sharp at 60. Used by `FrameRate.cs`.

## Save crystals, saving, Game Over and room transfers (2026-09-28, code read; the crystal's range seen 2026-09-29)

- **A save crystal** is an `NPCControl` with `entitytype` Object and `objecttype` SavePoint; `SetUp` gives it
  `interacttype` SavePoint and tints it from its entity data: yellow when `data[2] == 0`, red (a DeadLander's) when
  `data[1] >= 10`, blue otherwise (`NPCControl.SetUp`, the SavePoint case). `data` is the map's entity data, rebuilt on
  every map load; nothing of it is saved.
- **Only a hit starts one** (`NPCControl.OnTriggerEnter`, SavePoint): the Beemerang, or a hitbox tagged `BeetleHorn`,
  `BeetleDash` or `Icecle`. In order: `entity.anim.Play("BounceUp")`, `entity.PlaySound("Save", 0.5f)`,
  `MainManager.Heal()` if `data[2] == 0` (the full heal, HP and TP, **on the hit**, before any prompt), `HitPart`, then
  a red crystal turns its DeadLander, any other opens the prompt with `Interact("save")` when the player is within
  squared distance 30. The prompt is `menutext[4]` with Yes / No; Yes runs the text command `Save`, which calls
  `MainManager.Save(caller.vectordata[0])`.
- **An NPC's talking range** (`NPCControl`, every 3 frames): in the player's talk list while
  `MainManager.GetDistance(npc, player, ignoreY: false) < radius` and on the same inside. `radius` is the entity data's
  column 13 (`MapControl.CreateEntities`). Measured 2026-09-29 with the console's `radii` on
  `BugariaOutskirtsOutsideCity`: nearly every NPC 1.6 (one 2.5), the shop stands (SemiNPC) 1.2, and the save crystal
  `SaveTutorial` 0. Used by `SaveCrystals.cs`.
- **The confirm button never reaches a crystal:** `PlayerControl` calls `npc[0].Interact(null)` only for the NPC and
  SemiNPC types, and jumps (`DoJump`, private, called only from there) otherwise; each frame it also drops a non-NPC
  `npc[0]` from its list. The icon over the player for something to check is `entity.emoticonid = 1` with
  `emoticoncooldown = 2`: a "?", the same as at a discovery (seen by the user, 2026-10-01, at a save crystal).
- **The game's own word** (`textsearch crystal`, 2026-09-28): "ancient crystal"; the yellow one "will heal our HP and
  TP too. Try smacking it sometime." (SnakemouthFallRoom:19). No name for a save crystal as such.
- **Saving:** `MainManager.Save(pos)` is only `InputIO.Save(pos)`: `SaveFile` builds the text (line 0 the position
  given, or the player's; line 1 each member's HP as it is; line 2 `map.name`, the map id), then `save{slot}t.dat`, the
  old `save{slot}.dat` moved to `save{slot}backup.dat`, the temp renamed. Synchronous, no UI of its own, no
  precondition; the game has no "can't save here" flag. The mod's `SaveRedirect` prefix on `InputIO.Save` catches every
  caller.
- **Loading:** `Event22` loads `mapid` and puts the party at the saved position; `lastpos` and `lastloadzone` are set to
  it; `insideid` is reset to -1.
- **Death is only a party wipe in battle.** Hazards and falls never cost HP: `Hazards.HazardAction` puts the party back
  at `player.lastpos` (after 3 tries `lastloadzone`), falling below `map.ylimit` too.
- **Where the party comes back, in detail** (2026-10-02, code read; a loop seen by the user the same day, map travel
  to the swamp then a jump into the water by the crystal). Used by `RespawnLoop.cs`.
  - **The hazard's respawn:** `Hazards.OnTriggerEnter` starts `HazardAction` for the player only, during `minipause`
    too (it skips only others then). `HazardAction` holds `minipause` while it runs (unless one was already on, then
    it's instant). It puts the party at `player.lastpos`. When `respawntries > 3` **and** `player.movecd >= 10` (a
    direction held for 10 frames), it uses `player.lastloadzone` instead and resets `respawntries` to 0.
  - **`respawntries`** goes up by one per respawn and back to 0 after 60 frames (`respawncooldown`, counted only
    outside `minipause`) with no new one. It's private, one per hazard.
  - **Below the floor:** `PlayerControl.LateUpdate` sets the position to `lastpos` when it's below `map.ylimit` (-50;
    -150 on a map with a hole), with no fade and no `minipause`.
  - **Where `lastpos` is set:**
    - a `Respawn`-tagged trigger: its `vectordata[0]`, or the player's spot after 15 frames on ground inside it;
    - the end of `TransferMap`: `lastpos` and `lastloadzone` both to where the walk-in ends;
    - loading a save;
    - some scenes.
  - **A fall's timings, measured** (2026-10-04, the respawn guard's log; `RespawnLoop.cs`): jumping straight back into
    the river in Snakemouth Den's bridge room, the party was free 1.03 to 1.57 s between respawns and on ground only
    0.14 to 0.39 s at a stretch. A loop over water never touched ground and was free 0.00 to 0.03 s (the swamp's old
    landing, and a spot 1 above the river): the water is touched while the respawn still runs, so the game repeats it
    at once.
  - **The 2-argument `TransferMap(map, pos)`** arrives at `pos` with no walk, so whatever spot was given becomes
    `lastpos` and `lastloadzone`. Map travel landed beside a save point that way until 2026-10-02 (the dev console's
    `oldtravel` replays it; at the swamp's crystal, `SwamplandsBridge` entity 6, it loops, seen 2026-10-04). Since
    then it, and Warp to Start, use the 4-argument form with a real door's appear and walk-to spots; map travel that
    way seen at every destination with nothing odd (the user, 2026-10-03). Used by `WarpButton.cs`.
  - **`TransferMap` waits on the walk-in** (`while (player.entity.forcemove)`) with no time limit, so a walk to a spot
    it can't reach (over water) never ends and the transfer holds `minipause` (`DevConsole.Warp.cs`, `unstick`).
  - **Not yet seen:** which of these the chapter-2 door's loop was.
- **Game Over:** `BattleControl.DeadParty` runs `GameOver` unless `MainManager.battlelossevent` is set (a scripted loss:
  the battle just ends and the story goes on). The menu: Retry, Retry after changing medals, Load, Title; in a battle
  that can be fled only Load and Title. From the battle's start, `GameOver`'s setup restores `flags` and `flagvar`
  before the menu shows (`SetFlags`), and Retry also restores TP, each member's HP, attack, defence and `lockitems`, the
  bag (`items[0]`) and the key items (`items[1]`) (`ReloadInitialData`; code read
  2026-09-30, `BattleControl.cs:3437-3447, 3583-3598`; this line said `items[0]` only until then). Medals owned, money,
  EXP, `crystalbflags`, storage and the journal stay as they are. Load
  checks `InputIO.SaveExists(saveslot)`, then `MainManager.ReloadSave()` (public): stops the battle and event
  coroutines, destroys the map and party, `StartEvent(22)`. `GameOver` and `DeadParty` are private.
- **A door** (`DoorOtherMap`) runs `MainManager.TransferMap`, refused during `inevent`, `pause` or `minipause`; it sets
  `roomtransition` and `minipause`, `LoadMap`s, places the party, walks it in, then clears `minipause` and sets
  `player.lastpos = lastloadzone =` where the walk ended, and clears `roomtransition` a frame later. Cutscenes change
  maps with `LoadMap` alone. A map's auto-event starts once the player is free (`MapControl.LateUpdate`) and sets its
  flag as it starts.
- **What the calling door adds** (2026-09-30, code read; nothing seen in game): `TransferMap`'s five-argument form takes
  the door as `caller`, and only through it does the transfer apply the door's camera (`data[1..3]` switch on
  `vectordata[3..6]`: the camera offset, its angle, its limits) and its arrival jump (`caller.entity.emoticonoffset.x`
  above 0.1 jumps to the walk's end instead of walking). The two- and four-argument forms pass no caller: the camera is
  left as it is, and the party always walks in. Used by `QualityOfLife.cs`.
- **No map has a spawn spot of its own** (2026-09-30, code read): `LoadMap(id)` places no one. The party keeps its
  position from the map before or, with `recreateplayers`, is made anew by `SetPlayers()` with no position given. So
  every transfer names its own spot: a door's `vectordata[1]`, a scene's position set after `LoadMap`, a dialogue
  line's `|warp,map,x,y,z|`. The bar's hatch (Event61) sets one after `LoadMap(30, recreateplayers: true)`: each member
  dropped in at (-20.34, 9, 0.53), one unit higher per member (`EventControl.cs:9865-9872`).
- **The Golden Path** (2026-10-04, EntityDump and code read): `LoadZoneGoldenPath` on `BugariaOutskirtsOutsideCity`
  requires flag 41. Inside `BOGoldenPath`, `Loadzonetunnel` (onward) requires 67, and `blocker` (Event12) stands
  until 67, placed at the tunnel's door but spanning the path to `loadzonecave` (the Hermit's cave, no flag) too:
  seen 2026-10-04, it turned the party back on the way to the cave. Beetle grass (`ObjectTypes.BeetleGrass`) is cut
  only by a hit tagged `BeetleHorn` or `BeetleDash`, Kabbu's (`NPCControl.cs`, the BeetleGrass case).
- **A map enemy's drops, and the held key** (2026-10-04, code read by a search agent; seen in play): a map enemy's
  `vectordata` is its drop table, each entry (x item id, y marker). On a won battle (`BattleControl.cs` ~30985,
  `caller.entity.Death`), `EntityControl.Death` (~5351-5434) drops berries, then, unless the enemy has an `eventid` or a
  path behaviour, picks one entry at random (a negative pick drops nothing); an entry with y = -2 is always picked and
  spawns a key item (kind 1) that never despawns and carries `activationflag = limit[0]`; any other entry spawns an
  ordinary item for 600 frames (y > 0: only while `flags[y]`). Winning sets no flag; picking the key up does
  (`NPCControl.CheckItem`), and the enemy, whose `limit` is that flag, stops spawning. A name containing `ShwKEY` sets
  `showitem`, and only WaspDriller and Zombeetle draw the held item (`EntityControl.cs` ~552, ~2465). Two of the 327 map
  enemies hold one: `RubberPrisonPier`'s `ShwKEY wasp` (key 161, flag 584) and `UpperSnekRiverPuzzle`'s
  `ShwKEYZombeetle` (key 160, flag 526). **Seen 2026-10-04:** beating the wasp left `tempitem kind 1 id 161 flag 584`
  and eight berries; flag 584 stayed False until the key was taken, then True.
- **Free story flags are few** (2026-10-04, a scan): of `flags`' 750, 713 appear in the decompiled code (`flags[N]`),
  a dialogue's `flag,N` (ScriptDump), an entity's `requires`, `limit`, `activationflag` or dialogue condition
  (EntityDump) or flag-switched scenery (MapDump), leaving 37 (22, 36, 89, 144-148, 195, 277, 310-314, 333, 407,
  453, 505, 506, 588, 590-592, 672, 698, 708, 718, 741-749); the scan may still miss a text-only use. Not enough for a
  flag per map enemy (327 rows).
- **Which map enemies can drop, and the hook** (2026-10-04, EntityDump and code read): of 327 Enemy rows, 307 have
  no `eventid` (the death drop runs only then); of those, 255 have no `requires` or `limit` (always there, over 101
  maps), 27 come with a story flag, 25 leave with one (21 at flag 79, the Rubber Prison's turn). The game's -2 entry
  reads `npcdata.limit[0]`, which an enemy without a limit lacks; `EntityControl.CreateItem(startpos, itemtype,
  itemid, direction, timer)` is public static, so a drop can be made the game's way (`RandomItemBounce`,
  `TempIgnoreColision`, `LateVelocity`, timer -1) without touching the enemy's table.
- **A dropped key is an ordinary pickup to the mod** (2026-10-04, code read): `ItemSwap.Pickups` matches any item
  entity by map and `activationflag` when its line is shown, its ground scan finds entities made after the map
  loads, and `LocationChecks` watches the flag; so a held key's drop needs only a location entry
  `{map, flag}`.
- **The Rubber Prison yard's rock** (2026-10-04, EntityDump and code read; reported by the user): `rock`
  (`BreakableRock`) on `RubberPrisonPier`, just inside the left door from `RubberPrisonCheckpointCorridor`, until flag
  589; a breakable rock breaks only on a hit tagged `BeetleDash` (Horn Dash) or, with conditions, `BeetleHorn`
  (`NPCControl.cs`, the BreakableRock case).
- **The Rubber Prison's checkpoint corridor** (2026-10-04, EntityDump and code read; the user's account of play): its
  gates (`gate1 - Duplicate` and its copy, `ANDBlock`s) open and shut by switches; from the yard's side the only
  switch (`switch - Duplicate - Duplicate`, entity 8) exists from flag 79, the far side's two have no flag. The
  `PrisonDoor` (entity 6, until flag 538) by the way to the spike room opens with the Explorer Permit (the user,
  2026-10-04). A switch takes the Beemerang, a hit tagged `BeetleHorn`/`BeetleDash`, or ice (`Icefall`/`Icecle`)
  (`NPCControl.cs`, Switch).
- **The Flower Key** (2026-10-04, EntityDump, ScriptDump, the game's item text; seen in play): key item 54, "the key to
  the red house in the Ant City main plaza, bought from Beette at a discount!". Beette is the `smug bee` on
  `BeehiveBalcony` (made from flag 299, chapter 3's end); her line 21 is `checkmoney,150` then `giveitem,1,54`
  ("So...? 150 berries for the house. You in?", seen 2026-10-04). The plaza's `locked door` (a `LockedDoor`, gone from
  flag 229) runs `Event59`, whose key list puts key 54 at its `dialogues[0].y` of 0; the right key is taken. The house
  has no map of its own. The same hive's `clothingshop` (`BeehiveMainArea`, flags 173 to 252) sells the Bee Hat (key
  99, 40 berries) and the Pretty Ribbon (key 94, 50).
- **The ant tunnels** (2026-10-04, code read, EntityDump, MapDump): the hub `AntTunnels` has one `EventTrigger`
  door per far end, each starting `Event49` (the ride, both ways) and each made from its flag: Golden Hills 76, Defiant
  Root 75, Termite (the Barren Lands) 77, Metal Island 78, Rubber Prison 79, Far Grasslands 80; its `Covers` hide and
  `CaveEntrances` show from the same flags. Each far end (`GoldenSettlementEntrance`, `DefiantRoot2`,
  `BarrenLandsAntTunnel`, `MetalIsland2`, `FGCave`) has a miner, Diana, until its flag, and a `Base/AntTunnelRock`
  hidden from it. Her scene, `Event48`, sets `flagvar[0]` to the area's price (Golden Way 15, Defiant Root 25, Barren
  Lands 35, Far Grasslands 50, Rubber Prison 60, Metal Island 100); the first talk with any miner is an introduction
  (flag 81); after it she offers the dig, checks `money` against `flagvar[0]`, charges `|money,-N|` and sets the
  area's flag. Up in `AntPalace1`, the way down (`MineLoadZone`) is made from flag 67 and the shaft's railing
  (`Base/mineblock`) stands until 67: seen 2026-10-04, a party riding up stood boxed in. Used by `AntTunnels.cs`.
- **The Golden Settlement's desert gate** (2026-10-04, MapDump's flag scenery and code read; seen in play): on
  `GoldenSettlementEntrance` the shut gate is `Base/DesertGate/WoodenGate2` and `(1)` until flag 83, the open one
  `Base/WoodenGate2 (2)` and `(3)` from 83; `gateswitch` (a Switch, an attack hits it) starts `Event50`, which swings it.
  Behind it `Base/Cube`, an invisible wall, stands until flag 170, set on first entering `DesertDRSouthEntrance` from
  the desert (`MapControl.cs`). **Seen 2026-10-04:** the gate swung open by its switch, the wall still stopped the
  party.
- **Eetl's blocker has two triggers** (2026-10-04, EntityDump and code read; seen in play): on
  `BugariaOutskirtsOutsideCity`, `eetlblocker1 - Duplicate` (41) stands from flag 41 and `eetlblocker1` (40) from flag
  114, both until 67, both starting `Event12` (a line, then a walk back). Flag 114 is set by `Event63`, where Eetl
  starts following the party (`extrafollowers.Add(30)`). **Seen 2026-10-04:** with only the first kept away, the second
  turned the party back while Eetl followed.
- **The save tutorial outside the city** (2026-10-04, code read, EntityDump; seen in play): `Event19` has a ladybug and
  an ant (entities 13 `LadybugK` and 14 `shielderant`) hit the crystal `SaveTutorial` (1), heals, opens the save menu
  and sets flag 30 (`EventControl.cs`, `Event19`). Its trigger `SaveEventTrigger` (15) stands until flag 30; the three
  actors only until flag 41 (the first boss). Flag 30 means "the save tutorial was seen": `Event6`, the spider scene,
  plays its own crystal lesson while 30 is unset and then sets it. **Seen 2026-10-04:** a file that skipped the opening
  and beat the first boss walked into the trigger, and the scene played with none of its actors there.
- Used by `SaveCrystals.cs`, `DeathLinkGame.cs` and `AutoSave.cs`.

## Upper Snakemouth's boss: Leif out until its beam (2026-09-28, code read; seen in play)

- `EventControl.Event182` (`UpperSnekBossRoom`) starts battle 96 (`"Battle8"`, no escape), then gives the third party
  slot (`playerdata[2]`) the condition `BattleCondition.EventStop` for 99999 turns: that member (Leif) can't act.
- The boss's beam, the first time (`BattleControl`, the boss's AI, `enemydata[...].data[0] == 0`, not a hologram),
  revives every downed member at 1 HP, removes `playerdata[2]`'s `EventStop` (a rejoin animation, 116) and gives
  everyone a Shield before it hits.
- Seen in play (2026-09-28, vanilla save with the mod): Leif stuck in a pose and unusable until the boss's big
  attack, then active. By design, not the mod. For combat logic: the fight starts with two members.

## A dimmer fade-out never finishes early (2026-09-28, code read; the delay seen in play)

- `MainManager.Transition` sets `intransition` at its start and clears it only at its end (`MainManager.cs:8862`,
  `:9033`). A dimmer fade-out (id 1) eases towards clear: `r.color = Color.Lerp(r.color, Color.clear, framestep *
  speed)` while `r.color.a > 0f`, with `failsafe = 600f` counted down by `framestep` (`:8914-8920`). A float scaled
  by `1 - t` never reaches 0 (it stops at the smallest denormal), so the loop always ends at the failsafe: 600
  sixtieths, **10 seconds** of `intransition` after any dimmer fade-out, at any speed, long after it looks clear.
  The game's own `FreePlayer` ignores `intransition`, so the player walks around meanwhile.
- Seen: after the opening skip's fade-in (speed 0.02), received items waited about 2800 frames on "busy: changing
  maps" (seen in play: "5-10+ sec"). The dimmer is `transitionobj[0]`, a lone object named `Dimmer` with a
  `SpriteRenderer` (`:8880-8900`). Used by `ItemReceiver.cs` (`FadeAllButDone`). After the tail was ignored, the same
  wait was about 870 frames: the fade from opaque to 2% at speed 0.02 takes about 194 sixtieths (0.98^n), about 3 s.

## Text commands inside a substituted string, and the save's separators (2026-09-29, code read; not seen in game)

- **A substituted string is read again for commands.** `SetText`'s `string`, `sstring`, `menu` and `call` commands put
  `flagstring[n]` (or a menu or dialogue line) into the text being parsed, then step back one (`k--`), so parsing runs
  on through the inserted text (`MainManager.cs:12683-12705`). Any `|command|` inside it runs. The game's own `lore`
  command relies on this (`:13704`).
- **Commands that change the game** (each a `case Commands.X` in the same parser):
  - `flag` does `flags[n] = v` (`:12455-12464`), and `money` adds and clamps to 0-999 (`:12574-12590`).
  - Also among them: `setvar` (`:11419`), `giveitem` (`:11457`), `save` (`:12349`), `additem` (`:12552`), `warp` and
    `transfer` (`:13262-13263`), `loadmap` (`:13272`), `event` (`:13636`), `addquest` (`:13714`), `addprize`
    (`:13750`).
- **The save's separators:**
  - `flagstring` is written joined by `|SPLIT|` (`:7059-7066`), and read back by turning `|SPLIT|` into the not sign
    (U+00AC) and splitting on it (`:17282`).
  - The save file itself is split into lines (`:17034`, `:17040`).
  - So `|SPLIT|`, the not sign or a line break inside a saved string shifts the fields on the next load.
- Used by `ServerText.cs`.

## Music and jingles (2026-09-30, code read; nothing seen in game)

- **One music player.** `MainManager.music` is one looping AudioSource and `sounds` has 15, all on MainManager's
  object (`MainManager.cs:3121-3122`, `:3177-3192`).
  - Every music change ends in `ChangeMusic(AudioClip, float, int, bool)` (`:4825`), which starts `SwitchMusic`
    (`:4694`): a fade-out, 0.1 s, then `music[id].clip` is set and played (`:4741-4763`).
  - It sets `lastmusic` from `Enum.Parse(Musics, clip.name)` (`:4834-4843`), so a clip must carry one of the
    `MainManager.Musics` names. There are 75 (`:298-375`).
  - Music loads from `Audio/Music/<name>`; a map's tracks are `MapControl.music`, chosen by `musicflags`
    (`MapControl.cs:433-461`).
  - **Three names have no clip** (measured 2026-10-04, dev console `musiccheck`, each name loaded as the game loads a
    track): 72 of the 75 load; `Beetle`, `Giant2` and `Giant3` come back empty. Seen first as a seed's pier, given
    `Beetle`, staying itself (`[music] Beetle not found`). With `FixSamira`'s five (below) that is the 8 her count
    leaves out. The 11 jingles all load from `Audio/Sounds`. Used by `music.py` (`NO_CLIP`).
- **Loop points:** `Data/LoopPoints`, one `end;start` line per track (`LoopPoint`, `:7655-7668`). `LoopMusic`, run in
  `FixedUpdate`, sends the player back to the second value once it passes a non-zero first (`:7671-7684`).
- **What reads the playing track back.** None of these would survive a swapped clip:
  - The victory fanfare `BattleWon` plays only when the music is `Battle0` or `Battle6` (`BattleControl.cs:4069`).
  - The level-up return checks `LevelUp` (`:31041`).
  - The track is saved by name or clip and replayed later:
    - the retry state (`BattleControl.cs:608`, `:1513`);
    - the map track after a battle (`:754`, replayed at `:31051`, resumed at its time
      with `keepmusicafterbattle`, `:31049`);
    - `EventControl.cs:754`, `MainManager.cs:10106`, `:12100`;
    - the mono-audio switch (`:16724-16733`).
- **Samira's list** (`samiramusics`, in the save, `:7000-7015`):
  - A track is added when `SwitchMusic` finishes, with the clip on the player (`:4771-4773`). Other callers pass the
    intended track: a map's (`MapControl.cs:452`), a door's (`:1115`), the minigames' literals, a music zone's clip
    (`NPCControl.cs:2004`).
  - `FixSamira` drops Title, Wind, Water, MachineHum and Breathing (`:9668-9688`).
  - `SamiraGotAll` needs 75 - 8 bought (`:4404-4407`) for her key item (`EventControl.cs:5147-5150`).
  - Her event plays the picked song through `ChangeMusic`, and floats her notes (`internaltransform[0]`) while it plays
    (`:5205-5226`). `SamiraStop` removes them (`:5012-5018`).
- **Outside the player:**
  - The factory elevator crossfades `Dungeon2` and `Dungeon2b` (`seamless`, `EventControl.cs:17047-17051`) through a
    sound slot (`MainManager.cs:4700-4756`): the next clip starts on the slot at the playing clip's time, the player
    fades down as it comes up, then takes the clip at the slot's time. The game's only seamless change (read
    2026-10-04). Used by `MusicShuffle.cs`.
  - A music zone plays its own track on its entity and fades the main player out
    (`NPCControl.cs:882-890`, `:2016-2020`).
  - The game never sets `mute` on any source (no `.mute` in the code).
- **Jingles are sounds at music volume:**
  - `BattleWon` (`BattleControl.cs:4091`) and `Gameover` (`:3493`, stopped by name at `:3533`).
  - `Snakemouth` (`EventControl.cs:3079`) and `SandCastleRise` (`:19310`).
  - `"ch" + (chapterid + 2)` for chapter titles, where `chapterid` runs from -1 to 5, so `ch1`-`ch7`
    (`MainManager.cs:9536`; callers in `EventControl.cs`). The title waits for its clip's length (`:9543`).
- **Sound funnels:**
  - Every `PlaySound` ends in `PlaySound(AudioClip, int, float, float, bool)` (`:4509`).
  - Every `StopSound` by name or clip ends in `StopSound(AudioClip, float)` (`:4565`), which stops the slots holding
    that clip.
- Used by `MusicShuffle.cs`; the pool by `music.py`.

## An item entity's item (2026-09-30, code read; nothing seen in game)

- **Three fields hold it:** `animstate`, `itemstate` and `basestate`.
  - `EntityControl.Start` sets `itemstate = animstate` for an item (`EntityControl.cs:608-611`).
  - `NPCControl.Interact` (Shop case, `NPCControl.cs:4363-4366`) and `CreateDescWindow` (`:4186-4189`) copy
    `itemstate` back into `animstate` before reading it.
  - Some paths reset `animstate` from `basestate` (`EntityControl.cs:2937`, `:4582`).
- **The game's own swap:** the random-medal cheat (flag 681) turns a medal pickup into another as it's taken by
  setting `basestate`, `itemstate` and `animstate` to the new id, then calling `UpdateItem()`
  (`NPCControl.cs:5677-5683`).
- **What reads it:**
  - An item shop slot's price is `ceil(itemdata[animid, animstate, 4] * mmulti)`; its name goes to `flagstring[0]` and
    the item to `flagvar[0]`, which the buy line's `additem` adds (`NPCControl.cs:4374-4378`).
  - A pickup puts `animstate` in `flagvar[0]` and the name in `flagstring[0]`; its `additemtoss` adds `flagvar[0]`
    (`:5645`, `:5670`, `:5724`).
  - The sprite is `itemsprites[0, itemstate]` for an item (`EntityControl.UpdateItem`, `:3228`). `UpdateSprite` calls
    `UpdateItem` whenever `animstate` changes (`:4051-4057`).
- **Shop slots:** `MapControl.CreateEntities` makes slot n of a keeper as `Fixedshop<n>`, with `animstate = the keeper's
  data[n]` when the keeper's `dialogues[10].x` (the slot's `animid`) is 0 (`MapControl.cs:1717-1724`). Nothing renames
  it; `Start` only strips line breaks and marks a name containing "Fixed" as fixed (`EntityControl.cs:612-617`).
- **Map pickups:** `CreateEntities` sets an item's `animstate` to its row's item id, `animid` to its kind (`data[0]`)
  and `item = true` (`MapControl.cs:1681-1686`); its `regionalflag` and `activationflag` come from the row (`:1642`,
  `:1650`).
- **When:** `MainManager` sets `map` to the new map as it loads it (`MainManager.cs:9653`); the map's `Start` then runs
  `CreateEntities` (`MapControl.cs:298`).
- Used by `ShopInventories.cs` and `ItemShops.cs`.

## Fixed numbers in the enemies' scripts (2026-09-30, code read; nothing seen in game)

Enemy scaling scales an enemy's HP (and each hit's damage), but a number written into an enemy's script stays as it is.
`dev-scripts/enemy-numbers.py` lists every one in `BattleControl.cs` with its enemy (`case MainManager.Enemies.X` inside
`DoAction`, else the method): 39 fixed, 30 relative (a share of `maxhp`, `HPPercent`), which scale by themselves. Each
fixed one, read in its code:

| Where (BattleControl.cs) | Enemy | What | Verdict |
|---|---|---|---|
| :1014 | Spuder | `hp -= 15` at the battle's start, before flag 41 on Hard (flags 613/614) | scale (HP units, after scaling) |
| :1022 | Zasp, Mothiva | `hp += 15` with flag 606 | scale |
| :1036 | Maki, Kina, Yin | `hp += 10` with flag 614 | scale |
| :1067 | fire-area enemies | `hp += 3` | scale |
| :1355 | any | `hp = 1` when a start left it at 0 | keep: survive at 1 |
| :2792 | the spider tutorial | `hp = 999` | keep: invulnerable marker |
| :12820, :15691 | enemies 42 and 48 | `Heal(10)` when their shield breaks | scale |
| :14605 | Angry Plant | `Heal(heavystrike ? 1 : 1)` | keep: 1 |
| :17519 | Pisci | `Heal(4)` on an ally | scale |
| :18340 | Weevil | `Heal(heavystrike ? maxhp : 5)` | scale the 5 only |
| :18361 | the Beast | `hp <= 10` | keep: its scripted end (enemy scaling keeps the 10) |
| :19197 | Mothfly | `Heal(Clamp(maxhp × 0.075, 2 + …, maxhp))`, the healed one's `maxhp` | relative, with a floor: keep |
| :20213, :20215 | Peacock Spider | `hp < 1` → `hp = 1` | keep: survive at 1 |
| :20579 | Wasp King | `hp = 999` | keep: invulnerable marker |
| :20793 | Wasp King | `Heal(stolen ? 4 : 5)` | scale |
| :20826 | Everlasting King | `hp <= 10` | keep: its scripted end |
| :21016 | Everlasting King | `Heal(3)` | scale |
| :21658, :21686 | Carmina | `Heal(5)`; `hp = 1` | scale the 5; keep the 1 |
| :23183 | Kali | `Heal(5)` on Kenny | scale |
| :23794, :23796 | Bloatshroom | `hp <= 1` → `hp = 1` | keep: survive at 1 |
| :24347, :24585 | Stratos, Delilah | the partner revived at `hp = 7` (with a counter showing 7) | scale both |
| :24363-24364, :24609-24610 | Stratos, Delilah | `Heal(15)` on both | scale |
| :24637 | Delilah | `Heal(6)` on Stratos | scale |
| :25643 | Wild Chomper | `Heal(1)` each turn | keep: 1 |
| :26339, :26340 | Maki | a summoned ally `Heal(10)`, `hp = 10` | scale |
| :26916, :26993, :27146 | Kina, Yin, Wasp General | `Heal(6)`, `Heal(5)`, `Heal(4)` on an ally | scale |
| :31721, :31735 | the holo party (`HoloVi`) | acts at `hp >= 10`, pays `hp -= 3` | scale |
| :31962 | `EnemyHeavyThrow` | acts at `hp > 20` | scale |

The ratio is the one enemy scaling gives the enemy whose HP the number measures: the healed one for a heal, the actor
for its own threshold. Used by `enemy-numbers.py` and `EnemyScaling.cs` (the mod guide, step 17).

## The submarine (2026-09-30, code read, the dumps and the game's text; nothing seen in game)

- **No item, only story flags.** Nothing in `MainManager.Items` is the submarine. The Colosseum won sets 409
  (Event163, `EventControl.cs:27805`). The throne room's trigger (`TermiteRoyalChamber` entity 11, requires 409, limit
  379) starts Event164, the king's scene, which sets 379 (`:28085`). The Termite pier's scientist and queen (entities 9
  and 10, requires 379, limit 447) start Event165 (`|hide||event,165|`), whose pier branch sets 447 (`:28130`). The
  first landing at the Bugaria pier sets 448 (`:25451`), and Event165's other branch, Elizant's welcome there, sets 350.
- **Its name is the game's own:** `TermiteRoyalChamber` line 19, the king: "We call it the Subaquatic Maritime
  Neotransport! We're very proud of it." The queen: "Ehm, I like to call it Submarine for short." Line 23: "Be careful
  with the Subaquatic Maritime Neotransport! It is the only prototype we have." (the game's text asset
  `Data/Dialogues0/Maps`, read from `data.unity3d`.)
- **One scene runs every dock, Event153** (`:25368`). Boarding asks "Board the submarine?" (menutext 193), sets
  `PlayerControl.submarine` and loads the lake, `MetalLake` (map 183); landing takes the dock's index on the lake (0
  Termite pier, 1 Metal Island, 2 Bugaria pier, 3 Rubber Prison's pier, 4 the fishing village, 5 Mystery Island).
  Its only story reads: flag 447 (`:25370`; off, the dock starts Event165 instead) and 448 (`:25406`; off, every dock
  but 0 and 2 refuses). No other code reads 447 or 448.
- **The docks** (the entity dump; each `animid` 256, eventid 153): `TermitePier` 3 `Fixedsub` (requires 379),
  `BugariaPier` 16 `Fixedsub - Duplicate`, `MetalIsland1` 9 `Fixedsub - Duplicate`, `FishingVillage` 3 `Fixedsub -
  Duplicate - Duplicate`, `RubberPrisonPier` 2 `Fixedsub - Duplicate - Duplicate` (each requires 448), and
  `MysteryIsland` 1 `Fixedsub` (no requirement).
- **Past the prison:** the ant tunnels' `DoorMetalIsland` (entity 11) requires flag 78 and `DoorRubberPrison` (entity
  16) flag 79. The one write of 79 is in Event193 on the prison's bridge (`:32078`), so before it the prison is reached
  by the submarine only.
- **The Termite gate from inside** (Event149, `:24818`): the scene loads the plaza (174) from area 7 and the outside
  (173, `TermiteOutside`) from anywhere else. Before flag 384 (set by its first run, `:24964`) it then looks up
  entities 15 and 16 (`:24916`), which `TermiteOutside` doesn't have (entities 0-7), and stops at `e[0].flip`.
- Used by the docks' area modules in `logic/` (build step 36), `Submarine.cs` and `KeptOpen.cs` (the mod guide, step
  37).

## Spy Specs (2026-09-30, code read; nothing seen in game)

- **Medal 17** (`Spy Specs`, a code gift, "All medals by source" above). The game asks for it in three places, each the
  party-wide `BadgeIsEquipped(17)`, which is `BadgeIsEquipped(17, -1)` (`MainManager.cs:16791-16794`):
  - `BattleControl.StartBattle` sets `scopeequipped` from it (`BattleControl.cs:858`); an enemy's HP bar shows when
    it is spied, or `scopeequipped`, or an HP-showing medal says so (`:3148`). So it counts from a battle's start.
  - `Tattle` (the Spy action, `:5234`): with the medal no crosshair command, the spy always succeeds, and at the end
    `if (!hasmedal) EndPlayerTurn()` (`:5286-5287`): spying doesn't use the turn.
  - The battle menu's command list adds a small icon (219) beside Spy (`MainManager.cs:16291`).
- **Where each ask runs** (2026-10-04, code read): the battle start's is in `public static IEnumerator
  StartBattle(int[], int, int, string, NPCControl, bool)` (`BattleControl.cs:718`), the only write of `scopeequipped`;
  Spy's is in `private IEnumerator Tattle()` (`:5221`), its first step, kept in the local `hasmedal`, which both the
  skipped aim and the skipped `EndPlayerTurn` read, so those two can't be had apart without changing its code; the
  icon's is in `public static void ShowItemList(int, Vector2, bool, bool)` (`MainManager.cs:15336`). No other code asks
  for 17.
- Used by `MedalAssist.cs` (the Spy Specs row, the mod guide, step 39).

## The Settings list's arrows (2026-09-30, code read; the game's screen in the user's screenshots)

- **The Settings window** (pause window 4, `PauseMenu.cs:2702-2710`): `boxes[0]` is `Create9Box((0, -1), 13.5 x 7.25,
  type 1)`, the Archipelago panel's own box, and its list (`ShowItemList(17)`, `listammount` 9) is placed at
  `(-4.75, 2.45)` in it (`:2167-2172`).
- **The list's arrows** (`MainManager.ShowItemList`, the pause menu's branch): `guisprites[1]` at scale 1.25, x 11.25
  in the list for window 4; the up arrow turned 180° at `y 0.25 + 0.3` while rows are hidden above
  (`MainManager.cs:15583-15604`); the down arrow at `num3 + 0.5` after nine rows 0.7 apart from 0.25, so `-5.55`,
  while rows are hidden below (`:16364-16385`). In the box: up `(6.5, 3.0)`, down `(6.5, -3.1)`, the top-right and
  bottom-right corners over the border, as the screenshots show. Used by `ApMenu.cs`.
- **A Settings row** (`MainManager.ShowItemList`, type 17, `MainManager.cs:15619-15635`, `:15900-15935`): a `Bar`
  at list `(1.4, num3)`, `num3` from 0.25 down 0.7 a row (scale 1 in the pause menu); its text at `(-2, -0.15)` in
  the bar at 0.75; its two arrows (`slider0`, `slider1`) `guisprites[1]` at scale 1, at bar x 3.75 and 8.75, turned
  -90° and 90°; a value like Mash's at bar x 6.25, `|center||size,0.75|`; a volume row's ten pips at bar x 4.45, 0.4
  apart, `guisprites[59]` at 1/4, lit ones `guisprites[42]` at 1/3 in yellow. In the box: rows' text at
  `2.55 - 0.7 k`, arrows at x 0.4 and 5.4, the value at 2.9, pips from 1.1. Used by `ApMenu.cs`.

## Rooms seen on screen (2026-10-05, the user, every ability in hand)

Each room as mapped for the logic (`room-logic.md`, "How a room gets mapped"), vanilla only.

- **`BugariaOutskirtsOutsideCity` (16):** Artis's Hard Mode gift and Madeleine's house (both table pickups) need Jump.
  The gate towards Snakemouth Den (`DoorSnakemouth`) needs the Explorer Permit. The caravan's shop and the ladybug
  siblings' stump need nothing: the stump's side is walked up. Nothing else in the room needs anything.
  **Its four entrances** (each arrived through as the game's door does, the console's `warp ... from ...`): from the
  city, the Golden Path and the east road, the room is crossed with nothing. From Snakemouth Den's corridor
  (`DoorSnakemouth`), the walk-in's target is on the town side of the closed gate: the party walks into it and the
  game moves it past, so that arrival is a one-way onto the town side. **Its three dig spots** (seen 2026-10-05, missed
  the first time): crystal berry #30's, raised at (22, 4.5, 25), needs Jump; the Dark Cherry's below the house (flag
  642) only Beetle Dig; the Danger Spud's behind a fence (flag 487, `digspotinhideout`), Beetle Dig under the fence.
  Locations 91-93.
- **`NearSnakemouth` (1):** with the Snakemouth Barrier up (an Extra Roadblocks test seed), the horn tutorial's scene
  moved the party past the gate. With it gone, every spot and door is reached from each of the three entrances with
  nothing. The horn tutorial replayed (flag 17 cleared, arriving from the cave's side): it ran, and the room stayed
  fully reachable after it.
- **`OutsideSnakemouth` (2):** from the den's door (left), a small ledge is dropped from freely and climbed back only
  with Jump. Below it, the crystal berry (location 19) and the dig spot need nothing. The grass between there and the
  corridor's door (right) needs Kabbu's horn, both ways. The arrival discovery (location 28) is the map's auto-start
  scene (event 11, flag 22, `MapControl` starts it on load whatever the door), so either door records it (the user's
  read, matched in code). The dig spot `Mound` buries a Spicy Berry (item 2) behind flag 683: dug with Beetle Dig, the
  flag went off to on (read with the console), and the berry was granted locally, no check sent. Now location 79.
- **`BugariaOutskitsSnakemouthCorridor1` (17):** between its two ends, two gaps (right side) and two ledges (left
  side): crossing needs Jump, both ways. Its door to Seedling Haven needs nothing to walk out of, but reaching the other
  two doors from it, or it from them, needs Jump and Icicle (the user: "top/right, going up/down across the water").
  Nothing in it is a location or reads a story flag (the dump: no item, flag or `requires`; its enemies' regional
  flags only).
- **`BugariaOutskirtsSnakemouthCorridor2` (18):** grass across the middle needs Kabbu's horn, both ways; nothing else.
  Of its grass, only entity 13 (x -26, regional flag 13, vector data 0:0:0) drops an item, a Crunchy Leaf; entity 11
  sets regional flag 5 and drops nothing. A grass patch's item is its vector data's x (-1: none). Location 80.
- **`NearSnakemouth`'s `HoneyGrass`** (x -49.5, by the cave door, regional flag 7) drops a Honey Drop (item 1): location
  81. No other grass in rooms 1, 2, 16, 17 or 18 drops anything (the dump, 2026-10-05).
- **`ChucksAbode` (27):** crystal berry #3 behind the house needs the Horn Dash, to break the big rock (`rock`,
  `BreakableRock`); everything else (Chuck, the save crystal, the door) needs nothing.
- **`GoldenPathTunnel` (35):** four parts. The bottom right (the Outskirts' door; Dottle's ball, item 24, flag 82, and
  a hidden room's dig spot, a Lore Book, flag 488, walked into through the wall between the two right doors) needs
  nothing. A big boulder between it and the left (the Forsaken Lands' door) needs the Horn Dash, both ways. The top
  right (the Golden Hills' door; the Dart on the stump, item 88, flag 725, Jump and the Beemerang) is reached by
  freezing ice, knocking it into place with the horn and jumping on it (Freeze alone works but is harder; the vanilla
  way counted); dropped down from. The upper left (Tunnel2's door; the Life Cast, item 72, flag 462, needing nothing)
  can't be climbed to from the room; dropping from it lands in the left part. The mushroom spring (lim 39) and two black
  covers (`Base/Black (1)`, `(2)`, hidden by 39) never change in a seed: receiving the Horn Dash doesn't set 39.
- **`BOGoldenPath` (36):** three parts. From the middle (the tunnel's door) up ledges to the right (the Outskirts' door)
  needs Jump; down needs nothing. The left (the Hermit's cave door) is across water: Icicle and Jump, both ways. The
  grass with location 12 sits below the right's ledges: the horn, no Jump. Crystal berry #6's mound is on the right:
  Bee Fly and Beetle Dig. Nothing else (the dump: other grass empty, the save crystal under the map at y -50, chapter
  2's wasps, Neolith and seedlings from 67 to 73).
- **`BugariaPier` (54):** the door, the save crystal, the submarine dock and the Ship's Wheel discovery (`statuediscovery`,
  dialogue line 63: `discovery,49`, flag 654) need nothing. Crystal berry #10 behind the dock, the boat's captain
  (with the Boat Ticket) and the quest board need Jump. Its only item is that berry. **The boat back from Metal Island
  lands on the dock, up top:** from there without Jump, the captain (to sail again) and a drop to the berry, with no way
  back up; or across the house and down to the ground (the door, the discovery).
  **The submarine lands below the house, on the ground:** the door and the discovery need nothing; the berry, the quest
  board and the captain need Jump.
- **`BugariaOutskirtsEast1` (55):** left door to right door, Jump, both ways; neither door is blocked. Down the ledges
  to the lower ground is a drop, Jump back up. The Cave of Trials' door down there sits inside grass: the horn, in and
  out. The Drowsy Cake on the stone (location 25): the horn and Jump. The Dark Cherry dig spot (flag 633), across
  water: Icicle and Jump, there and back. The HP Plus medal (flag 137), inside the waterfall on the lower ground:
  Icicle and the Beemerang.
- **`BugariaOutskirtsEast2` (56):** left to right needs nothing. The way up to the Lost Sands' door: the crank between
  the two side doors turned with the Beemerang Halt, or Icicle platforms across the water and Jump. **Corrected the same
  day (the user): no drop down.** The top is across the water; the crank can't be turned from there, so the way back is
  only the ice, and a party arriving there without it is stuck. The Tangy Berry dig spot (flag 669): Icicle and Jump,
  there and back.
- **`BOLostSandsEntrance` (57):** a guard (`antguardclosed`, lim 130) blocks the north door to the desert until flag
  130 (Gen and Eri's chapter 3 story), seen on screen; the logic had no rule on that door. Kept open now (the guard
  away, `antguardopen` present, `Base/WoodenGate2` hidden, `(2)` shown, as 130 leaves them; the flag itself never
  set). With it open, nothing is needed across the room, seen from both doors (the user).
- **`Blank` (115):** seen with a plain warp: black, empty, in the void (the user). One entity (an empty NPC slot), no
  door, save point or item. Only Event111 loads it, as a backdrop for a flashback (`LoadMap(115)`, the "Sad" song,
  three characters placed), then moves on: never a start (starts come only from doors; it has none).
- **`BugariaAssociationAttack` (130):** chapter 3's copy of the area outside the city. Two ways in: its one door, from
  the plaza attack map (123), and Event119's landing at about (2, 0, -1.6), where control comes back. From either,
  only that door is reached; the town's cast stands around (the user). No item, dig spot, grass item, discovery or
  quest board in it or in 123-125 (the dump); its caravan (`Crickerly`, from flag 41) and medal slot sell the vanilla
  way, the shop locations being the regular caravan's.
- **`SeedlingHaven` (136):** a dead end with a save crystal; nothing needed in or out (the user). The Seedling King
  (lim 143) is one of Event124's five bounties; beating him gives the Crystal Fruit (item 118, `SeedlingCrystal`,
  seen in the user's bag), granted locally in a seed today, no location: for the quest pass, with the other four
  bounties' prizes. **A grass patch's drop
  is one entry of its vector data at random** (`NPCControl.CutGrass`), and any grass may add a `MoneySmall` 12% of the
  time. `grassitem` here lists items 6, 7 and 77: `MoneySmall`, `MoneyMedium` (berries, the money) and a Tangy Berry,
  so it drops berries mostly; seen dropping only money (the user). Not a location.
- **`CaveOfTrials` (185):** nothing needed in or out (the user). Its altar (`hole`, line 11) opens the trials with the
  Mysterious Piece (Event156, flag 411); the Explorer Permit is refused (seen). Two items on slabs, a Tangy Berry (flag
  507) and a Dark Cherry (508), with copies from flags 503 and 504: behind the altar, for the quest/chain pass.
- **`GoldenPathTunnel2` (200):** its bottom door (to `BarrenLandsSideGPT`) needs nothing. Up to its top door (down to
  the Golden Path tunnel's upper ledge): Jump, Icicle, the Horn Dash, Bee Fly and an attack for a lever; dropped down
  from (the user). Its `Fixedlifecast` sits at (100, 999, 100), off the map: the Life Cast's only reachable spot is the
  tunnel's ledge. Its grass drops `MoneySmall` only.
- **`HermitCave` (227):** nothing needed in or out (the user); no item. Talking to the hermit (line 4, Event195) adds
  board quest 54 as taken with no board (`ChangeBoardQuest(54, 1)`) and sets flag 576; a girl appears and a red flower
  goes from flag 578: for the quest pass.

## Still to measure

What a source says, or a question, that no measurement has settled yet. Wiki claims kept beside their facts above
are marked "per the wiki".

### Quests (when quests come into scope)

- **The pause menu's quest list groups quests by chapter and shows done / not done** (seen in play,
  2026-09-24). In the logic, a quest is reachable only once its chapter is.
- **Where that lives:** `boardquestdata` merges `Data/Dialogues<lang>/BoardQuests` (text columns) with
  `Data/BoardData` (numeric columns) per quest id (`MainManager.cs:3496`). The taken flag is column 3 and the
  difficulty column 5 (`QuestDump`, "The quest board", above); the chapter column and the reward aren't read yet.

### Key items

1. The list of key items and their ids.

Answered since: where the goal is detected (the artifacts are story flags that `SaveProgressIcons` counts, "Observed
in the running game"; the mod's goal sent and seen 2026-09-26), how a key item is granted (no single function: three
text commands, `GiveItem` and `items[1].Add`; "How the game grants items" and "Key-item grant sources, raw"), each place
that grants one (the same), how the save stores key items ("Observed in the running game", the save's lines), where the
received-item count lives ("Free save slots for the mod"), and the item popup the mod reuses (the found-item line, "What
the mod's code relies on").
