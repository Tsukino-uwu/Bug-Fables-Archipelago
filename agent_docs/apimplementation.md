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

**How it works**

1. [The big picture](#1-the-big-picture)
2. [Opening the connection](#2-opening-the-connection)
3. [Logging in](#3-logging-in)
4. [Sending what the player found](#4-sending-what-the-player-found)
5. [Receiving items](#5-receiving-items)
6. [Finishing the game](#6-finishing-the-game)
7. [Settings from the seed: slot_data](#7-settings-from-the-seed-slot_data)
8. [Use a library](#8-use-a-library)
9. [How this mod does it](#9-how-this-mod-does-it)
10. [Things that go wrong quietly](#10-things-that-go-wrong-quietly)

## Where it stands

Each step's own status is its last line (**Status:**). This section holds only what's next and what's known to
be wrong.

**Next** (decided by the user, 2026-09-24):

1. **Every key item and medal in the pool,** on logic that follows the vanilla story order: one region
   per chapter, entered once the chapter before is finished and the story's own keys and abilities are
   in hand. Medal gifts and medal shops each get a yaml on/off toggle.
   Shops (medal shops, item shops, the caravan): see build step 11. Other kinds of location (boss prize medals,
   placeholders, journal entries, enemy drops): see build step 10.
2. **Entrance randomizer (experimental):** every door, coupled, built; next, sorting the transfers that aren't doors
   into chosen and forced, then the room-by-room logic that removes the label. See build step 12.
3. **Field abilities shuffled as items** (hover, dig, horn dash, heavy dash, big icicle, bubble shield).
   Party members stay where the story puts them.
   The basic moves and party members as items (*Starting Party Member*): see build step 13.
4. **Open world, one gate at a time** (always on, never an option; the user, 2026-09-26): see build step 9.
5. **To test later (the user, 2026-09-25): a two-player room.** The user's slot plus a second one the agent drives,
   sending items while the user plays, to see items from another player arrive live: the hold-up on *All* and
   *Progression*, silence for a replay after a new save or reconnect, and the multiworld names ("X's item").
6. **A full bag:** key items keep arriving, only ordinary items wait.
7. **Goal:** the mod counts the game's artifact flags and sends "goal reached" at the required number: done, seen
   (build step 3).
8. **A release: three separate downloads** (the user, 2026-09-25): built, see build step 17; v0.1.0 out
   (2026-09-26). The next one: `dev-scripts/release.ps1 -Version vX.Y.Z` after bumping both versions.
9. **The chat feed**, then the in-game text client (see the design list in the mod guide, step 2).
10. **A "Quality of life" page in the Archipelago panel** (the user, 2026-09-25): on/off rows that speed the game
   up and make it smoother: skips first, others later. Battle tutorials next (the mod guide, step 10).
11. **Map fast travel, built (2026-09-26; the mod guide, step 10), seen travelling to the Outskirts** (planned by the
   user, 2026-09-25), apart from the Warp to Start button. On the pause menu's map
   (window 6, which lists areas), pick an area you've been to and confirm (Yes / No) to travel to its save point
   through the game's own map transfer. The game already records visited areas (`librarystuff[4, area]`, set by
   `MainManager.UpdateArea`). The logic never counts on it, like the warp. **One row with the warp (the user,
   2026-09-26):** the Warp button's on/off becomes *Travel: Off / Warp / Map / Both* (Warp to Start only, map fast
   travel only, or both), so the two are set together. **The look (the user, 2026-09-26):** alone, either button
   looks like the Warp button does now; with Both, the two get different background colours. Map's icon: proposed a
   map icon always (so the button says what it does), the user undecided. Both means a sixth button in the pause menu:
   it must fit and look good there, seen on screen before it counts as done. **The order (the user):** both sit to
   the right of the game's buttons, Warp first, Map last. Left from the first button wraps round to Map for quick
   access, and Warp sits in between, so it's reached by accident less often. **How it's picked (the user, 2026-09-26):** on the pause menu's map, target a
   visited area and press confirm: a "Travel to <area>?" Yes / No box (No first). Confirm flips an area's description
   pages today (`PauseMenu.cs:1407-1433`, with Z the other way, wrapping), so while map travel is on, Z alone flips
   pages; nothing is lost, as Z wraps round. The game's map: a free cursor (`sprites[0]`) snapping to the visited
   areas' markers (`sprites[area + 1]`), `option` the area. Each area needs a travel spot: a save point in it, from
   the entity dump and each map's area (`MapControl.areaid`, now in the map dump).
12. **A quest board in the starting house** (the user, 2026-09-25): the quests every board lists, taken without
   walking to the town or the bar. Every board shows the same list (`MEASURED.md`, "The quest board"), so it adds no
   quests, only a shorter way. Every board lists bounties too (built, build step 9); next, the house's board from
   the start. **Decided (the user, 2026-09-25): every quest on the board from a new file**, the quests themselves
   still to do, each with its own logic (reaching its NPC, what it needs), so quests can be done along the way. First
   the `QuestDump` (a Debug setting). **Dumped 2026-09-25** (`MEASURED.md`, "Every board quest, dumped"): no quest's
   accept flag does anything outside its own quest, so opening them all is safe for the save; the unlock conditions
   are story flags and visited areas, which the mod would skip by adding every quest to the open list on a new file
   (six are added only by a dialogue, not by the table). Each quest's logic (its NPC's map, which the table lists, and
   any items) is still to write, per quest, before its locations exist.
13. **Bounties as locations, a yaml toggle** (the user, 2026-09-25): *Shuffle Bounties*, its own category, off by
   default (five hard optional bosses; progression shouldn't sit behind them unless the player asks). Today they are
   not locations and pay their vanilla rewards. First, measure what each bounty pays and when (on the spot or on
   reporting back); then the logic for reaching each boss. Its own build step when built.
14. **Enemy shuffle** (the user, 2026-09-26): `enemies_only` built; bosses, `both`, `chaos` and the map look next.
   See build step 14.
15. **Enemy scaling, a panel setting** (the user, 2026-09-26): Off / Party level / Artifacts, Party level by default,
   balancing an area met earlier or later than vanilla would. Built, not yet seen; a mod-side setting with no logic,
   so its design and status live in the mod guide, step 17.
16. **EXP multiplier, a panel setting** (the user, 2026-09-26; built as 1x-10x on the Gameplay page, `documentation.md` step 19): *EXP Multiplier* on the Quality of life page, 1x to
   5x, default 1x: an opt-in for a faster, easier game. Levels still give HP, TP and MP, so it helps even without moves being shuffled. It stacks on
   top of enemy scaling's EXP. No check and no logic depend on it. Only while Archipelago is enabled.
17. **Berry multiplier, a panel setting** (the user, 2026-09-26; built as 1x-10x on the Gameplay page, `documentation.md` step 19): *Berry Multiplier* on the Quality of life page, 1x
   to 5x, default 1x, the same opt-in. Only the berries picked up in the world (lying there or dropped after a fight), never a
   check's reward from the server. Only while Archipelago is enabled.
18. **Random start** (the user, 2026-09-26): `anywhere` built, experimental; `towns` and named spots to come. See build
   step 15.
19. **Traps, an idea for later** (the user, 2026-09-26; not planned yet). A trap sent to this game takes effect when
   the server delivers it, after any open text box, like any received item. Held up at pickup: its own icon on a red
   starburst. One icon per trap, so the player knows what's coming. The user's examples: the Mistake medal poisons
   the party at the start of the next fight; a crystal berry (or something icy) freezes the player in an ice block
   for 1-3 seconds. Each trap: only with Archipelago on, never a soft-lock (a freeze always ends, even in a scene),
   nothing written to the save the game wouldn't write, never in logic. The game's own effects to reuse (code read
   2026-09-26, not yet measured): fight conditions (`MainManager.BattleCondition`: Poison, Freeze, Numb, Sleep,
   Inked, Sticky and more), map hazards (`Hazards.cs`, three `HazardAction` kinds, likely the knockback), falling
   off a map (put back at `lastpos`, `PlayerControl.cs:688-691`), and ice (`EntityControl.inice`, set by ice maps).
   First measure how each is applied. A yaml option (how many traps), so its own build step when built.

20. **Enemy group sizes, a yaml option** (the user, 2026-09-26): its own option, apart from *Enemy Shuffle*, off by
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

21. **Boat Ticket** (Discord, decided by the user, 2026-09-26): built, see build step 16.
22. **Healing save crystals, an idea for later** (suggested on Discord; the user, 2026-09-26): an item that makes the
   blue save crystals (save only) act like the yellow ones (save and heal), a nice filler or useful check. The colour
   is not baked into the art (code read, 2026-09-26): a save point is tinted in code from its entity data, yellow when
   `data[2] == 0`, red when `data[1] >= 10` (`NPCControl.cs:1190-1217`), so the mod could turn every blue crystal
   yellow by setting its data before the map builds it, as the enemy look test does. Still to find: where saving
   decides to heal (the save prompt's handling), so the item gives the heal and the look together.

23. **Progressive items, an idea for later** (the user, 2026-09-26): items that unlock in a fixed order however they're
   found, as Pseudoregalia's progressive sword (three copies of one item; the first gives the sword, the second breaking
   blocks, the third the ranged attack). Archipelago counts copies of one item (`Has(item, count)`), so the logic is
   simple. Candidates: each member's field abilities in their game order, and other chains; decided when abilities
   become items (Next 3, build step 13).
24. **The panel's settings on normal saves** (the user, 2026-09-26; built, `documentation.md` step 18): an opt-in row so Quality of life and
   Gameplay also apply with Archipelago off. A deliberate exception to "vanilla stays vanilla", which only the user can
   make; off by default. **Named (the user): *Use on normal saves*, ON / OFF**, help line "Quality of life and Gameplay
   also apply with Archipelago off." Only the two pages' settings; nothing tied to a seed (items, checks, the shuffles).

25. **Consumable keys, an idea for later** (the user, 2026-09-26): custom items used up on a door, as the game's own
   `removeitem` takes an item (`items[kind].Remove(id)`). The rule the crystal berries set (build step 11): no action may
   make a check unreachable, so a key either opens one named door, or the keys and the doors that take them are exactly
   as many, with no door that could waste one.

**Known issues:**

- **A Kabbu / horn rule is owed** once party members or the basic horn become items (Starting Party Member).
  Kabbu and his horn are always there today, so these locations have no rule for them: 21 (a berry in a bush),
  25 (under a stone), 31 (a grass discovery) and 32 (past grass). Without the rule, a seed could be impossible.
  Not location 2: the horn tutorial cuts its grass itself and played through with Leif alone (the user, 2026-09-25).
- **Crystal berry #2 (location 20)** sits in the Underground region, which needs Leif, though the room's
  upper-left entrance needs nothing. More cautious than the game, so safe; room-level regions would split it.
- **Landmark names** for locations 2, 22, 23, 24, 25 and 30 are still to come from the user.

- The generator's manifest warning ("will stop working with Archipelago 0.7.0") came from a hand-zipped
  apworld. The properly packaged file (build step 1) fixes it once it replaces the copy in the installed
  Archipelago's `custom_worlds`.

---

# How we built it

## Build step 1: a first, tiny apworld

The apworld started deliberately tiny: two early locations, one key item (the Explorer Permit), the gate
it opens, and "open that gate" as a temporary goal. Items and locations live in simple JSON files, so
growing the world is mostly adding data. It follows the layout of `worlds/apquest`, Archipelago's own
teaching example, and writes its rules with Archipelago's Rule Builder.

We wrote **tests**, including one that proves the gate really needs the permit. To make sure that test
could fail, we removed the rule on purpose, watched the test fail, and put the rule back. Archipelago's
own test suite passes for it too.

To try it, the world folder is linked into a local copy of Archipelago (run from source), and seeds are
generated with `Generate.py`.

**Packaging it as a `.apworld` file** (to generate with an installed Archipelago, or to share): run the
"Build APWorlds" component from the Archipelago checkout, for this world only:
`python Launcher.py "Build APWorlds" -- "Bug Fables"`. It writes `build/apworlds/bug_fables.apworld`, leaves
out `__pycache__`, and adds two fields to the file's `archipelago.json`, `version` and `compatible_version`.
Don't write those fields by hand. A hand-zipped copy made the generator warn "Invalid or missing manifest
file ... will stop working with Archipelago 0.7.0" (2026-09-24). The built copy goes in the installed
Archipelago's `custom_worlds` folder.

**Rules the world follows** (the user, 2026-09-24, matching Archipelago's own definitions in
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
- **Which class each item gets** (the user, 2026-09-24): **if an item can unlock even one location, at any point,
  even if only sometimes or not always, it is progression. No ifs or maybes.** Every field ability is *progression*; a key item is *progression*
  when any rule in the logic uses it, even for a single location; one nothing uses is *useful*. Crystal berries
  buy medals at the crystal berry shop, so they become progression in the same change that puts that shop in
  the seed (test `TestClassifications` enforces both directions). Every medal is *useful*, except the Hard Mode medal (#11), which is *filler*: it only makes
  fights harder, and the Archipelago panel can do the same. A test will check both directions: an item a rule uses is
  progression, and a progression item is used by some rule.
- **Logic lives on regions and locations, never on items.** An item doesn't say what it unlocks. A region's
  exits say what they need (the gate out of the Outskirts needs the Explorer Permit), and every location
  belongs to a region. A location needing something more than its region adds that to itself.
- **A location is named after where it is, never after what it gives** (the user, 2026-09-24). Once items
  are shuffled, a hint like "your Hover is at Outskirts: Explorer Permit" points at the wrong thing. The
  form is `<Area>: <Room>, <Spot>` (the user, 2026-09-24): the game's own area name; a short room name from a
  landmark, left out for a one-map area; and the spot as **just a landmark**, a noun of one to three words with
  no articles or verbs, like `Snakemouth Den: Bridge Room, Pillar` ("Ledge", "Chest", "Waterfall"; a qualifier
  like "Top of Pillar" only when a room needs telling apart; two rooftops became "Rooftop" and "Fountain Rooftop",
  not "On Top of the House by the Fountain", which the user found too descriptive, 2026-09-25). Not a sentence and not a hint at how to get it
  ("On Top of a Pillar", "Under a Rock" are too much). **When a room has more than one of that landmark, add
  `by the <Thing>`** after it, naming something a player can see next to it: `Snakemouth Den: Lake, Bush by the
  Sign` (the user, 2026-09-24: this is how to tell apart which bush, rock or pillar). It says where the spot is,
  never what to do there: the berry is inside that bush, so "Bush" is right. Gifts are `<Area>: <Who>'s Gift` or `<Who>'s Reward`,
  like `Outskirts: Maki and Eetl's Gift`. A character's name only when players will remember it (main and
  recurring ones); a minor one is described instead ("Ladybug Kid's Reward", "Ladybug Siblings' House", for
  Leby and Dib) (the user, 2026-09-24). Never the item, the flag or a mechanic ("Beemerang" goes stale once
  abilities are shuffled), Title Case, one word per kind of landmark everywhere. The test
  `TestLocationNames` fails if a location's name contains its own vanilla item's name. Renaming a location
  never changes its id or flag.

**Status:** done; the world has since grown to 64 locations (59 by default) and 44 items (counted 2026-09-25).

*Code: `apworld/bug_fables/world.py` (`BugFablesWorld`: `create_regions`, `create_items`), the data in
`data/items.json` and `data/locations.json` (read by `data_tables.py`), tests in `test/test_logic.py`
(`TestPermitGate`).*

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

*Code: `mod/BugFablesAP/ApConnection.cs` (`ConnectOnWorker`); the address settings in `Plugin.cs` (`Awake`).*

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
later. The log says what it decided: `[goal] 0 of 1 artifacts`, then `[goal] sent: ...`. **Seen (the user, 2026-09-26):** beating the
spider boss (a dev file) logged `[goal] reached, 1 of 1 artifacts` and `[goal] sent`, and the server released the
slot's remaining items and logged "Team #1 has completed all of their games!".

**Status:** in progress: the goal is in the apworld, with only the first artifact so far; the mod sends "goal reached" at the required count, seen working (2026-09-26); more artifacts come with more of the world (Next 7).

*Code: `apworld/bug_fables/options.py` (`ArtifactsRequired`), `world.py` (`generate_early` lowers the
number, `create_regions` adds the artifact events), test `TestArtifactsCapped`; the mod: `LocationChecks.CheckGoal`,
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
back instead of climbing, its thread count went down, and its memory stayed flat. The user's on-screen check
that the game stays smooth is still to come. When the server came back, the mod reconnected by itself
within about 6 seconds. (Since build step 5 the socket is a different library's, and the mod closes it with
that library's own close call instead. See step 5, point 5.)

**Status:** works: refusal, retry and reconnect tested on a local server, the drop measured (2026-09-24); the user's on-screen check that the game stays smooth is still to come.

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

Sending checks came before receiving items because it's easier to test: the user can reload a save and
redo the same find as often as needed. The test location is Artis's medal, the first medal in the game,
right after the Explorer Permit. It was added to the apworld as a third location for exactly that reason.

**How the mod knows a location is done.** The game already remembers every finished event with a *flag* in
the save. The probes showed which flag belongs to which location (`MEASURED.md`). So:

- The apworld's `locations.json` lists each location with its flag. The apworld sends that list to the mod
  in `slot_data` as `location_flags`. The generator stays the only source of truth: the mod only watches the
  flags of locations the seed actually has.
- Every frame, while a randomizer save is being played, the mod reads those few flags. When one becomes true,
  it sends that location's check. It only reads flags; it never changes them.

**Offline play needs no extra queue.** The flags are saved with the game, so a location finished while the
server is down is found again at the next login and sent then. **But the seed must be known first:** the mod
keeps no copy of `slot_data` on disk, so until the first login of a game run it can't tell a location from any
other pickup. The file select therefore refuses randomizer files until then (the user, 2026-09-24;
`documentation.md`, step 8). A drop after that login keeps everything in force. Within a session, the client library keeps
every check the server hasn't confirmed and sends it again with the next one.

**What the log shows:** `[check] watching ...` (which locations and flags), then `[check] location ... is
done (flag N set): sending`, `[check] sent ...`, and `[check] now checked on the server: ...`.

**Tests:** the apworld checks that every location has its flag in `slot_data` (the permit's is 15, the
medal's 32), and that the world version is written in one place only (the manifest). Both fail without the
change. The world version went to 0.2.0.

*Code: `apworld/bug_fables/world.py` (`fill_slot_data`), test `TestSlotData`; in the mod,
`LocationChecks.cs` (`Tick`) and `ApConnection.cs` (`ReadLocationFlags`, `SendChecks`).*

**Not yet:** the game still hands out its own item at the location, the medal here. Replacing that with the
server's item is the next step (done since: the mod guide, step 9). A save from another seed would have sent its finished locations here; build step 7 ties each save to its
seed, which closed that.

**Seen working (2026-09-24, local server).** The user loaded a save from before Artis, already past the
permit. On loading, the mod sent the permit's location at once (flag 15 was already set: the save acted as
the outbox). Talking to Artis sent the medal's location (flag 32). For both, the mod logged `sending`, the
server's confirmation and `sent`, and the server logged `BugTester sent ... (Outskirts: Explorer Permit)` and
`(Outskirts: Artis's Medal)`. (Those two were later renamed `Outskirts: Maki and Eetl's Gift` and
`Outskirts: Artis's Gift`; same ids and flags.)

**Status:** works, seen by the user (2026-09-24, local server).

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
  **Why it's needed** (the user asked, 2026-09-24): it's the seed that counts, not the address, so the same
  room hosted elsewhere is fine. But the count only means something within one seed, since items can be owned
  more than once and "give what's missing" can't be told from the bag. A save from another seed would skip or
  double items, and its old flags would send checks this seed never had.
- **For test files only, `AdoptSeed`** (Debug, off by default; the user, 2026-09-24): a save tied to another
  seed is re-tied to the connected one with its count back to 0, so a new test seed doesn't mean replaying the
  opening. The old seed's items and flags stay in the save, which is why it's never for a real game.

**Only when it's safe, one item per frame:** only while the player is free (no battle, dialogue, cutscene, pause
or map change). Never during a battle, because retrying a lost battle restores the count but not key items.

**Medals** (2026-09-24; tested: Hard Mode, sent from Artis's location, arrived in the medals menu, the user on
screen) go in through the game's own `MainManager.AddBadge`,
unequipped, like any medal found. The game numbers medals separately from items, and the two ranges overlap,
so a medal's Archipelago id is offset by 1000 (`data_tables.py`, `item_id`; the mod's `ItemIds.cs`), and
`item_kinds` marks it kind 2.

**Where items go:** key items to key items, ordinary items to the bag, then storage when the bag is full. If
both are full, the item waits until there's room (items are given strictly in order, so the count stays right).
**Decided by the user (2026-09-24), not yet built:** a full bag must never block progress. Key items keep
arriving, and only the ordinary items that don't fit are held until there's room. That needs a count that
can skip past a held item.
The same operations the game's own code uses put them there.

**Crystal berries** (the user, 2026-09-25) raise the game's berry counter, the one the pause menu shows and
Shades's shop spends. The game also keeps a *total found*, which it counts from the berry spots picked up; a
text command shows it, and 50 unlocks a logbook entry. In a seed, spots and berries are different things: a
spot sends a check, and the berry comes from the server. So the mod keeps **berries received** in number slot
69 (the last one found unused, `MEASURED.md`), and in a seed the total is that count plus berries picked up at
spots that aren't locations in this seed (all of them when the seed doesn't shuffle berries). Received berries
replay with everything else, so a fresh save rebuilds the count. A save that received berries before this
change counts only the berries it receives afterwards (test files only). Built, not yet seen in game.
Which dialogue shows the total isn't known yet; the mod logs each time the game asks for it (`[berries]`).

**Seen working (2026-09-24).** The server already held the Explorer Permit and the G-Bug Ranger Plushie from
the swap test. On loading, the save tied itself to the seed, and both arrived in key items as soon as the
player was free (the user saw them). Talking to Artis again showed the plushie but gave no second one: each
item comes once per seed, and the count in the save keeps it that way.

**Status:** works, seen by the user (2026-09-24): items and medals, each once; crystal berries built, not yet seen in game; the full-bag rule not built yet (Next 6).

*Code: `mod/BugFablesAP/ItemReceiver.cs`: `CountSlot` and `SeedSlot` (the two save slots),
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
level geometry, with no object at all. Decided (the user, 2026-09-24): **a cautious default, then checks
on screen.** Every location on a map with an ability's obstacle needs that ability. Each gate the user
confirms, or rules out, in game adds or loosens a rule, and hover and bubble-shield rules come only from those
checks. Cautious logic never makes a seed impossible; it only makes placement less random until it's refined.
**Some of it is readable after all.** The bubble shield walks across one kind of hazard, `WalkableSpike`
(the game switches that hazard's collision off while the shield is up), and hazards are components on each
map's prefab. So `MapDump` in the mod reads every map prefab without instantiating it: its hazards by
type, its electric triggers, and its auto-start events (story steps the map itself starts). That gives the
bubble shield's maps from data. Hover has no object at all; pits (`Hole` hazards) are only candidates.
Obstacles for moves that are never shuffled (Kabbu's horn on grass, Vi's beemerang on switches) aren't gates.

**Where each story step starts.** `dev-scripts/event-triggers.py` looks in every place the game starts an
event: talking to an entity, trigger objects, dig spots, pickups, locked doors, dialogue lines, a map's own
auto-start list and literal calls in code. It found the start of all but one of the gate events. Two
surprises: one gate is a locked door that needs a key item (so a key item gates a whole area), and dig
spots bury items, which are locations the floor-pickup count had missed, each needing dig.

**Indoor pickups are behind a door** (found on screen, 2026-09-24). A pickup the logic had open from the start
turned out to be inside a house that opens later, and the dump hadn't kept which building interior an entity
is in. It does now (`insideid`). An indoor pickup's region is behind its building's door and that door's
flags, never just its map. The wrongly placed location was retired: its id is never reused.

**Checking locations with the user, in game.** The data lists every pickup on a map, but not whether chapter 1
can reach it. So the dev console warps the user next to each candidate, and the user says whether it's reachable
and names its landmark (verdicts, 2026-09-24): the Snakemouth bridge room and lake pickups are reachable with
Vi's beemerang; the two in the mushroom pit need Leif, to freeze the water droplets; the top of Snakemouth and
the east Outskirts map are closed in chapter 1. The Snakemouth top's door has no story flag, so an ability or the
level itself blocks it. The east map's door has none either; an invisible blocker outside the city, there from
the first boss until chapter 2 starts (flag 67), is the likely reason, still to test.

**Leif and the water droplets** (the user, 2026-09-24): a room with water droplets needs Leif to freeze them, and
so does every room reached only through one. The dump marks droplets (`Dropplet` entities), so the split comes
from data: from the Snakemouth entrance, follow the doors without entering a droplet room. What that reaches
(entrance, bridge room, door room, fall room, lake) is the region *Snakemouth Den*; every other Snakemouth room
is *Snakemouth Den Underground*, whose entrance needs the story event *Leif*. Leif joins at the lake (Event14,
flag 16), which is on the open side, so the logic can't go in circles. The first boss's treasure room is
underground, so Artifact 1 needs Leif too. Story events like this one live in `locations.json` under
`story_events`; a location can also list extra `requires` of its own. Tests `TestLeif` fail without the rule.
**Three kinds of rule, kept apart** (the user, 2026-09-24, with entrance rando in mind): what it takes to *reach*
a room (on the connections into it), what a spot needs *once you're in the room* (on the location, e.g. the
mushroom pit's Gummies need Leif's ice while its medal needs nothing), and what it takes to *cross* a room from
one door to another (a connection through it; a room split by an obstacle becomes two regions). The in-room
rule is written even when the region already implies it, so a different way into the room can't lose it (test
`TestInRoomRules`).
**Into chapter 2** (the user's play-through, 2026-09-24): two more story events, each a region gate from the gate
table. *City Opened* (Event60, flag 107, after the first boss) opens the door from outside the city into *Bugaria
City*; *Chapter 2 Started* (Event45 at the Ant Palace, flag 67) opens the palace rooms and city districts (*Ant
Palace*, since renamed *Bugaria Inner City*: it also holds the districts). Story events can have their own
`requires` now (the city needs the first boss). Locations there: a Lore Book behind the library bookshelf (test
`TestChapterTwo`, which fails without the gate), and the old book delivery, board quest 33, whose reward is a
Lore Book (category quest; the user played it through). **Mid-quest items are shuffled too** (the user,
2026-09-24): otherwise a quest's middle stays vanilla. The same cicada hands over the old book (Quest Book, flag
241), which becomes its own location (*Old Book Delivery Start*); the Quest Book is a progression item, and the
reward (*Old Book Delivery Reward*, flag 243) requires it (test `TestMidQuestItem`). **The quest's middle step is its
own event** (the user, 2026-09-25: book from the cicada, handed to a reader in the palace library, back for both
rewards): *Old Book Delivered* (flag 242, the library) needs the book, and both rewards need that event (test
`TestOldBookChain`), so a room-level world can't expect the rewards without the library. A step event carries its
quest's category and is left out with it (`included_events`), since without the quest's items it couldn't be reached. With Shuffle Quests off the
whole quest stays vanilla together. Still to see in game: that the recipient accepts a Quest Book received from
the server.
**Mapping connections, one-way included** (the user, 2026-09-25: for room-level regions and a later entrance
rando). An entrance shuffle can only pair a two-way door with another two-way door; a one-way link marked two-way
can strand the player. So every connection is recorded with its direction. How:
1. `dev-scripts/door-graph.py <entitydump> [map prefix]` lists every door between maps, its gating flags, and the
   doors on the target map that lead back ("NONE": a one-way candidate).
2. Play decides the rest, since the dump can't see inside a map: a drop off a ledge, a barrier opened from one
   side, a door whose other side can't be climbed back to. The user's findings go into `MEASURED.md` as they're
   seen (the first: Snakemouth's switch-room ledges, paired doors that are one-way in play).
3. When rooms become regions, the table plus those notes become the region graph, each exit one-way or two-way.
**The general rule** (the user, 2026-09-24): if reaching something uses an ability, the logic requires that
ability. Leif is in effect the freeze ability. Some droplet rooms are optional, so this is stricter than the game,
which is the safe direction: never impossible, only less random.

**An impossible seed, and what it taught** (2026-09-25). The user stood in the Outskirts with nothing left to
reach: the seed had put the Explorer Permit on *Outskirts: Favor Reward*, which the logic thought was open from
the start. Its entry mixed two sources. Its check (flag 17) is set by a scene on `NearSnakemouth`, past the permit
gate (Event10, 10 berries, written in the event's code), while its "30 berries on the Outskirts" came from a
ScriptDump line that belongs to an unrelated NPC who appears only much later. So a location's flag, its give and
its region must all be traced to **the same scene**, and a give written in an event's code won't show up in the
ScriptDump at all. The fix moved it past the gate as *Outskirts: Near Snakemouth Den, Reward* with its real give,
and the test `test_only_two_locations_before_the_gate` now pins what the user knows from play: before the permit,
only Maki and Eetl's gift and Artis's gift are reachable. It fails on the old data.

Still to do: the one event not found, characters that block a path, the region graph built from all of
it, and the tests.

**Status:** in progress: the one event not found, characters that block a path, the region graph and its tests.

*Code: `dev-scripts/gate-table.py`, `dev-scripts/event-triggers.py`; the dumps in `mod/BugFablesAP/EntityDump.cs`,
`MapDump.cs` and `ScriptDump.cs`.*

---

## Build step 9: keeping the world open

The world is open by default: each gate the story would close (a blocker, a door, a guard, a story flag) is opened
by the seed on its own, from lists in `slot_data` decided at generation, tested on screen and known to the logic.
This step is those lists (`kept_open`, `kept_present`, `held_until`, `present_from` and the rest) and each gate
opened with them.

**The open start is not an option: every seed starts open** (the user, 2026-09-26: building everything twice, for a
linear and an open game, isn't worth it; open, metroidvania-like games work best in Archipelago). This replaces the
"open start" yaml option planned on 2026-09-24 (skip the prologue and tutorial, optionally with Leif from the start (the new-game party `{0, 1}`, `MainManager.cs:3591`, becoming
`{0, 1, 2}`; early cutscenes are written for two, so tested on a fresh file). A full story strip, as the Metroid
Fusion randomizer does, isn't the plan: here every cutscene also changes the world through flags.) Leif from the
start is now *Starting Party Member* (build step 13).
**Open world is the default, not an option** (the user, 2026-09-25: nobody picks a linear game in
Archipelago). The target: the world open as if the story were done, nothing collected, the ending gated by the
artifact count. **Built one gate at a time, never by forcing chapters done** (decided 2026-09-25): "chapter done"
is the artifact flag the goal counts, and a finished world is hundreds of story flags, many of which remove
locations (bosses beaten, characters gone, quests closed, cutscene gifts skipped). So each gate (a blocker, a
door, a guard, a story flag) is opened by the seed on its own, tested on screen, and known to the logic; key
items and abilities become the real gates (the Peculiar Gem for Upper Snakemouth); story events and bosses stay
as locations. The goal stays "collect N artifacts". The ending's gate is researched without spoiling it for the
user, who hasn't finished the game. Regions stay whole areas for now; one region per map (doors from the dump,
`dev-scripts/door-graph.py`) comes as the gates open (the user, 2026-09-24).
**Areas and doors that close later are kept open** (the user, 2026-09-24), as Pokémon Emerald keeps Mirage
Island visible: the mod makes the game's `CheckIfCanExist` answer "exists" for a list of doors and blockers
sent in `slot_data`, decided at generation, with no save writes. Each is checked in game first; where forcing
one open breaks the story state, its locations are left out instead. **First case, built 2026-09-24:** after the
first boss, Eetl turns you back outside the city (`eetlblocker1 - Duplicate`, Event12, until chapter 2's flag
67), closing the way back to Snakemouth Den (the user). Event12 only walks the player and sets no flags, so it's
safe to remove. `locations.json` lists it under `kept_open`, `slot_data` carries it, and the mod's `KeptOpen`
gives that entity a marker `limit` array after the map creates it, which its prefix on `CheckIfCanExist`
answers with "hide" (test `TestKeptOpen`). Not yet seen in game. Day/night map pairs are made reachable
both ways (like Emerald's Shoal Cave tides). One-way drops stay as they are: the logic handles one-way
connections.
**Keeping ways present** (the user, 2026-09-25: no dead end in chapter 1, and the Gem opens chapter 5 whenever
it's found). The reverse of kept open: `locations.json` lists under `kept_present` entities the story only makes
later, `slot_data` carries them, and the mod's `KeptOpen` gives each a marker `requires` array right after the map
creates its entities, which its `CheckIfCanExist` prefix answers with "exists". First three: Snakemouth's big door
to Upper Snakemouth (its model already looks open from the trapdoor fall, flag 14, per the map dump; the Peculiar
Gem slot behind it is the real gate), and the fall room's bounce mushroom and door back up (from the first boss in
vanilla). The fall room's chapter 1 blocker joins `kept_open`. The ordinary door down into the fall room is left
alone: before the trapdoor event it would skip that event, where Leif's joining starts. Tests `TestKeptPresent`.
**Built 2026-09-25, not yet seen in game;** it needs a fresh file played through chapter 1, to see that the spider
fight and Leif's joining still play out with the way back up open.
**Seen, and a hole found** (the user, 2026-09-25): the bounce mushroom and the door at its top were there before the
first boss, and the user went up. But then there was **no way back down**: the door room's door to the fall room
(`LoadZoneFallRoom`) also needs 41, and before the boss the fall room is reached only by the trapdoor drop. Going up
before the spider fight cut the file off from Leif, the spider fight and the lake. Keeping one direction of a
connection open means checking the other direction too. The fix, a new `slot_data` list, `present_from` (map, entity,
flag): the entity's `requires` becomes that earlier flag, so the door exists from the trapdoor (14) on. Not from the
start, which would skip the trapdoor scene where Leif's joining begins. Test `test_way_back_down_from_the_trapdoor`.
**Confirmed by the user (2026-09-25):** on a chapter 1 file before the first boss, they went up and down between the
fall room and the door room several times, and through the big door and back; then on to the spider fight. The
spider fight (Event6, discovery 1) and Leif's joining at the lake (Event14, flag 16) then played out as in the game,
with no errors: the ways kept open don't disturb the chapter 1 story (the user, 2026-09-25).
**The Outskirts rocks** (the user, 2026-09-25: before chapter 1 is done, a rock pile cuts the Outskirts off, so
only the way to chapter 1 is left). Everything that changes on that map at the end of chapter 1 is flag 41, read
from the entity dump and the map dump's flag-scenery list: the rocks (`Base/BlockingRocks`, scenery hidden from
41), the Golden Path exit (needs 41), NPCs swapping. The rocks guard three things: the ladybug siblings' house (no
gate of its own), the bottom-right exit (whose load zone still needs 41, so it stays a dead end) and the town
door. The town door's first visit is a trigger with no gate of its own either (Event60: the first-entry scene
into the city, which sets flag 107), and the city is written for chapter 2. So two new `slot_data` lists:
`scenery_hidden` (map plus the object's path in the map; the mod's prefix on `ConditionChecker.Start` gives it a
marker `limit`, which the existing check answers with "hide") and `held_until` (map, entity, flag; the mod adds the
flag to the entity's own `requires`, so the game keeps the trigger away until the first boss and brings it back
after). The user chose rocks and house now, the town as a later gate of its own. The house's logic stayed cautious
(it needed the first boss) until play showed otherwise. Tests `TestOutskirtsRocks`.
**Seen by the user (2026-09-25), chapter 1 file:** the rocks were gone, the user walked into the house (the ladybug
siblings, who come with flag 41, weren't there) and took its item: the seed's Crystal Berry, check sent from flag 679.
So the house now needs nothing. With the rocks gone the east road opened too; there the user knocked a stone with
Kabbu's horn and found a Drowsy Cake (flag 735), now a location (*Outskirts: East Road, Stone*), and picked
up crystal berry #10 at the pier with no abilities (*Outskirts: Pier*). The miners working
at the rocks (gone from 41 in the game) mine nothing now, so they join `kept_open`. The test
`test_only_what_play_showed_before_the_gate` pins the locations reachable before the permit (five, with the pier's
crystal berry). **The town door does nothing before the first boss** (the user walked up to it, 2026-09-25: "the
entrance does not work/do anything"), which is the held trigger. **The lists must reach a map already loaded:** after a
plugin reload or a seed change, a map loaded before the login is built as vanilla, and the user saw the rocks come
back until they left and re-entered. `KeptOpen.Tick` now applies a newly arrived set of lists to the current map (the
same marks as at map load, and the scenery hidden the way `ConditionChecker.Start` hides it); the log shows it
removing the miners on the Outskirts right after a reload.
**The town waits for its companion** (2026-09-25). Tried with Leif (the user: try the city with Leif): its first-entry
scene lines up Vi, Kabbu and Leif by character (`GetEntity(-4)`, `-5`, `-6`), so the hold moved to Leif's flag 16 for
one seed. With Leif added, the scene loaded the plaza and threw `ArgumentOutOfRange`: its fourth entry is
`GetEntity(1000)`, the map's first temporary follower, a companion who joins in Event63, the scene outside the city after
the first boss, which also sets flag 114 (`EventControl.cs:10034-10035`). Held until 114 for one seed, but that is
the first boss in practice, and the user wants the town open from the start ("especially if we are trying to make
this game openworld"). The arrival scene gives no check and its only effect is flag 107, which nothing but the city
doors reads (entity dump, map dump, ScriptDump). So **the scene is removed** (`kept_open`) and **the real door kept
present** (`kept_present`): the town is a plain door from the start, no companion needed. The plaza has no scene of
its own, and its blockers keep the party in it until chapter 2 starts. That start (Event45, the palace) lines up the
same companion, so its trigger is held until 114 (`held_until`), and in logic *Chapter 2 Start* now requires the
first boss explicitly, while the city region needs nothing and the *Entering the City* story event is gone. Tests
`test_town_open_from_the_start`, `test_the_city_is_open_from_the_start`, `test_chapter_two_needs_the_first_boss`.
**The companion in ordinary lines:** a theater NPC's line in the city also asked for the companion
(`GetEntity(1000)` inside `SetText`) and threw. The city is written for after chapter 1, so more lines will. The user
chose a fallback over closing the town again: `PartyFit` answers a follower lookup that finds nobody with the party's
leader (no crash; the companion's line comes from the leader) and logs each map and id once, so a scene that truly
needs him can be held back individually. Scenes that read `map.tempfollowers[0]` directly, like the palace's Event45,
aren't covered and stay held. **Seen (the user, 2026-09-25):** the plaza NPC's conversation played through; the log
shows the companion asked for on `BugariaMainPlaza` outside any scene and the leader answering.
**The rest of the town** (the user, 2026-09-25: "can we remove the block here"): three blockers in the plaza (`MM`,
`blockereetl2` and its duplicate, Event12, until flag 67) kept the party in the plaza. No city map has a scene that
starts on its own, and every other city scene trigger needs flag 67 or later (entity dump, map dump), so they join
`kept_open` and the districts can be walked early. The palace's own blockers stay (the story goes on there). The
districts' checks keep requiring chapter 2 in logic until they're seen working. Test `test_plaza_blockers_removed`. With the
blockers gone the exits still did nothing (the user): the plaza's doors to Commercial, Residential and the theater
require flag 67 themselves, and a `Cube` in the plaza hides at 67. The three doors join `kept_present` and the cube
`scenery_hidden`. Lesson: an area closed "until chapter N" is closed by several things at once (blockers, doors,
scenery); list every entity and scenery piece gated by that flag before opening it. First find
in the open town: the Bad Book (key item 174, flag 621) outdoors in the residential district, reached with Kabbu's
horn before chapter 2 (the user, 2026-09-25): a location in the *Bugaria City* region, open from the start. Then the
Bug Me Not! medal (flag 59), also outdoors in the residential district, which needed Leif's ice: the same region,
requiring Leif (test `TestTownMedal`).
**The bar and the quest boards** (the user, 2026-09-25). The way down to the underground bar (Shades's crystal-berry
shop, and a bounty board with no gate) is a spot examined on the Commercial map whose last line, the way down, needs
flag 135. That flag also brings story characters and a scene (Event79) to the district, a battle helper and more, so
it isn't set: a new `slot_data` list, `dialogue_flags` (map, entity, flag, to), repoints that one line to flag 691,
which the new-game scene always sets. An entity picks the last line whose flag is set (`NPCControl.cs:4320-4326`), so
the way down is always taken. The town's and the Outskirts' quest boards (requires 67) join `kept_present`; each quest
still needs checking before the logic counts on it. Tests `TestBarAndBoards`. Not yet seen.
**Madeleine's house on the Outskirts, open from the start** (the user, 2026-09-25). Everything tied to it is flag 390,
set by one conversation in a later area: a locked-door check (`lockeddoor`, until 390, now `kept_open`), the real door
(`doormadeleine`, from 390, `kept_present`) and a lock (`Base/lock (1)`, `scenery_hidden`). Inside, only her and her
butler need 390, so they stay away and none of her story starts early; the rest is two plain pickups, a Burly Tea
(flag 686, the retired location 4, back under id 44) and a Lore Book (flag 392, id 45), both reachable from the start.
Test `TestMadeleinesHouse`. **Seen (the user, 2026-09-25):** walked in on a chapter 1 file and took both; the swap
showed the seed's items (a Sleep Resistance medal, a Crystal Berry) and both checks went out.
**The boat to Metal Island crashed with two in the party** (2026-09-25). With the rocks gone the user reached the
pier on a chapter 1 file, paid the fare, and the boat scene (Event107) threw IndexOutOfRange: it seats three party
members (`p[0..2]`, `EventControl.cs:17944-17946`), and in the game the pier is behind the rocks until the first
boss, so Leif is always there. Opening a gate means checking every scene behind it for what the story guaranteed.
The sailor joins `held_until`, waiting for Leif (flag 16); the user chose Leif over the first boss, because a party
rule suits a random start later, when party members may be items and this becomes a party-size check. **Removed
(2026-09-26, the user):** with the stand-ins (the mod guide, step 11) the boat scene asked for Vi and Kabbu, got
invisible stand-ins, and the boat left with Leif alone, the fare waived by Free boat. The sailor is always there, as in
vanilla ("before we added the bandage workaround"), and Metal Island's checks need no party member for the boat.
**The plaza's discoveries open from the start** (the user, 2026-09-25): before chapter 2's briefing (flag 67) a
stand-in (`Discovery Pre Briefing`, a Check saying "We can check this out later. Let's hurry to the castle.") stands
where the plaza statue and the inn portrait will be; the discoveries themselves (`StatueDesc`, Event38, discovery 5;
`InnPortrait`, Event37, discovery 4) require 67. Both stand-ins are kept out of the way and both discoveries kept
present (`kept_open`, `kept_present`); the two events ask for members by name and set no story flag. Test
`TestKeptOpen`. Seeds generated solo and with APQuest; **seen (the user):** the statue can be examined.
**The inn from the start too** (the user, 2026-09-25: couldn't stay while escorted): the innkeeper's default line
(53) hands the talk to the follower, "We mustn't keep the Queen waiting."; from flag 67 line 1 offers a stay. The
line's flag is repointed to 691 (set by every new game, `dialogue_flags`, as for the bar entrance). Test
`TestKeptOpen.test_inn_open_before_the_briefing`; seeds solo and with APQuest. Not yet seen. Also found: the stay
costs a fixed 9 berries, "3 berries a bug" written for three (`checkmoney,9` then `money,-9`, line 2), whatever the
party's size.
**Chapter 2's three opening scenes held in story order** (the user, 2026-09-25: "it's the only place the follower is
removed and changed to another"): after the first boss a follower joins outside the city (Event63: follower 30, flag
114); the palace bridge swaps them for Maki (Event44: removes 30, adds Maki, flag 66), the only place follower 30
leaves; the briefing (Event45) uses Maki and clears the list. In the game the town opens only after chapter 1, so the
bridge always comes after the boss. **The briefing waits for the swap itself** (the user, 2026-09-25: a shuffled door
or a random start inside the palace could reach it without the bridge, with no follower or the wrong one): its hold
moved from 114 to 66, so first boss, follower, swap, briefing, whatever the way in. No new logic: the bridge is in the
town, which *Chapter 2 Start* already needs. With the town open from the start the bridge could come first: Maki early, and
follower 30 never leaving. The bridge's trigger (`makiautoevent`) is now held until 114, like the briefing's already
was (`held_until`). Test `TestKeptOpen.test_follower_swap_waits_for_the_first_follower`. Not seen (the user's file is
past it).

**Every quest board lists every open quest** (the user, 2026-09-25: all boards should act the same). The game keeps
one list of open quests, but each board filters it: the five bounties show only on the underground bar's board, and
every other board hides them (`MEASURED.md`, "The quest board"). In a randomizer save the mod drops that filter, so
any board, the town's or the starting house's, offers bounties too; what the game hides everywhere (the chapter
entries, Leif's) stays hidden. The logic needs no change: it never counted on a board, only on the quest's own
region. **Next, the starting house's board from the start:** it waits for chapter 2 (flag 67), and so does its
caretaker, an Eetl in the house whose line takes the quest; whether to keep him present early or have the mod play
that line is decided after reading it in game. *Code: `QuestBoards.cs`.*

**Status:** in progress: the Outskirts rocks, the fall room both ways, the town and its districts, the plaza's companion fallback and statue, Madeleine's house, and the bar with its quest board seen by the user (2026-09-25); every board listing bounties (built 2026-09-25), Eetl's blocker, the inn, the boat's hold and chapter 2's held scenes not yet seen; the open start is always on, not an option (the user, 2026-09-26).

---

## Build step 10: more kinds of location

Beyond floor pickups and gifts, a location can be a berry reward, a crystal berry, a pickup that comes back, a story
pickup, a quest's reward or its middle, a boss's prize medal, a journal entry, or later an enemy's first defeat.
Optional kinds each get a yaml toggle that states how many checks it adds.

**Berries are shuffled like items** (the user, 2026-09-24): a berry reward is the same `giveitem` as an item, type
-1, so it's a location, and its amount goes into the pool as an item such as *10 Berries* (kind 3, its own id range;
filler). Two checks can share one flag: the delivery quest pays 15 berries and a Lore Book at flag 243, so both
are locations and are sent together. In the mod, receiving berries uses the game's own money reward (capped at
999); at a berry location the command is turned, just before it runs, into a hand-over the item swap already
handles (`BerryPrefix`). **The pool is now exactly the included locations' vanilla items** plus padding: an item
whose vanilla spot isn't a location (the Plushie at the theater) stays with the game (test `TestBerries`).
**Crystal berries** (the user, 2026-09-24: the first thing you pick up): a counted currency (`flagvar[14]`, the
crystal berry shop's counter), 50 berry spots each known by its `crystalbflags` index. A berry location's check is
that index (`location_berries`), the pickup is recognised by it (`data[0]`), and all of them hold the one item
*Crystal Berry* (kind 4). The mod undoes the count the pickup code already raised, keeps the berry's "taken" mark,
shows the seed's item (a berry is a 3D model, so the model is hidden for a sprite), and drops the first-berry
tutorial; receiving one raises the count. First location: berry #0 outside the cave (test `TestCrystalBerries`).
They're a yaml category, *Shuffle Crystal Berries*, on by default (the user: some are obscure, like quests; test
`TestCrystalBerriesOff`).
**Respawning pickups** (the user, 2026-09-24, always shuffled, no option): some floor items have no flag of their
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
Snakemouth underground (a Honey Drop, a Mushroom and a Crunchy Leaf). **Seen in play by the user (2026-09-24):**
each first pickup showed the seed's item and sent its check, and after an area change the Honey Drop came back
and gave a real Honey Drop with no check. Two are named from the user's description (*Underground Door Room,
Pillar*; *Underground Bridge Room, Behind Pillar*, hidden from the camera); the Mushroom's landmark is still to come.
**A quest reward from the code** (2026-09-24): the lost ladybug kid at the lake gives a Lore Book once you've
beaten his monsters (Event31: `giveitem,1,52`, then flag 55, the check). He only appears after the first boss,
which is a story event of its own (*Snakemouth Den Cleared*, flag 41), and his cutscene moves all three party
members, so the location requires Leif and that event (test `TestLostKid`). Added from the code. The cutscene
also expects the kid's sister, the ladybug girl, to be following you (`FindEntity` of her character type): after
the first boss you talk to her outside the city and she comes along. Faked flags crashed it twice (no Leif, then
no sister), so it gets tested when a file reaches the first boss by play.
**Story pickups** (the user, 2026-09-24): some pickups have no "taken" flag of their own; the story makes them
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
the prize medals are paid out on every setting. Logic never needs either (the user, 2026-09-24). For
*Hardest*: its extras read flag 614 directly in about 35 places, so it means setting that flag. **Measured
2026-09-24: the game keeps no other record of a typed code.** `flagstring[10]` is only the typing buffer,
emptied as soon as the code is accepted (`EventControl.cs:2450-2455`), so flag 614 is the code's only trace,
so the mod marks a 614 it set itself, and keeps it out of every save (the user chose, 2026-09-24:
switchable, the save stays clean; the mod guide's design list, item 6).
**Built 2026-09-24 (not yet seen in game):** each tick outside battles and events, a prize slot reading "missed"
(2) is paid through the game's own `AddPrizeMedal(slot)` with Hard Mode answered "yes" for that call, because
most bosses test Hard Mode in their own event and write 2 directly. Artis's `Event33` then hands the prize over
with a `giveitem` the swap handles, and the location is done when the slot reaches 3 (`location_vars`, a number
slot instead of a flag). First location: *Outskirts: Artis's Prize for Snakemouth Den* (Quick Flea, which the
user saw for sale at the caravan and bought after a Normal kill: the missed-prize path, confirmed on screen).
**Everything in, placeholders for what isn't checked (the user, 2026-09-25):** every item, medal and other spot in the
game is added, so everything is randomized. A spot whose logic and name haven't been checked yet is a *Placeholder*:
"Placeholder" in its name, and it holds filler only (Archipelago's excluded type), so no progression item from any
game lands where the logic may be wrong. Its own vanilla item still goes into the pool and lands at a checked spot.
Each placeholder is promoted to a normal location once its requirements and name are checked, one at a time.
**Optional categories** (the user, 2026-09-24): a location can carry a `category`; its yaml option decides whether
the seed includes it. *Shuffle Quests* (on by default) covers quest-board and side-quest rewards; one-off NPC gifts
will have their own toggle. With a category off, its locations aren't created, their vanilla items stay out of the
pool, and they're left out of `slot_data`, so the client never swaps them and the game hands them out as usual
(tests `TestQuestsOff`, `TestQuestsOnByDefault`).
**Each toggle says how many checks it adds** (the user, 2026-09-25: so people know what they're getting into). The
option's description, which Archipelago copies into the yaml template, ends "Checks added in this version: N.", with
N counted from the location data when the world loads (`options.category_count`), so it never goes stale; test
`TestOptionCounts`. Seen in a generated template on 2026-09-25.
**Journal locations, each its own yaml option (the user, 2026-09-25).** The journal is `librarystuff[type, n]`,
set through `MainManager.UpdateJounal`, so a check can be "this entry became true", with no item to swap.
*Shuffle Discoveries* comes first (the simplest). **Built 2026-09-25**, opt-in (off by default): a location
source `discovery: n`, sent as `slot_data`'s `location_discoveries`, and the mod's `LocationChecks` sends the
check when `librarystuff[0, n]` turns true. No item to swap: a discovery gives none, so its pool slot is padding.
Where each discovery is came from four sources: `|discovery,N|` in map dialogue (ScriptDump), about 37
`UpdateJounal` calls in events (which event sets which), a few set from story flags in code, and each map's own
list (`MapControl.discoveryids`, MapDump). The five in regions the logic already has are locations: the pier
statue (49), the arrival outside Snakemouth (0, Event11), the spider fight (1, Event6), the bridge room's hidden
spot (2, Event13) and the Underground Door Room's grass (3, Event27). **First check seen:** the user had examined
the pier statue before the option existed; joining the new seed, the mod found discovery 49 recorded and sent
*Outskirts: Pier, Statue*. **First discovery seen live** (2026-09-25): the user examined the bridge room's hidden
spot, Event13 recorded discovery 2, and the check went out with the seed's item back. Tests `TestDiscoveriesOn`,
`TestDiscoveriesOffByDefault`. **Parked:** *Shuffle Bestiary* (an entry
comes only from Spy in battle or from Event65's catch-up NPC, who sells entries for enemies already fought, 19
berries, 49 for bosses, except the 23 in `excludeids`, which are the missable ones; seeing an enemy on the map
records nothing) and *Shuffle Recipes* (each needs its ingredients, which the seed may shuffle, so it waits
until the logic knows where ingredients come from). Two Quality of life rows go with the bestiary (the user):
auto-spy (a fought enemy counts as spied) and free entries at the catch-up NPC, perhaps folded with Free boat into
one "no NPC costs" row.
**Later idea, a yaml option (the user, 2026-09-25): enemy drops.** The first defeat of each ordinary enemy type is
a check (bosses and one-off fights left out), shown as a guaranteed drop; after that the type's drops are the
game's own, as with respawning pickups. The game already counts defeats per type in the save
(`enemyencounter[id, 1]`, raised on each win, `BattleControl.cs:30712`), so the check can be "that count reached
1", with no drop to swap. The logic needs, per type, a place where it's always fought. Another Bug Fables randomizer
reportedly does this: not looked at; its licence goes in `licensing.md` and
`references.md` is read before borrowing anything from it.
**Or per placed enemy, "enemy sanity" (the user, 2026-09-25), its own opt-in toggle:** each enemy standing on a
map (map plus entity index) is its own check on its first defeat, so the same enemy type in another room is
another check; afterwards it's the game's own again, as with respawning pickups. The entity dump holds 327 placed
enemies on 124 maps (some are one spot in different story states, swapped by flags, so fewer real spots). To
measure first: how a won battle knows which map enemy started it, and whether the mod has to keep what's done
(like respawning pickups, since nothing in the save marks a single map enemy beaten).

**Status:** in progress: respawning pickups (2026-09-24), the missed-prize path and discoveries (2026-09-25) seen by the user, crystal berry spots too (the mod guide, step 9); berries, the lost kid's reward and the prize payout not yet seen in game; Placeholders planned; bestiary, recipes and enemy checks parked.

---

## Build step 11: shops

Every medal a shop stocks, and the first purchase of each item in an item shop, is a location. The shelf shows the
seed's item, a purchase sends the check, and purchases are made permanent like checks, so no reload or spending
order can lock one away.

**Medal shops (the user, 2026-09-25), being built.** Each medal a shop stocks is a location; the shelf shows the
seed's item, buying runs the shopkeeper's `giveitem` (swapped as for a gift), and the check is the medal leaving the
stock (`badgeshops[shop]`), which the save keeps. A done location shows as sold, so a reloaded save never charges
twice. A *Shop prices: Normal / Half / Free* row (default Normal) scales the price columns. Merab's (berries) first:
berries can always be earned, so no lockout. **Shades's shop takes crystal berries, a consumable** (the user's
concern: consumable keys, lockout, savescumming): crystal berries are spent nowhere else (measured), and her stock
arrives in tiers whose Normal prices add up to 18, 25, 27, 40 and 50, exactly every berry in the game. **A tiered
rule (each item needs its tier's running total) was proposed and is wrong** (the user asked what happens when the
stock grows): with a later tier already on the shelf, berries spent there starve an earlier tier, and a rule that
raised the need once a later tier opens would not be monotonic, which Archipelago's fill doesn't allow. **Decided
(the user, 2026-09-25): every Shades location requires all 50 crystal berries, and her full stock (all 13) is on the
shelf from a new game.** The first stops spending order from locking anything out; the second stops a story event
that never runs (as the open world skips or bypasses scenes) from leaving a tier's medals, and their checks, never
appearing. The same holds for Merab's later additions when they become locations. Crystal berries become progression. **Also wanted (the user,
2026-09-25): Shades's counter showing 3 or 4 medals** instead of 2. The slot count is the shopkeeper's `data` length and
each slot's place its `vectordata` entry (`NPCControl.cs:1530-1534`), so longer arrays with new counter positions,
set before the shelf is built. Built: 4 on her counter (the mod guide, step 12).
**Full stock from the start for both medal shops, duplicates as their own locations** (the user, 2026-09-25: "a 2nd
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
**Item shops** (endless consumables, the user): the first purchase of each item in each shop is a check that shows
and gives the seed's item, then the shop sells its own item again, like respawning pickups, so restocking still works.
Their own yaml toggle, *Shuffle Item Shops*, default on, apart from *Shuffle Medal Shops*. Built after the medal shops.
**Built for Madame Butterfly's shop (2026-09-25, seen working):** five locations (*Item Shop 1* to *5*, ids 58-62), one
per stock entry, known by map, shopkeeper and item (`location_item_shops`); each puts its own item in the pool, with
no `give` entry, so an unrelated `giveitem` of the same item on that map is never swapped. *Shop Contents* covers
them too. The buy line adds the item with `additem` (no item-get box), so the mod takes that command out when the line
is read and treats berries paid as the purchase; the check goes out through the respawning pickups' queue. Tests
`TestItemShop*`. The other shops follow the same data.
**The caravan from the start (2026-09-25, seen: the user bought all three, each check sent, then her own items):** its keeper `Crickerly2` kept present, its stall
(`Base/Stall`, scenery) shown through a new `scenery_present` list, and `Crickerly1`, who stands there before it,
kept away; its three items (Spicy Berry, Burly Berry, Magic Seed) are *Outskirts: Caravan, Item Shop 1* to *3*
(ids 63-65), reachable from the start. Its stock is fixed (the keeper's own data); Crickerly's later stands on other
maps are other shops. **Lines about the rocks** (the user): every Outskirts line was searched (`line` dev command):
the waiting moth (`FuzzyMoth`, line 76) is kept away, the caravan husband's welcome (line 78) answers to flag 691
instead of 41 (line 75 was the rocks), Crickerly1 (line 74) is gone with the caravan. Gen and Eri, Artis and Eetl keep
their flag-41 lines: those are after the first boss (the river, Artis's prize, Eetl leading into chapter 2). The
Seen (the user, 2026-09-25): the moth gone, nobody mentioning the rocks any more. The
ladybug siblings (flag 41) are present from the start too, so the map doesn't feel empty (the user); their everyday
lines are neutral and their quest lines answer to the quest's own flags. Test `TestCaravan`.
**Shop Contents** (the user, 2026-09-25: shops are many easy checks in one place and soak up the good items, as in
Tevi): a yaml choice, *Anything*, *No Progression* (default) or *Filler Only*. No Progression is an `item_rule` on each
shop location refusing progression items from any game; Filler Only is Archipelago's excluded type (no progression,
no useful, `BaseClasses.py:1502`). 36 seeds (solo, with a second game, with discoveries) all generated.
**Filler Only falls back when the room can't hold it** (the user, 2026-09-25): an excluded spot takes only an item
that is neither progression nor useful, from any game, and with Merab's 22 copies a solo seed has 17 such items, so
generation failed. In `pre_fill`, once every world's items exist, the room's excludable items are counted against its
excluded spots; if short, this world's shops take No Progression instead, with a warning naming the player. 18 seeds
(solo, with APQuest, with discoveries, each Shop Contents) generated; the fallback fired solo and with APQuest (18
filler for 22 spots), not with discoveries on (22 for 22). Tests
`TestShopContents*`. **The caravan is there from the start** (the user), built with the item shops. **Reloads refund currency** (the user
caught this: buy, reload, keep the check and the berries), so purchases are made **permanent like checks**: spending
is tallied on the server (per-slot storage), each save brought in line on load (crystal berries exactly: received
minus spent; ordinary berries: the save's own paid record against the server's tally, the higher wins), and a
purchase made offline is recorded in the save and queued.
**Built for Merab's (2026-09-25):** no server tally needed. The save's bits (a bit per copy) are its paid
record, and the server's checked list says what was bought anywhere. A copy the server has checked with no bit in
the save was bought in another save, so on a save tied to the seed, outside events, the mod charges its price here
(clamped at 0, the rest forgiven) and sets its bit. Crystal berries (Shades's) still want the exact count.
**Seen in the log (2026-09-25):** the user loaded a save with 0 berries that had bought nothing, while the server held
all 22 copies' checks: each was charged (0 -> 0, forgiven) and marked paid. Forgiving can't be farmed: a save only
ever ends with fewer berries. A save that has berries losing exactly the prices isn't seen yet (same code path). A done location shows as sold. **Nothing requires a shop
bought out** (the user asked): the sold-out flags (587 Merab's, 588 Shades's, set in `MainManager.cs:14283-14291`) only
change dialogue (an NPC's line 159 on the Commercial map; Shades's greeting, `checktrue,588,92`). A check that ever
depends on a bought-out shop would need the full total, 50, still safe; a seed holding fewer than 50 crystal berries
leaves the unaffordable tiers out of the pool instead. **The bar is "no action can make a check unreachable"**, not
just "the logic never asks for it" (the user: a consumable that can lock a check away is a broken seed). Shades's
checks meet it because (1) crystal berries buy nothing but her stock, (2) the stock costs exactly 50, (3) all 50 are
always obtainable and never taken away, (4) purchases are permanent. Every berry spent buys one of her items, so what's
left always costs what's left to collect. **Shades's shop is only shuffled when the seed holds all 50 crystal
berries**; otherwise it stays vanilla. Merab's has no such risk: ordinary berries are renewable from battles.
**Confirmed (the user, 2026-09-26):** a player who turns crystal berries off doesn't want to deal with them, so
Shades's shop is then not a location at all: nothing from the seed goes there (no progression, useful or filler),
and she sells her own medals. No berry-spot events keep her shop shuffled (proposed and dropped: they would make that
player collect every berry). When her shop is built, both yaml texts say so: *Shuffle Medal Shops* that Shades's
shop joins only with *Shuffle Crystal Berries* on, and *Shuffle Crystal Berries* that turning it off leaves her
shop vanilla.

**Status:** in progress: Merab's medal shop (her full stock of 22 from a new game, seen 2026-09-25; the mod guide, step 12), Madame Butterfly's item shop and the caravan seen by the user (2026-09-25); Shades's shop not built (it waits for all 50 crystal berries as locations); the other item shops to follow.

---

## Build step 12: the entrance randomizer (experimental)

Every map-to-map door shuffled, as a yaml option labelled *experimental* until every door's logic is done. The
apworld pairs the doors and sends the result as `door_targets` in `slot_data`; the mod rewrites each door as its
map loads.

**Entrance randomizer (the user, 2026-09-25):** every map-to-map door, as an option labelled *experimental* until
every door's logic is done: until then a shuffled seed may be unfinishable (the one exception to "never
impossible", while that option is on; *Warp to start* gets the player out of a dead end). Coupled (a door and its way
back stay a pair) by default, decoupled as a choice. Order: a proof of concept (the mod rewriting a door's
destination, seen on screen), then every door, then the room-by-room logic that removes the experimental label.
**The proof of concept, seen (the user, 2026-09-25):** one door, then a coupled swap of two connections both ways
(the mod guide, step 13). **Every door, built (2026-09-25):** the yaml option *Entrance Randomizer (experimental)*,
*Off* (default) or *Coupled*; decoupled later. How it was built:
1. **A door table** (`data/doors.json`), exported by `dev-scripts/door-graph.py --export` from EntityDump: every
   door paired with its way back (the door the party arrives next to), 254 connections, 508 doors. Doors stay
   fixed when the mod couldn't tell them apart by name, when they have a story variant at the same spot, or when they
   lead into their own map; the table lists the map links those make.
2. **The shuffle** (`doors.py`), in `generate_early`, with the world's own random: the same doors joined in new
   pairs, x with y meaning x leads where y's old partner led (so you arrive next to y) and y where x's old partner
   led. Sent as `door_targets`, which the mod already applies.
3. **Every area stays reachable.** A plain random pairing strands areas: two dead-end rooms joined to each other are
   cut off. Measured: stranded in 20 of 20 seeds on the real table. So the shuffle grows the world outwards from one
   area (maps joined by fixed doors count as one): an open door of the reached part is joined to a door of an area
   not reached yet, taking an area with more doors whenever only one open door is left; the doors left at the end
   are paired at random. Tests `TestDoors*`: every way back leads back, every map is reachable, and a hub with dead
   ends is never stranded over 300 seeds (a plain random pairing strands 266 of them).
4. Six seeds generated with it on, three solo and three with APQuest. **Seen (the user, 2026-09-25), one generated
   pair both ways:** the town gate led into the desert (`DesertBeforeGH`, the log: rewritten like `DesertSouthern`'s
   left exit), and the exit there back to the start in front of the gate. The logic still assumes the vanilla doors
   (the named allowance).
5. **A dropped connection leaves the doors as the seed has them** (code: `door_targets` arrive with slot_data at login
   and are never cleared by a drop; each map's doors are rewritten from memory as it loads). **Seen (the user,
   2026-09-25):** with the server stopped, the town gate to the desert and back several times (15 rewrites in the log
   while offline); on restarting the server the mod logged back in by itself.
6. **Found while roaming:** a shuffled door reaches areas the seed has no locations in yet, whose items are the
   game's own (the user picked up the desert's Strong Start medal, flag 415). The Placeholders plan (every spot a
   location, unchecked ones filler-only) closes that.
7. **A trap found while roaming (the user, 2026-09-25):** in the bandit hideout's garden (`HideoutGarden`) a guard
   (`burglar`) caught the party, which starts the game's own caught scene (Event108, the log) and puts the party in
   `HideoutCell`, whose one door leads to the central room. **Getting out needs the dig ability** (the user,
   correcting a first guess of Leif's ice: you dig under the bars in the sand). The code agrees: in the story the
   cell is where dig is learnt: Event109, the first capture, takes the beemerang (flag 11 off), sends the party to
   the cell (`LoadMap(100)`) and there sets flag 18, dig; Event108, caught again later, expects you to dig out. So
   without dig it's a dead end (the Warp button is the way out), and the hideout's garden needs dig in the logic
   once the logic follows the doors. A guard catching you is a transfer that isn't a door.
8. **Transfers that aren't doors (decided, the user, 2026-09-25).** The game also moves the party by the dialogue
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

9. **Quests that cross rooms** (the user, 2026-09-25: a reward mustn't be expected when its middle steps can't be
   reached). Today a quest is safe because its steps share one big region (the old book's residential house and the
   palace library are both *Bugaria Inner City*) or pass on the way (the lost kid's sister waits outside the city,
   on the way to Snakemouth). With doors shuffled neither holds. The rule before the label comes off: **every quest
   step in another room is a logic event in that room's region** (the sister following; the library visit is done, build step 8), and the
   reward requires the whole chain; items handed out mid-quest are already progression (build step 10). Taking the
   quest is a step too, now just "reach any board" (build step 9). Known gap today: the lost kid's reward
   (location 10) doesn't require the sister's step.

**The Warp is always there with the entrance randomizer** (the user, 2026-09-26: "so we never get impossible
seeds/softlocks, even if we will check/make logic for things"). Coupled doors can always be retraced, but a one-way
transfer (a drop, a fall, a scripted move) could land the player in a pocket whose way out needs an item not yet found:
the seed stays possible, the player is stuck. The Warp to Start is that escape, shown whatever the Travel setting, as
with a random start (build step 15), where it also counts in the logic.

**Status:** in progress (experimental): every door, coupled, built, and a generated pair seen both ways, offline too (the user, 2026-09-25); next, sorting the transfers that aren't doors, then the room-by-room logic; decoupled later.

---

## Build step 13: party members and moves as items (in progress)

Field abilities, the basic moves and party members as items, each unusable until it arrives. Only a rehearsal is
built so far: a dev setting that starts the game with one member.

**Also wanted (the user, 2026-09-25): the basic moves as items**, a yaml option apart from the abilities: Vi's
beemerang, Kabbu's horn, Leif's freeze, and jumping, each unusable until its item arrives. Proposed: *Shuffle Basic
Moves* (the three field moves) and *Shuffle Jump* (its own toggle: it gates the most), both off by default. **The mod
side is small** (code read, 2026-09-25): the three moves are one method, `PlayerControl.DoActionTap`
(`PlayerControl.cs:1008`), switching on the leader's `animid` (0 Vi, 1 Kabbu, 2 Leif), so a prefix can refuse the move;
the game itself takes the beemerang away in the bandit hideout (flag 11, `case 0` checks it), so Vi without it is a
state the game already knows. The jump is its own method, `DoJump`, called from the button it shares with talking
(`PlayerControl.cs:372-392`), so it can be refused without touching talk. **The logic is the cost:** every spot that
needs a move (a ledge, a beemerang switch, grass, water to freeze) becomes a rule, seen room by room, and the start
must have checks that need none of them.
**Later idea, a yaml option (the user, 2026-09-25; off by default, confirmed 2026-09-26): party members as items.** Start with one random member and
find the other two, each its own item, on top of the abilities. **Its shape (the user, 2026-09-25, later):** *Starting
Party Member: Off / Vi / Kabbu / Leif / Random*; Off is the story's party, otherwise the game starts with that one
member and the other two are items. Prompted by the stand-ins for missing members in scenes, which make one-member play
look possible; the story's own joining scenes (Kabbu at the start, Leif at the lake) must then add nobody.
**The two joining moments become the two locations** (the user, 2026-09-25): with the option on, whoever starts, the
other two members are items, and the story has exactly two joining moments: Vi's in the opening scene and Leif's right
after the spider scene (where the mod now has him join). Both become locations whatever the start (Vi's even when you
start as Vi), named after the place, not the member ("Snakemouth Den: Fall Room, After the Spider"), so two items get
exactly two spots and no filler has to go. With the option off they're no locations; the members just join.
**Rehearsal built (2026-09-25), the mod only:** a dev setting `TestStartMember` (0 Vi, 1 Kabbu, 2 Leif) and a prefix
on `MainManager.ChangeParty(ids, fromscratch, destroyoldentity)`, which every party change of the story goes through
(the two-argument form forwards to it; about 20 calls in `EventControl`, some already one member: Kabbu alone after
the slides, Vi alone in one scene). The prefix keeps in `ids` only the starting member and those received (dev:
`addmember`), so the opening's Vi-and-Kabbu becomes the one member; it runs last so the opening skip's own prefix sees
the story's ids. **First test, Leif alone (the user, 2026-09-25):** the start and the camera right after four fixes
(the mod guide, step 11, item 1); Artis's talk (lines for Vi and Kabbu, now stand-ins in conversations too) gave the
permit; the gate scene played; the grass tutorial (Event10) played once stand-ins arrive at once, its reward sent.
**Then a real gate:** the corridor after the tutorial (`BugariaOutskirtsSnakemouthCorridor2`) needs the horn to
cross, so Snakemouth Den needs Kabbu (or the horn, once moves are items) in the logic.
**Further (the user, 2026-09-25):** the trapdoor scene, landing in the right spot once stand-ins stay where a scene
puts them; the spider fight's lead-in once stand-ins get their physics body when made; and **the spider fight itself
with Leif alone** (the user's screenshot: Leif alone against the spider, the battle menu working). In the story that
fight is Kabbu alone at first, then Vi joins.
**The first boss and after, Leif alone (the user, 2026-09-25):** the boss scene (Event26) and the one after the bridge
(Event63) played with Leif acting the story leader's part, no error in the log; the follower joined crossing the
bridge; the way back to the cave is closed after the boss; what the seed keeps open (the caravan, the ladybug
siblings) still works; the NPCs in the starting house moved away as the story has them. Chapter 2 next.
**Decided (the user, 2026-09-25): the members present act out the missing ones' parts** in scenes, where they would
be and what they would do, instead of standing idle beside invisible stand-ins ("looks more fun"). Plan: the real
leader plays the story's leader (the first member of the party the story expects); only other missing members stay
invisible stand-ins; in a party list by member (Vi, Kabbu, Leif) the leader's own slot then gets an invisible
stand-in, so a scene moving "each member" never moves the player twice (why this was set aside at first).
Animations play by number, so the leader shows his own animation with that number: the field action is
`animstate` 100 for everyone (Vi's throw and Kabbu's horn, `PlayerControl.cs:1029`, `:1076`), so Leif acting Kabbu's
horn swing casts his ice (wanted, the user: "Kabbu using the horn, Leif using ice"). States a character lacks show
as `Animator.GotoState: State could not be found`; the mod logs the number and maps it to the closest one. To build after the current
replay of the trapdoor and the spider fight, then replay the same scenes to compare. Opt-in only: fighting with one or two changes
the game a lot. Open questions: the story may need all three after chapter 1, and adding a member outside the
story's own event hasn't worked yet (log.md, 2026-09-24: `ChangeParty` left Leif without a character).
**Solved 2026-09-25:** without `fromscratch`, `ChangeParty`'s copy loop never runs (`for m < 0`,
`MainManager.cs:3805`), so the party list came out empty. `ChangeParty({0, 1, 2}, fromscratch: true)` rebuilds all
members (stats from defaults, then the stat bonuses reapplied), and `SetPlayers(positions)` makes their characters.
The user saw Leif join the party and fight on a file where he'd never joined (dev command `addleif`). Next measured:
the trapdoor, the spider fight and Leif's own joining scene with him already there.
**Chapter 1 scenes with Leif added early** (the user, 2026-09-25): Event2 (the Tattle tutorial, bridge room) played
fine; it moves only the first two (`GetEntity(-4)`, `(-5)`), so Leif stood still in it and was left behind until it
ended, which looked odd (the user). Scenes switch normal following off (`overridefollower`) and move only who they
name. Cosmetic fix for the open start: during a scene, a member the scene never moves walks along behind the one
ahead of him.
**The trapdoor scene broke with three** (2026-09-25): Event5 recreates the party with `SetPlayers(positions)` and a
list of two positions, and `SetPlayers` indexes it for every member: IndexOutOfRange, the scene stopped halfway, the
user stuck in the fall room (freed with `unstick`). The mod's `PartyFit` now lengthens a short position list before
the game uses it (each extra member a step behind the last listed one), which covers every scene that places the
party this way. **Retest:** `SetPlayers` then took the lengthened list, but the scene then places each member from
**its own** two-long list (`for m < playerdata.Length`, `array[m]`, `EventControl.cs:1476-1484`), which a fix outside
the scene can't reach. Stopped there (two fixes on one scene). `unstick` now also resets the party's bodies (gravity,
physics, forced animation), which the crash had left as the scene set them.
**Parked design (the user, 2026-09-25), for party members as items:** scenes find members by **character**
(`GetEntity(-4)` Vi, `(-5)` Kabbu, `(-6)` Leif search the party by `animid`; `-1` to `-3` are positions), so the leader's
order never matters. Two rules then: (1) a member a scene doesn't know about (Leif early in chapter 1) **steps out**
while it runs and rejoins after (the `addleif` method: `ChangeParty` with `fromscratch`, then `SetPlayers`); (2) a
scene that needs a member who isn't there (only Leif, no Vi) **waits**, held in the game and a rule in the logic for
any check it gives, like the boat. First step when this is picked up: list chapter 1's scenes by the characters they
use, from the code.
Scenes that need a particular member must then become rules. The horn tutorial near Snakemouth (Event10, a
location) is not one: the scene cuts the grass itself, and it played through with Leif alone once stand-ins
arrived at once (the user, 2026-09-25; the mod guide, step 11, item 3). The way down to Shades's shop is:
grass on the way there has to be cut with the horn (the user, 2026-09-25), so her locations will need Kabbu.

**Status:** in progress: a rehearsal only (dev `TestStartMember`), a one-member party (Leif) seen through chapter 1 into chapter 2 (the user, 2026-09-25); the yaml options (*Starting Party Member*, basic moves, jump, field abilities) not built.

## Build step 14: enemy shuffle (in progress)

A yaml option that changes who you fight at each place. It is not enemy checks (build step 10): nothing here is a
location, only the fights move.

**Decided (the user, 2026-09-26):**
- *Enemy Shuffle*, `off / enemies_only / bosses_only / both / chaos`, **off by default**. `both` swaps enemies with
  enemies and bosses with bosses; `chaos` puts them in one pool. It is in the yaml, not the panel, so a slot plays
  the same for anyone on it.
- **Each enemy on each map gets its own fight**, fixed by the seed and sent in `slot_data`, never decided at runtime.
- **A boss's reward stays with its place:** Snakemouth Den pays Snakemouth Den's prize, whatever boss was there.
- **The fight first, the look later:** the enemy walking around the map still looks like the original for now. The
  user wants it to match its fight ("it would feel weird to run into a seedling and then fight an octopus"), which
  is its own later piece of work.
- **The party rule:** only fights that can't be fled are limited to enemies the party guaranteed at that point can
  hit. Map fights can always be fled, so they shuffle freely. If enemy checks come, map fights count too. **Only the
  base attack counts** (the user, 2026-09-26): skills cost TP, which can run out, and items are used up, so the logic
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
3. **The shuffle:** in `generate_early`, `shuffle_encounters` groups the fights by size and shuffles each group
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

**The map look, first test (2026-09-26, seen by the user):** a dev console command, `enemylook <enemy id|off>`,
reloads the current map with every ordinary map enemy looking like one enemy. The mod sets each map enemy's
`animid` right after `MapControl.CreateEntities` and before the entity's own `Start`, which builds the model from
it, so the enemy sets itself up as the new character, the way the game would. The look takes that enemy's animation
set (the enemy table's column 0, the same value the rematch machine uses). With Spuder (enemy 2) on
`BugariaOutskirtsEast1`, the three map enemies looked like the spider; the fight was still the seed's. **The movement
stays the original's**: it comes from the map enemy's own row in the map data, so Spuder burrowed like the Underling
it replaced. So the real step copies an enemy's movement from a map where it appears naturally, not only its look.
**A boss from a map enemy, look and fight (2026-09-26, seen by the user):** with `enemylook 46` and
`enemyfight 46 9 0` (a second dev command: every map fight starts with those ids), the Outskirts enemies looked like
the Bee Boss, and touching one started a fight with the Bee Boss, a Seedling and a Cordyceps Ant. So a boss can be
shown and fought from a map enemy, which `chaos` needs. **Decided (the user): the map model is the strongest enemy in
its fight**, so the player knows what they're walking into: a boss if the fight has one, else the highest base HP
(the enemy table dump). Fixed data, so the seed decides it and sends it with each fight.
**Wanted (the user):** a Quality of life row, *Enemy movement: their own / the original's*, default their own; the
original's is there for fun ("looks fun when something does something else than the model is supposed to").

**To fix later (the user, 2026-09-26): the map movement.** A swapped look keeps the original enemy's movement
(Spuder burrowed like the Underling). Whether bosses can move around the map on their own at all is unknown; they
never do in vanilla. To measure before the real look step.

**The movement, fixed in the test (2026-09-26, seen by the user):** a map enemy's movement is its map row's
behaviours (fields 2-3), collider (11-12), radii and timers (13-21) (`MapControl.cs:1475-1497`); hovering comes from
the look itself (`CheckSpecialID` raises a flier to its minimum height). `enemylook <id> move` finds a **donor**: the
first map row, any map, whose fight starts with that enemy, and copies its movement fields onto the enemy before its
`Start`. With the Flying Seedling (donor `NearSnakemouth:7`) the Outskirts enemies walked around as they should,
not burrowing like the Underlings they replaced (the user). A boss has no map row, so a boss look has no donor: its
movement is still to decide: **tested per boss later, whatever looks best** (the user, 2026-09-26). In the real step,
the seed can pick each donor at generation.

**Next:**
- see the shuffled fights in the game;
- then bosses: each scripted fight read one by one, keyed by its event and its original ids;
- then `both` and `chaos`;
- then the map look.

**Status:** in progress: `enemies_only` built (2026-09-26), the apworld tests pass, a seed generated with a second
game (APQuest) carrying all 325 fights in `slot_data`, and **seen by the user** (2026-09-26): on
`BugariaOutskirtsEast1` an Underling + Flying Seedling map enemy started a Flying Seedling + Seedling fight, as the
seed and the log (`[enemies] BugariaOutskirtsEast1:4: 30 10 -> 10 9`) said; bosses,
`both`, `chaos` and the map look to come.

## Build step 15: starting location (experimental)

A yaml option for where a new file begins. **Experimental** by the user's ruling (2026-09-26), like the entrance
randomizer: "random start should be fully random… random spawn is experimental just like entrance rando". The logic
still starts outside Bugaria, so a seed started elsewhere may not be finishable until the room-by-room logic exists.

**Decided (the user, 2026-09-26):**
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
   skip's end, so **a seed start always skips the intro**, whatever *Skip cutscenes* says (the user asked, 2026-09-26);
   the setting still governs every other scene.
4. **Tests** (`test/test_start.py`): off gives `{}`; `anywhere` gives a save point from the table; the start is fixed.

**First tries in game (the user, 2026-09-26), three failures and how each was found:**
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

**Seen by the user (2026-09-26):** a new file ended at the seed's start, Rubber Prison's cell block, the pause map
marking only Rubber Prison visited; Warp to Start went there from the Outskirts.

**A worked example: starting on Metal Island** (the user asked, 2026-09-26). Leaving needs nothing (the island
sailor is unchanged); coming back needs the Boat Ticket, or the Warp (the seed's start) or map travel. Nothing can be
missed, and nothing there has to be filler: the logic already gates Metal Island on the ticket, so it never expects the
island's checks before the ticket, and the ticket can't be placed behind its own gate. Starting there only gives an
early look. **The rule for any start:** it's safe while every way out of it is free and every way back in is something
the logic already gates; only a start that could be left behind for good would need its checks to be filler.

**The rule that keeps every random start valid (the user, 2026-09-26: "really important for the logic"):** with a
random start, **Warp to Start is always available and counts in the logic**, whatever the Travel setting. The case it
closes: once the logic starts in the start room (the room-by-room logic), a way back into the start may need an item
lying in the start itself (the Boat Ticket on Metal Island); leaving without it would strand the seed, and Archipelago's
logic can't model giving access up. With the Warp guaranteed, the start can always be re-entered from anywhere, for
every start at once. The mod shows the Warp with a seed start even when Travel is Off or Map; the logic's side (the start
region reachable from every region) comes with the room-by-room logic.

**No music between the menu and the start (the user, 2026-09-26: "as if I'm going from the start menu directly to a
random spawn"):** the game starts the opening map's music as a new file loads. With a seed start, the mod turns any new
music into silence while the file is still on the opening map (a prefix on `ChangeMusic`'s full overload, which every
music change reaches), and its own opening music is a fade-out instead. Seen by the user: "looks/feels instant now".

**Any room, not just save points (the user, 2026-09-26: "an actual random area ... somewhere in a dungeon"):**
- **Values:** the user's off / towns / random, but Archipelago reserves `random`, so `anywhere` is the fully random one;
  `towns` is still to come (the save-point table, `data/starts.json`, stays for it).
- **The pool** (`ROOM_STARTS` in `data_tables.py`): every room entered through a door, both ways of each connection in
  `doors.json`. `slot_data` `start` is `{"map", "from"}`: the room, and the map whose door leads in.
- **The mod** reads the door in the `from` map that leads into the room (`QualityOfLife.DoorInto`, the dev test start's
  reader), and arrives as the game's own door transfer does: appear, then walk in. Warp to Start lands where that walk
  ends. A save-point start (`{"map", "entity"}`) still works.
- **Stuck starts are accepted while experimental (the user):** item and entrance logic everywhere comes later.
- **Chapter 1 test** (the user asked for one): seeds regenerated until the start was a Snakemouth Den room (seed 17,
  `SnakemouthFallRoom` from `SnakemouthDoorRoom`). **Seen by the user (2026-09-26):** the new file arrived right where
  the trapdoor scene drops you; jumping up out of the room is one-way, and the Warp brought the user back down.

**Status:** in progress (experimental): `anywhere` (any room) works, seen by the user (2026-09-26): a new file starts in the seed's room;
`towns` and the logic from the start to come; the intro is always skipped with a seed start.

## Build step 16: the Boat Ticket

The first of the mod's own items (custom gates, "How this mod does it"), suggested on Discord. **Decided (the user,
2026-09-26):** a progression key item, always in the pool; the pier sailor sails to Metal Island only with it, the
trip free and the ticket kept; the logic gates Metal Island on it, so Metal Island isn't open from the start; Free boat
(the Quality of life row) goes.

**Built (2026-09-26):**
1. **The item** (`CustomItems.cs`): id 200 in the game's item and sprite tables (the game's items end at 186, both
   hold 256), the Platinum Card's fields and sprite (item 176, the user's pick after mockups of combined icons), its
   own name "Boat Ticket" and the user's description "A boat ticket. Maybe we should visit the pier." (field 2, the one
   the menus show; `MEASURED.md`, "The item table's fields"). Only with Archipelago on. Seen by the user.
2. **In the pool** (`items.json`): kind 1 (key item), game id 200, progression, with `always`: it enters once in every
   seed. When every location already holds its vanilla item (the default seed has 59 for 59), one ordinary item or
   berries with a copy left makes room, picked with the seed's random; never a medal, never an item's last copy.
3. **The logic** (`locations.json`): a Metal Island region, reached from the Outskirts (the pier) with the Boat Ticket.
   No locations there yet.
4. **The sailor** (`BoatTicket.cs`): a postfix on `MainManager.GetDialogueText` on `BugariaPier`, since every line of
   his, the first included, comes through it. His lines as the user approved them, line by line (build step 16's
   record in Next 21's history): the offer "Would you fancy traveling to Metal Island? Show me your ticket.", the
   choice "Let's go!" / "Not yet!" with the ticket or "I lost my ticket!" without, the ticket checked where the fare
   was (lines 16 and 19), "...Ticket's in order. Hop on!" or "What?! No ticket, no trip! Get out of here!". The card
   Masters' discount (line 18) makes the same offer. English only.
5. **Free boat removed** (`QualityOfLife.cs`, `ApMenu.cs`): its fare rewrite and its row.
6. **Tests** (`test/test_boat_ticket.py`): once in the pool, progression; Metal Island unreachable without it,
   reachable with it. `TestPool` now allows the one filler copy the ticket takes. *Shop Contents: Filler Only* in a solo
   seed with discoveries on is now one filler short and falls back to No Progression, as it already did without
   discoveries; with other games' filler in the room it holds.

**Seen by the user (2026-09-26):** with the ticket, the sailor offered "Would you fancy traveling to / Metal Island?
Show me your ticket." (a `|line|` before the name, which wrapped mid-name at first), "Let's go!" / "Not yet!", and
"...Ticket's in order. Hop on! Our destination: Metal Island!", then the boat.

Without it (the ticket taken with the dev console's `take key 200`), the choice read "I lost my ticket!" and "Let's go!"
got the refusal; seen by the user.

**Status:** works both ways, seen by the user (2026-09-26); the pool and logic take effect in the next generated seed
(the apworld tests pass, 275).

## Build step 17: a release

Three separate downloads on a GitHub release (the user, 2026-09-25/26), made the way MeshGhost makes its TEVI release.

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
holds only `BepInEx/` and `README.txt`, where someone opening the zip sees it (the user, 2026-09-26: it was three
folders down at first). A plain name, though it lands next to `Bug Fables.exe` (the user: overwriting it is fine). `build-release.ps1`
refuses any other file at the top level, in `-Check` too.
BepInEx is not bundled; the player installs it first.

**How it was built:**
1. **The mod is built locally and committed.** CI can't build it: it compiles against the game's own
   `Assembly-CSharp.dll`, which never enters the repo. `dev-scripts/build-release.ps1` builds Release and stages
   `release/mod/` (with no debug info: the pdb isn't shipped, and its path would put the build machine's folders into
   the DLL; checked with `strings`), and writes `release/built-from.txt`: each source file's git blob hash (line endings normalised, so
   a Windows and a Linux checkout agree) and each shipped DLL's SHA-256. `.gitignore` lets exactly those four DLLs in.
2. **A stale gate.** `build-release.ps1 -Check` recomputes both lists and fails if they differ. CI runs it on every
   push, so a source change without a rebuild shows red. Tried both ways (2026-09-26): a probe line in a `.cs` file
   failed it, naming the file; removing it passed.
   The same run refuses a release whose dev tools or cheats are on by default (the user, 2026-09-26: never a release
   with 99 damage or infinite jump): every `Config.Bind("Debug", ...)` must default to off (`false`, `0`, `""`, or
   `-1`, TestStartMember's off). The dev tools still ship, off; only a hand-edited config turns them on (the user's
   choice over compiling them out). Tried both ways: InfJump defaulting to true failed it, naming the key.
3. **CI** (`.github/workflows/ci.yml`, every push, and called by the release): the gate, and the apworld on a
   Python matrix (3.11, 3.12, 3.13, what Archipelago's own CI tests at 0.6.7). Each leg checks out Archipelago
   `0.6.7`, installs it the way Archipelago's own `unittests.yml` does (then sets `SKIP_REQUIREMENTS_UPDATE=1`: on the first
   run, 2026-09-26, two worlds' pins clashed over `typing-extensions` on Python 3.12 and 3.13, and `Launcher.py` stopped
   at a press-Enter prompt with no one to press it), runs our tests, and generates three presets
   (default, every experimental option on, every location toggle off) with APQuest as a second game. The whole suite
   takes about 3 seconds, so the matrix splits by Python version, not by test file: every job pays the install.
   The 3.13 leg also builds the apworld (`Launcher.py "Build APWorlds" -- "Bug Fables"`, with our `LICENSE` copied
   in) and the template (`Launcher.py "Generate Template Options" -- --skip_open_folder`), then generates once more
   the way a player would: the built `.apworld` in `custom_worlds`, the template as the yaml, no loose world.
4. **The release** (`.github/workflows/release.yml`, run by hand): a guard first (the version is `vX.Y.Z` and
   matches `Plugin.cs` and `world_version`; the tag is free; no personal path in the highlights or in any commit
   subject the generated notes will publish; the patterns live in `.githooks/release-path-patterns.txt`, since the
   pre-commit hook refuses them anywhere else), then CI, then the publish job zips `release/mod/BepInEx` and attaches
   the three files. The body is the highlights (changes and new features, or nothing) plus GitHub's generated notes.
   `softprops/action-gh-release` is pinned to a commit, since it runs with write access.
5. **One command cuts it:** `dev-scripts/release.ps1 -Version v0.1.0 [-HighlightsFile notes.md]` (add `-Prerelease`
   only for a test build: a pre-release never shows as Latest, which hides it; the user, 2026-09-26). It
   refuses unless the versions match, `main` is clean and not behind, and the tag is free. Then its preflight runs
   the stale gate; a stale DLL is rebuilt and committed on the spot, and the gate runs again. Then it pushes, waits
   for CI to go green, dispatches the release and waits for it to publish. Running it is the go-ahead to push.

6. **Rehearsed before the first run (2026-09-26):** every CI step on a fresh clone of Archipelago `0.6.7`: the
   tests, the three two-game presets, Build APWorlds (its manifest gained `version` and `compatible_version` on its
   own), the template, and a seed from the built `.apworld` in `custom_worlds` with the template as the yaml (loaded
   as v0.1.0, no manifest warning). The zip, built the same way, extracted next to `Bug Fables.exe` lands only in
   `BepInEx/plugins/BugFablesAP/`.
7. **Trying the download in game:** `copy-dev.ps1 -Layout Release` swaps a dev install for exactly what the zip
   holds, and `-Layout Dev` swaps back (`development.md`, step 4 of the build-and-copy list).

**Versions.** The mod's `Plugin.Version` and the apworld's `world_version` both equal the tag without its `v`.
v0.1.0 is the first (the mod was 0.0.1 and the world 0.2.0 before).

**Status:** works: v0.1.0 published by `release.ps1` (2026-09-26), every job green, then remade the same day from
a later `main` for the zip's top-level README and switched from pre-release to a full release (the user: a pre-release
is hidden from Latest). The downloads fetched back and checked; the DLL is the build the user saw load and connect.

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
- `location_flags`, the game flag that marks each location done;
- `location_gives`, the `giveitem` that hands out a gift location's vanilla item;
- `location_pickups`, the map and flag of each location that is an item lying in the world;
- `item_kinds`, which inventory list each of its items goes to.

The mod does nothing from its own knowledge of the game's locations: every table it acts on comes from here.

## 8. Use a library

Writing all of the above by hand is possible, but libraries exist for most languages; the protocol doc
lists them. For C# (Unity, BepInEx) it's **Archipelago.MultiClient.Net**. It handles:

- the handshake and every packet;
- turning numbers into names;
- a single call to log in.

What it can't do for you:

- deciding **when** it's safe to hand the player an item in your game;
- **saving** the received-item count in your game's save;
- knowing **which spot** in your game is which location.

Those are the parts that make each game's client different.

## 9. How this mod does it

- The login call can take several seconds, so it runs **off the game's own thread**
  (`ApConnection.Connect`). The result is handed back to the game thread through fields the game reads each
  frame, and log lines through a queue (`Post`, `Tick`). The game never freezes on connect.
- Connection settings (address, port, slot, password, compression) and the Archipelago mod switch live in
  the mod's BepInEx config file (`Plugin.Awake`).
- The client library and its JSON library sit in `BepInEx/plugins`, loaded once, and the mod itself
  reloads on its own during development.

**Custom gates are the mod's own items** (the user, 2026-09-26). Where the randomizer wants a gate vanilla doesn't have,
the mod makes an item of its own, added to the game's item table at runtime (an existing sprite, its own name and
description), and the gate is "has the item": the logic reasons about it like any key item, and the mod checks it in
the game. The Boat Ticket is the first (Next 21); the party members as items (build step 13) are the same idea on the
location side, turning a story moment vanilla never made a check into one. Each new item takes a filler slot in the
pool, so a seed needs one to spare, which a test pins.

**Custom items' text** (the user, 2026-09-26): written to fit the game, as the menus and art do. The game's own voice,
seen in play: items are witty ("Don't say you weren't warned."), medals strictly informative, and **key items say what
they open, then a small joke** ("This keycard can open doors in the Honey Factory. Whoever lost this probably got
fired."; "This key opens a door in Rubber Prison. A guard probably dropped it while fleeing from the Wasps."; the
Platinum Card's is plain). A custom key item follows that: what it opens, and a light line in the team's voice; never a
rules explanation. (The key items' texts aren't in `itemdata[0, id, 1]`, which holds "Desc" for them; they were read
on screen.)

**Icon ideas for custom items** (the user, 2026-09-26, from a labelled contact sheet of the item sprites; `SpriteDump`,
the mod guide, step 7). An icon is only lent: the item has its own name and description, and a reused or similar icon
is fine in the menus as long as those differ (the user, looking at the Key Items list). Item sprite ids
(`itemsprites[0, id]`):

| Id | The game's item | Could stand for |
|---|---|---|
| 176 | Platinum Card (silver) | **the Boat Ticket** (the user's pick); 95 (Factory Pass, a yellow pass) the other card |
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

**Combined icons** (the user's idea): the mod can layer game sprites into a new icon, still the game's own art. Tried
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
