# The full Archipelago review (2026-09-29)

Everything Archipelago publishes for a world and a client, read against this project, because the project follows
every standard and recommendation Archipelago mentions (`CLAUDE.md`; `apimplementation.md`, How it works §8).

**What was read, in full:** every file in Archipelago's `docs/` at the tag we target (0.6.7), each also diffed against
`main`; every generic player guide (`worlds/generic/docs/`); the reference world APQuest, file by file, at 0.6.7 and on
`main`; and MultiClient.Net's own docs and source (v6.7.1 and `main`). Seven readers, one area each, after the earlier
audit of what we rebuilt (2026-09-29). Every claim below cites both sides; **checked** means I read both sides myself.

**The order of the work is in `apimplementation.md`, "Where it stands", Next 43.** This page holds the evidence, in the
same numbering. Kinds: *bug*, *required* (the doc says must), *recommended* (should, recommended, encouraged, and the
conventions the docs and APQuest show), *main only* (not in 0.6.7 yet). Line numbers in our own files are as of the
review (2026-09-29); the files have moved since, the claims checked again 2026-09-30.

## Bugs

1. **Shop Contents' fallback** (checked). `rules.fall_back_from_filler_only` runs in `pre_fill`, after Archipelago has
   applied a player's `exclude_locations` (`Main.py:121`) and the local and non-local item rules (`Main.py:137-140`);
   it assigns `item_rule` outright and sets every shop back to normal. Also: `priority_locations` on a shop is dropped
   (Filler Only: `Main.py:130-134`; No Progression: the item rule, `Fill.py:584`), and plando aimed at a shop fails
   silently (`force: silent`, `plando_en.md:82`); neither is written anywhere a player would read.
2. **Failed connect attempts left open** (checked). If reading slot_data fails right after a successful login
   (`ApConnection.cs:435`), the logged-in connection is neither kept nor closed, and the retry (`:502-506`) logs in
   again: one more client on the slot per retry. Refused (`:477-486`) and timed-out (`:487-491`) attempts leave their
   sockets open too.
3. **Two items named "Leif"** (checked). *Required:* "if you need to create multiple items with the same name, they
   will all have the same ID" (`world api.md:248-250`). With the story's party the story event *Leif Joins* creates an
   event item "Leif" with no id (`logic/snakemouth_den.py:69`), while `data/items.json:7` has the real item "Leif" with
   one.
4. **A test that can't fail** (checked). `test_shops.py:69-73` looks for progression in shops, but the test base stops
   before fill (`test/bases.py:58-82`), so every shop is empty and nothing is ever asserted.
5. **Respawning checks leave the outbox when sent, not when confirmed** (`ApConnection.cs:258-265`), though the log
   said they are sent again after the next login (`:333-334`; since 2026-09-30 it says only a flag's check is found
   again), and item-shop purchases share that outbox: the trap in `client-requirements.md` ("Clearing a check
   from the outbox on send"). Nothing is lost for good: the pickup comes back and can be taken again.

## Required

6. **The door shuffle belongs in `connect_entrances`** (checked): "entrance randomization is done exactly in
   `World.connect_entrances`" (`entrance randomization.md:371-378`); ours runs in `generate_early` (`world.py:56-59`).
   It can move now: `door_targets` is only read in `fill_slot_data`. **Done 2026-09-30** (build step 12): `world.py`
   shuffles the doors in `connect_entrances`.

## Recommended: the style guide

`style.md` says "should" or gives a plain rule, never "must", and `world api.md:21` only points to it.

7. **`style.md`:** closing brackets on the line of the last element (`data_tables.py:36-38`,
   `logic/__init__.py:15-16, 18-19`; `style.md:22-23`); a blank line at the end of `data_tables.py` (`style.md:7`);
   11 Markdown lines over 120 characters in the player docs (`en_Bug Fables.md:10, 25`; `setup_en.md:61, 67, 72, 73,
   75, 78, 79, 85, 87`; `style.md:44`).

## Recommended: the apworld and the website

8. **The WebWorld** (`web_world.py`): option groups (`adding games.md:149-150`; the first, "Aesthetic Options", came
   with build step 33), presets (`:151-152`; CI's three option
   sets are a start), option texts in reST with `rich_text_options_doc = True` (`world api.md:59-61`, `options api.md:96-98`;
   the value lists at `options.py:87-90, 123-126, 156-161` need reformatting first), a bug report page
   (`adding games.md:148`), and `game` set as APQuest does (`apquest/web_world.py:9-11`).
9. **`topology_present = True`**: our exits are gated, and it puts the paths into the spoiler (`world api.md:487`,
   `AutoWorld.py:283-284`; ours is left False).
10. **Location and item groups** (`AutoWorld.py:294-298`): a "Shops" group for `exclude_locations`, groups for hints and
    plando (`plando_en.md:98, 219-239`). The first item groups, *Submarine* and *Boat*, came with build step 36.
11. **Archipelago's helpers:** `World.world_version` (not reading `archipelago.json` ourselves, `data_tables.py:15-19`);
    `Region.add_locations` (`locations.py:31-33`); option values in slot_data through `options.as_dict`
    (`apquest/world.py:83-86`; ours by hand at `slot_data.py:75-76`, and the mod checks bools, `SeedData.cs:126-129`).
12. **`start_inventory_from_pool`** (a convention: 32 of the 81 game worlds at 0.6.7; `world api.md:627`).
13. **The Rule Builder:** Jump's blanket rule as an `OptionFilter` (`rule builder.md:80, 92, 103-110`; ours an `if` in
    `rules.py:35-42`); `__str__` on our rules (three then, five since: `CanUse`, `Member`, `MoveItem`, `Boat`, `WayBack`), so they print their argument (`:501, 506`); `@override` on
    `_instantiate` (`:206`); benchmark `CachedRuleBuilderWorld` and record the decision (`:177`; `apquest/rules.py`,
    main only).
14. **Trackers:** Universal Tracker support (`interpret_slot_data`) and a PopTracker pack (`other_en.md:31-37`).
    Read 2026-10-01 (licences in `licensing.md`): Universal Tracker runs the apworld's own rules ("using the actual
    generation logic", `other_en.md:37`); anything random not from the yaml or items comes back from slot_data
    through `interpret_slot_data` or `re_gen_passthrough`, and its `/explain` uses the Rule Builder's explanations
    (its `docs/apworld-integration.md` and `re-gen-passthrough.md`, branch `tracker`). PopTracker reads only its
    pack's JSON `access_rules` and Lua (`doc/PACKS.md`, "Rules"), so a pack's logic is exported: the Rule Builder
    "is intended to be written first in Python [...]. To facilitate exporting the rules to a client or tracker,
    rules have a `to_dict` method" (`rule builder.md:368`). Rules written as text would need the same export.
    Maps (read 2026-10-01): a PopTracker map is one fixed image, its pins at fixed `x`/`y`; Lua can hide or show pins
    and zoom, pan or switch tabs, but has no access to the UI beyond that (`PACKS.md`, Maps, Locations, Ui Hints), so
    nothing redraws a map per seed. Universal Tracker's map tab reads the same PopTracker JSON, takes all logic from
    the apworld, and with entrance tracking colours each door pin and shows where it leads once found; it advises
    against shipping map images in an apworld (its `docs/map-integration.md`).
    **Decided (the user, 2026-10-01), plans only for now:** full support for both, every feature their docs offer.
    Universal Tracker generates with no yaml (options and the seed's choices from slot_data) and always shows every
    location in logic: no deferred entrances. The PopTracker pack lives in its own folder, its own repo later, never
    here: a map drawn by code (no AI art, no game art, its look picked by the user on screen), Room Swap rooms shown
    in their new slot, Coupled/Decoupled rooms at their normal place with no lines, and an optional fog of war (on by
    default) that only PopTracker has. The mod would write three data storage keys for them (rooms visited, the
    current room, doors taken). The step-by-step plans are local files, not in the repo.
15. **slot_data: only what's necessary.** *Recommended:* "to not waste resources, it should be limited to data that
    is absolutely necessary"; for locations "it is preferable to use LocationScouts"; "the most common usage of slot
    data is sending option results" (`world api.md:878-887`). Ours sends seven entity tables (nine since build step
    36), `ability_items` and `item_kinds` (and since build step 36 `submarine_item`), the same in every seed (`slot_data.py:59-65, 78, 80`), and every location's detection data
    (`:41-57`), all fixed per location id. Measured (2026-09-29, doors shuffled): 65 KB, 56 KB of it the seed's own
    door shuffle, 3.3 KB the same-in-every-seed tables. **Decided (the user, 2026-09-29): follow it, and fix what
    it risks properly:**
    - slot_data keeps the world version, the option results (`options.as_dict`, item 11) and the seed's own rolls
      (doors, enemies, the start, a random starting member);
    - the fixed tables (each location's detection, the entity lists, item kinds) are built into the mod from the
      apworld's own data when the mod is built, with a check that the two never differ; never copied by hand;
    - the seed's locations come from the server (the library's `Locations.AllLocations`), which also means the mod
      never scouts an id the server doesn't know;
    - on connect the mod refuses a seed whose world version isn't the one its tables were built from, with a clear
      message: what keeps the mod from departing from what the generator knew (`CLAUDE.md`, reworded);
    - `ability_items` and `submarine_item` go: both are always true.

## Recommended: tests

16. **Where the base lives, and Archipelago's own tests:** the base in `test/bases.py`, with `world` annotated as
    APQuest does (`tests.md:19-20`; `apquest/test/bases.py:13-16`), `rule_parts` and `logic_rules` with it; and
    Archipelago's generic tests run on this world in CI (`ci.yml:63` runs only ours; on main, `AP_TEST_WORLDS`).
    `world api.md:921-935` still says `test/__init__.py`; `tests.md`, the newer, says it's deprecated.
17. **Test hygiene:** `run_default_tests = False` where a class repeats an option set (36 classes, 25 distinct sets;
    `tests.md:79-80, 103-105`); a plain `TestCase` for the 15 tests that never touch the multiworld (`tests.md:85-88`);
    every option a test depends on written out (`apquest/test/test_easy_mode.py:12-15`); `assertAccessDependency`
    where a test claims an item is needed (`tests.md:66-74`); annotations on members and helpers (`style.md:17-19`);
    the package name hard-coded at `test_slot_data.py:25`.

## Recommended: the client

18. **Room messages:** nothing in the mod shows `PrintJSON` ("sent to clients purely to display a message to the
    player", `network protocol.md:173, 189`), and it doesn't send `NoText` (`:779`). **Chosen (the user,
    2026-09-29): show them**, in the in-game text client already planned (`apimplementation.md`, Next 9; the design in
    the mod guide, step 2): a feed in the bottom-left corner (items sent and received, joins and leaves, DeathLinks),
    a filter per kind of message, and hints and commands typed in game, built on the library's message log and `Say`.
    Until it exists, the player docs are to point to the Launcher's Text Client for `!hint`, `!release` and `!collect`
    (`commands_en.md:13-15`); not written yet.
19. **Connect:** a `uuid` kept in Archipelago's `common.json` (`network protocol.md:298`, `shared_cache.md:12`; the
    library makes a new one each time); the targeted Archipelago version, not the library's default 0.6.0
    (`network protocol.md:299`); the DeathLink tag in Connect, with `ConnectUpdate` only for changes (the room is told
    "changed tags" at every login with DeathLink on, `MultiServer.py:1990-1996`); the event hooks attached before
    connecting (MultiClient.Net's `docfx/index.md:20-21`; ours after, `ApConnection.cs:443-457`).
20. **The rest of the protocol and the library:** a refusal with no error codes is retried forever (`:480-491`;
    `errors` is optional, `network protocol.md:121`); `InvalidPacket` is never logged (`:230-231`); the library's
    `SetGoalAchieved`, `Locations.AllLocations`, `GetRaceModeAsync` and `ColorUtils` (the earlier audit); its optional
    Analyzers package (`docfx/index.md:10-13`); `ClientPlaying` when play starts (`packets.md:91-95`, descriptive).

## Recommended: the player docs

21. **Against Archipelago's own guides:** a local server as `localhost` works in the generic guide
    (`setup_en.md:218-219`) but needs `ws://` in ours (the mod could add it itself); install with the Launcher's
    Install APWorld (`:142-144`), not by copying into `custom_worlds`; slot names are case-sensitive (`:228`); install
    only apworlds you trust (`:148-149`); a custom world is generated locally, then hosted (`:141-146`); link the
    generic setup guide, the universal options (`advanced_settings_en.md`) and the options page (as
    `apquest/docs/en_APQuest.md:3-6` does); say which platforms the mod supports (`mac_en.md`).

## Recommended: our own process

22. **`development.md`:** Python 3.11.9 to 3.13 and a venv, and a link to `running from source.md`; `--log_network` on
    the local server (`:25`); previewing our pages with a local WebHost (`:26-28`); the server's `/send_location` in
    place of `send-as-player.py`; and the world maintainer's duties we take on anyway (`world maintainer.md:17-21`):
    fix what a core change breaks, test on `main` and release candidates from time to time.

## With the room mapping

23. **Archipelago's entrance randomizer** in place of `doors.py`: **done 2026-09-30** (build step 12), ahead of the room
    mapping, on one region per map. Each door named where it is (`entrance randomization.md:232-236`), split with
    `disconnect_entrance_for_randomization` (`:228-230`), placed by `randomize_entrances` in `connect_entrances`
    (`:373-379`), `pairings` turned into `door_targets` (`:381-384`), the pairs in the spoiler's Entrances section. Still
    to do: one-way doors kept as doors, not map links, so they can be one-way entrances (`:238-242`; today the 17
    one-way fixed doors are plain entrances, never shuffled), and Menu joined to the random start's region. Connection
    plando (`plando_en.md:283-318`, `Options.PlandoConnections`): optional in the guide ("Support for connection plando
    may vary"), built all the same (2026-09-30, the user: every optional feature, plando included; build step 32).

## A second look at what we kept (2026-09-29)

After the slot_data decision, each kept item was checked again for a way Archipelago or its library does offer.

24. **The enemy shuffle stays in `generate_early`** (reversed 2026-09-30). `generate_basic` is "Useful for randomizing
    things that don't affect logic … i.e. … randomizing enemies" (`AutoWorld.py:408-413`), and ours runs in
    `generate_early` (`world.py:61-63`). The first reading was that map fights can always be fled, so the shuffle doesn't
    affect the logic. **The user, 2026-09-30:** it is logic. Bosses and other fights that can't be fled limit what may be
    placed there to enemies the party can beat (build step 14's party rule). *Shuffle Bestiary* checks (Next 44) would
    follow `enemy_swaps`. So the shuffle must be decided before the rules. What doesn't touch the logic, *Music
    Shuffle*, goes in `generate_basic` (build step 33).
25. **Receiving items through the library's queue** (to check first): MultiClient.Net documents `ItemReceived` and
    `DequeueItem` (`docfx/helpers/helpers.md:55-68`); the mod polls `AllItemsReceived` against the count in the save
    (`ItemReceiver.cs:81-123`), and the library's queue is never read. The queue restarts each session, so it has to
    work with the save's count: read the library before changing it.
26. **DeathLink: Archipelago's yaml option too.** Decided (the user, 2026-09-29): both. Archipelago's `DeathLink`
    option (`DeathLinkMixin`, `Options.py:1495-1498, 1726-1728`) sets it for the seed and turns the panel switch on or
    off at login; the panel switch stays, to change it mid-seed.
27. **The library's cache bug, reported upstream** (checked in its source, 6.7.1 and `main`): `GetFileSystemSafeFileName`
    returns its input unchanged, and the read uses the checksum uncleaned. No issue or pull request mentions it (six
    searches, 2026-09-29), and the file hasn't changed since 2024-05-27; PR #124 touches the same file but not this.
    Decided (the user, 2026-09-29): if reported, a text-only issue the user posts; never code from us. Our patch stays
    until a fixed release. **Reported:** the user posted it as
    [#143](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/issues/143) (2026-09-29).
28. **One of our workarounds has a fix waiting upstream** (checked in each pull request's diff, 2026-09-29): #141,
    opened by someone else, turns on websocket-sharp's compression in the websocket-sharp helper and the DLL our net40
    build uses (`DLLs/websocket-sharp.dll`), so our compression switch can go once a release carries it and we have
    seen it work. #142 (releasing the socket on disconnect) changes only the `System.Net.WebSockets` helper, which
    net40 doesn't use: it would not retire our dead-socket close. Neither is merged; until then, ours stay.

## Kept, because Archipelago has nothing for it

Closing a dead socket ourselves (the library's `Disconnect` closes only a live one; #142 doesn't cover net40, item 28); the offline
record of checks sent; reconnecting with a backoff; the compression switch (the net40 library never turns it on;
until #141, item 28); the received count in the save (the library's index resets each session; its queue, item 25);
the Harmony fix for the library's cache file names (until a fixed release, item 27); the enemy shuffle itself (its
step, item 24); the pool's make-room step; the DeathLink panel switch, next to Archipelago's yaml option (item 26);
our slot_data reader (it tolerates missing keys).

## Doesn't apply, and why

- `settings api.md`: host.yaml settings such as a ROM's path; the world needs none.
- `webhost api.md`: the website's HTTP API; nothing here calls it. Worth knowing: anyone with a room's tracker link can
  read its slot_data (`/slot_data_tracker`), the door shuffle included.
- `deploy using containers.md`, `webhost configuration sample.yaml`: running a WebHost; we host nothing.
- `running from source.md`: the Enemizer, SNI and the Linux build are for other games; the rest is item 22.
- `network diagram`: a picture; our client is its ".NET / MultiClient.Net / BepInEx" path.
- `mac_en.md`: the client is a Windows BepInEx mod; generation is pure Python and runs anywhere.
- Text plando and boss plando (`plando_en.md`): no texts or bosses shuffled; boss plando comes with a boss shuffle.
- `triage role expectations.md`, `code_of_conduct.md`, `CODEOWNERS`: Archipelago's own repository (below).

## For the main repository (written down; not planned)

A `CODEOWNERS` line with the exact GitHub user name (`CODEOWNERS:7`); the pull request labelled `is: new game`
(`triage role expectations.md:70-76`); the code of conduct over every pull request and message; GitHub Actions on our
fork (`contributing.md:20-23`); the website's options page and generation would then apply, and the setup guide would
drop the download and `custom_worlds` steps. The world maintainer's duties are item 22.

## Main only (not in 0.6.7 yet)

- **`quantity`**, a new top-level key in a player's yaml (`advanced_settings_en.md` on main, lines 63 and 81-82):
  "the amount of times this yaml should be used when generating", default 1; above 1 the name must use the numbering
  keywords, and the host must allow it (`allow_quantity`, `Generate.py` on main, 43-44 and 140-146). Each copy is an
  ordinary slot of ours, so nothing in the world or the mod changes.
- `AP_TEST_WORLDS=bug_fables pytest` runs Archipelago's generic tests on one world (`tests.md`).
- APQuest moved to the Rule Builder on 2026-04-18 (#5906), after our checkout.
- `Bounce` gains `teams` and `operator` (`network protocol.md`); our DeathLink sends tags and data only.
- `apworld specification.md` adds a section on choosing `minimum_ap_version` and `maximum_ap_version` (both fields
  are in 0.6.7 already): most worlds need only the minimum, as ours sets.
