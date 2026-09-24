# The Archipelago side: how it works, and how it was built

This is the Archipelago half of the Bug Fables randomizer: the apworld, seeds, the server, and how the mod
connects, sends what the player finds and receives items. It has two parts: **how we built it**, step by
step, and **how it works**, a plain explainer of how any game talks to Archipelago. The game side (the mod
itself, probing the game) has its own guide: [documentation.md](documentation.md).

The explainer follows Archipelago's own [network protocol doc](https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/network%20protocol.md)
(read at version 0.6.7). Where this file and that doc disagree, that doc is right.

## Where it stands

**Done so far:** a small apworld (15 locations, 13 items) that generates seeds and passes its tests, with the
goal "collect N artifacts"; the mod connecting on its own, compressed, to a local server or a hosted room on
archipelago.gg, retrying when the server is unreachable or drops; sending checks (build step 6); receiving
items, with the count kept in the save (build step 7); and the game's own item at a location swapped for the
seed's (the mod guide, step 9).

**Next** (decided by the user, 2026-09-24):

1. **Every key item and medal in the pool,** on logic that follows the vanilla story order: one region
   per chapter, entered once the chapter before is finished and the story's own keys and abilities are
   in hand. Medal gifts and medal shops each get a yaml on/off toggle.
   **Built 2026-09-24 (not yet seen in game):** each tick outside battles and events, a prize slot reading "missed"
   (2) is paid through the game's own `AddPrizeMedal(slot)` with Hard Mode answered "yes" for that call, because
   most bosses test Hard Mode in their own event and write 2 directly. Artis's `Event33` then hands the prize over
   with a `giveitem` the swap handles, and the location is done when the slot reaches 3 (`location_vars`, a number
   slot instead of a flag). First location: *Outskirts: Artis's Prize for Snakemouth Den* (Quick Flea, which the
   user saw for sale at the caravan and bought after a Normal kill: the missed-prize path, confirmed on screen).
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
2. **Field abilities shuffled as items** (hover, dig, horn dash, heavy dash, big icicle, bubble shield).
   Party members stay where the story puts them.
3. **An "open start" yaml option next** (the user, 2026-09-24), after chapter 1's locations: skip the prologue and
   tutorial, optionally with Leif from the start (the new-game party `{0, 1}`, `MainManager.cs:3591`, becoming
   `{0, 1, 2}`; early cutscenes are written for two, so tested on a fresh file). A full story strip, as the Metroid
   Fusion randomizer does, isn't the plan: here every cutscene also changes the world through flags.
   **An open-world option later,** researched one chapter at a time. Regions stay whole areas for now; one
   region per map (doors from the dump) comes before random start or open world (the user, 2026-09-24).
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
4. **A full bag:** key items keep arriving, only ordinary items wait.
5. **Goal:** the mod counts the game's artifact flags and sends "goal reached" at the required number.

**Known issues:**

- The generator's manifest warning ("will stop working with Archipelago 0.7.0") came from a hand-zipped
  apworld. The properly packaged file (build step 1) fixes it once it replaces the copy in the installed
  Archipelago's `custom_worlds`.

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
- **Which class each item gets** (the user, 2026-09-24): every field ability is *progression*. A key item is
  *progression* when any rule in the logic uses it, even for a single location; a key item nothing uses is
  *useful*. Every medal is *useful*, except the Hard Mode medal (#11), which is *filler*: it only makes
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
  like "Top of Pillar" only when a room needs telling apart). Not a sentence and not a hint at how to get it
  ("On Top of a Pillar", "Under a Rock" are too much). Gifts are `<Area>: <Who>'s Gift` or `<Who>'s Reward`,
  like `Outskirts: Maki and Eetl's Gift`. A character's name only when players will remember it (main and
  recurring ones); a minor one is described instead ("Ladybug Kid's Reward", "Ladybug Siblings' House", for
  Leby and Dib) (the user, 2026-09-24). Never the item, the flag or a mechanic ("Beemerang" goes stale once
  abilities are shuffled), Title Case, one word per kind of landmark everywhere. The test
  `TestLocationNames` fails if a location's name contains its own vanilla item's name. Renaming a location
  never changes its id or flag.

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

*Code: `apworld/bug_fables/options.py` (`ArtifactsRequired`), `world.py` (`generate_early` lowers the
number, `create_regions` adds the artifact events), test `TestArtifactsCapped`.*

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

**Status (2026-09-24, local server):** it works. The mod logs in compressed, the server's warning is gone,
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
server is down is found again at the next login and sent then. Within a session, the client library keeps
every check the server hasn't confirmed and sends it again with the next one.

**What the log shows:** `[check] watching ...` (which locations and flags), then `[check] location ... is
done (flag N set): sending`, `[check] sent ...`, and `[check] now checked on the server: ...`.

**Tests:** the apworld checks that every location has its flag in `slot_data` (the permit's is 15, the
medal's 32), and that the world version is written in one place only (the manifest). Both fail without the
change. The world version went to 0.2.0.

*Code: `apworld/bug_fables/world.py` (`fill_slot_data`), test `TestSlotData`; in the mod,
`LocationChecks.cs` (`Tick`) and `ApConnection.cs` (`ReadLocationFlags`, `SendChecks`).*

**Not yet:** the game still hands out its own item at the location, the medal here. Replacing that with the
server's item is the next step. A save from another seed would have sent its finished locations here; build step 7 ties each save to its
seed, which closed that.

**Status: works (2026-09-24, local server).** The user loaded a save from before Artis, already past the
permit. On loading, the mod sent the permit's location at once (flag 15 was already set: the save acted as
the outbox). Talking to Artis sent the medal's location (flag 32). For both, the mod logged `sending`, the
server's confirmation and `sent`, and the server logged `BugTester sent ... (Outskirts: Explorer Permit)` and
`(Outskirts: Artis's Medal)`. (Those two were later renamed `Outskirts: Maki and Eetl's Gift` and
`Outskirts: Artis's Gift`; same ids and flags.)

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

**Status: works (2026-09-24).** The server already held the Explorer Permit and the G-Bug Ranger Plushie from
the swap test. On loading, the save tied itself to the seed, and both arrived in key items as soon as the
player was free (the user saw them). Talking to Artis again showed the plushie but gave no second one: each
item comes once per seed, and the count in the save keeps it that way.

*Code: `mod/BugFablesAP/ItemReceiver.cs`: `CountSlot` and `SeedSlot` (the two save slots),
`SaveMatchesSeed`, `Tick` (one item per frame), `Busy` (is the player free), `Give` (where each item goes).
The slot survey was `VarDump.cs`.*

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
**Into chapter 2** (the user's play-through, 2026-09-24): two more story events, each a region gate from the gate
table. *City Opened* (Event60, flag 107, after the first boss) opens the door from outside the city into *Bugaria
City*; *Chapter 2 Started* (Event45 at the Ant Palace, flag 67) opens the palace rooms and city districts (*Ant
Palace*, since renamed *Bugaria Inner City*: it also holds the districts). Story events can have their own
`requires` now (the city needs the first boss). Locations there: a Lore Book behind the library bookshelf (test
`TestChapterTwo`, which fails without the gate), and the old book delivery, board quest 33, whose reward is a
Lore Book (category quest; the user played it through).
**Optional categories** (the user, 2026-09-24): a location can carry a `category`; its yaml option decides whether
the seed includes it. *Shuffle Quests* (on by default) covers quest-board and side-quest rewards; one-off NPC gifts
will have their own toggle. With a category off, its locations aren't created, their vanilla items stay out of the
pool, and they're left out of `slot_data`, so the client never swaps them and the game hands them out as usual
(tests `TestQuestsOff`, `TestQuestsOnByDefault`).
**The general rule** (the user, 2026-09-24): if reaching something uses an ability, the logic requires that
ability. Leif is in effect the freeze ability. Some droplet rooms are optional, so this is stricter than the game,
which is the safe direction: never impossible, only less random.

Still to do: the one event not found, characters that block a path, the region graph built from all of
it, and the tests.

*Code: `dev-scripts/gate-table.py`, `dev-scripts/event-triggers.py`; the dumps in `mod/BugFablesAP/EntityDump.cs`,
`MapDump.cs` and `ScriptDump.cs`.*

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
