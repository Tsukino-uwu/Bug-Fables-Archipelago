# The Archipelago side: how it works, and how it was built

This is the Archipelago half of the Bug Fables randomizer: the apworld, seeds, the server, and how the mod
connects, sends what the player finds and receives items. It has two parts: **how we built it**, step by
step, and **how it works**, a plain explainer of how any game talks to Archipelago. The game side (the mod
itself, probing the game) has its own guide: [documentation.md](documentation.md).

The explainer follows Archipelago's own [network protocol doc](https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/network%20protocol.md)
(read at version 0.6.7). Where this file and that doc disagree, that doc is right.

## Contents

**How we built it**

1. [Build step 1: a first, tiny apworld](#build-step-1-a-first-tiny-apworld)
2. [Build step 2: connect the mod to a real server](#build-step-2-connect-the-mod-to-a-real-server)
3. [Build step 3: the goal, counted in artifacts](#build-step-3-the-goal-counted-in-artifacts)
4. [Build step 4: connecting on its own, and staying connected](#build-step-4-connecting-on-its-own-and-staying-connected)
5. [Build step 5: a compressed connection](#build-step-5-a-compressed-connection)
6. [Build step 6: sending checks](#build-step-6-sending-checks)
7. [Build step 7: receiving items](#build-step-7-receiving-items)
8. [Build step 8: logic from the game's own gates (in progress)](#build-step-8-logic-from-the-games-own-gates-in-progress)
9. [Build step 9: keeping the world open](#build-step-9-keeping-the-world-open)
10. [Build step 10: more kinds of location](#build-step-10-more-kinds-of-location)
11. [Build step 11: shops](#build-step-11-shops)
12. [Build step 12: the entrance randomizer (experimental)](#build-step-12-the-entrance-randomizer-experimental)
13. [Build step 13: party members and moves as items (in progress)](#build-step-13-party-members-and-moves-as-items-in-progress)
14. [Build step 14: enemy shuffle (in progress)](#build-step-14-enemy-shuffle-in-progress)
15. [Build step 15: starting location (experimental)](#build-step-15-starting-location-experimental)
16. [Build step 16: the Boat Ticket](#build-step-16-the-boat-ticket)
17. [Build step 17: a release](#build-step-17-a-release)
18. [Build step 18: Starting Party Member](#build-step-18-starting-party-member)
19. [Build step 19: Archipelago's colours for players and items](#build-step-19-archipelagos-colours-for-players-and-items)
20. [Build step 20: All three members from the start (the default)](#build-step-20-all-three-members-from-the-start-the-default)
21. [Build step 21: Shuffle Field Moves](#build-step-21-shuffle-field-moves)
22. [Build step 22: Shuffle Jump](#build-step-22-shuffle-jump)
23. [Build step 23: every learned field ability an item](#build-step-23-every-learned-field-ability-an-item)
24. [Build step 24: how we plan and build the logic](#build-step-24-how-we-plan-and-build-the-logic)
25. [Build step 25: DeathLink, a panel row](#build-step-25-deathlink-a-panel-row)
26. [Build step 26: the tutorial leaf, an item the story puts in the bag](#build-step-26-the-tutorial-leaf-an-item-the-story-puts-in-the-bag)
27. [Build step 27: a found pickup is gone in every save](#build-step-27-a-found-pickup-is-gone-in-every-save)
28. [Build step 28: nothing unpublishable in the repo or a release](#build-step-28-nothing-unpublishable-in-the-repo-or-a-release)
29. [Build step 29: the logic in Python, one module per area, the Rule Builder's way](#build-step-29-the-logic-in-python-one-module-per-area-the-rule-builders-way)
30. [Build step 30: Room Swap (experimental)](#build-step-30-room-swap-experimental)
31. [Build step 31: Decoupled doors (experimental)](#build-step-31-decoupled-doors-experimental)
32. [Build step 32: Connection plando](#build-step-32-connection-plando)

**How it works**

1. [The big picture](#1-the-big-picture)
2. [Opening the connection](#2-opening-the-connection)
3. [Logging in](#3-logging-in)
4. [Sending what the player found](#4-sending-what-the-player-found)
5. [Receiving items](#5-receiving-items)
6. [Finishing the game](#6-finishing-the-game)
7. [Settings from the seed: slot_data](#7-settings-from-the-seed-slot_data)
8. [Use what Archipelago provides](#8-use-what-archipelago-provides)
9. [How this mod does it](#9-how-this-mod-does-it)
10. [Things that go wrong quietly](#10-things-that-go-wrong-quietly)
11. [The logic: regions, exits and rules](#11-the-logic-regions-exits-and-rules)

## Where it stands

Each step's own status is its last line (**Status:**). This section holds only what's next and what's known to
be wrong.

**Next** (decided from 2026-09-24 on; each item dated):

1. **Every key item and medal in the pool,** on logic that follows the vanilla story order: each chapter
   entered once the chapter before is finished and the story's own keys and abilities are in hand, its rooms
   split into regions as build step 24 describes. Medal gifts and medal shops each get a yaml on/off toggle.
   Shops (medal shops, item shops, the caravan): see build step 11. Other kinds of location (boss prize medals,
   placeholders, journal entries, enemy drops): see build step 10.
2. **Entrance randomizer (experimental):** every door, coupled or decoupled (build step 31), on Archipelago's own
   entrance randomizer, every map a region (2026-09-30, not yet seen in game); next, sorting the other transfers into chosen and forced, and the
   room-by-room logic that removes the label. See build step 12.
   **How each room gets mapped** (2026-09-27): the checklist in `room-logic.md`; the tester says what needs
   what, the agent turns it into areas and rules.
3. **Field abilities shuffled as items** (every learned ability built, build step 23) (by the game's names: Beemerang Halt, Bee Fly, Dash, Horn Dash, Beetle Dig, Icicle, Shield; `MEASURED.md`, every field ability).
   Party members stay where the story puts them.
   The three attacks and Jump as items: built, see build steps 21 and 22. Party members as items (*Starting Party Member*): built, see build step 18.
4. **Open world, one gate at a time** (always on, never an option; 2026-09-26): see build step 9.
5. **To test later (2026-09-25): a two-player room.** The tester's slot plus a second one the agent drives,
   sending items while the tester plays, to see items from another player arrive live: the hold-up on *All* and
   *Progression*, silence for a replay after a new save or reconnect, and the multiworld names ("X's item").
6. **A full bag:** key items keep arriving, only ordinary items wait.
7. **Goal:** the mod counts the game's artifact flags and sends "goal reached" at the required number: done, seen
   (build step 3).
8. **A release: three separate downloads** (2026-09-25): built, see build step 17; v0.1.0 out
   (2026-09-26), v0.2.0 (2026-09-27). The next one: `dev-scripts/release.ps1 -Version vX.Y.Z` after bumping both versions.
9. **The chat feed**, then the in-game text client (see the design list in the mod guide, step 2), so players never
   need the Launcher's Text Client (2026-09-29: DeathLinks shown too, a filter per kind of message, hints and
   commands from the text line, Enter to type in the field and in battles, a Chat menu in the panel). It is the
   answer to Next 43, item 18.
10. **A "Quality of life" page in the Archipelago panel** (2026-09-25): on/off rows that speed the game
   up and make it smoother: skips first, others later. Battle tutorials next (the mod guide, step 10).
11. **Map fast travel, built (2026-09-26; the mod guide, step 10), seen travelling to the Outskirts** (planned
   2026-09-25), apart from the Warp to Start button. On the pause menu's map
   (window 6, which lists areas), pick an area you've been to and confirm (Yes / No) to travel to its save point
   through the game's own map transfer. The game already records visited areas (`librarystuff[4, area]`, set by
   `MainManager.UpdateArea`). The logic never counts on it, like the warp. **One row with the warp
   (2026-09-26):** the Warp button's on/off becomes *Travel: Off / Warp / Map / Both* (Warp to Start only, map fast
   travel only, or both), so the two are set together. **The look (2026-09-26):** alone, either button
   looks like the Warp button does now; with Both, the two get different background colours. Map's icon: proposed a
   map icon always (so the button says what it does), still undecided. Both means a sixth button in the pause menu:
   it must fit and look good there, seen on screen before it counts as done. **The order:** both sit to
   the right of the game's buttons, Warp first, Map last. Left from the first button wraps round to Map for quick
   access, and Warp sits in between, so it's reached by accident less often. **How it's picked (2026-09-26):** on the pause menu's map, target a
   visited area and press confirm: a "Travel to <area>?" Yes / No box (No first). Confirm flips an area's description
   pages today (`PauseMenu.cs:1407-1433`, with Z the other way, wrapping), so while map travel is on, Z alone flips
   pages; nothing is lost, as Z wraps round. The game's map: a free cursor (`sprites[0]`) snapping to the visited
   areas' markers (`sprites[area + 1]`), `option` the area. Each area needs a travel spot: a save point in it, from
   the entity dump and each map's area (`MapControl.areaid`, now in the map dump).
12. **A quest board in the starting house** (2026-09-25): the quests every board lists, taken without
   walking to the town or the bar. Every board shows the same list (`MEASURED.md`, "The quest board"), so it adds no
   quests, only a shorter way. Every board lists bounties too (built, build step 9); next, the house's board from
   the start. **Decided (2026-09-25): every quest on the board from a new file**, the quests themselves
   still to do, each with its own logic (reaching its NPC, what it needs), so quests can be done along the way. First
   the `QuestDump` (a Debug setting). **Dumped 2026-09-25** (`MEASURED.md`, "Every board quest, dumped"): no quest's
   accept flag does anything outside its own quest, so opening them all is safe for the save; the unlock conditions
   are story flags and visited areas, which the mod would skip by adding every quest to the open list on a new file
   (six are added only by a dialogue, not by the table). Each quest's logic (its NPC's map, which the table lists, and
   any items) is still to write, per quest, before its locations exist.
13. **Bounties as locations, a yaml toggle** (2026-09-25): *Shuffle Bounties*, its own category, off by
   default (five hard optional bosses; progression shouldn't sit behind them unless the player asks). Today they are
   not locations and pay their vanilla rewards. First, measure what each bounty pays and when (on the spot or on
   reporting back); then the logic for reaching each boss. Its own build step when built.
14. **Enemy shuffle** (2026-09-26): `enemies_only` built; bosses, `both`, `chaos` and the map look next.
   See build step 14.
15. **Enemy scaling, a panel setting** (2026-09-26): Off / Party level / Artifacts, Party level by default,
   balancing an area met earlier or later than vanilla would. Built, seen on screen (2026-09-26); a mod-side setting
   with no logic, so its design and status live in the mod guide, step 17.
16. **EXP multiplier, a panel setting** (2026-09-26): built and seen, `documentation.md` step 19. *EXP
   multiplier* on the Gameplay page, 1x to 10x (first planned as 1x-5x on Quality of life), default 1x: an opt-in for a faster, easier game. Levels still give HP, TP and MP, so it helps even without moves being shuffled. It stacks on
   top of enemy scaling's EXP. No check and no logic depend on it. Only while Archipelago is enabled.
17. **Berry multiplier, a panel setting** (2026-09-26): built and seen, `documentation.md` step 19. *Berry
   multiplier* on the Gameplay page, 1x to 10x, default 1x, the same opt-in. Only the berries picked up in the world (lying there or dropped after a fight), never a
   check's reward from the server. Only while Archipelago is enabled.
18. **Random start** (2026-09-26): `anywhere` built, experimental; `towns` and named spots to come. See build
   step 15.
19. **Traps, an idea for later** (2026-09-26; not planned yet). A trap sent to this game takes effect when
   the server delivers it, after any open text box, like any received item. Held up at pickup: its own icon on a red
   starburst. One icon per trap, so the player knows what's coming. Examples: the Mistake medal poisons
   the party at the start of the next fight (dropped, below: traps never harm); a crystal berry (or something icy) freezes the player in an ice block
   for 1-3 seconds. Each trap: only with Archipelago on, never a soft-lock (a freeze always ends, even in a scene),
   nothing written to the save the game wouldn't write, never in logic. The game's own effects to reuse (code read
   2026-09-26, not yet measured): fight conditions (`MainManager.BattleCondition`: Poison, Freeze, Numb, Sleep,
   Inked, Sticky and more), map hazards (`Hazards.cs`, three `HazardAction` kinds, likely the knockback), falling
   off a map (put back at `lastpos`, `PlayerControl.cs:688-691`), and ice (`EntityControl.inice`, set by ice maps).
   **Traps annoy, never harm** (2026-09-28, after Celeste's flipped screen and Zelda's freeze and chickens):
   a trap never changes how a fight or a run goes, so no debuffs, no lost turns, nothing that can bring a Game Over
   (with DeathLink that would kill the whole room). Each wears off on its own: a few seconds or a timer on the
   overworld, cosmetic only in a fight. Ideas: frozen in an ice block for 1-3 s, reversed controls, a flipped camera,
   slippery movement (the game has no slippery floor, code read 2026-09-28: `EntityControl.inice` is the ice block, so
   it would be the mod's own), a silly look for the party in one fight. A lost turn (`EventStop`, `MEASURED.md`) was
   considered and dropped for this reason; the game's own conditions are never touched.
   First measure how each is applied. A yaml option (how many traps), so its own build step when built.
   **Frozen in an ice block, code read 2026-09-29 (not measured):** nothing in the game freezes the player on the
   field (`PlayerControl.frozencube` is declared, never used). Leif's ice freezes a map enemy through
   `NPCControl.freezecooldown` (300, 5 s), whose `Update` calls `EntityControl.Freeze()` (the ice cube) and later
   `BreakIce()`. `Freeze()` itself is safe on the player: its NPC-only lines are guarded and `CheckSpecialID` sizes a
   cube for every entity. The mod would bring the rest: its own timer and `BreakIce()` (which hops), the controls
   locked (`CancelAction()` first, as the dash ignores `lockkeys`), and a thaw at a battle start, a map transfer and a
   scene. A battle's Freeze (`SetCondition`, 2 turns on the party) costs turns, so it stays out, as above.
   **Disguised traps (the user, 2026-09-29):** on the ground the player can't tell their own trap from an item they
   want; a shop may tell (its description already says "A trap", `ClassWord`), and the pickup does.
   - *The player's own trap:* on the ground, a real progression or useful Bug Fables item not yet received, drawn
     exactly as a location holding it would be (`Describe`, `MarkColorOf`): its sprite and its class's backdrop, so
     the two always match ("a fake useful item with a purple background would be obvious"); with Item colors off the
     game's colour for its kind, with Item backgrounds off none. Picked up: red, its real look. A shelf shows it too.
   - *Another Bug Fables player's trap:* the trap item's own sprite on red, everywhere (the icon with *Archipelago
     icon: All players*), so you know it's a trap for someone else.
   - *Another game's trap:* the Archipelago icon on red, as today.

   **The look is always an item you don't have yet** ("you might want the hookshot so you will obviously try to go
   for the item until you have it"). The apworld picks, per trap location, an ordered list of wanted items with their
   class in that seed (a class can depend on options), with the seed's random, sent in `slot_data`; the mod shows the
   first not fully received (every copy, starting items included), the next once it arrives, and the last when
   nothing wanted is left. Fixed for the seed, never re-rolled on a visit, which would give it away. A fixed look can
   show a unique item you already own, which gives the trap away to anyone; this one only to a player who remembers
   what that spot showed. Only the two ground paths change (`ItemSwap.TickGround`, `ItemSwap.Redraws`); shelves and the
   pickup keep the real look. Scouting creates no hints (`HintCreationPolicy.None`); a hint the player asks for tells
   the truth, as in every game. **A yaml option, OoT's name and values:** *Trap Appearance*: Major Only (default:
   the wanted items above) / Junk Only / Anything, as OoT's and CV64's `ice_trap_appearance` (OoT picks per trap
   location at generation, from its own pool: `worlds/oot/Options.py:1130-1135`, `__init__.py:1081-1086`, 0.6.7).
   No Archipelago doc forbids hiding a trap before pickup; hints always show it, marked "avoid"
   (`network protocol.md:389`). Built with the traps, in their build step. Open: the `slot_data` shape (Next 43,
   item 15).

20. **Enemy group sizes, a yaml option** (2026-09-26): its own option, apart from *Enemy Shuffle*, off by
   default, for example `vanilla / shuffled / random` (fights of any size swap places; or 1-4 enemies rolled per
   fight). The game takes any number of ids: 4 on the field, the rest in reserve (`BattleControl.cs:787-800`). Only
   ordinary map fights change size. A boss or special fight is one unit of several slots (the Sand Wyrm's head and
   tail, Mother Chomper with two Fly Traps, the Wasp General's squad, Zasp and Mothiva, Cenn and Pisci, Stratos and
   Delilah, Maki's team; the rematch machine's switch lists them) and always moves whole, as the shuffle already
   moves whole id lists; a fight is never two bosses, nor a boss mixed with ordinary enemies. Built after enemy
   scaling, so a bigger group stays fair. **Summoners mostly guard themselves** (code read 2026-09-26, 24 `SummonEnemy` calls): the ordinary ones only summon
   when alone or nearly (Burglar, Wasp Healer, Leafbug Archer, Bloatshroom, Chomper Brute alone; Leafbug Ninja under
   3), bosses too (Bee Boss and Mother Chomper alone; Pitcher and Seedling King under 3; Midge Broodmother with a free
   spot). Only boss-internal parts have no count check (Venus's plants, Pisci's add, the Sand Wyrm's tail, the
   Everlasting King's tablets), and boss units move whole. So a bigger ordinary group mostly just stops a summoner
   summoning, as the game itself does. Still a guard before building: each branch read, and one full-field fight.
   Its own build step when built.

21. **Boat Ticket** (Discord, 2026-09-26): built, see build step 16.
22. **Healing save crystals, an idea for later** (suggested on Discord; 2026-09-26): an item that makes the
   blue save crystals (save only) act like the yellow ones (save and heal), a nice filler or useful check. The colour
   is not baked into the art (code read, 2026-09-26): a save point is tinted in code from its entity data, yellow when
   `data[2] == 0`, red when `data[1] >= 10` (`NPCControl.cs:1190-1217`), so the mod could turn every blue crystal
   yellow by setting its data before the map builds it, as the enemy look test does. **Built as a panel setting
   instead (2026-09-28):** *Healing crystals* on the Gameplay page, `documentation.md` step 30; the heal is
   in the crystal's hit, not the prompt (`MEASURED.md`, save crystals).

23. **Progressive items, an idea for later** (2026-09-26): items that unlock in a fixed order however they're
   found, as Pseudoregalia's progressive sword (three copies of one item; the first gives the sword, the second breaking
   blocks, the third the ranged attack). Archipelago counts copies of one item (`Has(item, count)`), so the logic is
   simple. Candidates: each member's field abilities in their game order, and other chains; decided when abilities
   become items (Next 3, build step 13).
   **Decided (2026-09-27): three progressive items, always, never an option, each *progression*** (every
   level unlocks checks): Vi's *Beemerang Toss* then *Beemerang Halt*; Kabbu's *Dash* then *Horn Dash*; Leif's
   *Freeze* then *Icicle* (the game's names and flags, `MEASURED.md`, every field ability). Why always: a second level
   without the first wouldn't work; in the game the Halt, the Horn Dash and the Icicle each extend the first
   (the Halt holds a thrown Beemerang, the Icicle is a second tap during the Freeze, the Horn Dash changes the Dash).
   With Shuffle Field Moves (build step 21), the Beemerang and Freeze items become the progressive ones' first copy.
   **The Dash without the Horn Slash (decided 2026-09-27):** the Dash starts as a second tap
   during the Horn Slash (`PlayerControl.cs:1083`), and its hitbox carries the slash's own tag (`BeetleHorn`, `:1139`),
   so today it cuts grass, pushes rocks and hits switches like the slash (grass takes `BeetleHorn` or the Horn Dash's
   `BeetleDash`, `NPCControl.cs:4764`). Proposed: with the Horn Slash locked, a double tap still starts the Dash, the
   first tap doing no slash and the dash's hitbox no horn effect, as the Horn Dash adds rock breaking to it; the Horn
   Dash then breaks rocks but leaves grass and the rest to the Horn Slash. The logic: grass and pushing need the Horn
   Slash, speed the Dash, rocks the Horn Dash; no progressive item needs another. **With the Horn Slash received the Dash
   is the game's own again**: it cuts grass and does everything the slash does, the Horn Dash too.
   **Why it suits the logic:** grass is always "Horn Slash", never "Horn Slash or Dash"; the grass rules
   written today (`abilities: ["Horn Slash"]`) stay right once the Dash is an item.
   **Every learned ability an item, always (2026-09-27):** Beemerang Halt, Bee Fly, Dash, Horn Dash, Beetle
   Dig, Icicle and Shield are always in the pool, not behind an option ("randomizing things the player would have
   found"); the three starting moves stay under Shuffle Field Moves ("removing things"). **Each unlock scene a location
   now** (as the party members' joining spots, no temporary double grant to forget), with a story-order rule
   until chapters 2-7 get room-level logic: each needs every ability learned before it, chapter 1 done, and the members
   and moves when those are items (more cautious than the game). **How (proposed):** the scene runs untouched and
   still sets its flag, which is the check; using the ability follows the item, the game's ability checks answered
   from the received items, never by writing a story flag. Its own build step.
24. **The panel's settings on normal saves** (2026-09-26; built, `documentation.md` step 18): an opt-in row so Quality of life and
   Gameplay also apply with Archipelago off. A deliberate exception to "vanilla stays vanilla", which only the project owner can
   make; off by default. **Named: *Use on normal saves*, ON / OFF**, help line "Quality of life and Gameplay
   also apply with Archipelago off." Only the two pages' settings; nothing tied to a seed (items, checks, the shuffles).

25. **Consumable keys, an idea for later** (2026-09-26): custom items used up on a door, as the game's own
   `removeitem` takes an item (`items[kind].Remove(id)`). The rule the crystal berries set (build step 11): no action may
   make a check unreachable, so a key either opens one named door, or the keys and the doors that take them are exactly
   as many, with no door that could waste one.
26. **Enemy stats randomized, planned for later** (2026-09-27): an option Off / Enemies / Bosses / Both that
   changes HP, defence and EXP per enemy type. Decided at generation (`slot_data`, per enemy id), applied after enemy
   scaling as a fixed multiplier, so the two stack. Defence is the risk: damage is attack minus defence, so a raised
   defence can make an early enemy unhurtable; HP and EXP take a wide range (about x0.5 to x2), defence at most +-1 and
   never above what a fight at that point can get through, bosses especially.
27. **Enemy attacks randomized: not planned** (asked about, 2026-09-27): each enemy's attacks are written for that
   enemy in `BattleControl.DoAction` (its own animation numbers, shape, positions, summons), so another enemy's attack
   would miss animations or wait forever on one and soft-lock the battle. At most, later, swaps between enemies built
   alike, tested one by one.
28. **Artifacts shuffled, an idea for later** (2026-09-27): the goal's artifacts found anywhere, not only at
   the chapter ends. Each chapter end (its artifact flag) becomes an ordinary location, and an Archipelago-only
   *Artifact* item goes into the pool; the goal counts Artifacts received. The item never sets the game's artifact
   flags: they drive the story (flag 41 is the logic's "Snakemouth Den Cleared"). **Decided (2026-09-27):**
   the pause menu's icons show Artifacts received (drawn only; the save's own count, which the file select shows, stays
   the game's). **Any order:** seven distinct items, *Artifact 1* to *7*, each drawn with its own icon, so
   Artifact 4 can come before 1; the goal is any N of them. The game draws the first N of `StartMenu.psprite` from a
   flag count (`PauseMenu.cs:2397-2402`), so the mod draws the received ones itself. Each item is tied to its chapter:
   its chapter's icon, and the game's own name for that chapter's artifact if it has one (`textsearch`);
   the chapter-end locations keep place names. **Icons:** the received ones only; all 7 with the missing
   ones faded is worth a look on screen when it's built, kept only if it looks good. The pool: always 7, *Artifacts Required* 1-7, so the game's 7 icons can show it (agreed); a
   bigger pool breaks nothing in Archipelago but needs a filler slot per Artifact and a display past 7 icons, so later
   if asked. A yaml
   option, so its own build step.
29. **A bosses goal, an idea for later** (2026-09-27): a *Goal* option (Artifacts / Bosses) and *Bosses
   Required* (all, or a number): each boss's beaten flag an event the goal counts, as artifacts are today. **Decided
   (2026-09-27): story bosses only**; the bounties maybe a side setting later, once *Shuffle Bounties* gives
   them logic (Next 13). Only a real
   choice once the world reaches past chapter 1 (today: one artifact, one story boss). Its own build step.
   **Several goals at once, asked 2026-09-29, as Super Metroid's objectives** (0.6.7, read the same day): SM's
   `objective` (up to 5 of about 40 goals) or `custom_objective` (N picked at random), every one selected required
   before the last boss (`worlds/sm/Options.py:318-370`); Satisfactory's `GoalSelection` with `GoalRequirement`, any
   one or all (`worlds/satisfactory/Options.py:468-499`); Hollow Knight's `Goal: any`, whose rule requires logical
   access to every goal (`worlds/hk/__init__.py:497-501`). Still to choose: all selected (SM's way) or any one (then
   the logic needs them all, as HK). Either way one `completion_condition`, the mod's `CLIENT_GOAL`, and every chosen
   goal reachable in logic. Goals so far: artifacts (built) and story bosses (this item); a count of Tattle entries
   (Next 44) would fit the same way (proposed).
30. **Library discovery milestones as locations, an idea for later** (2026-09-27): the librarian's 10
   rewards (one per 5 discoveries, 5 to 50; `MEASURED.md`, "Journal rewards") as locations, "done" when `flagvar[53]`
   reaches 1 ... 10 (the `location_vars` kind, as Artis's prize). Rule: milestone k needs 5 × k discoveries reachable,
   so only the first fits today's 5 listed discoveries. Five of the rewards are crystal berries (43-47). Still to find:
   the library's region and any story flag the librarian needs; how the mod turns `EventControl.GiveItem` into a check.
31. **The Explorer Permit split, an idea for later** (2026-09-27): one permit per gate, so one item never
   opens four areas (as custom roadblocks spread Surf's reach in Pokemon Emerald randomizers). The gates (`MEASURED.md`,
   "What the Explorer Permit opens"): the Outskirts gate, the Rubber Prison's `PrisonDoor` (the locked-door routine's
   list, index 16), and B.O.S.S. and the Cave of Trials (the wiki's word; which item they take is measured in game as
   the first part of this step, before they're gated). The names: Snakemouth, Prison, Lab and Trial Permit; proposed: the Explorer
   Permit stays the Snakemouth one (the game's own gate and lines), plus three of the mod's own items as the Boat
   Ticket was made (build step 16). **Proposed (asked for vanilla kept optional):** a yaml choice *Explorer Permit*:
   Vanilla (one permit, every permit gate behind it, the mod leaves the gates alone), Split (the three new permits in the
   pool, each gate checking its own); only these two ("either 4 permits, or 1 permit vanilla"). Through
   `slot_data`, so the gates change only in a Split seed; the new items always exist in the item table (Archipelago's
   names are fixed) and enter the pool only with Split, each taking a filler slot as the ticket does. **Default: Split**
   (2026-09-27). Its own build step.
32. **Key items shown without browsing, a Quality of life row, an idea for later** (2026-09-27): at a
   key-item prompt, the mod asks "Show the <item>?" (Yes / No) when you have the item it takes, and says you don't
   otherwise, as the Boat Ticket's sailor does, instead of the game's list to pick from. Every key-item prompt, not
   only the permits. Its own step in the mod guide.
33. **Sprint, an idea for later** (2026-09-27; code read, not measured). Decided so far:
   - **The button:** the HUD key (key 7, the Y button's "drop down"), on the overworld only. Its only field use
     (`PlayerControl.GetInput`) shows the HUD (HP, TP, berries) for 300 frames or hides it; little is lost, since the
     HUD shows itself when the player stands still. Its uses in the pause menu, the shop list
     (`MainManager`) and one battle spot (`BattleControl`) stay. Keys can be rebound, so the mod follows the key, not "Y".
     **In battle** the key only shows or hides the EXP bar (2026-09-27, seen on screen): little lost there too,
     if a later feature ever needs a battle button.
   - **A toggle, not a hold:** each press turns the sprint on or off, so a controller needs no held button.
   - **Whoever leads, the same for all three** (only the leader is controlled; the other two follow). A sprint is
     faster walking: it stops when the stick is let go and steers like walking. The game's dash keeps moving by itself,
     stops at a wall and turns slowly (`DashBehavior` eases toward the stick by 2.5% a frame; remembered as
     hard to steer).
   - **Kabbu's Dash stays the game's ability and a gate** (flag 699, and its upgrade the Horn Dash, flag 39; Next 3,
     the Dash as an item), with everything it breaks: that is its hitbox (`tbox`, tagged `BeetleHorn`, or `BeetleDash`
     with the Horn Dash, that
     `NPCControl`, `Hornable` and `ShakeHorn` react to), which the sprint never has. The dash starts as a second tap of
     the horn slash within 15 frames (`DoActionTap`, case 1); every leader's tap and hold is taken, hence the HUD key.
   - **Sprint and dash stack:** with the sprint on, the dash goes faster too (fun, and the toggle turns it off). Speed
     comes from `PlayerControl.basespeed` (5) in `RefreshSpeed`: walking `(basespeed + friction) × 1.3`, dashing
     `basespeed × 2.5`, the submarine `basespeed / 1.8`.
   - **Poses for Vi and Leif:** looked for on screen first (a dev command stepping through `animstate` numbers), the
     normal walk made faster if none fits. Kabbu's dash poses are 116/117, but each member's numbers mean different
     poses (battle sets 116 on `playerdata[2]`).
   - **A yaml option, its own build step:** *Sprint*: Start With (default) / Shuffled (an item; the button does
     nothing until it arrives) / Off (the HUD key keeps the game's own use, for players who don't want it). The
     speeds are a panel setting (taste, no logic).

   Still open: the speed values; whether it stays on across maps, fights and scenes (proposed: on until pressed
   again); feedback on a press (proposed: the HUD's own down/up sounds); that Y is key 7 by default on a controller;
   whether the followers keep up. **Before it ships:** a faster run or dash may jump further. If it reaches even one
   location the logic thinks locked, the item is progression and that reach goes into the logic; if not, useful.
34. **Early Jump, an idea for later** (2026-09-27): *Shuffle Jump* (build step 22) becomes Off / On /
   Early, where Early puts Jump in an early sphere, since it gates the most. Archipelago may already have this built in
   (an early-items setting): check `world api.md` at the targeted tag before building one of ours.
35. **Skip cutscenes and Speed up cutscenes, two rows, an idea for later** (2026-09-27). Today one row,
   *Skip cutscenes* (`QualityOfLife.cs`, `Scenes`; the mod guide, step 10), both skips and fast-forwards (8x speed, lines
   answered, battles at normal speed), chosen per scene by what is safe. The idea splits it by what a scene is:
   - **Skip:** scenes with no mechanic or check tied to them. Off: they play as in vanilla.
   - **Speed up:** story beats, scenes that change the world, and scenes that give an item or reward. Off: they play
     as in vanilla, at normal speed.
   - **Always, with Archipelago on, whatever the two rows say:** what the randomizer needs gone, or scripted parts
     that only slow the pace: the opening (the intro slides, the new-game combat tutorial) and the scripted first
     spider fight. **Also always (2026-09-27):** every tutorial and scripted scene, and the "can't go this
     way yet" scenes that turn the player back. Those are gates, so each goes through the open world (build step 9):
     cut only in the same change that fixes its gate and logic, never on its own (2026-09-27).

   How a scene is cut (skipped or fast-forwarded) stays per scene: a Skip scene that can't be cut out safely is sped
   up instead. **A scene the player should see happen is sped up, never cut out** (2026-09-27: the bridge
   falling, the fall through the trapdoor, the spider scene; a cut looks wrong, as the trapdoor's skip showed: just a
   teleport). A scene that only talks is cut out: without it the player just walks past, as normal (example: arriving
   at Snakemouth Den). **In short:** something happens or a check = Speed up; only dialogue = Skip;
   tutorials, scripted parts and turn-backs = always. A guide, not a shortcut: every scene is still read and sorted by
   hand. **Sorted (2026-09-27):**

   | Scene | Today | Decided |
   |---|---|---|
   | The opening: slides (Event8), Maki's talk, Vi joining, the tutorial battle (Event16) | always | always |
   | The first spider fight, ended at its start (`BeforeCheckEvent`) | *Skip cutscenes* | always |
   | The bridge message (Event0) | skipped | Skip |
   | The Tattle tutorial (Event2) | skipped | always (a tutorial) |
   | The barkeeper's first talk (Event83) | skipped | Skip |
   | Arriving outside Snakemouth Den (Event11) | skipped | Skip (the skip records discovery 0 itself) |
   | The rope (Event1) | fast-forwarded | Speed up (the bridge falls) |
   | The door room's puzzle solved (Event4, drops a Mushroom) | fast-forwarded | Speed up |
   | The trapdoor (Event5) | fast-forwarded | Speed up |
   | The spider scene (Event6) | fast-forwarded | Speed up |

   The spider scene is one game coroutine, played as scene, scripted fight, scene, second fight, scene: talk, the first fight
   (Kabbu alone, `flagvar[11]` 0), talk, the second fight (two enemies, `flagvar[11]` 2, a real one), then Leif's
   part, flags 30 and 27 and discovery 1. Its first fight ends at once always; the rest is sped up. Defaults proposed:
   both rows on. Only while Archipelago is enabled, as every row. A panel setting, so its own step in the mod guide.
36. **We Owe Ya! does something from the start, an idea for later** (2026-09-27). Today the medal calls a
   random helper only from those the story or a side quest has unlocked, so received early it does nothing (a tester
   saw it; `MEASURED.md`, We Owe Ya!'s helpers). The idea: with Archipelago on, the medal picks from every helper. Build it
   by changing only the medal's pick at a battle's start, **never by setting the helpers' flags**: those are story and
   quest state. Still to decide: every helper, or a set. It changes how a seed plays, so its own step.
   Until then the game page tells players (`docs/en_Bug Fables.md`).
37. **Attack boost, a panel setting** (2026-09-28): built, shown on the medals screen (seen), `documentation.md` step 27. *Attack boost* on the
   Gameplay page, Off / +1, off by default: +1 on each hit a party member lands, an opt-in for a hard fight. No check
   and no logic depend on it. Only while Archipelago is enabled, or with *Use on normal saves*.
38. **A Graphics page, render scale and MSAA** (2026-09-28): built, seen, then removed the same day (240 to
   about 95 fps for little visible gain), `documentation.md` step 28.
39. **DeathLink** (2026-09-28): built, not yet seen, build step 25. A row in the Archipelago panel on the
   main menu, not a yaml option, so it can be switched mid-seed by going back to the main menu. **Auto-save between rooms**, its own Gameplay row, built, not yet seen
   (`documentation.md` step 31), so a death costs one room rather than a long way back.
40. **The boat to Metal Island, always there in a seed** (2026-09-28, playing vanilla): chapter 6's story
   takes the boat away (wasps attack it). In a seed the pier's boat should always be available, since the logic
   (build step 16) gates Metal Island on the Boat Ticket and the pier only. Until then the logic is less cautious than
   the game past that scene. First step: read the scene and the flag that removes the boat, and whether a seed can
   reach it. Not built; nothing changed while the tester plays vanilla.
41. **A "!" over each unrecorded discovery, with the Detector on** (2026-09-28): today the Detector puts the
   game's "!" over the leader and beeps when a room has a check left (the mod guide, step 15); this shows where. The
   "!" is the game's emoticon (`EntityControl.emoticonid`, held with `emoticoncooldown`, and `alwaysemoticon` exists).
   **Only the ones you interact with** (2026-09-28): a stone to read, a statue to look at (the pier statue,
   `HiddenEvent`, the `AncientHouseDiscovery` grass), so the player knows to walk up to it. The ones recorded by just
   being there (arriving outside Snakemouth, the fall room's scene) need none, so no "!" of the mod's own at a spot.
   **Dig spots too** (2026-09-28): an undug one holding a check. A dig spot buries an item, a crystal berry
   or an event (`MEASURED.md`, dig spots); 12 hold plain berries, no check today (one dug up on screen). **Those as checks**
   (an idea): real locations, a new kind (its own build step, likely a yaml option); first measure whether
   they come back on re-entering (no one-time flag, likely), since a check needs a lasting record the save already
   has a place for. Not built.
42. **Playing the logic with the Logic Test apworld** (2026-09-29): installed in the Archipelago checkout, its
   copy of our seed checked (`logic-test-check.py`, with the tests), and a hosted seed played through by a script
   without the game, every sphere's items arriving (`development.md`, "Play-testing the logic"). Next: the game
   connects to such a room (its data package is 2.18 MB), then a seed played through. It matters most for the
   experimental options (build steps 12 and 15).
43. **Archipelago's way, everywhere: the full review** (2026-09-29, after the rule in How it works §8). Everything
   Archipelago publishes for a world and a client, read against the project: every doc (0.6.7, diffed against
   `main`), the generic player guides, APQuest and MultiClient.Net's docs. The evidence for each item is in
   [archipelago-review.md](archipelago-review.md), same numbers. Each is a step of its own, in this order:
    - **Bugs:** 1. the shop fallback (Known issues); 2. failed connect attempts left open, one more client on the
      slot per retry (Known issues); 3. two items named "Leif" (Known issues); 4. a shop test that can't fail;
      5. respawning checks leaving the outbox before the server confirms them.
    - **Required:** 6. the door shuffle in `connect_entrances`; 7. `style.md` (brackets, a trailing blank line, long
      Markdown lines).
    - **The apworld and the website:** 8. option groups, presets, reST option texts with rich text, a bug report
      page, the WebWorld's `game`; 9. `topology_present`; 10. location and item groups; 11. `World.world_version`,
      `Region.add_locations`, `options.as_dict`; 12. `start_inventory_from_pool`; 13. the Rule Builder's
      `OptionFilter` for Jump, `__str__` and `@override` on our rules, a caching benchmark; 14. Universal Tracker and
      PopTracker; 15. slot_data only what's necessary (decided 2026-09-29: the fixed tables built into the mod from
      the apworld's data, the seed's locations from the server, a world-version check on connect).
    - **Tests:** 16. the base in `test/bases.py` and Archipelago's generic tests in CI; 17. test hygiene (no repeated
      default runs, plain `TestCase` where no multiworld is used, options written out, `assertAccessDependency`).
    - **The client:** 18. room messages shown in game: the in-game text client (Next 9), the Launcher's Text
      Client named in the player docs until then; 19. the Connect
      packet (a kept `uuid`, the right version, DeathLink's tag, hooks before connecting); 20. the rest (a refusal
      with no codes, `InvalidPacket`, the library's `SetGoalAchieved`, `AllLocations`, `GetRaceModeAsync`,
      `ColorUtils` and Analyzers, `ClientPlaying`).
    - **Docs and process:** 21. the player docs against Archipelago's own guides; 22. `development.md` (Python and a
      venv, `--log_network`, a local WebHost preview, `/send_location`, the world maintainer's duties).
    - **With the room mapping:** 23. Archipelago's entrance randomizer in place of `doors.py`: done 2026-09-30
      (build step 12), ahead of the room mapping, on one region per map.
    - **A second look at what we kept:** 24. the enemy shuffle stays in `generate_early` (reversed 2026-09-30, the
      user: it is logic, since fights that can't be fled and Tattle checks depend on it); 25. items through the library's
      queue (to check first); 26. Archipelago's DeathLink yaml option, the panel switch kept too (decided
      2026-09-29); 27. the library's cache bug: reported by the user as MultiClient.Net #143, our patch until a fix;
      28. upstream #141, which would retire our compression switch once released (#142 doesn't cover our net40 build).
    - **Kept** (Archipelago has nothing for them) **and doesn't apply** (with why): on the review page.
44. **Shuffle Bestiary: Tattle checks, an idea for later** (asked again 2026-09-29, parked since 2026-09-25 in build
   step 10): each enemy spied a location, as a Pokemon dexsanity. The bestiary has 92 entries (`librarylimit[1]`); the
   check is `librarystuff[1, id]` turning true, read as discoveries are (`location_discoveries`). **Spy, then a
   death (code read 2026-09-29):** the entry is written to memory as the Tattle text closes (`BattleControl.Tattle`
   calls `UpdateJounal(Bestiary)`, which in a battle sets `librarystuff[1, id]` at once), and `LocationChecks` runs in
   battles too, so while connected the check goes out before the fight ends and stays done whatever follows. Retry
   keeps the entry as well (`GameOver` restores flags, not the bestiary); Reload save loses it unless saved, which
   matters only if the check wasn't sent (disconnected): spy again. To measure first: what allows Spy in a battle
   (`disablespy` in the tutorial fights, a flag?), a place each enemy is always fought (following the enemy shuffle's
   `enemy_swaps`), bosses spied only in their own fight, the 23 missable ids (Event65's `excludeids`), and the
   auto-spy row (build step 10). A yaml option, so its own build step when built.
45. **Room Swap (experimental)** (2026-09-29): whole rooms trade places with rooms of as many doors, a value of the
   entrance randomizer. Built, not yet seen in game; see build step 30. Next: the user plays a seed with it; later,
   doors matched by side.
46. **Every optional feature Archipelago offers** (2026-09-30, the user: "we should try to support all available
   things archipelago has/does, that includes plando"): connection plando built, not yet seen in game (2026-09-30,
   build step 32). Next, a sweep of the
   rest, each read in Archipelago's own code and guides before it's built or written off: item plando proven on a seed,
   the options Archipelago provides for a world to add (`Options.py`), and boss plando once bosses are shuffled.

**Known issues:**

- **Shop Contents' fallback has two bugs** (found by the audit, 2026-09-29; read in the code, not yet seen in a
  seed): `rules.fall_back_from_filler_only` runs in `pre_fill`, after Archipelago has applied a player's
  `exclude_locations` and the local and non-local item rules (`Main.py`, 121 and 137-140). It assigns `item_rule`
  outright, which drops those item rules on the shops, and it sets every shop back to normal, which undoes a player's
  own exclusion of a shop. Only when Filler Only falls back (too few filler items in the room). Also: a player's
  `priority_locations` on a shop, and plando aimed at one, are dropped without a word. Next 43, item 1.
- **Failed connect attempts are left open** (found by the full review, 2026-09-29; read in the code): if reading
  slot_data fails right after a successful login (`ApConnection.cs`), that logged-in connection is neither kept nor
  closed, and the retry logs in again, so every retry adds a client on the slot. A mod and an apworld of different
  versions would set it off. Refused and timed-out attempts also leave their sockets open. Next 43, item 2.
- **Two items named "Leif"** (found by the full review, 2026-09-29; read in the code): with the story's party, the
  story event *Leif Joins* makes an event item "Leif" with no id, while the real member item "Leif" has one;
  Archipelago's `world api.md` requires one id per item name. Next 43, item 3.

- **Found by the review for build step 28 (2026-09-29), each confirmed in the code; `docs/reviewing.md` lists them for
  reviewers:**
  - **Names from the server ran as game text commands.** Player and item names went into the game's text unescaped;
    the game runs `|...|` commands inside a substituted string (`MainManager.cs:12688-12704`), among them `flag` and
    `money`, and the save's separators could break a save. **Fixed (2026-09-29, the mod guide's step 33): every server
    string goes through `ServerText`; not yet seen in game.**
  - **wss:// accepts any certificate** (websocket-sharp's default, `return true`), and a bare address falls back to
    plain ws://, password included. What to do is the user's decision: Mono in Unity may hold no root certificates, so
    checking them could turn every connection into ws://. Measure that first. **The probe is built (2026-09-29):**
    the dev build's `TlsProbe` logs what this Mono's own check decides for each wss:// server, and still accepts the
    certificate. Next: a connection to archipelago.gg with it on, run once the user says so.
  - **MultiClient.Net 6.7.1's cache path used the server's game name and checksum unsanitised** (its
    `GetFileSystemSafeFileName` returns its input). **Fixed in the mod (2026-09-29, the mod guide's step 34): two
    patches make both a plain file name; not yet seen in game.** Reported upstream by the user as MultiClient.Net
    [#143](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/issues/143) (2026-09-29; text only, never
    code from us). Our patch stays until a release carries a fix.
- **Horn rules:** written as `abilities: ["Horn Slash"]` since the horn became an item (build step 21): locations
  11, 19, 25, 30 and 32, and 31 through the Den's entrance (build step 13). Not location 2: the horn tutorial cuts its
  grass itself and played through with Leif alone (2026-09-25). **Upper Snakemouth, when it gets locations:** the big door in the door room stays shut until flag 14 (its closed
  halves stand until the trapdoor fall, `MEASURED.md`, scenery switched by flags), so its rule is the trapdoor (the
  door room's horn puzzle: the Horn) and the Peculiar Gem for the slot behind it (2026-09-27).
  Also location 19 (crystal berry #0 outside Snakemouth Den): the horn from the Outskirts' side, or the way round
  through the cave (2026-09-26; `MEASURED.md`): today it takes the Horn, more cautious than the game;
  room-level regions would add the cave.
- **Crystal berry #2 (location 20)** sits in the Underground region, which needs Leif, though the room's
  upper-left entrance needs nothing. More cautious than the game, so safe; room-level regions would split it.
- **Landmark names** for locations 2, 22, 23, 24, 25 and 30 are still to come from the tester, and the seven ability
  scenes' names (68-74, build step 23) are provisional.
- **Uncap FPS (mod guide, step 24) still speeds some things up.** Each to compare at 60 and above on screen, then
  step at the game's own rate, as the other per-frame sites are:
  - **Being hit plays too fast, for enemies and the party** (a tester, 2026-09-27, FPS unlocked). Cause not read yet;
    with the `ShakeSprite` fix below, nothing odd was seen in combat on screen (2026-09-28; no before/after seen).
  - **A frozen enemy shimmers slightly while it flies** after a knock (not interpolated while frozen; the slow motion
    and the short slide are fixed and seen, mod guide, step 24).
  - **A slight shimmer while standing on a platform or bridge** (2026-09-27, at 240). The slow motion there
    is fixed (mod guide, step 24); the party isn't interpolated while a platform carries it, so it's drawn at physics
    steps. Smoothing it relative to the platform is left for later.
  - **Bushes shaking before the leaf gang's ambush looked blurry** (2026-09-27, at 240; the swamp,
    Event128): `ShakeObject` fixed (mod guide, step 24), not yet seen. Shaky text: fixed and seen.
  - **Hits:** a character's own shake (`ShakeSprite`) was per frame and is fixed, seen without anything odd (2026-09-28); which part of a hit
    looked fast is still to be told apart on screen (the flinch pose is timed in seconds, the screen shake in physics
    steps).
  - **Other shakes still rolled every frame** (code read, 2026-09-27, not seen): a numb character's twitch (a 5% roll
    per frame, `EntityControl.Numb`), the fountain (`ObjectTypes.Geizer`, which Freeze freezes) and the crumbling platform (`NPCControl`), Heavy Strike's charge sound
    (its pitch rises per frame). Each to be compared on screen. **The screen shake is fine:** the swamp bridge's
    collapse (Event130, a `ShakeScreen`) looked normal at 240 (2026-09-27), so it's not the fast hit either.

---

# How we built it

## Build step 1: a first, tiny apworld

The apworld started deliberately tiny: two early locations, one key item (the Explorer Permit), the gate
it opens, and "open that gate" as a temporary goal. Items and locations lived in simple JSON files, so
growing the world was mostly adding data (the logic moved to Python modules, one per area, in build step 29; it is
still mostly adding data). It follows the layout of `worlds/apquest`, Archipelago's own teaching example, and writes
its rules with Archipelago's Rule Builder.

**One file per job, as APQuest does** (2026-09-27, a refactor that changed nothing a seed contains): `world.py` holds
only the `World` class, whose steps call module functions that take the world: `items.py` (the item class and the
pool), `locations.py` (the location class, which categories a seed includes, placing locations and events),
`regions.py` (regions and exits), `rules.py` (what each spot needs, the shop policy, the goal), `slot_data.py`
(everything the client reads), `web_world.py`, and the two shuffles `doors.py` (since 2026-09-30 `entrances.py`) and
`enemies.py`. `data_tables.py`
loads the JSON and names the id scheme's kinds (`ITEM_KIND` ... `MOVE_KIND`); `options.py` has the one table from a
location category to its yaml toggle (`CATEGORY_OPTIONS`). **How "changed nothing" was proven:** before the split, a
script generated 13 option sets x 2 seeds and saved each seed's regions, locations, pool, the locations each
progression item's absence locks, the fill and `slot_data`; after it, the same output byte for byte. The script was
first shown to catch a one-word rule change. Tests are split by subject too (`test_logic.py` gates, `test_slot_data.py`,
`test_pool.py`, `test_categories.py`, `test_shops.py`), and `BugFablesTestBase.state_with` builds a state holding
just the named items or events.

**Typed data** (2026-09-28, a refactor that changes nothing a seed contains, as the Pokémon Crystal apworld keeps its
data): each data file's entries become frozen dataclasses in `data_types.py`, read once in `data_tables.py`, so code
reads `item.kind` instead of `item["kind"]`, and a key a record doesn't have is refused when the world loads instead
of being silently ignored. A file's schema lives in its record's docstring. One table per commit, each proven by
`seed-snapshot.py` (identical slot_data and spoilers), the tests and the fuzzer. Done for every table: items,
encounters, doors and room starts, the client's entity lists, then regions, exits, locations, story events and
artifacts together (`rules.requires` reads exits and spots alike, through their shared `Needs` fields). The schema
strings that headed `items.json` and `locations.json` are now those docstrings. `TestDataRecords` proves an unknown
key is refused; with the check switched off, it fails. (Since build step 29 the logic's records are written in Python,
`logic/`, where a misspelt field fails by itself; the data files left are items, doors, enemies and save points.)

We wrote **tests**, including one that proves the gate really needs the permit. To make sure that test
could fail, we removed the rule on purpose, watched the test fail, and put the rule back. Archipelago's
own test suite passes for it too. Since 2026-09-28 every apworld change is also fuzzed: 10000 seeds from random
yamls (`development.md`, "Fuzzing the apworld"), which finds the option combinations no test thought of; since
2026-09-29 CI fuzzes every push too (build step 17). A change
meant to alter nothing (a refactor) is also proven with a seed snapshot: fixed seeds before and after, whose slot_data
and spoilers must come out identical (`development.md`, "Proving a refactor changed nothing").

All of those check the logic against itself. **Since 2026-09-29 the logic can be tested against the game:** the Logic
Test apworld (`development.md`, "Play-testing the logic") turns a seed into spheres played one at a time, with every
location a check you must do. Stuck in a sphere means the logic is looser than the game; a key from a later sphere
means it's stricter. It works only if its second generation of our world equals the real one, which it doesn't check
itself. So `logic-test-check.py` checks it, and runs with the tests whenever the Logic Test is in the checkout.

To try it, the world folder is linked into a local copy of Archipelago (run from source), and seeds are
generated with `Generate.py`.

**Packaging it as a `.apworld` file** (to generate with an installed Archipelago, or to share): run the
"Build APWorlds" component from the Archipelago checkout, for this world only:
`python Launcher.py "Build APWorlds" -- "Bug Fables"`. It writes `build/apworlds/bug_fables.apworld`, leaves
out `__pycache__`, and adds two fields to the file's `archipelago.json`, `version` and `compatible_version`.
Don't write those fields by hand. A hand-zipped copy made the generator warn "Invalid or missing manifest
file ... will stop working with Archipelago 0.7.0" (2026-09-24). The built copy goes in the installed
Archipelago's `custom_worlds` folder.

**Rules the world follows** (2026-09-24, matching Archipelago's own definitions in
`BaseClasses.py`, `ItemClassification`):

- **Items are classified the Archipelago way.** *Progression*: anything logic depends on (it unlocks a
  location). *Useful*: especially good to have; never placed on an excluded location. *Filler*: can be
  ignored; the only kind an excluded location gets. *Trap*: detrimental to receive; a yaml option may swap
  filler for traps.
- **The pool is each location's own vanilla item** (2026-09-24, once two locations held an HP Plus medal), then
  one of every other item, then padding for the rest (test `TestPool`, which also fails if a location's vanilla
  item is missing from `items.json`). *Padding* is a mark in `items.json` for
  the filler that may fill leftover locations in any number (a Crunchy Leaf). A filler item without it, like
  the Hard Mode medal, is a real item and goes in once (2026-09-24; test `TestMedals`). The G-Bug Ranger Plushie
  (a key item) joined as *useful* on 2026-09-24, so a test could put it on Artis's medal. Its own vanilla
  spot at the Bugaria theater isn't a location yet, so the game still hands that copy out there.
- **Which class each item gets** (2026-09-24): **if an item can unlock even one location, at any point,
  even if only sometimes or not always, it is progression. No ifs or maybes.** Every field ability is *progression*; a key item is *progression*
  when any rule in the logic uses it, even for a single location; one nothing uses is *useful*. Crystal berries
  buy medals at the crystal berry shop, so they become progression in the same change that puts that shop in
  the seed (test `TestClassifications` enforces both directions). Every medal is *useful*, except the Hard Mode medal (#11), which is *filler*: it only makes
  fights harder, and the Archipelago panel can do the same. A test will check both directions: an item a rule uses is
  progression, and a progression item is used by some rule.
- **Logic lives on regions and locations, never on items.** An item doesn't say what it unlocks. A region's
  exits say what they need (the gate out of the Outskirts needs the Explorer Permit), and every location
  belongs to a region. A location needing something more than its region adds that to itself.
- **A location is named after where it is, never after what it gives** (2026-09-24). Once items
  are shuffled, a hint like "your Hover is at Outskirts: Explorer Permit" points at the wrong thing. The
  form is `<Area>: <Room>, <Spot>` (2026-09-24): the game's own area name; a short room name from a
  landmark, left out for a one-map area; and the spot as **just a landmark**, a noun of one to three words with
  no articles or verbs, like `Snakemouth Den: Bridge Room, Pillar` ("Ledge", "Chest", "Waterfall"; a qualifier
  like "Top of Pillar" only when a room needs telling apart; two rooftops became "Rooftop" and "Fountain Rooftop",
  not "On Top of the House by the Fountain", found too descriptive, 2026-09-25). Not a sentence and not a hint at how to get it
  ("On Top of a Pillar", "Under a Rock" are too much). **When a room has more than one of that landmark, add
  `by the <Thing>`** after it, naming something a player can see next to it: `Snakemouth Den: Lake, Bush by the
  Sign` (2026-09-24: this is how to tell apart which bush, rock or pillar). It says where the spot is,
  never what to do there: the berry is inside that bush, so "Bush" is right. Gifts are `<Area>: <Who>'s Gift` or `<Who>'s Reward`,
  like `Outskirts: Maki and Eetl's Gift`. A character's name only when players will remember it (main and
  recurring ones); a minor one is described instead ("Ladybug Kid's Reward", "Ladybug Siblings' House", for
  Leby and Dib) (2026-09-24). Never the item, the flag or a mechanic ("Beemerang" goes stale once
  abilities are shuffled), Title Case, one word per kind of landmark everywhere. The test
  `TestLocationNames` fails if a location's name contains its own vanilla item's name. Renaming a location
  never changes its id or flag.

**Status:** done; the world has since grown to 66 locations (61 by default) and 52 items (counted 2026-09-27).

*Code: `apworld/bug_fables/world.py` (`BugFablesWorld`), `regions.py`, `locations.py`, `items.py`, `rules.py`, the
data in `data/items.json` (read by `data_tables.py`) and the logic in `logic/` (build step 29), tests in
`test/test_logic.py` (`TestPermitGate`).*

## Build step 2: connect the mod to a real server

We generated a seed with the tiny world, started a local Archipelago server (`MultiServer.py`), and had the
mod log in from inside the running game, using the official .NET client library
(Archipelago.MultiClient.Net).

The first try timed out. The server's own log showed what happened: the library first tried a secure
connection, which the plain local server rejected. Giving the address as `ws://…` fixed it, and the mod
logged in. This also proved the game's runtime can run the client library, which had been an open risk.

**Lesson:** when two programs talk, read the logs on *both* ends. For the same reason, a server on your own
computer is entered as `ws://127.0.0.1` with port `38281`. (The mod's default address is now
`archipelago.gg`, for hosted rooms.)

**Status:** works (2026-09-24, local server; hosted rooms on archipelago.gg since build step 5).

*Code: `mod/BugFablesAP/Core/ApConnection.cs` (`ConnectOnWorker`); the address settings in `Plugin.cs` (`Awake`).*

## Build step 3: the goal, counted in artifacts

The game shows up to 7 artifacts on the pause menu and on each save file. Reading how it draws them showed
they aren't items at all: the game counts how many of 7 story milestones you've reached. That makes a good
goal. It's cheap for the mod to check, it's real progress, and **"any N of 7"** doesn't care about order, so
it keeps working with options like a random start.

The apworld has an option, *Artifacts Required* (1 to 7). Each artifact is an **event** in the region where
the game grants it, and the goal is "have N of them". An event holds no real item; it exists so the
generator can prove the goal is reachable. The world only includes the first artifact so far, so a request
for more is lowered, with a warning, instead of producing a seed that can't be won. That rule has a test,
and so does the permit gate: remove the permit rule and two tests fail.

One rule came out of this for every later option: **every seed can be completed from wherever it starts.**
Whatever an area or the goal needs is written into the logic, and the mod never hands things out to patch
a gap.

**The mod reports the goal (2026-09-26).** It reads `artifacts_required` from `slot_data` and each frame compares it
with the game's own count, `MainManager.SaveProgressIcons()` (the seven artifact flags; `MEASURED.md`). Once the count
is reached it sends Archipelago's `StatusUpdate` with `ClientGoal`, the way `adding games.md` asks (never an event),
through MultiClient.Net 6.7.1's `StatusUpdatePacket`. It's sent once per login while reached, so a send lost with the
connection goes again at the next one, and the server keeps it. The game counts all seven flags while the logic
knows only the ones the world includes, so the mod can see the goal reached sooner than the logic proves it, never
later. The log says what it decided: `[goal] 0 of 1 artifacts`, then `[goal] sent: ...`. **Seen (2026-09-26):** beating the
spider boss (a dev file) logged `[goal] reached, 1 of 1 artifacts` and `[goal] sent`, and the server released the
slot's remaining items and logged "Team #1 has completed all of their games!".

**Status:** in progress: the goal is in the apworld, with only the first artifact so far; the mod sends "goal reached" at the required count, seen working (2026-09-26); more artifacts come with more of the world (Next 7).

*Code: `apworld/bug_fables/options.py` (`ArtifactsRequired`), `world.py` (`generate_early` lowers the
number), `locations.py` (`create_all_locations` adds the artifact events), test `TestArtifactsCapped`; the mod: `LocationChecks.CheckGoal`,
`ApConnection.SendGoal`.*

## Build step 4: connecting on its own, and staying connected

Players enter the room's address, port and slot in an Archipelago panel on the main menu. While the
Archipelago mod is enabled and those are filled in, **the mod connects by itself**, with no Connect button.
Failures are sorted into two kinds, using the refusal codes the client library reports:

- **Refused** (a wrong slot or password): the reason is shown, and nothing is retried until a detail changes.
- **Unreachable, or the connection dropped**: it retries on its own, waiting 2, 4, 8, 15, then 30 seconds.

A dropped server turned out to be invisible: an idle connection doesn't notice the other side is gone. So
while connected, the mod asks the server something tiny every 5 seconds (the documented read-only value
`_read_race_mode`), and treats 15 seconds of silence, a failed send or a socket error as a lost connection.

Tested against a local server: a wrong slot was refused and left alone; with the server stopped the mod kept
retrying, and when the server came back it connected by itself.

**A dropped connection must be closed by force.** Stopping the server while connected made the game lag and
eat memory (2026-09-24: five threads spinning, memory growing about 2.5 MB a second). The client library
keeps reading "while the socket is open", and in this game's version of .NET a dead socket still reports
itself open, so the read failed and retried forever. Asking the library to disconnect politely doesn't help,
because the goodbye can't reach a dead server. The mod now aborts the socket itself whenever a connection
is lost or replaced, which ends the loop. It also gives every connect attempt 12 seconds: the library's
login step can wait forever, and a stuck attempt had stopped all further retries. Measured after the fix
(2026-09-24): stopping the server while connected logged `socket closed: Open -> Aborted`. The game's CPU fell
back instead of climbing, its thread count went down, and its memory stayed flat. The tester's on-screen check
that the game stays smooth is still to come. When the server came back, the mod reconnected by itself
within about 6 seconds. (Since build step 5 the socket is a different library's, and the mod closes it with
that library's own close call instead. See step 5, point 5.)

**Status:** works: refusal, retry and reconnect tested on a local server, the drop measured (2026-09-24); the tester's on-screen check that the game stays smooth is still to come.

*Code: `Plugin.cs` (`AutoConnect`); `ApConnection.cs`: `ConnectOnWorker` (refused or retry),
`RetrySeconds` and `ScheduleRetry` (the waits), `Watchdog` (the 5-second ping, 15 seconds of silence, the
12-second connect deadline), `MarkLost` and `KillSocket` (closing a lost socket).*

## Build step 5: a compressed connection

The Archipelago server tells every client that doesn't compress its traffic: *"your client does not support
compressed websocket connections! It may stop working in the future."* It's only a warning today, so this
step is optional. We did it anyway, as a worked example. Here's how it goes, in the order we found things out.

**Versions we ship** (read from the project file and the DLLs, 2026-09-24): Archipelago.MultiClient.Net 6.7.1
(its net40 build), websocket-sharp 1.0.2.34775 (the copy bundled in that package's net40 folder), and
Newtonsoft.Json 11.0.1 (the netstandard2.0 copy bundled in the same package; see point 7).

**1. Find out what "compressed" means here.** Websockets have a standard compression add-on called
*permessage-deflate*. The client offers it when it connects, and the server accepts or declines. The
server's code shows it looks only for that add-on, and it's set up with one extra setting,
`server_max_window_bits=11` ([`MultiServer.py`, tag 0.6.7](https://github.com/ArchipelagoMW/Archipelago/blob/0.6.7/MultiServer.py#L57-L58)).

**2. Check what the client library can do.** The library comes in several builds, one per kind of .NET. The
build we'd used runs on .NET's own websocket, and the version of .NET inside this game has no compression
at all. The library's older builds (net35, net40) run on a different websocket library, **websocket-sharp**,
which does support compression, but nothing in Archipelago's library turns it on.

**3. Look for someone who tried first.** The library has an open pull request,
[#141](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/pull/141), doing exactly this. It warns
that websocket-sharp **refuses the server's answer when it includes `server_max_window_bits`**.
We confirmed that in both codebases. websocket-sharp accepts only two named settings in the answer, and the
server always adds the window setting. So just switching compression on would make every connection fail.
That setting only limits how the *server* compresses, and any decompressor can read it, so it's safe to
ignore.

**4. The change, in three parts:**

- Build against the library's **net40 build** instead of letting NuGet pick. The project file points at
  those DLLs by hand, and the mod ships websocket-sharp next to it.
- A **Harmony patch** on the private library method that creates the websocket switches compression on in
  the moment between creating the socket and connecting it. It also routes websocket-sharp's own error
  messages into our log, since otherwise they go only to the console.
- A second patch removes `server_max_window_bits` from the server's answer before websocket-sharp checks it.
  Every other setting is still checked as before.

Both patched methods are **private**: `ArchipelagoSocketHelper.CreateWebSocket` in the client library, and
`WebSocket.validateSecWebSocketExtensionsServerHeader` in websocket-sharp. Private methods can be renamed or
changed by any library update without warning. If either one can't be found, the mod installs neither patch
and logs `[ws] compression left off: ... not found`; the connection then works uncompressed. A changed
method that keeps its name would not be caught that way, so re-test compression after updating either
library.

*Code: `mod/BugFablesAP/BugFablesAP.csproj` (the net40 references); `WebSocketCompression.cs` (`Enable`, the
patches `AfterCreate` and `BeforeValidate`).*

**5. Mind what the new layer changes.** Swapping the websocket library is not free:

- Before every send, the client library's net40 build checks that the connection is alive
  (`webSocket.IsAlive` in `ArchipelagoSocketHelper`). In websocket-sharp, that check sends a ping and
  **waits for the answer, up to 5 seconds** (a client socket's wait time). websocket-sharp's own send doesn't
  ping; the check before it does. So the mod never sends from the game's own thread. Otherwise the game would
  stutter on every send and freeze when the server is gone.
- Closing a lost connection works differently too, so the socket fix from step 4 was redone for the new
  layer, and its drop test runs again. The mod now closes the socket with websocket-sharp's `Close`, which
  waits up to 5 seconds for the server's reply, so that happens off the game's thread too.

*Code: `ApConnection.cs`: sends run on background threads in `Watchdog` (the keepalive), `SendChecks`,
`Scout` and `Connect`; closing is `KillSocket`.*

**6. Prove it on both ends.** The mod logs the compression it agreed with the server, read back from the
socket itself (`[ap] connected over ws, compression: permessage-deflate; ...`, in
`ApConnection.ConnectOnWorker`). The server stops posting its warning.

**7. The first run failed, and not because of compression.** The handshake passed, the server logged the
connection, and then the login timed out without a word. The library reports socket errors only through an
event we hadn't been listening to yet during the connect, so the mod now logs them there too, with the
full stack. That showed `PlatformNotSupportedException` from **Newtonsoft.Json**, the JSON library. Its net40
build compiles small pieces of code at runtime, and this game's .NET can't; the BepInEx log says so at every
start (`Supports SRE: False`). The message had already been decompressed correctly by then. The fix is to
ship the net40 client library with the **netstandard2.0** Newtonsoft.Json. Both are the same version, so they
fit together. Lesson: when you swap one library build, every library that comes with it is swapped too.

**Checked (2026-09-24, local server):** it works. The mod logs in compressed, the server's warning is gone,
switching the mod off closes the connection cleanly, stopping the server is caught and leaves the game at
normal CPU and flat memory, and the mod reconnects by itself, compressed, when the server comes back. A
`Compression` setting in the config (section `Connection`, on by default, defined in `Plugin.Awake`) turns it
off if it ever misbehaves. The mod sets compression explicitly both ways, on or off, so the setting still
works if a library update starts turning compression on by itself (pull request #141 would). Checked on
both ends (2026-09-24, local server): with the setting on, the mod logged `compression: permessage-deflate…`
and the server said nothing; switched off and hot-reloaded, the mod logged `compression off (setting)` and
`compression: none`, and the server posted its warning. **A hosted room on archipelago.gg
works too (2026-09-24):** with a bare `archipelago.gg` address, the mod connected over `wss` (encrypted),
compressed, and the room's log showed no warning. The TLS worry didn't come true. The mod now logs which kind
of connection it made (`connected over wss, compression: ...`), because a bare address tries `wss://` first
and falls back to `ws://` without saying which one worked.

**Status:** works (2026-09-24): compressed on a local server and on archipelago.gg, checked on both ends.

## Build step 6: sending checks

Sending checks came before receiving items because it's easier to test: the tester can reload a save and
redo the same find as often as needed. The test location is Artis's medal, the first medal in the game,
right after the Explorer Permit. It was added to the apworld as a third location for exactly that reason.

**How the mod knows a location is done.** The game already remembers every finished event with a *flag* in
the save. The probes showed which flag belongs to which location (`MEASURED.md`). So:

- The apworld's logic (`logic/`) lists each location with its flag. The apworld sends that list to the mod
  in `slot_data` as `location_flags`. The generator stays the only source of truth: the mod only watches the
  flags of locations the seed actually has.
- Every frame, while a randomizer save is being played, the mod reads those few flags. When one becomes true,
  it sends that location's check. It only reads flags; it never changes them.
- **That per-frame read makes no garbage.** It once rebuilt its status text and every shop's sorted list each frame,
  68 KB a frame, which forced a 42 ms garbage collection every couple of seconds, a visible hitch (found 2026-09-27
  with the console's `frames`; the mod guide, step 25). The lists are built once per `slot_data` now, and the
  status text only when it changes.

**Offline play needs no extra queue.** The flags are saved with the game, so a location finished while the
server is down is found again at the next login and sent then. **But the seed must be known first:** the mod
keeps no copy of `slot_data` on disk, so until the first login of a game run it can't tell a location from any
other pickup. The file select therefore refuses randomizer files until then (2026-09-24;
`documentation.md`, step 8). A drop after that login keeps everything in force. Within a session, the client library keeps
every check the server hasn't confirmed and sends it again with the next one.

**What the log shows:** `[check] watching ...` (which locations and flags), then `[check] location ... is
done (flag N set): sending`, `[check] sent ...`, and `[check] now checked on the server: ...`.

**Tests:** the apworld checks that every location has its flag in `slot_data` (the permit's is 15, the
medal's 32), and that the world version is written in one place only (the manifest). Both fail without the
change. The world version went to 0.2.0.

*Code: `apworld/bug_fables/slot_data.py` (`build_slot_data`), test `TestSlotData`; in the mod,
`LocationChecks.cs` (`Tick`) and `ApConnection.cs` (`ReadLocationFlags`, `SendChecks`).*

**Not yet:** the game still hands out its own item at the location, the medal here. Replacing that with the
server's item is the next step (done since: the mod guide, step 9). A save from another seed would have sent its finished locations here; build step 7 ties each save to its
seed, which closed that.

**Seen working (2026-09-24, local server).** The tester loaded a save from before Artis, already past the
permit. On loading, the mod sent the permit's location at once (flag 15 was already set: the save acted as
the outbox). Talking to Artis sent the medal's location (flag 32). For both, the mod logged `sending`, the
server's confirmation and `sent`, and the server logged `BugTester sent ... (Outskirts: Explorer Permit)` and
`(Outskirts: Artis's Medal)`. (Those two were later renamed `Outskirts: Maki and Eetl's Gift` and
`Outskirts: Artis's Gift`; same ids and flags.)

**Status:** works, seen on screen (2026-09-24, local server).

## Build step 7: receiving items

Every item comes from the server, the player's own included. The server keeps a numbered list of everything
it has sent to a slot, and replays the whole list at every login. The client's job is to give each item
**exactly once**.

**The count lives in the save.** The mod keeps "how many of the server's items this save already has" in the
game's own save, so saving, loading and starting over all stay correct. A new save starts at 0 and gets
everything; an older save gets exactly what it's missing. The rules forbid a new save format, so the mod
uses a slot the game already saves but never uses. Finding one took a measurement:

- The game saves two small arrays of numbers and text for its scripts (`flagvar`, 70 numbers, and
  `flagstring`, 15 texts). Its code uses most slots, and its dialogue scripts can use any slot by number.
- A one-off dump from the running game listed every slot any of its 2,437 text files touches. Together with the
  code, that left **number slot 60 used by nothing**, and text slot 5 as well (`MEASURED.md`).
- So slot 60 holds the count, and text slot 5 holds **the seed's name**. That ties each save to its seed: a
  save from another seed neither receives items nor sends checks. That closes the gap left open in step 6.
  **Why it's needed** (2026-09-24): it's the seed that counts, not the address, so the same
  room hosted elsewhere is fine. But the count only means something within one seed, since items can be owned
  more than once and "give what's missing" can't be told from the bag. A save from another seed would skip or
  double items, and its old flags would send checks this seed never had.
- **For test files only, `AdoptSeed`** (Debug, off by default; 2026-09-24): a save tied to another
  seed is re-tied to the connected one with its count back to 0, so a new test seed doesn't mean replaying the
  opening. The old seed's items and flags stay in the save, which is why it's never for a real game.

**Only when it's safe, one item per frame:** only while the player is free (no battle, dialogue, cutscene, pause
or map change). Never during a battle, because retrying a lost battle restores the count but not key items.

**Medals** (2026-09-24; tested: Hard Mode, sent from Artis's location, arrived in the medals menu, seen on
screen) go in through the game's own `MainManager.AddBadge`,
unequipped, like any medal found. The game numbers medals separately from items, and the two ranges overlap,
so a medal's Archipelago id is offset by 1000 (`data_tables.py`, `item_id`; the mod's `ItemIds.cs`), and
`item_kinds` marks it kind 2.

**Where items go:** key items to key items, ordinary items to the bag, then storage when the bag is full. If
both are full, the item waits until there's room (items are given strictly in order, so the count stays right).
**Decided (2026-09-24), not yet built:** a full bag must never block progress. Key items keep
arriving, and only the ordinary items that don't fit are held until there's room. That needs a count that
can skip past a held item.
The same operations the game's own code uses put them there.

**Crystal berries** (2026-09-25) raise the game's berry counter, the one the pause menu shows and
Shades's shop spends. The game also keeps a *total found*, which it counts from the berry spots picked up; a
text command shows it, and 50 unlocks a logbook entry. In a seed, spots and berries are different things: a
spot sends a check, and the berry comes from the server. So the mod keeps **berries received** in number slot
69 (the last one found unused, `MEASURED.md`), and in a seed the total is that count plus berries picked up at
spots that aren't locations in this seed (all of them when the seed doesn't shuffle berries). Received berries
replay with everything else, so a fresh save rebuilds the count. A save that received berries before this
change counts only the berries it receives afterwards (test files only). Built, not yet seen in game.
Which dialogue shows the total isn't known yet; the mod logs each time the game asks for it (`[berries]`).

**Nothing given before the seed's tables are read** (an outside review, 2026-09-28). At login the connection was
published before `slot_data` was parsed, on the connection thread. A game frame in that gap saw a live session with no
item table, skipped the item as unknown and still counted it, so it was lost for good (rare: the first login of a
launch with a save already loaded). Now the session is published last, and the receiver waits while the table is
missing (`[recv] waiting: the seed's item table isn't loaded`). Built, not yet seen in game.

**Not held up by a fade that's already gone** (2026-09-28: items came "5-10+ sec" after getting control).
The receiver waited while the game reported a transition, and a dimmer fade-out reports one for its full 10 s
failsafe, long after it looks clear (`MEASURED.md`, a dimmer fade-out never finishes early). Ignoring the invisible
tail left about 870 frames (the opening's fade-in, speed 0.02, takes about 3 s to reach 2%), so giving and showing
were split: an item goes into the bag during a fade (only a map load holds it), and only its hold-up waits for the
screen (a dimmer above 25%, about 1 s into the opening's fade-in; 2% was tried first and took about 3 s; or another
transition until it ends). Items seen given 5 frames after the opening (2026-09-28, log); the 1 s box
wait was kept ("we keep it"), and the new-game start confirmed on screen the same day.

**Seen working (2026-09-24).** The server already held the Explorer Permit and the G-Bug Ranger Plushie from
the swap test. On loading, the save tied itself to the seed, and both arrived in key items as soon as the
player was free (seen on screen). Talking to Artis again showed the plushie but gave no second one: each
item comes once per seed, and the count in the save keeps it that way.

**Status:** works, seen on screen (2026-09-24): items and medals, each once; crystal berries built, not yet seen in game; the full-bag rule not built yet (Next 6); nothing given before the seed's tables are read: built 2026-09-28, not yet seen.

*Code: `mod/BugFablesAP/Items/ItemReceiver.cs`: `CountSlot` and `SeedSlot` (the two save slots),
`SaveMatchesSeed`, `Tick` (one item per frame), `Busy` (is the player free), `Give` (where each item goes).
`CrystalBerryTotal.cs`: the berries-received slot and the total. The slot survey was `VarDump.cs`.*

---

## Build step 8: logic from the game's own gates (in progress)

The logic has to tell the truth about every gate in the game, and the game has hundreds of maps. Instead of
playing through and noting each blocked path, we read the gates out of the game's own data.

1. **Every entity, with its flags.** The mod's `EntityDump` (the mod guide, step 7) lists every entity on
   every map, including each door to another map, the map it leads to, the flags it needs to exist and the
   flags that hide it.
2. **Who sets each flag.** Each flag's setters come from the decompiled code (`flags[N] = true`, and the
   event it sits in). A few are set only by dialogue lines.
3. **Which chapter that is.** Story events are numbered in story order: the chapter-end events and the
   chapter title cards both rise with the chapters. So an event's number places it in a chapter
   (`MEASURED.md`, "Chapters"; to confirm in game).
4. **The join.** `dev-scripts/gate-table.py` combines the three into one table: each gated door, its flags,
   and the chapter each flag belongs to. 59 doors turned out to depend on only 22 flags.

What it showed, for the design:

- **Some doors need an ability's flag** (dig, bubble shield). So a received ability item has to turn on the
  game's own flag, which every door and move already checks, rather than the mod faking the ability.
- **Some doors vanish later in the story.** Logic can only say "reachable from here on", never "until
  chapter N", so each of those is judged by hand: an alternate version of the same map (day and night), or
  a place that really closes, whose locations then need another way in or must not be locations.

**Gates the data can't see.** The dump shows that a map has a dig spot or a breakable rock and a pickup, but
not whether the one blocks the way to the other; and hover gaps and hazards for the bubble shield are just
level geometry, with no object at all. Decided (2026-09-24): **a cautious default, then checks
on screen.** Every location on a map with an ability's obstacle needs that ability. Each gate the tester
confirms, or rules out, in game adds or loosens a rule, and hover and bubble-shield rules come only from those
checks. Cautious logic never makes a seed impossible; it only makes placement less random until it's refined.
**Some of it is readable after all.** The bubble shield walks across one kind of hazard, `WalkableSpike`
(the game switches that hazard's collision off while the shield is up), and hazards are components on each
map's prefab. So `MapDump` in the mod reads every map prefab without instantiating it: its hazards by
type, its electric triggers, and its auto-start events (story steps the map itself starts). That gives the
bubble shield's maps from data. Hover has no object at all; pits (`Hole` hazards) are only candidates.
Obstacles for moves that are never shuffled (Kabbu's horn on grass, Vi's beemerang on switches) aren't gates.

**Where each story step starts.** `dev-scripts/event-triggers.py` looks in every place the game starts an
event: talking to an entity, trigger objects, dig spots, pickups, switches, AND gates, pressure plates, locked
doors, dialogue lines, a map's own auto-start list and literal calls in code (switches, AND gates and plates added
2026-09-28 after an audit found them missing; rerun, no gate changed). It found the start of all but one of the gate events. Two
surprises: one gate is a locked door that needs a key item (so a key item gates a whole area), and dig
spots bury items, which are locations the floor-pickup count had missed, each needing dig.

**Indoor pickups are behind a door** (found on screen, 2026-09-24). A pickup the logic had open from the start
turned out to be inside a house that opens later, and the dump hadn't kept which building interior an entity
is in. It does now (`insideid`). An indoor pickup's region is behind its building's door and that door's
flags, never just its map. The wrongly placed location was retired: its id is never reused.

**Checking locations with the tester, in game.** The data lists every pickup on a map, but not whether chapter 1
can reach it. So the dev console warps the tester next to each candidate, and the tester says whether it's reachable
and names its landmark (verdicts, 2026-09-24): the Snakemouth bridge room and lake pickups are reachable with
Vi's beemerang; the two in the mushroom pit need Leif, to freeze the water droplets; the top of Snakemouth and
the east Outskirts map are closed in chapter 1. The Snakemouth top's door has no story flag, so an ability or the
level itself blocks it. The east map's door has none either; an invisible blocker outside the city, there from
the first boss until chapter 2 starts (flag 67), is the likely reason, still to test.

**Leif and the water droplets** (2026-09-24): a room with water droplets needs Leif to freeze them, and
so does every room reached only through one. The dump marks droplets (`Dropplet` entities), so the split comes
from data: from the Snakemouth entrance, follow the doors without entering a droplet room. What that reaches
(entrance, bridge room, door room, fall room, lake) is the region *Snakemouth Den*; every other Snakemouth room
is *Snakemouth Den Underground*, whose entrance needs the story event *Leif*. Leif joins at the lake (Event14,
flag 16), which is on the open side, so the logic can't go in circles. The first boss's treasure room is
underground, so Artifact 1 needs Leif too. Story events like this one live in their area's module (`logic/`) under
`STORY_EVENTS`; a location can also have a rule of its own. Tests `TestLeif` fail without the rule.
**Three kinds of rule, kept apart** (2026-09-24, with entrance rando in mind): what it takes to *reach*
a room (on the connections into it), what a spot needs *once you're in the room* (on the location, e.g. the
mushroom pit's Gummies need Leif's ice while its medal needs nothing), and what it takes to *cross* a room from
one door to another (a connection through it; a room split by an obstacle becomes two regions). The in-room
rule is written even when the region already implies it, so a different way into the room can't lose it (test
`TestInRoomRules`).
**Into chapter 2** (a play-through, 2026-09-24): two more story events, each a region gate from the gate
table. *City Opened* (Event60, flag 107, after the first boss) opens the door from outside the city into *Bugaria
City*; *Chapter 2 Started* (Event45 at the Ant Palace, flag 67) opens the palace rooms and city districts (*Ant
Palace*, since renamed *Bugaria Inner City*: it also holds the districts). Story events can have their own
`requires` now (the city needs the first boss). Locations there: a Lore Book behind the library bookshelf (test
`TestChapterTwo`, which fails without the gate), and the old book delivery, board quest 33, whose reward is a
Lore Book (category quest; played through on screen). **Mid-quest items are shuffled too**
(2026-09-24): otherwise a quest's middle stays vanilla. The same cicada hands over the old book (Quest Book, flag
241), which becomes its own location (*Old Book Delivery Start*); the Quest Book is a progression item, and the
reward (*Old Book Delivery Reward*, flag 243) requires it (test `TestMidQuestItem`). **The quest's middle step is its
own event** (2026-09-25: book from the cicada, handed to a reader in the palace library, back for both
rewards): *Old Book Delivered* (flag 242, the library) needs the book, and both rewards need that event (test
`TestOldBookChain`), so a room-level world can't expect the rewards without the library. A step event carries its
quest's category and is left out with it (`included_events`), since without the quest's items it couldn't be reached. With Shuffle Quests off the
whole quest stays vanilla together. Still to see in game: that the recipient accepts a Quest Book received from
the server.
**Mapping connections, one-way included** (2026-09-25: for room-level regions and a later entrance
rando). An entrance shuffle can only pair a two-way door with another two-way door; a one-way link marked two-way
can strand the player. So every connection is recorded with its direction. How:
1. `dev-scripts/door-graph.py <entitydump> [map prefix]` lists every door between maps, its gating flags, and the
   doors on the target map that lead back ("NONE": a one-way candidate).
2. Play decides the rest, since the dump can't see inside a map: a drop off a ledge, a barrier opened from one
   side, a door whose other side can't be climbed back to. The tester's findings go into `MEASURED.md` as they're
   seen (the first: Snakemouth's switch-room ledges, paired doors that are one-way in play).
3. When rooms become regions, the table plus those notes become the region graph, each exit one-way or two-way.
**The general rule** (2026-09-24): if reaching something uses an ability, the logic requires that
ability. Leif is in effect the freeze ability. Some droplet rooms are optional, so this is stricter than the game,
which is the safe direction: never impossible, only less random.

**An impossible seed, and what it taught** (2026-09-25). The tester stood in the Outskirts with nothing left to
reach: the seed had put the Explorer Permit on *Outskirts: Favor Reward*, which the logic thought was open from
the start. Its entry mixed two sources. Its check (flag 17) is set by a scene on `NearSnakemouth`, past the permit
gate (Event10, 10 berries, written in the event's code), while its "30 berries on the Outskirts" came from a
ScriptDump line that belongs to an unrelated NPC who appears only much later. So a location's flag, its give and
its region must all be traced to **the same scene**, and a give written in an event's code won't show up in the
ScriptDump at all. The fix moved it past the gate as *Outskirts: Near Snakemouth Den, Reward* with its real give,
and the test `test_only_two_locations_before_the_gate` now pins what the tester knows from play: before the permit,
only Maki and Eetl's gift and Artis's gift are reachable. It fails on the old data.

Still to do: the one event not found, characters that block a path, the region graph built from all of
it, and the tests.

**Status:** in progress: the one event not found, characters that block a path, the region graph and its tests.

*Code: `dev-scripts/gate-table.py`, `dev-scripts/event-triggers.py`; the dumps in `mod/BugFablesAP/Dev/EntityDump.cs`,
`MapDump.cs` and `ScriptDump.cs`.*

---

## Build step 9: keeping the world open

The world is open by default: each gate the story would close (a blocker, a door, a guard, a story flag) is opened
by the seed on its own, from lists in `slot_data` decided at generation, tested on screen and known to the logic.
This step is those lists (`kept_open`, `kept_present`, `held_until`, `present_from` and the rest) and each gate
opened with them.

**The open start is not an option: every seed starts open** (2026-09-26: building everything twice, for a
linear and an open game, isn't worth it; open, metroidvania-like games work best in Archipelago). This replaces the
"open start" yaml option planned on 2026-09-24 (skip the prologue and tutorial, optionally with Leif from the start (the new-game party `{0, 1}`, `MainManager.cs:3591`, becoming
`{0, 1, 2}`; early cutscenes are written for two, so tested on a fresh file). A full story strip, as the Metroid
Fusion randomizer does, isn't the plan: here every cutscene also changes the world through flags.) Leif from the
start is now *Starting Party Member* (build step 13).
**Open world is the default, not an option** (2026-09-25: nobody picks a linear game in
Archipelago). The target: the world open as if the story were done, nothing collected, the ending gated by the
artifact count. **Built one gate at a time, never by forcing chapters done** (decided 2026-09-25): "chapter done"
is the artifact flag the goal counts, and a finished world is hundreds of story flags, many of which remove
locations (bosses beaten, characters gone, quests closed, cutscene gifts skipped). So each gate (a blocker, a
door, a guard, a story flag) is opened by the seed on its own, tested on screen, and known to the logic; key
items and abilities become the real gates (the Peculiar Gem for Upper Snakemouth); story events and bosses stay
as locations. The goal stays "collect N artifacts". The ending's gate is researched without spoiling it for the
tester, who hasn't finished the game. Regions stay whole areas for now; each room's areas as regions (doors from the
dump, `dev-scripts/door-graph.py`; build step 24) come as the gates open (2026-09-24; areas within a room since
2026-09-27).
**Areas and doors that close later are kept open** (2026-09-24), as Pokémon Emerald keeps Mirage
Island visible: the mod makes the game's `CheckIfCanExist` answer "exists" for a list of doors and blockers
sent in `slot_data`, decided at generation, with no save writes. Each is checked in game first; where forcing
one open breaks the story state, its locations are left out instead. **First case, built 2026-09-24:** after the
first boss, Eetl turns you back outside the city (`eetlblocker1 - Duplicate`, Event12, until chapter 2's flag
67), closing the way back to Snakemouth Den. Event12 only walks the player and sets no flags, so it's
safe to remove. Its area's module (`logic/`) lists it under `KEPT_OPEN`, `slot_data` carries it, and the mod's `KeptOpen`
gives that entity a marker `limit` array after the map creates it, which its prefix on `CheckIfCanExist`
answers with "hide" (test `TestKeptOpen`). Not yet seen in game. Day/night map pairs are made reachable
both ways (like Emerald's Shoal Cave tides). One-way drops stay as they are: the logic handles one-way
connections.
**Keeping ways present** (2026-09-25: no dead end in chapter 1, and the Gem opens chapter 5 whenever
it's found). The reverse of kept open: an area's module lists under `KEPT_PRESENT` entities the story only makes
later, `slot_data` carries them, and the mod's `KeptOpen` gives each a marker `requires` array right after the map
creates its entities, which its `CheckIfCanExist` prefix answers with "exists". First three: Snakemouth's big door
to Upper Snakemouth (its model already looks open from the trapdoor fall, flag 14, per the map dump; the Peculiar
Gem slot behind it is the real gate), and the fall room's bounce mushroom and door back up (from the first boss in
vanilla). The fall room's chapter 1 blocker joins `kept_open`. The ordinary door down into the fall room is left
alone: before the trapdoor event it would skip that event, where Leif's joining starts. Tests `TestKeptPresent`.
**Built 2026-09-25, not yet seen in game;** it needs a fresh file played through chapter 1, to see that the spider
fight and Leif's joining still play out with the way back up open.
**Seen, and a hole found** (2026-09-25): the bounce mushroom and the door at its top were there before the
first boss, and the tester went up. But then there was **no way back down**: the door room's door to the fall room
(`LoadZoneFallRoom`) also needs 41, and before the boss the fall room is reached only by the trapdoor drop. Going up
before the spider fight cut the file off from Leif, the spider fight and the lake. Keeping one direction of a
connection open means checking the other direction too. The fix, a new `slot_data` list, `present_from` (map, entity,
flag): the entity's `requires` becomes that earlier flag, so the door exists from the trapdoor (14) on. Not from the
start, which would skip the trapdoor scene where Leif's joining begins. Test `test_way_back_down_from_the_trapdoor`.
**Confirmed on screen (2026-09-25):** on a chapter 1 file before the first boss, the tester went up and down between the
fall room and the door room several times, and through the big door and back; then on to the spider fight. The
spider fight (Event6, discovery 1) and Leif's joining at the lake (Event14, flag 16) then played out as in the game,
with no errors: the ways kept open don't disturb the chapter 1 story (2026-09-25).
**The Outskirts rocks** (2026-09-25: before chapter 1 is done, a rock pile cuts the Outskirts off, so
only the way to chapter 1 is left). Everything that changes on that map at the end of chapter 1 is flag 41, read
from the entity dump and the map dump's flag-scenery list: the rocks (`Base/BlockingRocks`, scenery hidden from
41), the Golden Path exit (needs 41), NPCs swapping. The rocks guard three things: the ladybug siblings' house (no
gate of its own), the bottom-right exit (whose load zone still needs 41, so it stays a dead end) and the town
door. The town door's first visit is a trigger with no gate of its own either (Event60: the first-entry scene
into the city, which sets flag 107), and the city is written for chapter 2. So two new `slot_data` lists:
`scenery_hidden` (map plus the object's path in the map; the mod's prefix on `ConditionChecker.Start` gives it a
marker `limit`, which the existing check answers with "hide") and `held_until` (map, entity, flag; the mod adds the
flag to the entity's own `requires`, so the game keeps the trigger away until the first boss and brings it back
after). Rocks and house were chosen now, the town as a later gate of its own. The house's logic stayed cautious
(it needed the first boss) until play showed otherwise. Tests `TestOutskirtsRocks`.
**Seen (2026-09-25), chapter 1 file:** the rocks were gone, the tester walked into the house (the ladybug
siblings, who come with flag 41, weren't there) and took its item: the seed's Crystal Berry, check sent from flag 679.
So the house now needs nothing. With the rocks gone the east road opened too; there the tester knocked a stone with
Kabbu's horn and found a Drowsy Cake (flag 735), now a location (*Outskirts: East Road, Stone*), and picked
up crystal berry #10 at the pier with no abilities (*Outskirts: Pier*). The miners working
at the rocks (gone from 41 in the game) mine nothing now, so they join `kept_open`. The test
`test_only_what_play_showed_before_the_gate` pins the locations reachable before the permit (five, with the pier's
crystal berry). **The town door does nothing before the first boss** (seen on screen, 2026-09-25: "the
entrance does not work/do anything"), which is the held trigger. **The lists must reach a map already loaded:** after a
plugin reload or a seed change, a map loaded before the login is built as vanilla: the rocks were seen coming
back until the tester left and re-entered. `KeptOpen.Tick` now applies a newly arrived set of lists to the current map (the
same marks as at map load, and the scenery hidden the way `ConditionChecker.Start` hides it); the log shows it
removing the miners on the Outskirts right after a reload.
**The town waits for its companion** (2026-09-25). Tried with Leif (as asked: try the city with Leif): its first-entry
scene lines up Vi, Kabbu and Leif by character (`GetEntity(-4)`, `-5`, `-6`), so the hold moved to Leif's flag 16 for
one seed. With Leif added, the scene loaded the plaza and threw `ArgumentOutOfRange`: its fourth entry is
`GetEntity(1000)`, the map's first temporary follower, a companion who joins in Event63, the scene outside the city after
the first boss, which also sets flag 114 (`EventControl.cs:10034-10035`). Held until 114 for one seed, but that is
the first boss in practice, and the town is wanted open from the start ("especially if we are trying to make
this game openworld"). The arrival scene gives no check and its only effect is flag 107, which nothing but the city
doors reads (entity dump, map dump, ScriptDump). So **the scene is removed** (`kept_open`) and **the real door kept
present** (`kept_present`): the town is a plain door from the start, no companion needed. The plaza has no scene of
its own, and its blockers keep the party in it until chapter 2 starts. That start (Event45, the palace) lines up the
same companion, so its trigger is held until 114 (`held_until`), and in logic *Chapter 2 Start* now requires the
first boss explicitly, while the city region needs nothing and the *Entering the City* story event is gone. Tests
`test_town_open_from_the_start`, `test_the_city_is_open_from_the_start`, `test_chapter_two_needs_the_first_boss`.
**The companion in ordinary lines:** a theater NPC's line in the city also asked for the companion
(`GetEntity(1000)` inside `SetText`) and threw. The city is written for after chapter 1, so more lines will. A
fallback was chosen over closing the town again: `PartyFit` answers a follower lookup that finds nobody with the party's
leader (no crash; the companion's line comes from the leader) and logs each map and id once, so a scene that truly
needs him can be held back individually. Scenes that read `map.tempfollowers[0]` directly, like the palace's Event45,
aren't covered and stay held. **Seen (2026-09-25):** the plaza NPC's conversation played through; the log
shows the companion asked for on `BugariaMainPlaza` outside any scene and the leader answering.
**The rest of the town** (2026-09-25: "can we remove the block here"): three blockers in the plaza (`MM`,
`blockereetl2` and its duplicate, Event12, until flag 67) kept the party in the plaza. No city map has a scene that
starts on its own, and every other city scene trigger needs flag 67 or later (entity dump, map dump), so they join
`kept_open` and the districts can be walked early. The palace's own blockers stay (the story goes on there). The
districts' checks keep requiring chapter 2 in logic until they're seen working. Test `test_plaza_blockers_removed`. With the
blockers gone the exits still did nothing (seen on screen): the plaza's doors to Commercial, Residential and the theater
require flag 67 themselves, and a `Cube` in the plaza hides at 67. The three doors join `kept_present` and the cube
`scenery_hidden`. Lesson: an area closed "until chapter N" is closed by several things at once (blockers, doors,
scenery); list every entity and scenery piece gated by that flag before opening it. First find
in the open town: the Bad Book (key item 174, flag 621) outdoors in the residential district, reached with Kabbu's
horn before chapter 2 (2026-09-25): a location in the *Bugaria City* region, open from the start. Then the
Bug Me Not! medal (flag 59), also outdoors in the residential district, which needed Leif's ice: the same region,
requiring Leif (test `TestTownMedal`).
**The bar and the quest boards** (2026-09-25). The way down to the underground bar (Shades's crystal-berry
shop, and a bounty board with no gate) is a spot examined on the Commercial map whose last line, the way down, needs
flag 135. That flag also brings story characters and a scene (Event79) to the district, a battle helper and more, so
it isn't set: a new `slot_data` list, `dialogue_flags` (map, entity, flag, to), repoints that one line to flag 691,
which the new-game scene always sets. An entity picks the last line whose flag is set (`NPCControl.cs:4320-4326`), so
the way down is always taken. The town's and the Outskirts' quest boards (requires 67) join `kept_present`; each quest
still needs checking before the logic counts on it. Tests `TestBarAndBoards`. Not yet seen.
**Madeleine's house on the Outskirts, open from the start** (2026-09-25). Everything tied to it is flag 390,
set by one conversation in a later area: a locked-door check (`lockeddoor`, until 390, now `kept_open`), the real door
(`doormadeleine`, from 390, `kept_present`) and a lock (`Base/lock (1)`, `scenery_hidden`). Inside, only her and her
butler need 390, so they stay away and none of her story starts early; the rest is two plain pickups, a Burly Tea
(flag 686, the retired location 4, back under id 44) and a Lore Book (flag 392, id 45), both reachable from the start.
Test `TestMadeleinesHouse`. **Seen (2026-09-25):** walked in on a chapter 1 file and took both; the swap
showed the seed's items (a Sleep Resistance medal, a Crystal Berry) and both checks went out.
**The boat to Metal Island crashed with two in the party** (2026-09-25). With the rocks gone the tester reached the
pier on a chapter 1 file, paid the fare, and the boat scene (Event107) threw IndexOutOfRange: it seats three party
members (`p[0..2]`, `EventControl.cs:17944-17946`), and in the game the pier is behind the rocks until the first
boss, so Leif is always there. Opening a gate means checking every scene behind it for what the story guaranteed.
The sailor joins `held_until`, waiting for Leif (flag 16); Leif was chosen over the first boss, because a party
rule suits a random start later, when party members may be items and this becomes a party-size check. **Removed
(2026-09-26):** with the stand-ins (the mod guide, step 11) the boat scene asked for Vi and Kabbu, got
invisible stand-ins, and the boat left with Leif alone, the fare waived by Free boat. The sailor is always there, as in
vanilla ("before we added the bandage workaround"), and Metal Island's checks need no party member for the boat.
**The plaza's discoveries open from the start** (2026-09-25): before chapter 2's briefing (flag 67) a
stand-in (`Discovery Pre Briefing`, a Check saying "We can check this out later. Let's hurry to the castle.") stands
where the plaza statue and the inn portrait will be; the discoveries themselves (`StatueDesc`, Event38, discovery 5;
`InnPortrait`, Event37, discovery 4) require 67. Both stand-ins are kept out of the way and both discoveries kept
present (`kept_open`, `kept_present`); the two events ask for members by name and set no story flag. Test
`TestKeptOpen`. Seeds generated solo and with APQuest; **seen:** the statue can be examined.
**The inn from the start too** (2026-09-25: couldn't stay while escorted): the innkeeper's default line
(53) hands the talk to the follower, "We mustn't keep the Queen waiting."; from flag 67 line 1 offers a stay. The
line's flag is repointed to 691 (set by every new game, `dialogue_flags`, as for the bar entrance). Test
`TestKeptOpen.test_inn_open_before_the_briefing`; seeds solo and with APQuest. Not yet seen. Also found: the stay
costs a fixed 9 berries, "3 berries a bug" written for three (`checkmoney,9` then `money,-9`, line 2), whatever the
party's size.
**Chapter 2's three opening scenes held in story order** (2026-09-25: "it's the only place the follower is
removed and changed to another"): after the first boss a follower joins outside the city (Event63: follower 30, flag
114); the palace bridge swaps them for Maki (Event44: removes 30, adds Maki, flag 66), the only place follower 30
leaves; the briefing (Event45) uses Maki and clears the list. In the game the town opens only after chapter 1, so the
bridge always comes after the boss. **The briefing waits for the swap itself** (2026-09-25: a shuffled door
or a random start inside the palace could reach it without the bridge, with no follower or the wrong one): its hold
moved from 114 to 66, so first boss, follower, swap, briefing, whatever the way in. No new logic: the bridge is in the
town, which *Chapter 2 Start* already needs. With the town open from the start the bridge could come first: Maki early, and
follower 30 never leaving. The bridge's trigger (`makiautoevent`) is now held until 114, like the briefing's already
was (`held_until`). Test `TestKeptOpen.test_follower_swap_waits_for_the_first_follower`. Not seen (the tester's file is
past it).

**Every quest board lists every open quest** (2026-09-25: all boards should act the same). The game keeps
one list of open quests, but each board filters it: the five bounties show only on the underground bar's board, and
every other board hides them (`MEASURED.md`, "The quest board"). In a randomizer save the mod drops that filter, so
any board, the town's or the starting house's, offers bounties too; what the game hides everywhere (the chapter
entries, Leif's) stays hidden. The logic needs no change: it never counted on a board, only on the quest's own
region. **Next, the starting house's board from the start:** it waits for chapter 2 (flag 67), and so does its
caretaker, an Eetl in the house whose line takes the quest; whether to keep him present early or have the mod play
that line is decided after reading it in game. *Code: `QuestBoards.cs`.*

**The exits near Snakemouth Den open from the start** (2026-09-26: "go to the cave first", a turn-back
before chapter 2). On `NearSnakemouth` two Event12 triggers (`BlockLeft`, `BlockRight`, hidden by 41) guard two doors
made only from 41: `loadingzonechuck` into Chuck's Abode and `loadingzonefields`, a shortcut to the first corridor.
The triggers are `kept_open` and the doors `kept_present`; no scenery there waits on 41 (map dump). Inside Chuck's
Abode nothing waits on the story (Chuck, a save point, crystal berry #3 behind a rock). No logic change: nothing there
is a location yet. **Owed:** Chuck's quest (flag 44, the Mighty Pebble) becomes reachable from here, so when it becomes
a location its rule is this map's region plus the quest's own needs (as pointed out: take the quest
from a board, have the chef in town (by the two shops) cook a Hearty Breakfast, deliver it to Chuck here. **Decided (2026-09-26):** the rule requires the chef's cooking, even
if a Hearty Breakfast can also be found or bought (the cautious side); the cook's own gates to check in code before the rule is written). Test
`test_near_snakemouth_exits_open_before_the_boss`. **Seen (2026-09-26):** walked into Chuck's Abode before the
boss; resting and the save point work there (a dead end with a rest and a save, before the cave).

**Status:** in progress: the Outskirts rocks, the fall room both ways, the town and its districts, the plaza's companion fallback and statue, Madeleine's house, and the bar with its quest board seen on screen (2026-09-25); every board listing bounties (built 2026-09-25), Eetl's blocker, the inn, the boat's hold and chapter 2's held scenes not yet seen; the open start is always on, not an option (2026-09-26).

---

## Build step 10: more kinds of location

Beyond floor pickups and gifts, a location can be a berry reward, a crystal berry, a pickup that comes back, a story
pickup, a quest's reward or its middle, a boss's prize medal, a journal entry, or later an enemy's first defeat.
Optional kinds each get a yaml toggle that states how many checks it adds.

**Berries are shuffled like items** (2026-09-24): a berry reward is the same `giveitem` as an item, type
-1, so it's a location, and its amount goes into the pool as an item such as *10 Berries* (kind 3, its own id range;
filler). Two checks can share one flag: the delivery quest pays 15 berries and a Lore Book at flag 243, so both
are locations and are sent together. In the mod, receiving berries uses the game's own money reward (capped at
999); at a berry location the command is turned, just before it runs, into a hand-over the item swap already
handles (`BerryPrefix`). **The pool is now exactly the included locations' vanilla items** plus padding: an item
whose vanilla spot isn't a location (the Plushie at the theater) stays with the game (test `TestBerries`).
**Crystal berries** (2026-09-24: the first thing you pick up): a counted currency (`flagvar[14]`, the
crystal berry shop's counter), 50 berry spots each known by its `crystalbflags` index. A berry location's check is
that index (`location_berries`), the pickup is recognised by it (`data[0]`), and all of them hold the one item
*Crystal Berry* (kind 4). The mod undoes the count the pickup code already raised, keeps the berry's "taken" mark,
shows the seed's item (a berry is a 3D model, so the model is hidden for a sprite), and drops the first-berry
tutorial; receiving one raises the count. First location: berry #0 outside the cave (test `TestCrystalBerries`).
They're a yaml category, *Shuffle Crystal Berries*, on by default (some are obscure, like quests; test
`TestCrystalBerriesOff`).
**Respawning pickups** (2026-09-24, always shuffled, no option): some floor items have no flag of their
own, only a *regional* flag the game wipes on every area change, so they come back. They're locations too: the
first pickup sends the check and gives nothing, and once the check is done the spot is the game's own again, with
its vanilla item each time it comes back (so it stays useful locally). How it works:
1. The apworld marks such a location with `source.regional`, its regional flag, and `slot_data` sends it inside
   `location_pickups` (`"regional": N`, flag -1). The client recognises the pickup by map plus regional flag.
2. The game sets nothing that stays in the save, so the check can't be read back later like a flag. The mod sends
   it from the pickup itself: `ItemSwap`'s pickup prefix queues it, and `LocationChecks` sends the queue each tick
   while connected.
3. "Done" is the server's checked list from the last login, its updates, and the checks queued here. A pickup made
   while the connection is down waits in the queue, tagged with its save's seed, and is sent after the next login
   to that seed. The queue is only memory: if the game closes first, the spot shows the seed's item again and the
   pickup is made again. Nothing is lost and nothing is doubled.
Tests: `TestRespawningPickups` (the client gets the regional flag and no flag entry; the vanilla item is in the
pool; the logic's region) and `TestSlotData` (every location watched exactly one way). First three: chapter 1's
Snakemouth underground (a Honey Drop, a Mushroom and a Crunchy Leaf). **Seen in play (2026-09-24):**
each first pickup showed the seed's item and sent its check, and after an area change the Honey Drop came back
and gave a real Honey Drop with no check. Two are named from the tester's description (*Underground Door Room,
Pillar*; *Underground Bridge Room, Behind Pillar*, hidden from the camera); the Mushroom's landmark is still to come.
**A quest reward from the code** (2026-09-24): the lost ladybug kid at the lake gives a Lore Book once you've
beaten his monsters (Event31: `giveitem,1,52`, then flag 55, the check). He only appears after the first boss,
which is a story event of its own (*Snakemouth Den Cleared*, flag 41), and his cutscene moves all three party
members, so the location requires Leif and that event (test `TestLostKid`). Added from the code. The cutscene
also expects the kid's sister, the ladybug girl, to be following you (`FindEntity` of her character type): after
the first boss you talk to her outside the city and she comes along. Faked flags crashed it twice (no Leif, then
no sister), so it gets tested when a file reaches the first boss by play.
**Story pickups** (2026-09-24): some pickups have no "taken" flag of their own; the story makes them
appear and hides them for good (the trapdoor Mushroom in the Snakemouth door room exists between flags 13 and 14,
and taking it starts Event5, which sets 14). The rule: a pickup is a location if, once taken, a story flag hides
it for good; items that come back are not. A story pickup is known by **the story event picking it up starts**
(`source.event`, sent in `slot_data`; the pickup's `data[1]`), not by its entity name: the play-through log showed
the scene creating its own copy, `tempitem`, while the map's `MushroomItem` only appears on a later visit, and both
start Event5. Its check is the flag that event sets, and an option that skips the event can leave the location
out (test `TestStoryPickup`). For a future story strip or open world, the
mod could force such a pickup to exist (like the doors kept open) and send its check on pickup, making it
independent of the story.
**Hard Mode boss prize medals** (23, `MEASURED.md`) are always shuffled, with no option: every boss pays
its prize as if Hard Mode were on, whatever the player's setting, so a prize can never be skipped and its
location is simply "beat this boss". The mod does that by widening the Hard Mode test inside the game's
own `AddPrizeMedal`, never by writing the prize slot itself. **The Archipelago panel gets a
row, "Difficulty: Normal / Hard / Hardest",** that only adds a way in: *Hard* acts as if the Hard Mode medal
(Artis's, #11) were equipped, *Hardest* as if the save had been started with the HARDEST code (the game's
two levels, `MEASURED.md`). Equipping the medal or typing the code still works as the game made it, and
the prize medals are paid out on every setting. Logic never needs either (2026-09-24). **Only boss prizes**
(an audit, 2026-09-28): one of the 23 slots (medal 24) is a dialogue gift the game marks missed when skipped, so the
missed-prize payout skips it and the game sells it at the caravan as usual (`MEASURED.md`, Hard Mode boss prizes). For
*Hardest*: its extras read flag 614 directly in about 35 places, so it means setting that flag. **Measured
2026-09-24: the game keeps no other record of a typed code.** `flagstring[10]` is only the typing buffer,
emptied as soon as the code is accepted (`EventControl.cs:2450-2455`), so flag 614 is the code's only trace,
so the mod marks a 614 it set itself, and keeps it out of every save (chosen 2026-09-24:
switchable, the save stays clean; the mod guide's design list, item 6).
**Built 2026-09-24 (not yet seen in game):** each tick outside battles and events, a prize slot reading "missed"
(2) is paid through the game's own `AddPrizeMedal(slot)` with Hard Mode answered "yes" for that call, because
most bosses test Hard Mode in their own event and write 2 directly. Artis's `Event33` then hands the prize over
with a `giveitem` the swap handles, and the location is done when the slot reaches 3 (`location_vars`, a number
slot instead of a flag). First location: *Outskirts: Artis's Prize for Snakemouth Den* (Quick Flea, seen
for sale at the caravan and bought after a Normal kill: the missed-prize path, confirmed on screen).
**Everything in, placeholders for what isn't checked (2026-09-25):** every item, medal and other spot in the
game is added, so everything is randomized. A spot whose logic and name haven't been checked yet is a *Placeholder*:
"Placeholder" in its name, and it holds filler only (Archipelago's excluded type), so no progression item from any
game lands where the logic may be wrong. Its own vanilla item still goes into the pool and lands at a checked spot.
Each placeholder is promoted to a normal location once its requirements and name are checked, one at a time.
**Optional categories** (2026-09-24): a location can carry a `category`; its yaml option decides whether
the seed includes it. *Shuffle Quests* (on by default) covers quest-board and side-quest rewards; one-off NPC gifts
will have their own toggle. With a category off, its locations aren't created, their vanilla items stay out of the
pool, and they're left out of `slot_data`, so the client never swaps them and the game hands them out as usual
(tests `TestQuestsOff`, `TestQuestsOnByDefault`).
**Each toggle says how many checks it adds** (2026-09-25: so people know what they're getting into). The
option's description, which Archipelago copies into the yaml template, ends "Checks added in this version: N.", with
N counted from the location data when the world loads (`options.category_count`), so it never goes stale; test
`TestOptionCounts`. Seen in a generated template on 2026-09-25.
**Journal locations, each its own yaml option (2026-09-25).** The journal is `librarystuff[type, n]`,
set through `MainManager.UpdateJounal`, so a check can be "this entry became true", with no item to swap.
*Shuffle Discoveries* comes first (the simplest). **Built 2026-09-25**, opt-in (off by default): a location
source `discovery: n`, sent as `slot_data`'s `location_discoveries`, and the mod's `LocationChecks` sends the
check when `librarystuff[0, n]` turns true. No item to swap: a discovery gives none, so its pool slot is padding.
Where each discovery is came from four sources: `|discovery,N|` in map dialogue (ScriptDump), about 37
`UpdateJounal` calls in events (which event sets which), a few set from story flags in code, and each map's own
list (`MapControl.discoveryids`, MapDump). The five in regions the logic already has are locations: the pier
statue (49), the arrival outside Snakemouth (0, Event11), the spider fight (1, Event6), the bridge room's hidden
spot (2, Event13) and the Underground Door Room's grass (3, Event27). **First check seen:** the tester had examined
the pier statue before the option existed; joining the new seed, the mod found discovery 49 recorded and sent
*Outskirts: Pier, Statue*. **First discovery seen live** (2026-09-25): the tester examined the bridge room's hidden
spot, Event13 recorded discovery 2, and the check went out with the seed's item back. Tests `TestDiscoveriesOn`,
`TestDiscoveriesOffByDefault`. **Parked:** *Shuffle Bestiary* (Next 44; an entry
comes only from Spy in battle or from Event65's catch-up NPC, who sells entries for enemies already fought, 19
berries, 49 for bosses, except the 23 in `excludeids`, which are the missable ones; seeing an enemy on the map
records nothing) and *Shuffle Recipes* (each needs its ingredients, which the seed may shuffle, so it waits
until the logic knows where ingredients come from). Two Quality of life rows go with the bestiary:
auto-spy (a fought enemy counts as spied) and free entries at the catch-up NPC, perhaps folded with Free boat into
one "no NPC costs" row.
**Later idea, a yaml option (2026-09-25): enemy drops.** The first defeat of each ordinary enemy type is
a check (bosses and one-off fights left out), shown as a guaranteed drop; after that the type's drops are the
game's own, as with respawning pickups. The game already counts defeats per type in the save
(`enemyencounter[id, 1]`, raised on each win, `BattleControl.cs:30712`), so the check can be "that count reached
1", with no drop to swap. The logic needs, per type, a place where it's always fought. Another Bug Fables randomizer
reportedly does this: not looked at; its licence goes in `licensing.md` and
`references.md` is read before borrowing anything from it.
**Or per placed enemy, "enemy sanity" (2026-09-25), its own opt-in toggle:** each enemy standing on a
map (map plus entity index) is its own check on its first defeat, so the same enemy type in another room is
another check; afterwards it's the game's own again, as with respawning pickups. The entity dump holds 327 placed
enemies on 124 maps (some are one spot in different story states, swapped by flags, so fewer real spots). To
measure first: how a won battle knows which map enemy started it, and whether the mod has to keep what's done
(like respawning pickups, since nothing in the save marks a single map enemy beaten).

**Status:** in progress: respawning pickups (2026-09-24), the missed-prize path and discoveries (2026-09-25) seen on screen, crystal berry spots too (the mod guide, step 9); berries, the lost kid's reward and the prize payout not yet seen in game; Placeholders planned; bestiary, recipes and enemy checks parked.

---

## Build step 11: shops

Every medal a shop stocks, and the first purchase of each item in an item shop, is a location. The shelf shows the
seed's item, a purchase sends the check, and purchases are made permanent like checks, so no reload or spending
order can lock one away.

**Medal shops (2026-09-25), being built.** Each medal a shop stocks is a location; the shelf shows the
seed's item, buying runs the shopkeeper's `giveitem` (swapped as for a gift), and the check is the medal leaving the
stock (`badgeshops[shop]`), which the save keeps. A done location shows as sold, so a reloaded save never charges
twice. A *Shop prices: Normal / Half / Free* row (default Normal) scales the price columns. Merab's (berries) first:
berries can always be earned, so no lockout. **Shades's shop takes crystal berries, a consumable** (the
concern: consumable keys, lockout, savescumming): crystal berries are spent nowhere else (measured), and her stock
arrives in tiers whose Normal prices add up to 18, 25, 27, 40 and 50, exactly every berry in the game. **A tiered
rule (each item needs its tier's running total) was proposed and is wrong** (the question: what happens when the
stock grows): with a later tier already on the shelf, berries spent there starve an earlier tier, and a rule that
raised the need once a later tier opens would not be monotonic, which Archipelago's fill doesn't allow. **Decided
(2026-09-25): every Shades location requires all 50 crystal berries, and her full stock (all 13) is on the
shelf from a new game.** The first stops spending order from locking anything out; the second stops a story event
that never runs (as the open world skips or bypasses scenes) from leaving a tier's medals, and their checks, never
appearing. The same holds for Merab's later additions when they become locations. Crystal berries become progression. **Also wanted
(2026-09-25): Shades's counter showing 3 or 4 medals** instead of 2. The slot count is the shopkeeper's `data` length and
each slot's place its `vectordata` entry (`NPCControl.cs:1530-1534`), so longer arrays with new counter positions,
set before the shelf is built. Built: 4 on her counter (the mod guide, step 12).
**Full stock from the start for both medal shops, duplicates as their own locations** (2026-09-25: "a 2nd
copy is a 2nd check", like the delivery quest's two checks on one flag). The story adds some medals twice (Merab: TP
Plus 1 and Ambusher 86; Shades: medal 6), so each copy is a location: Merab 22 (20 medals, two doubled), Shades 13
(costing exactly 50). **The mod owns each shop's stock:** the shelf is the full list minus the copies whose checks are
done. Buying removes a copy as the game does; one copy fewer than expected marks the next undone copy done. A reloaded
save with extra copies, or the story adding stock, is trimmed back to the list (a done location shows as sold). An
offline purchase stays in the save's stock and its check goes out on reconnecting.
**Built for Merab's (2026-09-25; seen since, the mod guide, step 12):** her 12 later copies are locations *Medal Shop 11* to *22*
(ids 46-57), with 10 new medal items. "One copy fewer than expected" turned out unworkable: a fresh file holds 10 of
the 22 and would read as 12 purchases. So the save keeps a bit per copy bought (`flagvar[7]`, the mod guide, step 12; slot_data's `location_shops`
lists each copy's location with its shop and medal),
set by the purchase's swapped `giveitem`, and the stock is set from those bits and the server's checks. Test
`TestMedalShop` pins the 22 copies in story order.
**Item shops** (endless consumables): the first purchase of each item in each shop is a check that shows
and gives the seed's item, then the shop sells its own item again, like respawning pickups, so restocking still works.
Their own yaml toggle, *Shuffle Item Shops*, default on, apart from *Shuffle Medal Shops*. Built after the medal shops.
**Built for Madame Butterfly's shop (2026-09-25, seen working):** five locations (*Item Shop 1* to *5*, ids 58-62), one
per stock entry, known by map, shopkeeper and item (`location_item_shops`); each puts its own item in the pool, with
no `give` entry, so an unrelated `giveitem` of the same item on that map is never swapped. *Shop Contents* covers
them too. The buy line adds the item with `additem` (no item-get box), so the mod takes that command out when the line
is read and treats berries paid as the purchase; the check goes out through the respawning pickups' queue. Tests
`TestItemShop*`. The other shops follow the same data.
**The caravan from the start (2026-09-25, seen: all three bought, each check sent, then her own items):** its keeper `Crickerly2` kept present, its stall
(`Base/Stall`, scenery) shown through a new `scenery_present` list, and `Crickerly1`, who stands there before it,
kept away; its three items (Spicy Berry, Burly Berry, Magic Seed) are *Outskirts: Caravan, Item Shop 1* to *3*
(ids 63-65), reachable from the start. Its stock is fixed (the keeper's own data); Crickerly's later stands on other
maps are other shops. **Lines about the rocks**: every Outskirts line was searched (`line` dev command):
the waiting moth (`FuzzyMoth`, line 76) is kept away, the caravan husband's welcome (line 78) answers to flag 691
instead of 41 (line 75 was the rocks), Crickerly1 (line 74) is gone with the caravan. Gen and Eri, Artis and Eetl keep
their flag-41 lines: those are after the first boss (the river, Artis's prize, Eetl leading into chapter 2). The
Seen (2026-09-25): the moth gone, nobody mentioning the rocks any more. The
ladybug siblings (flag 41) are present from the start too, so the map doesn't feel empty; their everyday
lines are neutral and their quest lines answer to the quest's own flags. Test `TestCaravan`.
**Shop Contents** (2026-09-25: shops are many easy checks in one place and soak up the good items, as in
Tevi): a yaml choice, *Anything*, *No Progression* (default) or *Filler Only*. No Progression is an `item_rule` on each
shop location refusing progression items from any game; Filler Only is Archipelago's excluded type (no progression,
no useful, `BaseClasses.py:1502`). 36 seeds (solo, with a second game, with discoveries) all generated.
**Filler Only falls back when the room can't hold it** (2026-09-25): an excluded spot takes only an item
that is neither progression nor useful, from any game, and with Merab's 22 copies a solo seed has 17 such items, so
generation failed. In `pre_fill`, once every world's items exist, the room's excludable items are counted against its
excluded spots; if short, this world's shops take No Progression instead, with a warning naming the player. 18 seeds
(solo, with APQuest, with discoveries, each Shop Contents) generated; the fallback fired solo and with APQuest (18
filler for 22 spots), not with discoveries on (22 for 22). Tests
`TestShopContents*`. **The caravan is there from the start**, built with the item shops. **Reloads refund currency** (the tester
caught this: buy, reload, keep the check and the berries), so purchases are made **permanent like checks**: spending
is tallied on the server (per-slot storage), each save brought in line on load (crystal berries exactly: received
minus spent; ordinary berries: the save's own paid record against the server's tally, the higher wins), and a
purchase made offline is recorded in the save and queued.
**Built for Merab's (2026-09-25):** no server tally needed. The save's bits (a bit per copy) are its paid
record, and the server's checked list says what was bought anywhere. A copy the server has checked with no bit in
the save was bought in another save, so on a save tied to the seed, outside events, the mod charges its price here
(clamped at 0, the rest forgiven) and sets its bit. Crystal berries (Shades's) still want the exact count.
**Seen in the log (2026-09-25):** the tester loaded a save with 0 berries that had bought nothing, while the server held
all 22 copies' checks: each was charged (0 -> 0, forgiven) and marked paid. Forgiving can't be farmed: a save only
ever ends with fewer berries. A save that has berries losing exactly the prices isn't seen yet (same code path). A done location shows as sold. **Nothing requires a shop
bought out**: the sold-out flags (587 Merab's, 588 Shades's, set in `MainManager.cs:14283-14291`) only
change dialogue (an NPC's line 159 on the Commercial map; Shades's greeting, `checktrue,588,92`). A check that ever
depends on a bought-out shop would need the full total, 50, still safe; a seed holding fewer than 50 crystal berries
leaves the unaffordable tiers out of the pool instead. **The bar is "no action can make a check unreachable"**, not
just "the logic never asks for it" (a consumable that can lock a check away is a broken seed). Shades's
checks meet it because (1) crystal berries buy nothing but her stock, (2) the stock costs exactly 50, (3) all 50 are
always obtainable and never taken away, (4) purchases are permanent. Every berry spent buys one of her items, so what's
left always costs what's left to collect. **Shades's shop is only shuffled when the seed holds all 50 crystal
berries**; otherwise it stays vanilla. Merab's has no such risk: ordinary berries are renewable from battles.
**Confirmed (2026-09-26):** a player who turns crystal berries off doesn't want to deal with them, so
Shades's shop is then not a location at all: nothing from the seed goes there (no progression, useful or filler),
and she sells her own medals. No berry-spot events keep her shop shuffled (proposed and dropped: they would make that
player collect every berry). When her shop is built, both yaml texts say so: *Shuffle Medal Shops* that Shades's
shop joins only with *Shuffle Crystal Berries* on, and *Shuffle Crystal Berries* that turning it off leaves her
shop vanilla.

**Status:** in progress: Merab's medal shop (her full stock of 22 from a new game, seen 2026-09-25; the mod guide, step 12), Madame Butterfly's item shop and the caravan seen on screen (2026-09-25); Shades's shop not built (it waits for all 50 crystal berries as locations); the other item shops to follow.

---

## Build step 12: the entrance randomizer (experimental)

Every map-to-map door shuffled, as a yaml option labelled *experimental* until every door's logic is done. The
apworld pairs the doors and sends the result as `door_targets` in `slot_data`; the mod rewrites each door as its
map loads.

**Entrance randomizer (2026-09-25):** every map-to-map door, as an option labelled *experimental* until
every door's logic is done: until then a shuffled seed may be unfinishable (the one exception to "never
impossible", while that option is on; *Warp to start* gets the player out of a dead end). Coupled (a door and its way
back stay a pair) by default, decoupled as a choice. Order: a proof of concept (the mod rewriting a door's
destination, seen on screen), then every door, then the room-by-room logic that removes the experimental label.
**Every room's survey includes every flag it reads** (2026-09-28, after chapter 6 took the boat away,
Next 40). Not a logic step of its own: gates and roadblocks are checked with everything else in the room (its doors,
the moves it needs, its checks), because they change the logic. For each room, list every flag its objects, doors and scenes read (a thing shown, hidden or moved, a blocker
added or removed), and for each: what sets it and when, and whether a seed can reach that. A flag that can take a way
through away (a boat, a bridge, a door) is either kept from happening in a seed or becomes a rule; one that adds a
roadblock is a rule. **Look across rooms, too** (2026-09-28): a flag or a follower a room needs may come
from elsewhere, from a quest or a character who has to walk with the party from another room (the throne room needs
Maki, from two rooms away). Each such need names the room or quest it comes from, so the rule follows it there, or,
when it isn't a quest (a scene that happens to want a follower), the mod may remove the need for good so the room works
on its own. Such a removal is always on in a seed, never part of the *Skip cutscenes* setting
(2026-09-28): the logic counts on it. Decided case by case, like the rest of the logic, and fixed in the mod and the logic alike, never
at runtime. No room is done until its flags are listed.
**The proof of concept, seen (2026-09-25):** one door, then a coupled swap of two connections both ways
(the mod guide, step 13). **Every door, built (2026-09-25):** the yaml option *Entrance Randomizer (experimental)*,
*Off* (default) or *Coupled*; decoupled later (built 2026-09-30, build step 31). How it was built:
1. **A door table** (`data/doors.json`), exported by `dev-scripts/door-graph.py --export` from EntityDump: every
   door paired with its way back (the door the party arrives next to), 254 connections, 508 doors. Doors stay
   fixed when the mod couldn't tell them apart by name, when they have a story variant at the same spot, or when they
   lead into their own map; the table lists the map links those make.
2. **The shuffle** (`doors.py`, replaced by Archipelago's own on 2026-09-30, below), in `generate_early`, with the
   world's own random: the same doors joined in new
   pairs, x with y meaning x leads where y's old partner led (so you arrive next to y) and y where x's old partner
   led. Sent as `door_targets`, which the mod already applies.
3. **Every area stays reachable.** A plain random pairing strands areas: two dead-end rooms joined to each other are
   cut off. Measured: stranded in 20 of 20 seeds on the real table. So the shuffle grows the world outwards from one
   area (maps joined by fixed doors count as one): an open door of the reached part is joined to a door of an area
   not reached yet, taking an area with more doors whenever only one open door is left; the doors left at the end
   are paired at random. Tests `TestDoors*`: every way back leads back, every map is reachable, and a hub with dead
   ends is never stranded over 300 seeds (a plain random pairing strands 266 of them).
4. Six seeds generated with it on, three solo and three with APQuest. **Seen (2026-09-25), one generated
   pair both ways:** the town gate led into the desert (`DesertBeforeGH`, the log: rewritten like `DesertSouthern`'s
   left exit), and the exit there back to the start in front of the gate. The logic still assumes the vanilla doors
   (the named allowance).
5. **A dropped connection leaves the doors as the seed has them** (code: `door_targets` arrive with slot_data at login
   and are never cleared by a drop; each map's doors are rewritten from memory as it loads). **Seen
   (2026-09-25):** with the server stopped, the town gate to the desert and back several times (15 rewrites in the log
   while offline); on restarting the server the mod logged back in by itself.
6. **Found while roaming:** a shuffled door reaches areas the seed has no locations in yet, whose items are the
   game's own (the tester picked up the desert's Strong Start medal, flag 415). The Placeholders plan (every spot a
   location, unchecked ones filler-only) closes that.
7. **A trap found while roaming (2026-09-25):** in the bandit hideout's garden (`HideoutGarden`) a guard
   (`burglar`) caught the party, which starts the game's own caught scene (Event108, the log) and puts the party in
   `HideoutCell`, whose one door leads to the central room. **Getting out needs the dig ability**
   (correcting a first guess of Leif's ice: you dig under the bars in the sand). The code agrees: in the story the
   cell is where dig is learnt: Event109, the first capture, takes the beemerang (flag 11 off), sends the party to
   the cell (`LoadMap(100)`) and there sets flag 18, dig; Event108, caught again later, expects you to dig out. So
   without dig it's a dead end (the Warp button is the way out), and the hideout's garden needs dig in the logic
   once the logic follows the doors. A guard catching you is a transfer that isn't a door.
8. **Transfers that aren't doors (decided, 2026-09-25).** The game also moves the party by the dialogue
   script commands `|transfer|` and `|warp|` (`MainManager.cs:13263-13270`) and by story events (about 88 `LoadMap`
   calls in `EventControl`). **Entrances the player chooses** (the bar's hatch, elevators, the boat) are doors in all
   but name: shuffled like doors, coupled with their way back where they have one, behind their own toggle at first.
   **Places the game sends you** (caught by guards, a fall, a story scene) keep their destination: the scene expects
   to end there, they have no way back to pair with, and a destination you didn't choose is only confusing. They
   become one-way connections in the logic, which must make sure you can leave where they put you; some happen only
   at some story points, and some can strand you. **The Warp button and fast travel stay outside the logic.** First
   step: list every such transfer from the data (the script dump and `event-triggers.py`), map, trigger and target.
   **Listed (2026-09-25):** ScriptDump gained a column of the moving commands on each dialogue line (7 lines,
   all `|warp|` or `|loadmap|`), and `dev-scripts/event-transfers.py` lists each event method's `LoadMap` calls and
   targets from the decompiled code (88 calls in 63 events); `event-triggers.py` on those events says what starts
   each. It found the bar's hatch (Event61) and the hideout cell (Events 108/109) at once (`MEASURED.md`,
   "Transfers that aren't doors"). Next: sort them into chosen and forced, reading each event.

9. **Quests that cross rooms** (2026-09-25: a reward mustn't be expected when its middle steps can't be
   reached). It was safe while its steps shared one big region (the old book's residential house and the palace
   library were both *Bugaria Inner City*) or passed on the way (the lost kid's sister waits outside the city, on the
   way to Snakemouth). With doors shuffled neither holds; since 2026-09-30 each spot sits in its own map. The rule before the label comes off: **every quest
   step in another room is a logic event in that room's region** (the sister following; the library visit is done, build step 8), and the
   reward requires the whole chain; items handed out mid-quest are already progression (build step 10). Taking the
   quest is a step too, now just "reach any board" (build step 9). Known gap today: the lost kid's reward
   (location 10) doesn't require the sister's step.

**The Warp is always there with the entrance randomizer** (2026-09-26: "so we never get impossible
seeds/softlocks, even if we will check/make logic for things"). Coupled doors can always be retraced, but a one-way
transfer (a drop, a fall, a scripted move) could land the player in a pocket whose way out needs an item not yet found:
the seed stays possible, the player is stuck. The Warp to Start is that escape, shown whatever the Travel setting, as
with a random start (build step 15). The logic never counts it (build step 24, rule 9).

**Archipelago's own entrance randomizer, with the room-by-room logic** (2026-09-29, the user: "shouldn't we use
officially made things?"): `doors.py` shuffled the door table on its own because, when it was built, no door was an
entrance in the logic (it had ten large regions), so Archipelago's generic entrance randomizer (`entrance_rando.py`,
`randomize_entrances`) had nothing to shuffle. Once the rooms are regions (build step 24), every door is an `Entrance`
of its room's region, and `randomize_entrances` replaces `doors.py`: coupled mode, one-ways paired only with one-ways,
placed with the logic so the logic follows the doors, its `pairings` turned into the same `door_targets` the mod
already reads (How it works §8; Archipelago's `entrance randomization.md`).

**Rooms as regions, done first (2026-09-30; the user: "implement proper archipelago entrance rando now … whatever
Archipelago does").** The graph changed, not the rules. The rules stay what they were, moved to where the graph needs
them; room-by-room rules (build step 24) replace them later.

1. **One region per map:** Menu, then the 241 maps of the door table (`SnakemouthEmpty`, an unused room nothing leads
   into, and `TestRoom`, the debug room, left out; the user: "looked like a empty/test map", "TestRoom sounds obvious")
   and `MetalLake`,
   `TermiteColiseum2`, `BugariaEndThrone`, reached only by transfers. 244 regions, 582 entrances.
2. **Every door an entrance of its map's region**, named where it is, `"<map>: <door>"` (the naming the entrance
   randomization doc recommends), connected as the game has it: 508. The 39 fixed doors are plain entrances.
3. **The transfers that join the door graph's parts** (the doors alone split it into 10), each a `Transfer` in its
   area's module, from the decompiled events and the dumps (read 2026-09-29): the boat (`Boat Ticket`), the Beehive
   elevator, the submarine docks, the ant tunnels, the termite gate, the arena, the Roach Village lifts, the Golden Hills
   elevator, the attack on the city and the ending (one-way), and the way down to the underground bar (one-way, by
   talking to someone in the commercial district, the user). Chapters 2-7's are as cautious as those chapters. Transfers
   inside a part the doors already join wait for the room mapping.
4. **Where a door is itself the gate, its exit has the rule:** the Golden Path door (made only after the first boss)
   and the palace hall's doors to the library, the war room and the mine (chapter 2). The rule goes with the door
   wherever the shuffle sends it.
5. **What the big regions needed became each spot's `reach`:** a spot sits in its map's region, keeps its own `rule`,
   and gains `reach`, the old region's requirement (`PAST_GATE`, `DEN`, `UNDERGROUND`, `GOLDEN_PATH`, `INNER_CITY`,
   `LATER_CHAPTERS`, named in their modules). In-map gates (the Permit gate, the grass, the droplets) stay there until
   the rooms are split into areas.
6. **Proven the same with the doors off:** before the change, which spots 300 random item states reach under 7 option
   sets (the defaults, the story's party, each single member, moves and Jump shuffled, categories off and on); after it,
   all 2100 came out identical.

Tests (`test_areas.py`): every spot in a map and in its source's map, every door an entrance where the game has it,
gates and transfers naming real places, every region reachable with everything (which found the three maps nothing
leads into).

**Archipelago's entrance randomizer in place of `doors.py` (2026-09-30).** `entrances.py` replaces `doors.py`, which
is gone, its grow-outwards pass with it (Archipelago's randomizer grows the world from the start itself):

1. **In `connect_entrances`**, where the doc says it belongs: each door's entrance typed two-way and split with
   `disconnect_entrance_for_randomization`, then `randomize_entrances(world, coupled=True,
   target_group_lookup={0: [0]})`. It places the doors with the logic, so the logic now follows the doors.
2. **Its `pairings` become `door_targets`** (the doc: "used to populate slot data"): a pairing (x, y) means x now
   leads to y's side, so x leads where y's partner leads in the game and you arrive next to y. The mod is unchanged.
   A test proves the two agree: each door rewritten as `door_targets` says arrives in the map its entrance leads to in
   the region graph, so what the generator proved is what the mod does.
3. **The spoiler lists the shuffled doors** in its Entrances section (`spoiler.set_entrance` from
   `write_spoiler_header`, as The Messenger does), each pair once, both ways.
4. **The preflight's import list widened for it** (the user's call, 2026-09-29): `entrance_rando`'s two functions,
   `BaseClasses`' `Entrance` and `EntranceType`.
5. **Not now: `PlandoConnections`.** No Archipelago doc mentions it and nothing requires it (The Messenger has it as an
   extra); the review page lists it under what doesn't apply.

The experimental label stays: the rules inside rooms aren't mapped yet (build step 24), so a shuffled seed can still
put the party where the game needs more than the logic knows; the Warp stays the way out.

**Room Swap** (2026-09-29), whole rooms moved instead of single doors, is a value of the same option with a step of its
own: build step 30.

**Status:** in progress (experimental): every door, coupled, built, and a generated pair seen both ways, offline too (2026-09-25); every map a region and every door an entrance, and Archipelago's own entrance randomizer in place of `doors.py`, built, not yet seen in game (2026-09-30); next, sorting the other transfers and the room-by-room logic.

*Code: `regions.py` (every map a region, every door an entrance), `entrances.py` (the shuffles, `door_targets`, the
spoiler), `logic/` (`DOOR_RULES`, `TRANSFERS`, each spot's `reach`), `data/doors.json`; `DoorShuffle.cs` in the mod;
tests `test_doors.py`, `test_areas.py`.*

---

## Build step 13: party members and moves as items (in progress)

Field abilities, the basic moves and party members as items, each unusable until it arrives. Only a rehearsal is
built so far: a dev setting that starts the game with one member.

**Also wanted (2026-09-25): the basic moves as items**, a yaml option apart from the abilities: Vi's
beemerang, Kabbu's horn, Leif's freeze, and jumping, each unusable until its item arrives. Proposed: *Shuffle Basic
Moves* (the three field moves) and *Shuffle Jump* (its own toggle: it gates the most), both off by default. **The mod
side is small** (code read, 2026-09-25): the three moves are one method, `PlayerControl.DoActionTap`
(`PlayerControl.cs:1008`), switching on the leader's `animid` (0 Vi, 1 Kabbu, 2 Leif), so a prefix can refuse the move;
the game itself takes the beemerang away in the bandit hideout (flag 11, `case 0` checks it), so Vi without it is a
state the game already knows. The jump is its own method, `DoJump`, called from the button it shares with talking
(`PlayerControl.cs:372-392`), so it can be refused without touching talk. **The logic is the cost:** every spot that
needs a move (a ledge, a beemerang switch, grass, water to freeze) becomes a rule, seen room by room, and the start
must have checks that need none of them.
**Later idea, a yaml option (2026-09-25; off by default, confirmed 2026-09-26): party members as items.** Start with one random member and
find the other two, each its own item, on top of the abilities. **Its shape (2026-09-25, later):** *Starting
Party Member: Off / Vi / Kabbu / Leif / Random*; Off is the story's party, otherwise the game starts with that one
member and the other two are items. Prompted by the stand-ins for missing members in scenes, which make one-member play
look possible; the story's own joining scenes (Kabbu at the start, Leif at the lake) must then add nobody.
**The two joining moments become the two locations** (2026-09-25): with the option on, whoever starts, the
other two members are items, and the story has exactly two joining moments: Vi's in the opening scene and Leif's right
after the spider scene (where the mod now has him join). Both become locations whatever the start (Vi's even when you
start as Vi), named after the place, not the member ("Snakemouth Den: Fall Room, After the Spider"), so two items get
exactly two spots and no filler has to go. With the option off they're no locations; the members just join.
**Rehearsal built (2026-09-25), the mod only:** a dev setting `TestStartMember` (0 Vi, 1 Kabbu, 2 Leif) and a prefix
on `MainManager.ChangeParty(ids, fromscratch, destroyoldentity)`, which every party change of the story goes through
(the two-argument form forwards to it; about 20 calls in `EventControl`, some already one member: Kabbu alone after
the slides, Vi alone in one scene). The prefix keeps in `ids` only the starting member and those received (dev:
`addmember`), so the opening's Vi-and-Kabbu becomes the one member; it runs last so the opening skip's own prefix sees
the story's ids. **First test, Leif alone (2026-09-25):** the start and the camera right after four fixes
(the mod guide, step 11, item 1); Artis's talk (lines for Vi and Kabbu, now stand-ins in conversations too) gave the
permit; the gate scene played; the grass tutorial (Event10) played once stand-ins arrive at once, its reward sent.
**Then a real gate:** the corridor after the tutorial (`BugariaOutskirtsSnakemouthCorridor2`) needs the horn to
cross, so Snakemouth Den needs Kabbu (or the horn, once moves are items) in the logic.
**Further (2026-09-25):** the trapdoor scene, landing in the right spot once stand-ins stay where a scene
puts them; the spider fight's lead-in once stand-ins get their physics body when made; and **the spider fight itself
with Leif alone** (seen in a screenshot: Leif alone against the spider, the battle menu working). In the story that
fight is Kabbu alone at first, then Vi joins.
**The first boss and after, Leif alone (2026-09-25):** the boss scene (Event26) and the one after the bridge
(Event63) played with Leif acting the story leader's part, no error in the log; the follower joined crossing the
bridge; the way back to the cave is closed after the boss; what the seed keeps open (the caravan, the ladybug
siblings) still works; the NPCs in the starting house moved away as the story has them. Chapter 2 next.
**Decided (2026-09-25): the members present act out the missing ones' parts** in scenes, where they would
be and what they would do, instead of standing idle beside invisible stand-ins ("looks more fun"). Plan: the real
leader plays the story's leader (the first member of the party the story expects); only other missing members stay
invisible stand-ins; in a party list by member (Vi, Kabbu, Leif) the leader's own slot then gets an invisible
stand-in, so a scene moving "each member" never moves the player twice (why this was set aside at first).
Animations play by number, so the leader shows his own animation with that number: the field action is
`animstate` 100 for everyone (Vi's throw and Kabbu's horn, `PlayerControl.cs:1029`, `:1076`), so Leif acting Kabbu's
horn swing casts his ice (wanted: "Kabbu using the horn, Leif using ice"). States a character lacks show
as `Animator.GotoState: State could not be found`; the mod logs the number and maps it to the closest one. To build after the current
replay of the trapdoor and the spider fight, then replay the same scenes to compare. Opt-in only: fighting with one or two changes
the game a lot. Open questions: the story may need all three after chapter 1, and adding a member outside the
story's own event hasn't worked yet (log.md, 2026-09-24: `ChangeParty` left Leif without a character).
**Solved 2026-09-25:** without `fromscratch`, `ChangeParty`'s copy loop never runs (`for m < 0`,
`MainManager.cs:3805`), so the party list came out empty. `ChangeParty({0, 1, 2}, fromscratch: true)` rebuilds all
members (stats from defaults, then the stat bonuses reapplied), and `SetPlayers(positions)` makes their characters.
Seen on screen: Leif joined the party and fought on a file where he'd never joined (dev command `addleif`). Next measured:
the trapdoor, the spider fight and Leif's own joining scene with him already there.
**Chapter 1 scenes with Leif added early** (2026-09-25): Event2 (the Tattle tutorial, bridge room) played
fine; it moves only the first two (`GetEntity(-4)`, `(-5)`), so Leif stood still in it and was left behind until it
ended, which looked odd on screen. Scenes switch normal following off (`overridefollower`) and move only who they
name. Cosmetic fix for the open start: during a scene, a member the scene never moves walks along behind the one
ahead of him.
**The trapdoor scene broke with three** (2026-09-25): Event5 recreates the party with `SetPlayers(positions)` and a
list of two positions, and `SetPlayers` indexes it for every member: IndexOutOfRange, the scene stopped halfway, the
tester stuck in the fall room (freed with `unstick`). The mod's `PartyFit` now lengthens a short position list before
the game uses it (each extra member a step behind the last listed one), which covers every scene that places the
party this way. **Retest:** `SetPlayers` then took the lengthened list, but the scene then places each member from
**its own** two-long list (`for m < playerdata.Length`, `array[m]`, `EventControl.cs:1476-1484`), which a fix outside
the scene can't reach. Stopped there (two fixes on one scene). `unstick` now also resets the party's bodies (gravity,
physics, forced animation), which the crash had left as the scene set them.
**The scene's own list (2026-09-26):** the one read that loop makes (`array[m]`, the only `Vector3` read after
`SetPlayers` in Event5's step method, `MEASURED.md`) is swapped by a Harmony transpiler for `PartyFit.PlaceAt`: the
same value inside the list, and past its end a step behind the last listed member, as the lengthened `SetPlayers` list
has him. The loop is Event5's alone among the scenes (a search of `EventControl`). **Seen (2026-09-27):**
down the trapdoor with Leif, Vi and Kabbu, the scene ran to its end, no error; the landing spot moved to a door
arrival (the mod guide, step 11).
**Parked design (2026-09-25), for party members as items:** scenes find members by **character**
(`GetEntity(-4)` Vi, `(-5)` Kabbu, `(-6)` Leif search the party by `animid`; `-1` to `-3` are positions), so the leader's
order never matters. Two rules then: (1) a member a scene doesn't know about (Leif early in chapter 1) **steps out**
while it runs and rejoins after (the `addleif` method: `ChangeParty` with `fromscratch`, then `SetPlayers`); (2) a
scene that needs a member who isn't there (only Leif, no Vi) **waits**, held in the game and a rule in the logic for
any check it gives, like the boat. First step when this is picked up: list chapter 1's scenes by the characters they
use, from the code.
Scenes that need a particular member must then become rules. The horn tutorial near Snakemouth (Event10, a
location) is not one: the scene cuts the grass itself, and it played through with Leif alone once stand-ins
arrived at once (2026-09-25; the mod guide, step 11, item 3). The way down to Shades's shop is:
grass on the way there has to be cut with the horn (2026-09-25), so her locations will need Kabbu.

**Rules name the move, not the member (2026-09-26):** "assume horn/boomerang/ice for logic, same as
having Kabbu/Vi/Leif", so the logic already holds for a random start, one member and missing moves before any move is
an item. A spot or exit lists `abilities` (Horn, Beemerang, Ice, Jump); the world turns each into who has it today
(`ABILITY_HOLDERS` in `rules.py`: Horn Kabbu, Beemerang Vi, Ice Leif; Jump the whole party, so nothing), and only when members
are items, as for `members`. The two horn spots (25, 32) moved from `members` to `abilities`, and location 19 (crystal
berry #0 outside the den, behind grass from the Outskirts' side) got the Horn: cautious, since the cave's side needs
no horn, which room-level regions will count. Location 30 (the bridge room's hidden spot, behind grass) got the Horn too
(2026-09-26). Both sit in regions that need all three members today, so the Horn changes nothing yet; it keeps the
rule true once regions stop asking for everyone. The way into Snakemouth Den needs the Horn too (grass in the second corridor and
outside the cave), and so does the trapdoor spot (location 11: the door room's horn puzzle, 2026-09-26);
test `test_the_den_needs_the_horn`. Tests `TestAbilities`; three seeds with a random start and APQuest
generated. **Next, after the current tests:** the three attacks as items, one per member, and Jump as one
item for the whole party; then every move's spot from `MEASURED.md` (where a move is needed) written as `abilities`.

**Status:** in progress: a one-member party (Leif) seen through chapter 1 into chapter 2 (2026-09-25); *Starting Party Member* built as its own step (build step 18); the three attacks and Jump built as their own steps (21, 22); field abilities not built.

## Build step 14: enemy shuffle (in progress)

A yaml option that changes who you fight at each place. It is not enemy checks (build step 10): nothing here is a
location, only the fights move.

**Decided (2026-09-26):**
- *Enemy Shuffle*, `off / enemies_only / bosses_only / both / chaos`, **off by default**. `both` swaps enemies with
  enemies and bosses with bosses; `chaos` puts them in one pool. It is in the yaml, not the panel, so a slot plays
  the same for anyone on it.
- **Each enemy on each map gets its own fight**, fixed by the seed and sent in `slot_data`, never decided at runtime.
- **A boss's reward stays with its place:** Snakemouth Den pays Snakemouth Den's prize, whatever boss was there.
- **The fight first, the look later:** the enemy walking around the map still looks like the original for now. The
  goal is for it to match its fight ("it would feel weird to run into a seedling and then fight an octopus"), which
  is its own later piece of work.
- **The party rule:** only fights that can't be fled are limited to enemies the party guaranteed at that point can
  hit. Map fights can always be fled, so they shuffle freely. If enemy checks come, map fights count too. **Only the
  base attack counts** (2026-09-26): skills cost TP, which can run out, and items are used up, so the logic
  never relies on them; the free base attack is always there. Skills can still win a fight the logic rules out,
  which is allowed (more cautious than the game, never less).

**Measured first** (`MEASURED.md`, "Battles, for enemy shuffle"), so the design rests on the game's code:
1. Every fight goes through one function, `BattleControl.StartBattle`. A map enemy passes itself as `calledfrom`.
2. A boss's prize and story flags come from the event after any win, never from the enemy beaten.
3. The game's own rematch machine fights every listed boss on one neutral stage, which is the evidence that bosses
   can be fought outside their story event. Two story fights change their boss once it has started, so they can't
   be swapped yet.
4. The entity dump got a `battleids` column and the enemy table its own file (the mod guide, step 7). That gave
   327 map enemies on 124 maps, fights of 1 to 4 enemies, no boss on any map, and each enemy's start position (air,
   ground, underground).

**Built for `enemies_only` (2026-09-26):**
1. **The data:** `dev-scripts/enemy-table.py --export` writes `data/enemies.json` from the dump: each map enemy
   (map, entity index) and its fight, 325 of them (TestRoom left out). Generated, never edited by hand.
2. **The option:** `enemy_shuffle` (`options.py`), with only `off` and `enemies_only` for now. The other three come
   when their parts are built. An option value that does nothing would mislead.
3. **The shuffle:** in `generate_early`, `shuffle_encounters` (`enemies.py`) groups the fights by size and shuffles each group
   with the seed's random. A lone enemy stays a lone enemy, and every fight still happens exactly once, somewhere
   else. The result goes out as `slot_data` `enemy_swaps`: `{"map:entity index": [enemy ids]}`.
4. **The mod:** `EnemyShuffle.cs` puts a prefix on `BattleControl.StartBattle`. When a map enemy starts a fight, it
   looks up `map:entity index` (the entity's own `mapid` is its row in the map's table, the same index the dump
   writes) and hands the game a copy of the seed's fight. It must be a copy because the game's own `EnemyCheck`
   rewrites the array it's given. It only acts with Archipelago enabled and a seed known, and logs what it decided
   for every map fight.
5. **Tests** (`test/test_enemies.py`):
   - off gives no swaps;
   - `enemies_only` lists every map enemy;
   - sizes are kept;
   - every fight happens exactly once;
   - most fights move;
   - the same seed gives the same fights.

**The map look, first test (2026-09-26, seen on screen):** a dev console command, `enemylook <enemy id|off>`,
reloads the current map with every ordinary map enemy looking like one enemy. The mod sets each map enemy's
`animid` right after `MapControl.CreateEntities` and before the entity's own `Start`, which builds the model from
it, so the enemy sets itself up as the new character, the way the game would. The look takes that enemy's animation
set (the enemy table's column 0, the same value the rematch machine uses). With Spuder (enemy 2) on
`BugariaOutskirtsEast1`, the three map enemies looked like the spider; the fight was still the seed's. **The movement
stays the original's**: it comes from the map enemy's own row in the map data, so Spuder burrowed like the Underling
it replaced. So the real step copies an enemy's movement from a map where it appears naturally, not only its look.
**A boss from a map enemy, look and fight (2026-09-26, seen on screen):** with `enemylook 46` and
`enemyfight 46 9 0` (a second dev command: every map fight starts with those ids), the Outskirts enemies looked like
the Bee Boss, and touching one started a fight with the Bee Boss, a Seedling and a Cordyceps Ant. So a boss can be
shown and fought from a map enemy, which `chaos` needs. **Decided: the map model is the strongest enemy in
its fight**, so the player knows what they're walking into: a boss if the fight has one, else the highest base HP
(the enemy table dump). Fixed data, so the seed decides it and sends it with each fight.
**Wanted:** a Quality of life row, *Enemy movement: their own / the original's*, default their own; the
original's is there for fun ("looks fun when something does something else than the model is supposed to").

**To fix later (2026-09-26): the map movement.** A swapped look keeps the original enemy's movement
(Spuder burrowed like the Underling). Whether bosses can move around the map on their own at all is unknown; they
never do in vanilla. To measure before the real look step.

**The movement, fixed in the test (2026-09-26, seen on screen):** a map enemy's movement is its map row's
behaviours (fields 2-3), collider (11-12), radii and timers (13-21) (`MapControl.cs:1475-1497`); hovering comes from
the look itself (`CheckSpecialID` raises a flier to its minimum height). `enemylook <id> move` finds a **donor**: the
first map row, any map, whose fight starts with that enemy, and copies its movement fields onto the enemy before its
`Start`. With the Flying Seedling (donor `NearSnakemouth:7`) the Outskirts enemies walked around as they should,
not burrowing like the Underlings they replaced (seen on screen). A boss has no map row, so a boss look has no donor: its
movement is still to decide: **tested per boss later, whatever looks best** (2026-09-26). In the real step,
the seed can pick each donor at generation.

**Scene-only enemies stay out (2026-09-27):** Leif in the web (enemy 12, the spider scene's second
fight) is in no map encounter, and scene fights (`calledfrom` not a map enemy) are never swapped, so neither the fight
nor the enemy is shuffled. Test `TestSceneOnlyEnemies`.

**Next:**
- see the shuffled fights in the game;
- then bosses: each scripted fight read one by one, keyed by its event and its original ids;
- then `both` and `chaos`;
- then the map look.

**Status:** in progress: `enemies_only` built (2026-09-26), the apworld tests pass, a seed generated with a second
game (APQuest) carrying all 325 fights in `slot_data`, and **seen on screen** (2026-09-26): on
`BugariaOutskirtsEast1` an Underling + Flying Seedling map enemy started a Flying Seedling + Seedling fight, as the
seed and the log (`[enemies] BugariaOutskirtsEast1:4: 30 10 -> 10 9`) said; bosses,
`both`, `chaos` and the map look to come.

## Build step 15: starting location (experimental)

A yaml option for where a new file begins. **Experimental** (ruled 2026-09-26), like the entrance
randomizer: "random start should be fully random… random spawn is experimental just like entrance rando". The logic
still starts outside Bugaria, so a seed started elsewhere may not be finishable until the room-by-room logic exists.

**Decided (2026-09-26):**
- *Starting Location (experimental)*: `off / anywhere` for now (`towns`, and a named spot if players ask, later), off
  by default. **Not `random`:** Archipelago reserves that word for every Choice (any option can be set to random), so
  the generator refuses it as a value.
- **Fully random:** beside any save point in the game, even mid-dungeon.
- **Warp to Start goes to the seed's start**; the pause menu's map keeps fast travel to the areas you've visited.

**Built (2026-09-26):**
1. **The data:** `dev-scripts/save-points.py --export` writes `data/starts.json` from the entity dump: every save
   point (map, entity index), 72 of them (TestRoom's and duplicate copies left out). The start uses a save point's
   spot, not the save point itself, so one that only exists in some story states is still a fine spot.
2. **The option and the pick:** `starting_location`; `generate_early` picks one save point with the seed's random
   and sends it as `slot_data` `start`: `{"map", "entity"}`, or `{}` for the game's own start.
3. **The mod:** the opening's one-time transfer (the Quality of life skip's end, where a dev `TestStart` already
   warped: it only happens once per file, so no save field is spent on "started") goes beside the seed's save point,
   as Warp to Start lands (`WarpButton.SavePointSpot`). Warp to Start goes there too. The transfer hangs on the intro
   skip's end, so **a seed start always skips the intro**, whatever *Skip cutscenes* says (2026-09-26);
   the setting still governs every other scene.
4. **Tests** (`test/test_start.py`): off gives `{}`; `anywhere` gives a save point from the table; the start is fixed.

**First tries in game (2026-09-26), three failures and how each was found:**
- **Frozen, with a `NullReferenceException` in `TransferMap`.** The transfer's first step is the game's fade to black,
  and it waits on that fade's sprite; the intro skip's own fade-in started a frame later, and `PlayTransition` destroys
  the running fade's sprite. Read from the stack trace and the game's `PlayTransition`. Now, with a seed start, the
  skip sets the party but leaves the fade-in to the transfer, which waits a frame after the party is set.
- **Left at the game's start, no transfer.** The mod's "opening due" state outlived the file it belonged to (a file
  quit before its opening ran), so the next file skipped the step that books the start. Found in the log of the file
  before. The state now resets with the game's own `SetVariables` (the title screen), and the intro skip's end books
  the start as well. The start is also asked at the transfer, not before the login.
- **A glimpse of the opening map before the transfer.** The skip removed the slides' black backdrop at once; with a
  seed start it now stays until the new map has loaded, as the dev `TestStart` already did.

**Seen (2026-09-26):** a new file ended at the seed's start, Rubber Prison's cell block, the pause map
marking only Rubber Prison visited; Warp to Start went there from the Outskirts.

**A worked example: starting on Metal Island** (2026-09-26). Leaving needs nothing (the island
sailor is unchanged); coming back needs the Boat Ticket, or the Warp (the seed's start) or map travel. Nothing can be
missed, and nothing there has to be filler: the logic already gates Metal Island on the ticket, so it never expects the
island's checks before the ticket, and the ticket can't be placed behind its own gate. Starting there only gives an
early look. **The rule for any start:** it's safe while every way out of it is free and every way back in is something
the logic already gates; only a start that could be left behind for good would need its checks to be filler.

**The rule that keeps every random start valid (2026-09-26: "really important for the logic"; revised 2026-09-29):**
with a random start, **Warp to Start is always available**, whatever the Travel setting, and **the logic never counts
it** (build step 24, rules 4 and 9). The case: once the logic starts in the start room (the room-by-room logic), a way
back into the start may need an item lying in the start itself (the Boat Ticket on Metal Island); leaving without it
would strand the player, and Archipelago's logic can't model giving access up. Until 2026-09-29 the Warp closed that
case by counting in the logic. Now rule 4 does: leaving a start counts in the logic only together with what it takes
to get back in, so the logic never expects the player to leave Metal Island without the ticket, and a player who does
anyway has the Warp. The mod shows the Warp with a seed start even when Travel is Off or Map.

**No music between the menu and the start (2026-09-26: "as if I'm going from the start menu directly to a
random spawn"):** the game starts the opening map's music as a new file loads. With a seed start, the mod turns any new
music into silence while the file is still on the opening map (a prefix on `ChangeMusic`'s full overload, which every
music change reaches), and its own opening music is a fade-out instead. Seen on screen: "looks/feels instant now".

**Any room, not just save points (2026-09-26: "an actual random area ... somewhere in a dungeon"):**
- **Values:** off / towns / random as proposed, but Archipelago reserves `random`, so `anywhere` is the fully random one;
  `towns` is still to come (the save-point table, `data/starts.json`, stays for it).
- **The pool** (`ROOM_STARTS` in `data_tables.py`): every room entered through a door, both ways of each connection in
  `doors.json`. `slot_data` `start` is `{"map", "from"}`: the room, and the map whose door leads in.
- **The mod** reads the door in the `from` map that leads into the room (`QualityOfLife.DoorInto`, the dev test start's
  reader), and arrives as the game's own door transfer does: appear, then walk in. Warp to Start lands where that walk
  ends. A save-point start (`{"map", "entity"}`) still works.
- **Stuck starts are accepted while experimental:** item and entrance logic everywhere comes later.
- **Chapter 1 test**: seeds regenerated until the start was a Snakemouth Den room (seed 17,
  `SnakemouthFallRoom` from `SnakemouthDoorRoom`). **Seen (2026-09-26):** the new file arrived right where
  the trapdoor scene drops you; jumping up out of the room is one-way, and the Warp brought the party back down.

**Status:** in progress (experimental): `anywhere` (any room) works, seen on screen (2026-09-26): a new file starts in the seed's room;
`towns` and the logic from the start to come; the intro is always skipped with a seed start.

## Build step 16: the Boat Ticket

The first of the mod's own items (custom gates, "How this mod does it"), suggested on Discord. **Decided
(2026-09-26):** a progression key item, always in the pool; the pier sailor sails to Metal Island only with it, the
trip free and the ticket kept; the logic gates Metal Island on it, so Metal Island isn't open from the start; Free boat
(the Quality of life row) goes.

**Built (2026-09-26):**
1. **The item** (`CustomItems.cs`): id 200 in the game's item and sprite tables (the game's items end at 186, both
   hold 256), the Platinum Card's fields and sprite (item 176, chosen after mockups of combined icons), its
   own name "Boat Ticket" and the description "A boat ticket. Maybe we should visit the pier." (field 2, the one
   the menus show; `MEASURED.md`, "The item table's fields"). Only with Archipelago on. Seen on screen.
2. **In the pool** (`items.json`): kind 1 (key item), game id 200, progression, with `always`: it enters once in every
   seed. When every location already holds its vanilla item (the default seed has 59 for 59), one ordinary item or
   berries with a copy left makes room, picked with the seed's random; never a medal, never an item's last copy.
3. **The logic** (`logic/metal_island.py`): a Metal Island region, reached from the Outskirts (the pier) with the Boat Ticket.
   No locations there yet.
4. **The sailor** (`BoatTicket.cs`): a postfix on `MainManager.GetDialogueText` on `BugariaPier`, since every line of
   his, the first included, comes through it. His lines as approved, line by line (build step 16's
   record in Next 21's history): the offer "Would you fancy traveling to Metal Island? Show me your ticket.", the
   choice "Let's go!" / "Not yet!" with the ticket or "I lost my ticket!" without, the ticket checked where the fare
   was (lines 16 and 19), "...Ticket's in order. Hop on!" or "What?! No ticket, no trip! Get out of here!". The card
   Masters' discount (line 18) makes the same offer. English only.
5. **Free boat removed** (`QualityOfLife.cs`, `ApMenu.cs`): its fare rewrite and its row.
6. **Tests** (`test/test_boat_ticket.py`): once in the pool, progression; Metal Island unreachable without it,
   reachable with it. `TestPool` now allows the one filler copy the ticket takes. *Shop Contents: Filler Only* in a solo
   seed with discoveries on is now one filler short and falls back to No Progression, as it already did without
   discoveries; with other games' filler in the room it holds.
7. **When no duplicate is left** (the fuzzer, 2026-09-28: 1163 of 10000 seeds failed with *Shuffle Field Moves* on and
   item shops and discoveries off): the last copy of an ordinary item or berries gives way too, only then. No
   progression, useful item or medal ever does, so the fill and the logic are unchanged; the seed has a few fewer
   plain items. `TestSmallPool` (the smallest option set), then 0 of 10000 fuzzed seeds failed, and 0 of 2000 with APQuest.

**Seen (2026-09-26):** with the ticket, the sailor offered "Would you fancy traveling to / Metal Island?
Show me your ticket." (a `|line|` before the name, which wrapped mid-name at first), "Let's go!" / "Not yet!", and
"...Ticket's in order. Hop on! Our destination: Metal Island!", then the boat.

Without it (the ticket taken with the dev console's `take key 200`), the choice read "I lost my ticket!" and "Let's go!"
got the refusal; seen on screen.

**Status:** works both ways, seen on screen (2026-09-26); the pool and logic take effect in the next generated seed
(the apworld tests pass, 275).

## Build step 17: a release

Three separate downloads on a GitHub release (2026-09-25/26), made the way MeshGhost makes its TEVI release.

| Download | What it is |
|---|---|
| `bugfables-archipelago.zip` | The mod. Extract it into the Bug Fables folder, next to `Bug Fables.exe`; it holds `BepInEx/plugins/BugFablesAP/` and `README.txt`. |
| `bug_fables.apworld` | The world, for Archipelago's `custom_worlds` folder. |
| `bug_fables.yaml` | The player options template. |

**Names.** No version in any file name: the release and its tag carry it, and `releases/latest/download/<name>`
links stay the same. The apworld's name is fixed: Archipelago 0.6.7 imports the module named after the file
(`worlds/__init__.py`, `world_name = Path(apworld.path).stem`), so it must match the folder inside, `bug_fables`.
The template generator names the yaml `Bug Fables.yaml`; GitHub turns spaces in asset names into dots, so it ships
as `bug_fables.yaml`.

**The layout.** A subfolder of `BepInEx/plugins` works: BepInEx 5.4.23.5's chainloader scans `plugins` with
`SearchOption.AllDirectories` (`BepInEx/Bootstrap/TypeLoader.cs`), and its runtime resolver
(`BepInEx.Preloader/Entrypoint.cs`, `LocalResolve`) looks for a missing assembly in every subfolder of `plugins`
(`Utility.TryResolveDllAssembly`). The folder holds the four DLLs, `LICENSE.txt` (ours), `THIRD-PARTY-NOTICES.txt`
(the three libraries' MIT notices, which the NuGet package doesn't carry; `licensing.md`). The zip's top level
holds only `BepInEx/` and `README.txt`, where someone opening the zip sees it (2026-09-26: it was three
folders down at first). A plain name, though it lands next to `Bug Fables.exe` (overwriting it is fine). `build-release.ps1`
refuses any other file at the top level, in `-Check` too.
BepInEx is not bundled; the player installs it first.

**How it was built:**
1. **The mod is built locally and committed.** CI can't build it: it compiles against the game's own
   `Assembly-CSharp.dll`, which never enters the repo. `dev-scripts/build-release.ps1` builds Release and stages
   `release/mod/` (with no debug info: the pdb isn't shipped, and its path would put the build machine's folders into
   the DLL; checked with `strings`), and writes `release/built-from.txt`: each source file's git blob hash (line endings normalised, so
   a Windows and a Linux checkout agree) and each shipped DLL's SHA-256. `.gitignore` lets exactly those four DLLs in.
2. **A stale gate.** `build-release.ps1 -Check` recomputes both lists and fails if they differ. It runs when
   releasing: a job in the release workflow and `release.ps1`'s preflight. It ran on every push at first, which kept
   `main` red between releases, where a DLL older than its sources is expected; moved on 2026-09-28. Tried both ways (2026-09-26): a probe line in a `.cs` file
   failed it, naming the file; removing it passed.
   **The dev tools don't ship** (2026-09-28, reversing the 2026-09-26 choice to ship them switched off). The Release
   build leaves `Dev/` out (`BugFablesAP.csproj`: `<Compile Remove="Dev/**">` outside Debug, and `DEV` defined only
   in Debug), so the download has no console, cheats, probes or dumps, and no `[Debug]` settings. The same run
   refuses a release otherwise:
   - a `Config.Bind("Debug", ...)` outside `Dev/`;
   - a `[Debug]` setting on by default, for dev installs;
   - one of `Dev/`'s own types, or a "Dev only" text, found in the built DLL. `-Check` repeats that last check on the
     committed DLL.

   Tried both ways (2026-09-28). A probe `[Debug]` bind in a feature file failed it, naming the key and file. The first
   version of the check counted every file as in `Dev/`, because PowerShell's `-match` ignores case and the checkout
   sat under a folder named `dev`; it is case-sensitive now. The release DLL went from 389,632 to 308,224 bytes, and it
   loaded in game (`copy-dev -Layout Release`) with no dev line in the log and every feature installed.
3. **CI** (`.github/workflows/ci.yml`, every push, and called by the release): the apworld on a
   Python matrix (3.11, 3.12, 3.13, what Archipelago's own CI tests at 0.6.7). Each leg checks out Archipelago
   `0.6.7`, installs it the way Archipelago's own `unittests.yml` does (then sets `SKIP_REQUIREMENTS_UPDATE=1`: on the first
   run, 2026-09-26, two worlds' pins clashed over `typing-extensions` on Python 3.12 and 3.13, and `Launcher.py` stopped
   at a press-Enter prompt with no one to press it), runs our tests, and generates three presets
   (default, every experimental option on, every location toggle off) with APQuest as a second game. The whole suite
   takes about 3 seconds, so the matrix splits by Python version, not by test file: every job pays the install.
   A `build` job of its own (on 3.13) builds the apworld (`Launcher.py "Build APWorlds" -- "Bug Fables"`, with our
   `LICENSE` copied in) and the template (`Launcher.py "Generate Template Options" -- --skip_open_folder`), then
   generates once more the way a player would: the built `.apworld` in `custom_worlds`, the template as the yaml, no
   loose world. Until 2026-09-29 those were extra steps of the 3.13 leg, and a changed matrix would have stopped them
   without a word, until the release found no files. A `fuzz` job runs `test-apworld.ps1` as we do locally: the
   tests, the Logic Test check and 10000 fuzzed seeds, with the fuzzer and the Logic Test at pinned commits
   (`development.md`, "Fuzzing the apworld"). No one waits on CI after a push; a failure arrives by email.
4. **The release** (`.github/workflows/release.yml`, run by hand): a guard first (the version is `vX.Y.Z` and
   matches `Plugin.cs` and `world_version`; the tag is free; nothing unpublishable in the highlights or in any commit
   subject the generated notes will publish, checked by the preflight's text rules since 2026-09-29, build step 28),
   then CI, the preflight workflow and the stale gate, then the publish job zips `release/mod/BepInEx` and attaches
   the three files. The body is the highlights (changes and new features, or nothing), a section on what the code
   may do that changed since the last release, and GitHub's generated notes.
   `softprops/action-gh-release` is pinned to a commit, since it runs with write access. Since 2026-09-29 (build
   step 28) the files are checked against the commit before they are uploaded and again once published, and the
   apworld and yaml carry GitHub's provenance attestation.
5. **One command cuts it:** `dev-scripts/release.ps1 -Version v0.1.0 [-HighlightsFile notes.md]` (add `-Prerelease`
   only for a test build: a pre-release never shows as Latest, which hides it; 2026-09-26; since releases became
   immutable on 2026-09-29, a pre-release stays a draft with its files until published from the releases page). It
   refuses unless the versions match, `main` is clean and not behind, and the tag is free. Then its preflight runs
   the stale gate; a stale DLL is rebuilt and committed on the spot, and the gate runs again. Then it pushes, waits
   for CI and the preflight workflow to go green, dispatches the release and waits for it to publish. Running it
   is the go-ahead to push.
   **The highlights' format (2026-09-27, the standard from v0.2.0 on):** short one-line bullets under
   `### Features`, `### Logic` and `### Bug Fixes` (a heading left out when empty), as other Archipelago mods write
   theirs. What a player notices only: no internal fixes, and no "update both, regenerate" line (players are assumed
   to be on the latest version). A fix to the base game's own bug says so ("a base-game stutter"). GitHub adds the
   Full Changelog link below.

6. **Rehearsed before the first run (2026-09-26):** every CI step on a fresh clone of Archipelago `0.6.7`: the
   tests, the three two-game presets, Build APWorlds (its manifest gained `version` and `compatible_version` on its
   own), the template, and a seed from the built `.apworld` in `custom_worlds` with the template as the yaml (loaded
   as v0.1.0, no manifest warning). The zip, built the same way, extracted next to `Bug Fables.exe` lands only in
   `BepInEx/plugins/BugFablesAP/`.
7. **Trying the download in game:** `copy-dev.ps1 -Layout Release` swaps a dev install for exactly what the zip
   holds, and `-Layout Dev` swaps back (`development.md`, step 4 of the build-and-copy list).

**Versions.** The mod's `Plugin.Version` and the apworld's `world_version` both equal the tag without its `v`.
v0.1.0 is the first (the mod was 0.0.1 and the world 0.2.0 before). v0.2.0 is the second (2026-09-27), cut from `main` as it stood
before the tester played chapters 5 to 7, with highlights written from the commits since v0.1.0.

**Status:** works: v0.1.0 published by `release.ps1` (2026-09-26), every job green, then remade the same day from
a later `main` for the zip's top-level README and switched from pre-release to a full release (a pre-release
is hidden from Latest). The downloads fetched back and checked; the DLL is the build seen to load and connect on screen.
v0.2.0 published by `release.ps1` (2026-09-27), every job green, after a stale check of every doc against the code;
the downloads fetched back: the zip's DLL matches `built-from.txt` and reports 0.2.0, the apworld's manifest says 0.2.0.
The stale gate moved from push CI into the release workflow (2026-09-28), with `release/` rebuilt, so `main` is green
between releases; `-Check` passes locally, and the moved job first runs at the next release.
The dev tools are out of the release build (2026-09-28): the gate checks it, and the Release DLL ran in game with none.

## Build step 18: Starting Party Member

The first part of build step 13 made real: a yaml option that starts a new file with one party member and makes the
other two items. Its design (2026-09-25) and the rehearsal behind it are in build step 13; this is the
option itself. **Decided (2026-09-26): cautious logic.** The gates measured so far become rules, and
everything past the Outskirts gate needs all three members until the rooms there are measured.

**Built (2026-09-26):**
1. **The option** (`options.py`): *Starting Party Member: Off / Vi / Kabbu / Leif / Random Member*, Off by default.
   Not "Random": Archipelago reserves that word, and it would pick Off too. The seed picks a random member in
   `generate_early`, with its own random.
2. **The items** (`items.json`): Vi, Kabbu and Leif, a new kind 5 (game id 0, 1, 2; Archipelago id base + 4000 + the
   member), progression. With the option on, the starting member is start inventory (`push_precollected`), which the
   server sends the client like any item, and the other two go in the pool. With it off, none of them exists.
3. **Two locations, whoever starts** (`logic/`, category `party_member`, only with the option on): *Outskirts:
   Outside the City, Opening* (flag 15, where Vi joins in the opening) and *Snakemouth Den: Fall Room, After the Spider*
   (flag 27, where the mod has Leif join). Two items, two spots, no filler removed. The story's "Leif Joins" event
   (category `story_party`) exists only with the option off, so Leif isn't handed out for free.
4. **The rules** (`logic/`, `Member` rules, which count only with the option on): the way past the Outskirts gate
   needs Vi, Kabbu and Leif (on top of the permit), which covers the measured gates past it (the horn corridor needs
   Kabbu, the first boss needs Vi); *East Road, Stone* and *Residential District, Rooftop* need Kabbu (his horn). The
   Leif rules that already existed (droplets, the fountain rooftop) now need the item instead of the story's event.
5. **slot_data** `starting_member`: -1 for Off, else 0 Vi, 1 Kabbu, 2 Leif.
6. **The mod** (`ApConnection.cs`, `PartyMembers.cs`, `ItemReceiver.cs`, `ItemSwap.cs`): the seed's
   `starting_member` replaces the dev `TestStartMember` for the member guard (the mod guide, step 11), so the opening's
   Vi and Kabbu become the one member. A received member (kind 5) joins on the spot, the way the dev `addmember` does;
   the members allowed are the start plus those among the items this save has counted (`flagvar[60]`), recomputed
   every frame and cleared on the title screen, so one file's members never carry into another. A hold-up for a
   member shows the pause menu's party icon (`guisprites[94 + member]`) in the member's colour (`charcolor`). An item
   kind the mod doesn't know is now skipped with a log line; before, it went into the bag as an ordinary item.
   **The opening is always skipped with a starting member (2026-09-26),** whatever *Skip cutscenes* says: the
   opening's tutorial battle was written for two and was never played with one. Then made the rule for every
   Archipelago file (the mod guide, step 10, item 5).
   **Fixed after the first play (2026-09-26):** the rooftop pickup showed Vi's icon far too large and the
   Crunchy Leaf's description (a member's number read as item 0). The icon is now scaled to an item sprite's size, a
   member gets no description box (the game has no item row for him), and no article: "You got Vi!", the line's
   space after the article dropped for that one line (`MEASURED.md`, the found-item line). Seen in a hold-up.
7. **Tests** (`test/test_party.py`): Off adds nothing; each start is start inventory with the other two in the pool;
   both locations exist with flags 15 and 27; the gate needs all three; the horn spots need Kabbu; a random start is one
   of the three. `TestClassifications` counts `members` too. Generated with APQuest for all four choices: every seed
   finished generating (Leif start: Kabbu on the Pier, Vi on the Fountain Rooftop, which needs Leif).

**Known before the first play:** with all three needed before Snakemouth Den, the trapdoor scene (Event5) always runs
with three members, and it broke with three before (build step 13: its own two-long position list). A three-member
spider scene is also still open (the mod guide, step 11). Both come up in the first play past the gate.
The trapdoor's own list is now patched (build step 13, 2026-09-26). Test seed for the first play: a Leif start, Vi and
Kabbu on the two opening locations, the permit on Madeleine's table (plando).
**Vi joined with no box (2026-09-26):** your own item gets no arrival hold-up, since the location's own scene
shows it; Kabbu's did (the opening's gift line, swapped). But the two joining moments are only a story flag and show
nothing, so Vi arrived silently. `slot_data` now lists `silent_locations` (every location whose source is only an event
and a flag: exactly these two), and the mod's receiver gives your own item from one of them the usual hold-up, as it
does another player's. Tested (`test_joining_moments_are_silent`). **Seen (2026-09-26, a new seed):** both boxes, Kabbu's then
Vi's. Vi already stood in the party during Kabbu's box (a member joins on arrival, the box waits its turn); left as
it is (the order doesn't matter, the player can't act in between).

**Seen (2026-09-26), a Kabbu start:** the opening left Kabbu alone and sent its check; Artis's gift was Leif,
who joined on the spot; the Fountain Rooftop held Vi, who joined too (party 1, 2, 0). Then each member's hold-up: "You got
Vi / Kabbu / Leif from TestPlayer!", item-sized in the starburst, each in his colour, no description. A member lying on
the ground at the new size, seen too (dev `spawn member`, a screenshot of Vi on the grass).

**Seen (2026-09-26), a Vi start with a second player:** Vi alone after the opening; Kabbu and Leif both
arrived from the other player's chests and joined (party 0, 1, 2).

**A story-party seed started Leif alone (2026-09-27):** the mod took the dev `TestStartMember` (left at 2)
whenever the seed's `starting_member` was -1, so a seed with the option off still used it. Now a seed that sends the key
decides, -1 included, and the dev setting only stands in with no seed (`PartyMembers.SeedSaysMember`). **Seen (the
same day):** a new file on that seed started in the town plaza with Kabbu and Vi. Warped to the fall room
(dev), the spider scene at speed, the first fight ended at once, and Leif joined after it, as the story has him.

**Status:** works, seen on screen: before the Outskirts gate with a Kabbu start and a Vi start, members arriving from
this world and from another player (2026-09-26); past the gate with a Leif start, the trapdoor and spider scenes with
all three, Leif back in the party after (2026-09-27).

## Build step 19: Archipelago's colours for players and items

Archipelago's own clients colour a message the same way everywhere, so players read "whose, and how important" at a
glance. This mod follows that standard in the game's item boxes (2026-09-26: it follows the expected
Archipelago colours, so it belongs here as well as in the mod guide).

**The standard** (your Archipelago checkout, `NetUtils.py`, `JSONtoTextParser`, and `data/client.kv`, read 2026-09-26):
- a player's name: magenta `EE00EE` when it's you, yellow `FAFAD2` for anyone else (`_handle_player_id`);
- an item by its flags (`_handle_item_name`): progression (`0b001`) plum `AF99EF`, useful (`0b010`) slate blue
  `6D8BE8`, trap (`0b100`) salmon `FA8072`, anything else (filler) cyan `00EEEE`;
- a location: green `00FF7F`.
The flags come with every item the client sees (`NetworkItem.flags`; MultiClient.Net's `ItemInfo.Flags`, and a
scouted item's), so the colour needs no table of our own.

**Built (2026-09-26):**
1. **The same meaning, darkened.** Those colours are picked for a dark window; the game's text box is near white, and
   pale yellow, plum, slate blue and cyan faded on it (three rounds on screen with the tester). Used: another player
   dark yellow `B8860B`, progression dark plum `8A63D2`, useful dark slate blue `4A6BD8`, filler dark cyan `008B8B`,
   trap `E9573F` (salmon was too pale; red would read as the game's own red item names). Your own name never shows:
   only another player's item gets a "from" or "'s".
2. **Where:** "You got <item> from <player>!" for an item another player found for you, and "You found <player>'s
   <item>!" for another player's item found here (a gift or a pickup). Your own finds keep the game's red.
3. **A choice:** the Quality of life row *Item colors: Rarity / Archipelago / Off* (the mod guide, steps 20 and 22).
   Archipelago's plum, slate blue and cyan are neighbours on the colour wheel and blurred together as big starbursts
   side by side, so the default is a loot game's ladder in the same order of importance (filler green, useful blue,
   progression purple, trap red); Archipelago keeps its own client's colours for those who know them.
4. **In the game** (the mod guide, step 9): the game's text colours by palette index only, so the colours are added
   after the game's own; that and the lines' wording are game-side, written up there.

**Seen (2026-09-26):** "You got Kabbu from TestPlayer!" in dark plum and dark yellow (a dev hold-up), and
every colour on screen while picking them. **Then with a real second slot** (an APQuest slot "Other", items placed by
plando both ways; `development.md`, "A second player"): found here, "You found Other's Key!" (a gift, progression),
"...Health Upgrade!" (a pickup, useful), "...Confetti Cannon!" (filler) and "...Math Trap!" (trap), each in its
colour; received, "You got Kabbu from Other!" and "You got Leif from Other!" when Other's chests were checked.

**Status:** works, seen on screen with a real second player (2026-09-26), every class and both directions; Rarity, the
default since, seen on a gift (the icon and its text in purple).

## Build step 20: All three members from the start (the default)

*Starting Party Member* gets a sixth choice, **All Three**: a new file starts with Vi, Kabbu and Leif, and no member is an
item. It is the default (2026-09-27: "so that you can start with all 3 if you don't want to rando partners";
then "all 3 could probably be the default"). With *Off*, Leif joins at a fixed spot after the spider, behind the
Explorer Permit, so a late permit makes him late; with All Three nothing waits on him.

1. **The world:** `option_all_three = 5`, the default. `starting_member` becomes 3 (`ALL_MEMBERS`) and all three are
   start inventory; the pool has no member, and filler takes the two slots they'd have held. The two joining moments stay
   locations, as with one member (the category is on for any start); the story's "Leif Joins" event is off. Rules ask
   for members as before, and all three are held from the start, so none waits.
2. **The mod:** `starting_member` 3 allows every member (`PartyMembers.AllMembers`). The start inventory arrives as
   received items, and each member joins as a received one does. **A story party change no longer drops a member the
   story hasn't reached yet** (Leif before flag 16): the opening's "Vi and Kabbu" keeps Leif (`KeepMembersAhead`), in
   every mode. The spider scene (Event6) is the exception: its fights stay the story's, and Leif rejoins after it.
3. **Tests:** `TestStartAllThree` (start inventory, no member in the pool, the two locations, nothing waits),
   `TestPartyDefault`; the tests of the story party's logic (Leif's droplet rooms, the permit gate, the town medal, a
   shop count) now pin *Off*, whose logic they check. A default seed with APQuest generated: all three in Starting Items.

**First play (2026-09-27):** all three were there, but no member showed a box and Leif appeared a moment
after the start. No box: the members are start inventory, which the server has at login, and the receiver showed no
box for what it had at login. Since 2026-09-28 replays are held up too (the mod guide, step on Item animation), so the
starting members get their boxes on a new file. Leif late: items are given only while the player is free, after the opening skip and a map
change. Now, with All Three, the opening's own party change adds whoever the story hasn't reached yet, so Leif is
there from the first frame; his item then finds him already in.

**Status:** works, seen on screen (2026-09-27): a new file starts with all three at once (log: the opening done
with party 0, 1, 2; Leif's item found him already in). On a fresh seed both opening spots showed their box (Poison
Resistance, then Sleep Resistance from the silent spot); a second file on a used seed shows only the gift's, since the
server already holds the silent spot's item at login.


## Build step 21: Shuffle Field Moves

Vi's Beemerang, Kabbu's Horn and Leif's Ice become items (2026-09-27: "field moves on/off, and jump as its
own on/off thing as well due to how much it impacts, both off by default"). The rules were already written in moves
(build step 13), so the logic only learns that a move is also an item.

1. **The game's own move** (`PlayerControl.DoActionTap`, `MEASURED.md`): the leader's field attack by his `animid`;
   Vi's already waits for flag 11, Kabbu's and Leif's are always on.
2. **The world:** option `shuffle_field_moves` (off). Three items, kind 6 (`MOVE_ID_OFFSET`), in the pool only with it
   on. `requires` (`rules.py`) turns an ability into its member (when members are items; with the story's party only Leif, who
   joins late) and, with moves shuffled, its item. **Cautious like members** (chosen): the gate's exit lists
   `moves` (all three items, not who uses them, so the story's Leif isn't pulled before the gate); the measured spots
   before it name their ability (the two horn spots; the fountain rooftop and the droplets now say Ice). `slot_data`
   `shuffle_moves`.
3. **The mod** (`FieldMoves.cs`): a prefix on `DoActionTap` refuses the leader's move until its item has been counted
   (recomputed every frame from the save's counted items, as members are), with the game's own
   `MainManager.PlayBuzzer()` (a short "can't" sound). Only with Archipelago on and the seed saying so. A
   move item's box shows its member's party icon and colour, no article, "<member> can use the <move>."
4. **Tests** (`test/test_moves.py`): off by default; the three in the pool; the gate needs every move; a horn spot
   needs the Horn, an ice spot the Ice; with the story's party the ice spot needs Leif too. Five seeds with both
   options, a random member and APQuest generated.

**First play (2026-09-27):** the jump stayed locked until its item, but **every attack worked from the
start**. The prefix on `DoActionTap` never ran: the method only builds its coroutine and is small enough for the
runtime to inline into its callers, where a patch never runs (no "refused" line for any attack in the log, while
`DoJump`'s patch worked). The gate moved to the coroutine's own first step (`MoveNext` at state 0), which ends it
before it sets `action`; the game clears `actionroutine` only at a tap's end, so the mod clears it a frame later.
**The moves as key items (the same day):** "make all these locked abilities into visual key items... so you
can see in your inventory that you have jump". Four key items of the mod's own after the Boat Ticket (201 Beemerang,
202 Horn, 203 Ice with the member's party icon; 204 Jump with the Archipelago icon; `CustomItems.cs`). Receiving a move
puts its key item in the bag, and the gate reads the bag, so the inventory shows exactly what works.
**The game's own names (2026-09-27: "we shouldn't make up names if there is something vanilla"):** the
game's `Skills` text names the three **Beemerang Toss**, **Horn Slash** and **Freeze** (`MEASURED.md`); items, rules
and key items use them. Jump has no entry there, so it stays "Jump".

**Seen:** the attacks now refused. **Holding the attack button buzzed nonstop** (B only; the jump fires
once per press): the game retries a held attack every few frames (`DoActionHold`), so the buzzer now plays only when a
refused press comes after a quiet 0.25 s, once per press. **Then it felt delayed** (in play): the game fires a tap
on the button's release, so the refusal came then. Now the attack's buzz plays on the press itself (a per-frame check
of the button while the leader's move is locked and the player is free), and the refusals stay silent; the jump
fires on its press and buzzes there. **Seen (2026-09-27): "works way better".**

**Seen (2026-09-27):** bought Freeze and Horn Slash at the caravan; each worked from then on, and Freeze
alone didn't unlock the others. (Buying first failed with the dev berry cheat on: it refilled the berries, and item
shops see a purchase as berries going down; the cheat is now a one-time top-up.)

**Save crystals without a move (2026-09-28):** the game only starts one from an attack's hit, so with no move
item there was no save or heal; confirm next to a crystal now uses it, as an NPC is talked to (`documentation.md`,
step 29).

**Status:** works, seen on screen (2026-09-27): each attack locked until its own item, the buzz on the press,
Beemerang Toss from Madeleine's table; the key items in the bag (Freeze and Horn Slash with Leif's and Kabbu's party
icons, Jump with the Archipelago icon, "Kabbu can use Horn Slash." as the description; seen in a screenshot).

## Build step 22: Shuffle Jump

Jump becomes one item for the whole party ("jump would just apply for any member/the whole party, unlike
the attacks"), behind its own option, `shuffle_jump` (off).

1. **The game's jump** (`PlayerControl.DoJump`, called only by the jump button): no gate of its own.
2. **Cautious logic** (chosen): with it on, every location and story event (artifacts included) needs Jump
   except those seen reachable without it, marked `no_jump` in the data. **Measured on screen (2026-09-27, the
   starting map and the town):** the ladybug siblings' house item needs no jump; Madeleine's house does, and so do
   the Hard Mode NPC's gifts and the inn's item (neither a location yet); the plaza statue discovery (not a location
   yet), the caravan and the Commercial District's two shops need nothing; the underground bar needs the Horn (grass);
   the inn review quest's completion needs nothing (if quest completions become locations). The two opening checks
   need nothing either (they happen on their own). Jump lands in one of those spots, or in another game.
3. **The Warp is forced on** with it (like a random start or the entrance randomizer), the way out of a spot
   you can't jump out of: `QualityOfLife.WarpOn` reads `FieldMoves.JumpShuffled` from `slot_data` `shuffle_jump`.
4. **The mod:** a prefix on `DoJump` refuses the jump with the buzzer until the item is counted. Jump's box shows the
   Archipelago icon (it belongs to no member).
5. **Tests** (`test_moves.py`, `TestJump`): the measured spots are reachable with nothing; Madeleine's house and the
   first artifact need Jump.

**Status:** works, seen on screen (2026-09-27): the jump locked until its item (the Ladybug house), then free for
the whole party; the Warp stayed in the pause menu with Travel set to Off.

## Build step 23: every learned field ability an item

Decided (2026-09-27): every ability the story teaches is an item, **always** (not an option: "randomizing things the
player would have found"), and the scene that teaches it is its location, as the party members' joining spots are, so
no temporary double grant is ever left to remove. Three are **progressive, always**, in the game's own order (a second
level without the first "wouldn't work"): Beemerang Toss then Halt, Dash then Horn Dash, Freeze then Icicle. All are
*progression*: each unlocks checks.

1. **Read first** (`MEASURED.md`, the unlock scenes and every field ability): seven scenes in chapters 2-6, each
   setting one flag that is also a story gate (the Dig flag opens the hideout's next scenes, the Horn Dash flag removes
   a rock); the same flags give battle skills (`MainManager.RefreshSkills`).
2. **The ability table** (`abilities.py`): each ability by the game's name, its member, its item, its level (copies of
   the item it takes) and the option that makes its base level an item. With Shuffle Field Moves off the party starts
   with the Toss and the Freeze, so their progressive items take one copy, the upgrade. Rules name abilities, never
   items; `rules.requires` turns them into item counts (`HasAllCounts`), and the pool puts in enough copies for the
   highest level. Existing ids kept: the Beemerang and Freeze items were renamed, not renumbered.
3. **The locations:** the seven scenes, each checked by its own flag (`source.flag`), in a region *Later Chapters*
   past chapter 2's start that needs everything the story used before (the permit, the Boat Ticket, the first boss,
   the party and its attacks). Until chapters 2-7 get room-level logic, each needs every ability taught before it
   (story order): more cautious than the game. They show no item of their own (`silent_locations`). Their names are
   provisional (Known issues). 7 locations and 7 items: the pool stays balanced in every option set.
4. **Combat stays basic**: no fight needs a battle skill or a medal in the logic, only each member's plain
   attack. It keeps enemies and bosses simple, and leaves room to play out of logic for fun (Kabbu with a medal that
   hits fliers might beat the spider the logic gives to Vi).
5. **The mod** (`Abilities.cs`): each learned ability is a key item of the mod's own (205-211, after the moves'),
   named and described from the game's own Skills text (`skilldata`) in the player's language. The receiver gives a
   progressive item's next level. Where the game reads an ability flag to *use* it (8 reads in `PlayerControl`, the
   thrown Beemerang's hold in `NPCControl`, 15 in `RefreshSkills`), the read (`ldfld flags; ldc.i4 n; ldelem.u1`)
   becomes a call answering from the bag; the story's reads and the flags themselves stay the game's, so every scene
   still runs and marks its own check. The counts come from the game's IL (an ILSpy IL dump), not its decompiled C#,
   and the mod logs how many it patched against them. A battle skill comes with its ability's key item
   (seven key items, not fourteen). The Warp is always on with it (`QualityOfLife.WarpOn`): a spot such as the
   hideout's cell is left by an ability that may not have come yet. Only with `slot_data` `ability_items`, so older
   seeds play as before.
6. **Tests** (`test_abilities.py`; `test_moves.py` and `test_party.py` updated): every learned ability in the pool;
   **exactly one location per ability**, by its flag (a guard against a second check for the same one);
   story order; the Horn Dash as the second copy. 395 tests pass; every option set of the scratch snapshot generates,
   is beatable and gains exactly the seven locations; a room with APQuest generates.

**Status:** built (2026-09-27): the logic and pool tested, the mod built, its patch counts taken from the game's IL and
confirmed in the running game (its log: 8 of 8, 2 of 2, 15 of 15). Not yet seen in game: a received ability working, its battle skill, the key items' text, a scene sending its check.
Decided and still to build: without the Horn Slash the Dash only moves (Next 23), for Shuffle Field Moves.

## Build step 24: how we plan and build the logic

The logic is what Archipelago uses to prove a seed can be finished, and some options will lean on it hard: one party
member, a random start in any room, a decoupled entrance randomizer, shuffled attacks, no Jump. So before the rooms of
chapters 1-7 are mapped, this is how every part of it gets done (2026-09-27: "the logic has to be precise").

**The rules** (1 to 4 the user's, 2026-09-29, "to simplify things"; How it works §11 explains regions and rules)

1. **A location says what it needs; an item never says what it opens.** Every need is written on what it guards: the
   location, the exit into a region, the story event. No item, ability or member lists what it unlocks. Which items
   matter is read back from the rules (Archipelago's `item_dependencies`, in `TestClassifications`), so writing the
   rule is the only step.
2. **A need is what the vanilla game expects:** what the game asks of a player going the intended way, with no tricks,
   skips or clever routes. The rules stay simple, players stay free to go out of logic, and every seed stays
   completable (rule 5).
3. **And, or, never not.** Everything one way needs is an *and* (`&`). When there are several ways, each is written
   and any one will do (*or*, `|`): a second way into an area is a second exit into its region (the region graph does
   the *or*); two ways to one spot inside an area are an `|` in the spot's own rule. Never a *not* on an item or a story
   event: in Archipelago, receiving something may never make anything harder to reach (the same reason the tiered shop
   rule was wrong, build step 11). An option may decide a rule, since it's fixed when the seed is made.
4. **No point of no return in the logic** (the user: never expected "to go past a point of no return, where they can't
   logically go back"). A one-way (a ledge dropped without Jump, a door with no way back, a transfer that leaves you
   somewhere) counts in the logic only together with what it takes to get back. So the player can always retrace their
   steps to the start, which is what Archipelago assumes of its origin region (§11). The Warp is never that way back.
5. **The logic may demand more than the game does, never less.** A rule that asks for too much only makes a seed a
   little stricter; a rule that asks for too little can place an item somewhere the player can't reach, and the seed
   is impossible. Anything not yet measured is written the cautious way.
6. **Nothing counts as known until the tester has seen it on screen.** Each need goes into `MEASURED.md` with its date.
7. **The mod never departs from what the generator knew.** Anything it changes comes from `slot_data`, decided at
   generation, never at runtime.
8. **Combat stays basic:** only each member's plain attack, never a battle skill or a medal. It keeps fights simple
   and leaves room to play out of logic for fun.
9. **The Warp is a safety net, never logic** (the user, 2026-09-29). It always takes you back to the seed's spawn, and
   it's forced on with a random start, the entrance randomizer, Shuffle Jump and the abilities as items, so a player
   who leaves the logic is never stuck. The logic never counts it, nor the map's fast travel, to reach anything, the
   start included. (Until 2026-09-29 build step 15 counted it to re-enter a random start; rule 4 does that job now.)

**The method: `room-logic.md`**, one checklist for every room:

1. **A draft from the game's data:** the entity dump's objects in the map (grass, rocks, dig walls, springs, droplets,
   fountains, wind, switches, platforms), its doors, its flag-gated doors, and the scenes that move the party.
2. **The questions, per room:** where each entrance puts you and whether you can leave the way you came; what crossing
   the room needs, in each direction; ledges without Jump; forced fights; scenes that move you; changes that stay
   (a switch, a broken rock); what each location needs, and whether you can get back; story state (what changes with
   the chapter); spawning in each part of the room; what each member manages alone.
3. **Checked on screen by the tester**, one part of the room at a time: the tester says what needs what.
4. **Written into the area's module by the agent** (`logic/<area>.py`, build step 29): each room split into the parts
   you can walk around freely, each way between them one-directional with its own rule, each location in its part.
5. **Tested:** each measured need gets a test that fails without it; every part reachable from every arrival once
   everything is collected; no arrival strands the player.

**The safeguards already in place:** the tests generate seeds across option sets and check they're beatable;
`TestClassifications` makes an item progression the moment a rule uses it; one location per ability; the entrance
randomizer and a random start stay labelled experimental until their room-level logic is done and tested.

**When the first area is mapped room by room** (2026-09-29): a test for rule 4 comes with it (every one-way's rule
holds what its way back needs). Each map is already a region with its doors as entrances (build step 12, 2026-09-30),
so mapping a room splits its region into areas and replaces its spots' `reach` with the room's own rules.

**Status:** planned (2026-09-27): the method and the checklist written (`room-logic.md`), no room mapped with it yet;
the rules for writing it (1 to 4, and 9) and the region explainer (§11) written 2026-09-29, the logic in Python since
build step 29. Today's rules are still by large areas, kept as each spot's `reach` over one region per map (build
step 12, 2026-09-30).

## Build step 25: DeathLink, a panel row

**What it is:** Archipelago's DeathLink, one of its "bounce" features (`docs/network protocol.md` at 0.6.7, "DeathLink"):
a client wearing the `DeathLink` tag sends a `Bounce` with `time`, `source` and an optional `cause` when its player
dies, and the server passes it to every other client wearing the tag. Each game decides what "die" means.

**Decided (2026-09-28):**
- **A row in the Archipelago panel, *DeathLink*, ON / OFF, off by default; not a yaml option**, so a player can change
  their mind mid-seed. Only while Archipelago is enabled; the seed and the logic know nothing of it.
- **In the panel on the main menu, next to *Archipelago*, not on the Gameplay page** (2026-09-28: "its an AP
  setting"; first built on the Gameplay page, which the pause menu's Settings also opens). Changing your mind means
  going back to the main menu, on purpose: a death waiting in game can't be switched away from the pause
  menu, and returning to the title drops it, which costs as much as the death.
- **A death DeathLink caused never sends one** (a common apworld bug, two games killing each other in a
  loop, or a death queued before you're back in game that kills you and sends again). Only the game's own deaths send.
- **A death that can't land yet waits, and strikes at whatever comes first afterwards:** free on the map, or the
  party's turn in a normal battle. Never inside a cutscene, a text box, a menu, a door or a scripted fight.
- **In a battle: the game's own Game Over menu** (Retry, Retry after changing medals, Load, Title). **On the map:** a
  Game Over (music out, black, its sound), then the last save, as the menu's Load does.

**What counts as a death here** (`MEASURED.md`, save crystals, saving, Game Over): only a party wipe in a battle.
Hazards and falls cost no HP; they put the party back. A scripted loss (`battlelossevent`, the story's own defeats)
isn't a Game Over, and neither sends nor receives.

**How it works:**
- **The connection** (`ApConnection.cs`): MultiClient.Net's `DeathLinkService` (read at `v6.7.1`: `EnableDeathLink` /
  `DisableDeathLink` change the tag with a `ConnectUpdate`, `SendDeathLink`, `OnDeathLinkReceived`). The tag is set
  **after** login from the row, and again whenever the row changes, so it never depends on what `Connect` carried
  (`client-requirements.md`, the tag-before-`slot_data` trap). A `ConnectUpdate` waits on the socket, so it runs off
  the game thread, as every send does. Received deaths wait in a queue for the game thread.
- **Our own echo:** the library drops a received death equal to the last one it sent, by source and time to the
  second, never by name alone (the two-clients-on-one-slot trap).
- **Sending** (`DeathLinkGame.cs`): a prefix on the first step of `BattleControl.GameOver` with its setup (only a wipe
  starts that; the game's own re-shows of the menu skip it). The cause names the slot, as the protocol asks:
  "<slot>'s party was defeated in Bug Fables."
- **Receiving:** in a battle, at the party's turn (no action running, no death check, no Game Over yet), the game's own
  `DeadParty` is started, which runs the game's Game Over; that Game Over is marked as the link's, so it sends nothing.
  On the map, once the player is free and a save exists, our Game Over, then `MainManager.ReloadSave()`.
- **One at a time:** from a strike until play is back (the Game Over's menu answered, or free on the map after the
  reload), deaths that arrive join it. A waiting death is dropped if the row is switched off, or on the title screen.
- **Logged at every decision** (`[death]`): received, waiting and for what, joined, struck and how, not sent and why.

**Status:** built (2026-09-28), not yet seen in game or in a room.

*Code: `DeathLinkGame.cs`; the service in `ApConnection.cs` (`SetDeathLinkTag`, `SendDeath`, `TakeDeath`); the row on
the panel's first page in `ApMenu.cs` and `ApMenu.Rows.cs`.*

---

## Build step 26: the tutorial leaf, an item the story puts in the bag

**Why:** on a new file a Crunchy Leaf was already in the bag before any check (2026-09-28). Items are
remote only, so it becomes a check (chosen over keeping it or taking it back).

**Where it came from:** the opening (`Event16`) adds it straight to the bag for its tutorial battle
(`items[0].Add(0)`, not a `giveitem`, and never taken back; items are locked in that battle, so it isn't used). With
the Archipelago mod on, the opening is skipped and the mod's skip redoes its effects, the leaf included
(`QualityOfLife.Opening.cs`). So the mod added it, and no game code needed patching.

**A new kind of location:** `source.added`, an item the story puts straight into the bag (type and item, as a
`giveitem` has). The apworld reads it as the location's vanilla item (so the leaf joins the pool) and lists it in
`slot_data` as `location_added`; it is done by the opening's flag 15, with the other two opening checks, and it's
silent (no scene of its own shows an item). The location: *Outskirts: Outside the City, Tutorial Battle*, reachable
from the start. The mod's opening skip leaves the leaf out when the seed has it as a location
(`[qol] opening: the tutorial leaf is a location; left out of the bag`).

**Tests:** `test_tutorial_leaf_is_a_location` (the slot_data entry, its flag, silent, the leaf in the pool). Two tests
moved with it: the permit gate's reachable set gains the location, and a solo *Filler Only* seed with discoveries on
now has exactly enough filler (the leaf adds one), so its shops stay filler-only. 400 tests pass; 0 of 10000 fuzzed seeds fail.

**Seen (2026-09-28):** on a new file no leaf in the bag; the three opening checks sent together, and
their items (a Lore Book, Mistake, Bee Fly in one seed) in the bag with the three starting members, with no boxes.

**Status:** works, seen on screen (2026-09-28).

*Code: `logic/outskirts.py` (id 75), `data_tables.vanilla_item`, `slot_data.py` (`location_added`, the silent rule);
`ApConnection.cs` (`LocationAdded`), `QualityOfLife.Opening.cs` (`RunOpening`, `SeedAdded`), wired in `Plugin.cs`.*

---

## Build step 27: a found pickup is gone in every save

**Why:** on a new file every pickup was back, those already found included: the game keeps "picked up" in the save,
and a new save starts empty. Picking one up again sent and gave nothing (2026-09-28: one Meditation after
two pickups), so it was clutter. Pseudoregalia's way was chosen: a pickup whose check is done isn't there.

**What:** when a map's entities are made, a one-time pickup (a floor item or a crystal berry) whose location the server
has as checked is kept away, the way the open world keeps blockers away (build step 9): its hiding list is replaced by
a marker the game's own existence check answers "gone" to. Nothing is written to the save. **Not hidden:** a
respawning pickup (once its check is done it is the game's own item again, the named exception, build step 10) and a
story pickup (it starts its scene). Shops already left out a copy whose check is done (build step 11).
Logged: `[open] <map>: <entity> kept away (location <id> is already checked)`.

**Seen (2026-09-28):** on a new file the Ladybug Siblings' house was empty (its pickup found in an earlier
file; `LadybugMistake kept away (location 7720014 ...)`), and Meditation and 15 Berries (Artis's gift) arrived as
replays with their boxes. A check sent from outside (Madeleine's left table, checked by a second client on the slot)
arrived with its box while the player stood in the house; the pickup stayed until the house was entered again, then
was gone, the right one still there: hiding applies when a map is made, not live. **Live, for a shared slot** ("good
for if people share 1 slot"): every 15 frames, while the player is free, a one-time pickup on the current map whose
check is done but which this save hasn't taken (its flag, or its crystal berry mark, unset) is hidden the way the game
hides an entity, `SetActive(false)` (`NPCControl.Start`). A pickup this save took is left to the game: its scene may
still be running on it. Logged: `[swap] location <id>: found by another client on this slot; its pickup on <map>
hidden`. **Seen (2026-09-28):** standing in Madeleine's house, the right table checked by a second
client: the pickup went at once, and Poison Resistance arrived with its box.

**A flash between rooms** (the same day: gone outside and inside, but visible while going in or out).
A house is an *inside* of its map: `MapControl.RefreshInsides` turns that inside's entities on without asking whether
they exist (`MapControl.cs:1262-1265`). A postfix now turns every entity the mod keeps away off again in the same frame
(found pickups and the open world's blockers alike), and a pickup hidden live gets the same marker. **Seen
(2026-09-28):** no flash going in and out of the houses.

**Status:** works, seen on screen (2026-09-28): a floor item, on the next entry into its room; a crystal berry not yet seen.

*Code: `KeptOpen.cs` (`AfterCreate`, the found pickups), `ItemSwap.Pickups.cs` (`IsPickup`, now shared).*

---

## Build step 28: nothing unpublishable in the repo or a release

**Why (2026-09-29):** the Archipelago community's Developer Code of Conduct makes whoever publishes a project answer
for all of it: where the code came from, what it does, and that a release holds exactly what the repository holds.
Everyone who runs the mod or the apworld trusts it with their machine. So the repository now refuses, by itself,
anything that couldn't be published or could harm whoever runs it, and a reviewer can check each of those claims
without taking the author's word for it ([docs/reviewing.md](../docs/reviewing.md)).

**One gate: `dev-scripts/preflight.py`.** It reads what git holds, which is exactly what a commit contains, never the
working copy, and runs a list of sections that each pass or fail on their own. It needs only Python's standard library
and git, so anyone can run it. Two files hold everything it decides by:
- **`dev-scripts/preflight-patterns.json`:** what each section refuses (credential formats, home-path patterns, game
  file names), and the few things let through by name (the four release DLLs, the libraries' hashes).
- **`docs/capabilities.md`:** everything the code may do that reaches beyond its own files, each with its reason.
  It is exact: something the code does that isn't listed fails, and so does a row nothing uses any more.
  Adding a row is the maintainer's decision.

A section that finds nothing to check fails rather than passing: "0 files scanned" would otherwise look like
"0 problems".

**The first sections (2026-09-29):**
- **Hooks armed:** this clone runs the hooks, and the hooks are marked executable (on Linux and macOS, git silently
  skips a hook that isn't).
- **Index sanity:** plain files only (no links, submodules or conflicts), names that work on every system, no two
  paths differing only in case.
- **Known kinds only:** every file matches a kind in the patterns file ("a C# file of the mod", "a player doc"); a
  file of any other kind fails.
- **Binaries:** the only binary files are the four release DLLs, each a .NET assembly, and the three libraries are
  byte for byte the files NuGet ships.
- **Hidden characters:** no invisible, bidirectional or control character anywhere. Those can make code read
  differently from what runs (the "Trojan Source" trick). In code, the only non-ASCII characters are `é`, `…` and `±`.
- **Encoded blobs and long lines:** no long base64 or hex run, and no code line over 1000 characters, where a payload
  could hide.
- **Secrets:** no token, key or webhook in any file, the mod's DLL included.
- **Personal paths and names:** no home-folder path, and not this machine's user or computer name, in any file, the
  DLL included. This replaced the old pre-commit check, which saw only text and skipped its own folder.
- **Game files:** no game assembly, asset, save or decompiled code.
- **Hosts and addresses:** every host named anywhere is in the capabilities list with a reason, and no public IP
  address appears (the four-part version numbers that look like one are listed by name).
- **Licences:** every project cited has its row in `licensing.md`, and the release carries the licence and every
  shipped library's notice.
- **Commit messages** (`--history` only): no credential, home path or hidden character in any message.

**The apworld's rules (2026-09-29).** The apworld is Python that runs on whichever machine generates a seed, the
archipelago.gg website's included, and Archipelago imports it on every start, even when nobody plays Bug Fables.
So it gets the strictest rules, read from its syntax tree, not by searching text:
- **Apworld imports:** every name it takes from Archipelago or Python is listed in the patterns file (30 names
  from 12 modules), and a plain `import` only for `json`, `logging` and `pkgutil`, each with the few functions it
  may use (`json.loads`, `pkgutil.get_data`). Listing names matters because a module hands on everything it
  imported: Archipelago's `BaseClasses` can pass along a helper that runs programs.
- **Apworld runs nothing unexpected:**
  - Only listed builtins, so no `open`, `eval`, `exec` or `__import__`.
  - No hidden attributes (`__class__`, `__globals__`), not even named in a string.
  - No attribute that writes files, runs programs or opens connections, on any object.
  - No world hook that is handed files or settings to write (`generate_output`, `settings`).
  - Annotations are only type expressions. Archipelago evaluates option annotations as code.
  - Nothing runs at import but definitions.
  - A lookup by a computed name (`getattr(x, name)`) must be listed in `docs/capabilities.md` with where the name
    comes from. There are four.
- **Apworld data and docs:** the data files are strict JSON (no repeated key, no `NaN`) with no hidden attribute
  name inside, the manifest has exactly its four keys, and the player docs, which the website renders, hold no
  raw HTML or script link.

**The mod's and the scripts' rules (2026-09-29):**
- **Mod source:** the C# is read with its comments blanked but its strings kept, since a name in a string can
  still be reached. Twelve kinds of call are refused outright, among them starting programs, web requests, raw
  sockets, loading code, native calls, the registry, base64 payloads, opening web pages, reading who the player is,
  JSON that picks its own type, and code written at run time. None appears today. What the mod does touch beyond
  the game is listed file by file in `docs/capabilities.md`:
  - the one server connection;
  - the randomizer's own saves;
  - the clipboard, on paste and copy;
  - finding game types by name;
  - the dev build's dump tools, which never ship.

  Reflection on a type named in the code (Harmony's everyday tool) is not listed: the target is in plain sight.

  **Text from the server is read in one place only (2026-09-29):** a player, item, location or game name, or the
  seed's name, read anywhere but `Core/ServerText.cs` fails. So no new text path can skip its cleaning (the mod
  guide, step 33).
- **Dev scripts and hooks:**
  - Python is read from its syntax tree: no `eval`, no `exec`, no `shell=True`, no pickle, sockets or web modules.
  - PowerShell and shell are read with comments blanked: no running text as code, no encoded commands, no downloads,
    no compiling, no system settings.
  - What each script does (runs programs, writes files, talks to GitHub, connects to a local test server) is listed
    per script, so anyone about to run one can see what it will do.

**The compiled DLL itself (2026-09-29).** The mod's DLL is the one file nobody can rebuild without the game, so
it is read directly, the way the .NET runtime reads it: `dev-scripts/dotnet_metadata.py` parses its PE headers,
its metadata tables and every method's IL, with Python's standard library only. It is strict:
- the tables must fill their stream to the byte (one zero, then padding to 4, as the .NET writer lays it out);
- an unknown opcode stops it;
- it parses all four shipped DLLs: 7,700 method bodies from four different compilers.

Four sections read it:
- **Release staging:** the download holds exactly its seven files. Each DLL is the one `built-from.txt` records,
  and the sources that file lists are exactly those of the commit it names. Sources changed since then only warn
  (a committed DLL is older than its sources between releases); with `--release`, they fail.
- **Shipped DLL structure:** a plain compiled library.
  - Sections `.text`, `.rsrc` and `.reloc`, with no data after them.
  - Only `mscoree.dll!_CorDllMain` imported.
  - The five standard metadata streams.
  - No native or P/Invoke methods, no embedded resources, and no stored data outside the compiler's array
    initialisers.
- **Shipped DLL reach:**
  - It references only the 13 expected assemblies.
  - None of 42 denied kinds of call appears among its 12,762 references: processes, `System.Net`, loading code,
    `Marshal`, the registry, code written at run time, opening URLs, reading who the player is, JSON picking its
    own type.
  - No denied name or unlisted host in its strings, not even in attribute data.
  - Each file, clipboard, connection and by-name lookup is traced to the type that makes it, and must be listed
    for a source file declaring that type.
  - Every Harmony patch target outside the game is listed. The first run found three that were written down
    nowhere, all legitimate: MultiClient.Net's socket creation and one websocket-sharp method (compression), and
    Unity's `Animator.Play` (a guard). They now have their rows.
  - **A target given by name** (`[HarmonyPatch("Type, Assembly", "Method")]`, for a class `typeof` can't reach
    because it's internal) is read as a type and a method, never as a method name alone (2026-09-29).
  - **A patch newer than the committed DLL** (2026-09-29): between releases the DLL predates the source. A listed
    patch it lacks is only a warning if today's source makes it (a `[HarmonyPatch]` naming that method, in a file
    naming its type). Otherwise, or once the DLL is current (a release build, `--release`), a row with no patch
    fails. Three fixtures cover it: stale with no such source, current, and stale with the source.
- **The DLL says only what its source says:** read against the sources of the commit it was built from.
  - Every type, method, field and called member name, and every one of its 1,237 strings, must come from that
    source. The strings are matched to the literals, the pieces of interpolated strings, and the constants the
    compiler joins.
  - The names the compiler makes up (`<Run>b__3_0`, tuple fields, operators) are allowed only by exact name or
    when built from a source name.
  - None of `Dev/`'s types may be in it.

  This is the strongest check possible without the game: code in the DLL that the source doesn't have shows up
  as names and strings the source never wrote. What it can't show is how the IL wires the allowed pieces
  together; only a rebuild with the game (documentation step 32) or reading the decompiled DLL proves that.

Each has its fixtures: same-length byte patches to a copy of the DLL (a namespace renamed to `System.Net`, a
Harmony target moved outside the game, a URL written into a string, a type renamed, the native entry point flag,
data appended). **`build-release.ps1`** runs the three DLL sections on each fresh build before staging it, and
its `-Check` (the release's gate) is now preflight with `--release`.

**The workflows, the dependencies and the list itself (2026-09-29):**
- **Workflows:** every action pinned to a full commit hash, with its version in a comment. The four first-party
  actions were pinned by tag only until then; a tag can be moved, a commit can't.
  - The workflow-level permissions are empty, and a job asks only for what the patterns file lists (today: the
    release's publish job, to write the release).
  - Only four triggers (push, pull request, a call from another workflow, a manual run), and only GitHub's own
    runners.
  - No secret but GitHub's own token, and no `${{ }}` inside a script, where text from outside would run as code.
  - No `continue-on-error` and no YAML anchors.
  - The release's publish job must wait for every gate.
- **Dependencies pinned:**
  - Every package at one exact version, and the lock file agreeing with the project, a content hash for each.
    The SDK adds `NETStandard.Library` itself, so that one is listed by version in the patterns file.
  - Each NuGet feed mapped to its packages.
  - One SDK, rolling forward at most a patch.
  - Every MSBuild switch that keeps outside build files out.
- **Capabilities list:** every table in `docs/capabilities.md` is one a section checks, and every row has a
  reason. A table nothing enforced would read as if something did.

**Checking a release against the repository (2026-09-29): `dev-scripts/verify-release.py`.** Given a release's
files and the tag, it checks:
- the mod zip holds exactly `release/mod` at the tag, byte for byte;
- the apworld holds exactly `apworld/bug_fables`, byte for byte, but for the three things Archipelago's builder
  adds: the licence and two version fields in the manifest;
- the three libraries are the NuGet package's own files;
- the yaml, which Archipelago writes from the options, carries no credential, home path or hidden character;
- for a release made after the preflight, preflight's DLL sections pass on the zip's DLL.

It needs only git and Python, no network: download the files first. **Both earlier releases pass** (v0.1.0 and
v0.2.0, run 2026-09-29). A zip with one byte changed in a library, and an apworld with a file added, both fail.

**CI and the release (2026-09-29):**
- **Every push and pull request** (`.github/workflows/preflight.yml`), on Python 3.11 and 3.13: preflight on the
  commit, `--history` over everything ever pushed, and the gate's own test. A second job downloads the
  Archipelago.MultiClient.Net package from nuget.org and checks the three libraries against it.
- **The release** waits for that workflow too. Before uploading, it checks the files against the commit: the
  apworld is built in a job that also installs Archipelago's own dependencies from PyPI, so it is checked, not
  trusted.
- **Provenance:** it adds GitHub's signed provenance attestation to the apworld and the yaml, the two files CI
  builds; `gh attestation verify` checks them. Not to the mod zip, which would suggest CI built its DLL.
- **The notes:** they carry `docs/capabilities.md`'s diff since the last release, so a new capability is on the
  release page whatever the highlights say.
- **After publishing:** a last job downloads what was published and runs `verify-release.py` on it.
- **`release.ps1`** waits for both push workflows before dispatching.

**Where it runs so far:**
- **Every commit:** the pre-commit hook, quiet unless something fails.
- **Every push** (`.githooks/pre-push`): preflight on each pushed commit, and `--history` on everything new in the
  push. A commit made past the other hooks is caught here, before it leaves the machine. When the push changes the
  gate itself, the gate's own test (below) runs too.
- **Every release:** the release guard runs its text rules on the release notes and on every commit subject the
  notes will publish. This replaced a separate pattern list.

**The gate is tested by making it fail** (`dev-scripts/negative-test-preflight.py`). A check that only ever runs on
a clean tree says "pass" whether it works or not. So for every section, and every mode it runs in (a commit, the
history, free text), the test plants a real violation and checks that the section reports FAIL and preflight exits
non-zero. It works in a throwaway clone outside the repo, with its link back to the repo removed. The clone holds what
the next commit contains (HEAD plus everything staged), or, from pre-push, exactly the commit being pushed:
1. **A clean baseline** in all three modes, so a failure afterwards is the plant's doing.
2. **One fixture per kind of violation** (76 on 2026-09-29, counted from the test itself): a bidi override in a doc, a homoglyph in code, every
   credential format at once (each must be named), a home path inside the DLL, a library changed by one byte, a
   symlink, a submodule, a stale host row, a secret committed and then removed, and more. The fake credentials and
   paths are assembled at run time, so the test file holds none itself.
3. **The hooks for real:** a normal commit carrying a credential is refused, and so is a gate change mixed with mod
   code. A commit made past the hooks is refused at push, and the test remote stays unchanged.
4. **Coverage is total:** a section without a fixture in a mode it runs in fails the test, and so does a credential
   format or denied kind of call in the patterns file with no sample.
5. **What must pass, passes:** a denied call that only sits in a comment must not trip the mod's section, and a
   tree put back to the DLL's own sources must not count as stale. The staleness fixtures start from that tree:
   between releases the real one is legitimately newer, which would hide what they plant.

It takes about 25 s. **Tested the other way round (2026-09-29):** with the Secrets section made blind on purpose,
all four of its fixtures failed the test. Writing the test also caught its own slips: a sample written out whole
(preflight flagged the test file itself), a name git on Windows refuses to hold, and a fixture that stopped reaching
its section when a second table was added below it. A plant that changes nothing now stops the test.

**The hooks around it:**
- **They find a Python that runs** (`.githooks/python.sh`). On this machine `python3` is the Microsoft Store's
  stand-in, which only prints an install hint, so each candidate is tried before use. A clone can name its own with
  `git config preflight.python <path>`.
- **`commit-msg` fails closed.** Its subject-length check used to pass silently when no Python answered.
- **A change to the gate is a commit of its own.** The preflight's files, the hooks and the workflows can't be
  committed together with mod or apworld code, so every change to what is checked stands alone in the history.

**Switched on (2026-09-29):** the first run took 0.4 s over 162 files. It caught the hooks not being executable, the old
pattern list and hook spelling out home paths, and two slips in its own code: a real zero-width character where an
escape was meant, and a string that read as a URL. All were fixed before the first commit.

**The whole history, checked (2026-09-29):** `preflight.py --history` reads every file version and every commit
message ever pushed (964 commits, 2843 file versions, in 19 s). It found no credential, no home path, no hidden
character, no game file, and no binary other than the release DLLs. It first flagged two kinds of harmless thing:
- **Old data files written on one line.** JSON is only ever parsed as data, so the long-line rule now skips it; the
  encoded-blob rules still read it.
- **Three old versions of the path checker.** They spelled out the home-path patterns they refused. These three are
  exempt by their exact git hash (`history_reviewed` in the patterns file), each with its reason, so a different
  file can't hide behind the exemption.

**The pages for reviewers (2026-09-29):**
- **`docs/reviewing.md`** is for anyone checking the project before running it, the Archipelago Discord's
  Developer Advocates among them. It covers:
  - how AI is used;
  - the checks that need only git and Python;
  - what runs on whose machine;
  - where a server's data goes, with what is and isn't checked;
  - what's proven about the committed DLL, and what can't be;
  - what the gates can't prove.
- **`.github/SECURITY.md`** says how to report a problem.
- **The README** carries the AI notice and links to both.

Writing the server-data part meant tracing every server input through the mod. That turned up three gaps, each
confirmed in the code, listed under Known issues in "Where it stands" above.

**GitHub's own settings (2026-09-29, each on the user's yes, set with `gh api` and read back):**
- **Secret scanning and push protection:** already on. GitHub refuses a push carrying a known token format.
- **Private vulnerability reporting:** on, for `SECURITY.md`'s *Report a vulnerability* button.
- **Immutable releases:** a published release's files and tag can't be changed.
  - The release action already uploads to a draft before publishing, so releases work unchanged.
  - A pre-release, which it would publish first, now stays a draft with its files, to be published from the
    releases page.
- **Two rulesets, with no one allowed to bypass them:**
  - `main` can't be force-pushed or deleted, so history can't be rewritten out of sight.
  - `v*` tags can't be deleted or moved, so a release always points at the commit it was checked on.

**The coding agent's guard (2026-09-29, on the user's yes):** the hooks stop a commit, but an agent could still step
around them. Claude Code runs `.claude/hooks/agent-guard.py` before each shell command and file edit its agent makes
here (`.claude/settings.json`).
- **It refuses** whatever gets past the git hooks: `--no-verify` (and its short forms), `git commit -n`, changing
  `core.hooksPath` (setting it to `.githooks` is allowed), git config set through the environment, and the plumbing that
  writes history by hand (`commit-tree`, `update-ref`). It reads each command word by word, so a commit message may
  name any of these; `bash -c`, `powershell -Command` and the like are read inside too.
- **It asks the user first** before:
  - an edit to `docs/capabilities.md`, the patterns file, `.claude/` (the guard itself, and the local settings that
    could switch it off) or `.git/`;
  - a commit that may carry one of those, which also covers a file a script wrote rather than an edit;
  - a `gh api` call that writes, since that changes GitHub without passing a hook.
- **Everyday work asks nothing** (the user, 2026-09-29: "I don't want to constantly have to confirm things"). The
  first version also asked before every commit touching any gate file and before every push. That was a prompt per
  commit of gate work, and one on each push the user had already asked for, so both went the same day.
- **It fails closed.** If it can't run (no Python, a crash, input it can't read), the settings' command exits 2 and
  Claude Code refuses the action.
- **It stays small.** It calls `py -3` or `python3` directly, about 0.3 s a call: the hooks' own finder would double
  that on every command.
- **The settings can't grow.** Claude Code runs a repo's hooks for anyone who opens it in Claude Code, so
  preflight's "Dev scripts and hooks" holds `.claude/settings.json` to `ask` and `deny` rules and the one listed
  command; the guard script is read like every dev script. A file `.claude/settings.local.json` is left out of git.
- **Proven:** the harness plants settings that do more (their own `env`, an allow list, another event, another
  command), and runs the guard on 37 cases, among them a commit carrying `docs/capabilities.md` (asks) and one
  carrying only `preflight.py` (doesn't). Staging a guard missing its
  `--no-verify` check, and a command that exits 1 instead of 2, made both tests fail (2026-09-29).
- **Not a wall.** A determined script can still get round it. Pre-push, CI and the release checks are what catch that,
  and a weakened gate still shows up as a commit of its own.

**Status:** built (2026-09-29): the sections above, their test, `verify-release.py`, every place they run
(pre-commit, pre-push, CI on every push, the release), the reviewer pages, the GitHub settings and the agent's guard.
The CI half first ran on the push of 2026-09-29 (`0fc15ce`), all green: preflight on Python 3.11 and 3.13 (the tree,
all history, the harness's 76 fixtures), the libraries byte for byte against NuGet's package, and `ci.yml`. The guard
went live in the session that made it. Next: the TLS measurement (Known issues); the cache fix waits for a look in
game (the mod guide's step 34).

*Code: `dev-scripts/preflight.py`, `dev-scripts/preflight-patterns.json`, `dev-scripts/dotnet_metadata.py`;
`docs/capabilities.md`;
`dev-scripts/negative-test-preflight.py`; `.githooks/pre-commit`, `.githooks/pre-push`, `.githooks/commit-msg`,
`.githooks/python.sh`; `dev-scripts/verify-release.py`; `.github/workflows/preflight.yml`, and the guard, publish
and verify jobs in `.github/workflows/release.yml`; `.claude/settings.json`, `.claude/hooks/agent-guard.py`.*

## Build step 29: the logic in Python, one module per area, the Rule Builder's way

**Why (2026-09-29):** the project follows Archipelago's own way in everything (How it works §8). Archipelago's Rule
Builder is "intended to be written first in Python", and APQuest keeps its regions, locations and rules in Python. Ours
were JSON (`data/locations.json`) in a format of our own, turned into rules by a function of our own.

**The gate first:** the preflight's list of names the apworld may take from Archipelago gains the Rule Builder's `Rule`,
so the world can register its own rules the way the Rule Builder's doc shows (a change to the gate is a commit of its
own).

**Decided with the user (2026-09-29):** one Python module per game area, not per room ("all of Snakemouth Den" in one;
the Outskirts' corridors and Outside Snakemouth in the Outskirts, as the location names already say). A room's logic
often reaches into its neighbours, and a tester checks an area in one sitting; one file per room would be hundreds of
files with nearly every exit crossing into another. None of it is in the mod: the mod only follows what the generator
decided (`slot_data`).

**Built (2026-09-29):**

1. **`logic/`**, one module per area: `outskirts.py`, `snakemouth_den.py`, `bugaria_city.py`, `metal_island.py`,
   `later_chapters.py`. Each lists its `REGIONS` (each with its exits and their rules), its `LOCATIONS`,
   `STORY_EVENTS` and `ARTIFACTS` (each with its own rule), and the entities the seed changes there (`KEPT_OPEN` and
   the rest). A spot lives in its region's module; only exits cross from one module into another. `logic/__init__.py`
   gathers them, `LOCATIONS` sorted by id so moving a spot between modules never changes a seed. The old JSON notes
   became comments.
2. **The rules are the Rule Builder's:** `Has("Explorer Permit")` for an item or a story event, `&` for "and", `|` for
   "or". Bug Fables' own needs, the ones that depend on the options, are three rules registered the documented way
   (`custom_rules.py`, each resolving to Archipelago's own `HasAllCounts`): `CanUse("Horn Slash")` (the ability's item
   copies when it's an item, its member when members are items), `Member("Vi")` (only when members are items) and
   `MoveItem("Freeze")` (the item alone, for ground not measured yet). `rules.requires` and the `Needs` fields are gone.
3. **Menu, the origin, is made in code** (`regions.py`, as APQuest does), with its one exit to where a new game begins.
   Jump's blanket rule stays in `rules.py`: with Shuffle Jump, every spot not marked `no_jump` also needs `Has("Jump")`.

For example, the way into the droplet rooms and a spot inside them:

```python
Region("Snakemouth Den", exits=(
    # Water droplets: Leif freezes them.
    Exit("Snakemouth Den Underground", CanUse("Freeze")),
)),
...
Location("Snakemouth Den: Mushroom Pit, Droplets", 9, "Snakemouth Den Underground",
         Source(flag=724, pickup=Pickup(map="SnakemouthMushroomPit", type=0, item=144)),
         rule=CanUse("Freeze")),
```

**Proven the same:** before the change, every spot's rule, every spot's reachability and every exit's rule were
recorded on 300 random sets of items, under the defaults, the story's party, one member, moves shuffled, Jump
shuffled and both mixes; after it, all 1306 rows came out identical.

**Tests:** `test_areas.py` (every exit leads to a region; each spot sits in a region of its own module; names unique;
every region has a way in and is reachable with everything; ids in order; every name a rule uses exists: item, story
event, ability, member). `test_rules.py` (each custom rule under the option sets that change it; `base & (A | B)` and
`A | B` on real states; a way that needs nothing makes the "or" free). `TestClassifications` now reads the items each
rule uses through Archipelago's own `item_dependencies()`, in a seed where every member, move and Jump is an item. With
`CanUse` broken on purpose (never asking for the member), 7 tests fail. 445 tests, the Logic Test check (90 of 90) and
the fuzzer (0 of 10000, every room with APQuest) pass.

**Status:** built (2026-09-29): the logic in `logic/`, its rules the Rule Builder's, proven identical to the JSON's.

*Code: `logic/`, `custom_rules.py`, `data_types.py`, `data_tables.py`, `regions.py`, `rules.py`; tests `test_areas.py`,
`test_rules.py`, `test_logic.py` (`TestClassifications`), `test_party.py`.*

## Build step 30: Room Swap (experimental)

Whole rooms trade places with rooms that have as many doors, so the map keeps the game's shape and only which room
sits where changes. A value of the yaml option *Entrance Randomizer (experimental)*, `room_swap`, sharing the door table
and `door_targets` with build step 12, but a step of its own (the user, 2026-09-29: "room swap deserves its own doc
section separated from entrance rando").

**What it is, and why it is a value of the entrance randomizer** (2026-09-29; the user asked what a shuffle like
Super Metroid's Map Rando is called, rooms with the same number of entrances swapping places):

- **A room swap is a coupled shuffle too:** each door still leads back where it came from. So *Room Swap* and
  *Coupled* on together would look like *Coupled* alone. One option, then, each value allowing everything the one
  before it does: *Off*, *Room Swap*, *Coupled*, *Decoupled* (build step 31). The user chose the name `room_swap`: "rooms"
  alone says less, and Hollow Knight's randomizer uses "room randomizer" for every transition shuffled.
- **No decoupled room swap:** a room put where a room with more doors stood leaves the neighbours' extra doors leading
  nowhere, and pairing such loose doors is the coupled shuffle. *Coupled* and *Decoupled* keep their one meaning:
  whether turning round takes you back.
- Super Metroid's Map Rando lays out a new map on a grid. Bug Fables' maps are separate scenes on no grid, so the
  swap keeps the game's own map.

**The numbers first** (measured on `data/doors.json`, 2026-09-29):

- **What moves is an area** as `doors.py` counted it: a map with the maps its fixed doors join (a fixed door can't be
  rewritten, so it travels with its room; since 2026-09-30 only fixed doors both ways, below). 215 areas; by doors: 72 with 1, 75 with 2, 38 with 3, 14 with 4, 7 with 5,
  4 with 6, one each with 7, 11 and 23, two with 8.
- **The doors alone split the world into 10 parts** (180, 22, 17, 5, 4, 4, 3, 3, 2 and 2 maps), which boats,
  elevators and scenes join (the `Beehive` and `Factory` maps, `RubberPrison` with `GiantLair`, the `Termite` maps...).
  A room swapped into another part strands its own part, so **rooms swap only within their part**: 198 of the 215
  areas have a partner there; 17 stay put, mostly hubs (`SandCastleMainRoom`, 11 doors; the area of
  `GoldenPathTunnel2`, 23).

**How it was built:**

1. **`shuffle_rooms`** (`doors.py`, since `entrances.py`'s `room_pairs`): the areas and their doors as the coupled
   shuffle had them; the parts are the
   same grouping with every door added to the fixed links. Areas are grouped by part and door count and each group
   shuffled: each area's place goes to another, whose doors take the place's doors in a random order (which side of a
   room a door is on isn't in the table). Each of the game's pairs (d, n) becomes the pair of doors now standing at
   d and n.
2. **The same `door_targets`:** the pairs go through the coupled shuffle's own last step (`_targets`), so the mod
   needed no change, and the Warp is forced on as with any `door_targets` (build step 12).
3. **Nothing stranded, for free:** the map keeps the game's shape, so the coupled shuffle's grow-outwards pass isn't
   needed. (Wrong twice, found on 2026-09-30: below.)
4. **Ours, not Archipelago's:** its entrance randomizer (`randomize_entrances`) pairs single entrances by group and
   can't move a room's doors together (its `can_connect_to` hook refuses one pairing at a time and it never goes back),
   so this is custom work where Archipelago has none (How it works §8).

**On the region graph, and two things that were wrong (2026-09-30).** With every map a region (build step 12), the
swap moved onto the same entrances as Archipelago's randomizer, and the first test that could check every region
from the start found two holes:

1. **`room_pairs` in `entrances.py`** connects the split door entrances by hand, the dangling exit to the target named
   after the other door (the pattern of The Messenger's `connect_plando`), then the same `door_targets` and spoiler as
   the coupled shuffle. The logic follows the swap.
2. **A one-way fixed door joins nothing.** 19 of the 39 fixed doors go one way (drops in the Barren Lands, the Golden
   Settlement's night maps, the wizard tower, the wasp kingdom). An area joined by one could be entered on its far
   side with no way back (seed 6: five maps cut off). Areas and parts now join only fixed doors that go both ways:
   225 areas (75 with 1 door, 79 with 2, 41 with 3, 15 with 4, 8 with 5, 3 with 6, one each with 7 and 11, two with
   8), still 10 parts, 209 with a partner.
3. **A gated door moves with its room.** The Golden Path door needs the first boss; seed 4 put the Outskirts where that
   door was the only way on, with the boss behind it. Archipelago's randomizer follows the logic while it places; the
   swap can't, so each try is checked the way the randomizer checks (everything the seed holds, every region reached)
   and undone if it fails, up to 20 tries. Before the check 13 of 200 tries cut regions off; after it, 0 of 200.

**Tests** (`test_doors.py`): what every mode shares (doors rewritten, only the table's doors named, every way back
leads back, every region reached, the spoiler listing each pair once, and **the mod doing what the logic proved**:
each door as `door_targets` rewrites it arrives where its entrance leads in the region graph); the parts stay whole;
**the map keeps its shape**: each area's part, its door count and the door counts of the areas its doors lead into
are the game's (the coupled shuffle breaks that in 50 of 50 seeds); on small made-up tables over 100 seeds, two parts
never trade rooms (without the part rule, broken in 93) and an area joined both ways moves whole (ignoring fixed
links, broken in 85). On the real table about 490 of the 508 doors are rewritten. Seeds generated alone and with
APQuest (the spoiler: *Room Swap*).

**Later:** doors matched by side (an exit on the right leads into a door on the left), once each door's side is read
from the entity dump.

**Status:** built, not yet seen in game (2026-09-29); on the region graph, the logic following it (2026-09-30);
experimental like build step 12: the rooms' own rules aren't mapped yet.

*Code: `entrances.py` (`room_pairs`, `_swap_rooms`, `door_targets`), `options.py`, `world.py`; tests `test_doors.py`.*

## Build step 31: Decoupled doors (experimental)

A fourth value of *Entrance Randomizer (experimental)*, `decoupled`: every door may lead to any other and its way back
is shuffled too, so turning round can take you somewhere else. Planned since build step 12 (2026-09-25: "coupled by
default, decoupled as a choice"); the user, 2026-09-29: added now, as its own step.

**How it was built (2026-09-30):**

1. **Archipelago's randomizer, uncoupled:** the same split door entrances as Coupled (build step 12), and
   `randomize_entrances(world, coupled=False, ...)`: its doc's "uncoupled randomization", nothing of ours. Every door
   stays two-way typed, so a door is only ever paired with a door.
2. **The same `door_targets`:** each door is rewritten on its own (x leads where y's partner leads), which is all the
   mod ever did, so the mod needed no change; a door of the pair no longer names the other.
3. **The spoiler** lists every door on its own (`=>`, 508 lines) instead of each pair once (`<=>`).

**Tests** (`test_doors.py`): what every shuffle shares (doors rewritten, only table doors named, every region reached,
the mod doing what the logic proved); the way back no longer always leads back; the spoiler lists every door.

**Status:** built, not yet seen in game (2026-09-30); experimental like build step 12.

*Code: `entrances.py` (`shuffle`, `write_spoiler`), `options.py`; tests `test_doors.py`.*

## Build step 32: Connection plando

A player's yaml can pin doors: "this door leads there". Archipelago's own plando option for it, `plando_connections`
(`Options.PlandoConnections`, the plando guide's "Connection Plando"). Optional in the guide ("Support for connection
plando may vary"), built anyway (2026-09-30, the user: "we should try to support all available things archipelago
has/does, that includes plando"; How it works §8).

**How it was built (2026-09-30):**

1. **The option is Archipelago's class** with our names: `DoorPlando(PlandoConnections)`, whose `entrances` and
   `exits` are every shuffled door's entrance name, `"<map>: <door>"`, as the spoiler lists them (`DOOR_NAMES` in
   `data_tables.py`). Archipelago checks the names (any case), refuses a door used twice, and drops the whole option
   when the host hasn't turned plando's "connections" on.
2. **Placed first, the rest by the randomizer:** after the doors are split, each plando connection is joined by hand
   (The Messenger's pattern), then `randomize_entrances` places what's left. entrance: the entrance door leads next to
   the exit door; exit: the exit door leads back to the entrance door; both: both. Coupled always joins both ways (as
   The Messenger does), Decoupled only the ways asked for. A door used by two connections is refused with the player's
   name.
3. **Only with Coupled or Decoupled:** with the doors off, or on Room Swap (whose rooms move whole), the connections
   are ignored with a warning in the generation log.
4. **Planned doors are pairings like any other,** so `door_targets` and the spoiler carry them: the mod needed no
   change.
5. **The preflight's import list** gained `PlandoConnections` from `Options`, in a commit of its own.

**Checked:** seeds generated through Archipelago's own Generate with a plando connection, alone and with APQuest, under
Coupled, Decoupled and Room Swap: the planned door leads where the yaml says (`<=>` coupled, `=>` decoupled in the
spoiler), and Room Swap ignores it. Tests (`test_doors.py`): Coupled joins each planned door both ways, Decoupled
only the way asked for, names match in any case, an unknown door is refused, Room Swap ignores plando, and every
shuffle test holds with plando on.

**Status:** built, not yet seen in game (2026-09-30).

*Code: `options.py` (`DoorPlando`), `entrances.py` (`_plando`), `data_tables.py` (`DOOR_NAMES`); tests `test_doors.py`.*

# How it works

## 1. The big picture

```
 generator + apworld  ──(makes)──>  seed file  ──(loaded by)──>  server
                                                                   ▲
                                                        websocket  │  JSON messages
                                                                   ▼
                                                     your game + its client (the mod)
```

- The **apworld** runs once, when a seed is generated, and decides where every item goes. It never runs
  while anyone plays.
- The **server** hosts the seed. It knows which item sits at every location.
- The **client** lives in or next to the game. It only ever talks to the server.

## 2. Opening the connection

The client opens a **websocket** to the server's address, for example `archipelago.gg:38281` or a local
`127.0.0.1:38281`. All messages are JSON, called "packets", each with a `cmd` naming its kind.

- `wss://` is the encrypted kind, `ws://` the plain kind. A local server you started yourself is plain, so
  write `ws://127.0.0.1:38281`. Without the prefix, a library may try the encrypted one first and time out.
- Rooms on the website can change port, so a client must let the player edit the port.

## 3. Logging in

The server speaks first. The order, from the protocol doc:

1. The client opens the websocket.
2. The server sends **RoomInfo**: the room's details.
3. Optionally, the client asks for the **DataPackage**, the tables that turn item and location numbers into
   names.
4. The client sends **Connect**: the game's name, the player's slot name, the password if any, and how it
   wants items delivered.
5. The server answers **Connected** (you're in) or **ConnectionRefused** (with the reason).
6. The server sends **ReceivedItems** with anything the player is owed.

**The game name in Connect must match the apworld's `game` exactly** (here, `Bug Fables`).

**How items are delivered** is chosen in Connect with three switches (`items_handling`):

| Switch | Meaning |
|---|---|
| items from other worlds | always wanted by a normal client |
| items from your own world | on: even items you find yourself come from the server |
| your starting inventory | on: the server sends it on connect |

This mod turns all three on, which makes it a **"remote items"** client: picking something up never gives
it directly, and everything arrives from the server.

## 4. Sending what the player found

When the player completes a location, the client sends **LocationChecks** with that location's number.

- **Duplicates are harmless.** The doc says the server ignores repeats. So after a reconnect a client can
  simply send every location it knows is done, which is also how checks made while offline get delivered.
- The server then sends the item at that spot to whoever it belongs to, you included in a remote-items
  game.

## 5. Receiving items

Items arrive in **ReceivedItems** packets. Each carries an `index`: the item's position in that player's
list of everything ever received.

- **After every connect, the server sends the whole list again**, including items from past sessions. So
  the client must remember **how many items it has already given the player**, and skip those.
- **Keep that number in the player's save**, per the doc. Then loading an older save gives back exactly
  what that save hadn't had yet, and a brand-new save gets everything.
- An `index` of **0** means "this is the full list"; anything else continues from where the last packet
  stopped. If the numbers don't line up, the client sends **Sync** and gets the full list again.
- A client must cope with **any item arriving any number of times**. Admins can send items, and starting
  inventory repeats.

## 6. Finishing the game

When the player reaches the goal, the client sends **StatusUpdate** with status **30** (goal reached).
Nothing else marks a slot as finished.

## 7. Settings from the seed: slot_data

The apworld can hand the client a small dictionary, **slot_data**, which arrives inside Connected. It's the
only way a setting chosen at generation (an option, a version number) reaches the game. This world puts in:

- `world_version`, so a mismatched mod and apworld can be caught;
- `artifacts_required`, the goal;
- how each location is done: `location_flags` (a game flag), `location_berries` (a crystal berry),
  `location_discoveries` (a journal discovery), `location_shops` and `location_item_shops` (a shop's copy or first
  purchase), `location_vars` (a number reaching a value, a boss prize);
- `location_gives`, the `giveitem` that hands out a gift location's vanilla item;
- `location_added`, an item the story puts straight into the bag at a location (build step 26);
- `location_pickups`, the map and flag of each location that is an item lying in the world;
- `silent_locations`, checks that show no item of their own (a member joining), where the player's own item is shown;
- `quiet_locations`, the opening's checks, whose items arrive with no hold-up (the mod guide, Item animation);
- the open world (build step 9): `kept_open`, `kept_present`, `scenery_hidden`, `scenery_present`, `held_until`,
  `present_from` and `dialogue_flags`, the story's blockers and scenery the mod keeps the way the logic assumes;
- `door_targets` (the entrance randomizer), `enemy_swaps` (enemy shuffle) and `start` (the starting location);
- `starting_member`: 0 Vi, 1 Kabbu, 2 Leif alone, 3 all three, -1 the story's party (build steps 18 and 20);
- `shuffle_moves` and `shuffle_jump`, whether the attacks and Jump are items (build steps 21 and 22);
- `item_kinds`, which inventory list each of its items goes to.

The mod does nothing from its own knowledge of the game's locations: every table it acts on comes from here.

## 8. Use what Archipelago provides

**The rule (2026-09-29):** whatever Archipelago or its official client library already does, we use, the way its
[docs](https://github.com/ArchipelagoMW/Archipelago/tree/main/docs) describe it (the local checkout at the targeted tag
says what our version has). We never build our own version of it. In the user's words: "there is a reason we are using
whatever archipelago does for websockets etc, and not trying to do something dumb/stupid like reinventing and
rebuilding archipelago inside the game just to connect/work with archipelago". A home-made version is more code to get
wrong, it drifts as Archipelago changes, and nobody who knows Archipelago can read it. **Recommendations too**
(2026-09-29, the user: "we should do all standards & recommendations that Archipelago mentions"): what the docs call
"should", "recommended" or "encouraged" (option groups, presets, a bug report page) is done like a requirement.
**Optional features too** (2026-09-30, the user: "we should try to support all available things archipelago has/does,
that includes plando"): what Archipelago offers a world as optional, connection plando first, is supported, not
written off as optional. **Read all of it** (2026-09-29, the user: "we should read and take a look at everything/anything Archipelago. don't
skip/assume"): every doc, every generic guide and the reference world, APQuest, including the ones that look meant for
someone else (the world maintainer's duties, the website's API); what doesn't apply is written down as not applying,
with why.

**The connection: a library.** Writing all of the above by hand is possible, but libraries exist for most languages;
the protocol doc lists them. For C# (Unity, BepInEx) it's **Archipelago.MultiClient.Net**. It handles:

- the handshake and every packet;
- turning numbers into names;
- a single call to log in.

**The apworld: Archipelago's own tools.**

- Rules with the Rule Builder, written in Python as its doc intends ("The rule builder is intended to be written first
  in Python"): `&` for and, `|` for or, and a game's own needs as custom rules registered the way the doc shows.
- Regions, exits and locations in Python modules, as `worlds/apquest` does.
- Tables generated from the game's own data (the door graph, the enemies) may stay JSON, read with `pkgutil`, as core
  worlds do (Pokémon Emerald's `data.py`, KDL3's `regions.py`).
- Doors shuffled by Archipelago's entrance randomizer (`entrance_rando.py`, its doc `entrance randomization.md`).
- Tests on `WorldTestBase`; packaging with the "Build APWorlds" launcher component; code in `style.md`'s style.

**Our own work only where Archipelago has nothing.** What no library or Archipelago tool can do for you:

- deciding **when** it's safe to hand the player an item in your game;
- **saving** the received-item count in your game's save;
- knowing **which spot** in your game is which location.

Those are the parts that make each game's client different. Anything still home-made that Archipelago does provide is
listed in "Where it stands" (Next), each replaced in a step of its own.

## 9. How this mod does it

- The login call can take several seconds, so it runs **off the game's own thread**
  (`ApConnection.Connect`). The result is handed back to the game thread through fields the game reads each
  frame, and log lines through a queue (`Post`, `Tick`). The game never freezes on connect.
- Connection settings (address, port, slot, password, compression) and the Archipelago mod switch live in
  the mod's BepInEx config file (`Plugin.Awake`).
- The client library and its JSON library sit in `BepInEx/plugins`, loaded once, and the mod itself
  reloads on its own during development.

**Custom gates are the mod's own items** (2026-09-26). Where the randomizer wants a gate vanilla doesn't have,
the mod makes an item of its own, added to the game's item table at runtime (an existing sprite, its own name and
description), and the gate is "has the item": the logic reasons about it like any key item, and the mod checks it in
the game. The Boat Ticket is the first (Next 21); the party members as items (build step 13) are the same idea on the
location side, turning a story moment vanilla never made a check into one. Each new item takes a filler slot in the
pool, so a seed needs one to spare, which a test pins.

**Custom items' text** (2026-09-26): written to fit the game, as the menus and art do. The game's own voice,
seen in play: items are witty ("Don't say you weren't warned."), medals strictly informative, and **key items say what
they open, then a small joke** ("This keycard can open doors in the Honey Factory. Whoever lost this probably got
fired."; "This key opens a door in Rubber Prison. A guard probably dropped it while fleeing from the Wasps."; the
Platinum Card's is plain). A custom key item follows that: what it opens, and a light line in the team's voice; never a
rules explanation. (The key items' texts aren't in `itemdata[0, id, 1]`, which holds "Desc" for them; they were read
on screen.)

**Icon ideas for custom items** (2026-09-26, from a labelled contact sheet of the item sprites; `SpriteDump`,
the mod guide, step 7). An icon is only lent: the item has its own name and description, and a reused or similar icon
is fine in the menus as long as those differ (seen in the Key Items list). Item sprite ids
(`itemsprites[0, id]`):

| Id | The game's item | Could stand for |
|---|---|---|
| 176 | Platinum Card (silver) | **the Boat Ticket** (chosen); 95 (Factory Pass, a yellow pass) the other card |
| 95 | Factory Pass | a pass or ticket, though players know it as the Factory Pass |
| 161 | Prison Key | a generic "Key", if one is needed (the other keys have odd shapes) |
| 111 | Rusty Key | looks like a sword |
| 160 | Lab Card | an ID or pass (reads differently from a plain card) |
| 138 | Shady Note | a paper, note or code |
| 159 | Big Gear | a cog: fixing something broken |
| 187 | (no name; the queen's face) | something enemy related |
| 171 | Big Mistake | a pixelated Mistake: a bad or negative item, or a trap |
| 42 | Magic Ice | an ice cube: an ice trap |
| 181 | Danger Dish | eating something bad: a poison trap |
| 180 | Plumpling Pie | a derpy face: a trap |
| 150, 155 | Crystal Feather, Aphid Shake | jump or other moves, if moves are split into items |
| 29 | Coal Crystal | a plain block: something neutral |
| 98 | Crimson Ore | a stone, ore or gem |
| 110 | Game Tokens | currency or tokens (e.g. medal shops locked behind tokens) |
| 140 | Red Paint | looks like soup |
| 149 | Package | a box or mail |

**Combined icons**: the mod can layer game sprites into a new icon, still the game's own art. Tried
as mockups for the ticket (2026-09-26): emblems in the card's corner looked stuck on; centred and tilted with the card
they read better, but grey or brown on silver has no contrast. The ticket uses the plain card; a combined icon needs an
emblem that contrasts with its base.

## 10. Things that go wrong quietly

Most connection mistakes don't crash; they just silently do nothing, or look like they worked. The full
list we keep is in [client-requirements.md](client-requirements.md), "Known failure modes". The ones that
matter first:

- The wrong game name, or a missing `ws://` for a local server, and the login fails or times out.
- Treating "the socket is open" as "we're in a seed": after a disconnect, rules must stay in force.
- Not saving the received-item count: every reload hands out every item again.
- An uncompressed connection works today, but the server warns that one day it may not. Turning compression
  on in websocket-sharp without accepting `server_max_window_bits` breaks every connection (build step 5).
- A lost connection that is only "disconnected" politely can keep a reading loop spinning in the background.
  The game just gets slower and uses more memory, with no error. Close the socket itself (build step 4).

## 11. The logic: regions, exits and rules

### In short

The logic is how the generator knows where it may put an item. It is made of three things:

- A **region** is a place: a part of the game you can walk around freely once you're in it.
- An **exit** is a one-way path from one region into another, with a **rule**: what you need to go through.
- A **location** (a check) sits in one region, and may have a rule of its own: what that one spot needs once you're
  there.

The generator starts in one region, the **origin** ("Menu"), and walks: through every exit whose rule the items it has
so far meet, into every region that opens. Every location it reaches whose own rule is met is somewhere it may place
the next item. That walk, repeated as items are placed, is how Archipelago proves a seed can be finished.

```
 Menu ──> BugariaOutskirtsOutsideCity ──[LoadZoneGoldenPath: Snakemouth Den Cleared]──> BOGoldenPath
                   │ DoorSnakemouth                                                      • Golden Path, Grass
                   ▼
   BugariaOutskitsSnakemouthCorridor1 ──> … ──> SnakemouthLake ──> … ──> SnakemouthMushroomPit
                                                                          • Mushroom Pit, Droplets [Freeze]
```

**Is a region a collection of rules?** Not quite. A region is a place; the rules sit on its ways in (the exits) and on
its spots (the locations). What a region does is *share*: every location inside gets "you got here" for free, and any
one of its ways in will do.

### In depth

- **Reaching a region:** some chain of exits leads to it from the origin, and every rule on that chain is met. Along
  one chain the rules add up (an *and*); between chains, any one will do (an *or*). So two exits into one region are
  an "or" nobody has to write.
- **Reaching a location** takes its region plus its own rule. Archipelago checks the region by itself (`world api.md`:
  entrances and locations "implicitly check for the accessibility of their parent region").
- **The origin:** Archipelago assumes the player can always get back to the region the logic starts from. The logic
  here leans on nothing outside the graph for that (the Warp never counts, build step 24, rule 9), so the graph itself
  makes it true: a one-way counts only together with what it takes to get back (rule 4).
- **Why a rule may only get easier:** the generator places items one at a time, each where the items placed so far can
  reach. That works only if having more items never shuts anything, so a rule can say "has" but never "hasn't"
  (rule 3).
- **Events:** some needs aren't items: a story step, a boss beaten. Each is an event, a location with no id holding a
  logic-only item (*First Boss Beaten* holds "Snakemouth Den Cleared"). It's reached like any location, and from then
  on its item counts for every rule, so a rule names a story step with `Has`, as it would an item. The goal is one
  too: "has enough Artifacts".
- **The rules are Archipelago's Rule Builder** (build step 29): `Has("Boat Ticket")`, `a & b` for "and", `a | b` for
  "or". Needs that depend on the seed's options are Bug Fables' own registered rules: `CanUse("Horn Slash")` (the
  ability's item when it's an item, its member when members are), `Member("Vi")` and `MoveItem("Freeze")`. When a seed
  is made, each becomes Archipelago's plain item checks, so an option that makes something free drops it from the rule.

### When to make a region, and why

Make a new region for:

- a part of a room you can't walk to freely from the rest (an obstacle, a ledge, water, a one-way drop);
- where a door or a transfer puts you (each arrival is a place of its own);
- where a scene moves the party;
- a place several spots share a need in, or a place with more than one way in.

Don't make one for a single spot that needs something extra where you can otherwise walk around: that's the spot's own
rule. A spot's own rule is written even when its region already implies it, so a different way into the region (the
entrance randomizer, a random start) can't lose it (build step 8, `TestInRoomRules`).

Why regions at all, instead of a full rule on every spot:

- **Written once:** what a place needs sits on its ways in, not copied onto every spot inside.
- **The "or" for free:** two ways in are two exits.
- **Doors can move:** the entrance randomizer rewires exits, and each rule moves with its exit.
- **The start can move:** a random start only changes where the walk begins.

### How this world does it

- **All of the logic is in the apworld; none is in the mod.** The mod only does what the generator decided
  (`slot_data`; build step 24, rule 7), so the game's side has no logic and no file per room.
- **Every map is a region, every door an entrance** (build step 12, 2026-09-30): `regions.py` makes them from the door
  table, named `"<map>: <door>"`, so Archipelago's entrance randomizer can shuffle them.
- **One Python module per game area** (`logic/`, build step 29): `outskirts.py`, `snakemouth_den.py`,
  `bugaria_city.py`, `metal_island.py`, `later_chapters.py`. Each lists its locations and story events (each in its
  map, with its own rule), its door gates (`DOOR_RULES`) and ways between maps that aren't doors (`TRANSFERS`). Per
  area, not per room: a room's logic often reaches into its neighbours, and an area is tested in one sitting. Menu is
  made in `regions.py`.
- **Today** (2026-09-30): 244 regions (Menu included), 582 entrances, 74 locations, 4 story events and 1 artifact
  event. Until the rooms are mapped (build step 24), what the old large areas needed is kept on each spot as its
  `reach`, beside its own `rule`. Adding a room is adding lines to its area's module, not code.
- **What a module looks like** (shortened):

  ```python
  # Every Snakemouth room with water droplets, or reached only through one: Leif freezes the droplets.
  UNDERGROUND = DEN & CanUse("Freeze")
  LOCATIONS = (
      Location("Snakemouth Den: Mushroom Pit, Droplets", 9, "SnakemouthMushroomPit",
               Source(flag=724, pickup=Pickup(map="SnakemouthMushroomPit", type=0, item=144)),
               rule=CanUse("Freeze"), reach=UNDERGROUND),
  )
  ```

  A spot with two ways to it inside its area writes both: `rule=Has("Key A") | Has("Key B")`.
- **Checking it:** a test for each measured need, which fails without the rule; `test_areas.py` for how the modules
  fit together; the Logic Test apworld, to play a seed's logic without the game (Next 42); and Archipelago's
  `Utils.visualize_regions`, which draws the whole region graph as a PlantUML diagram.
