# The full Archipelago review (2026-09-29)

Everything Archipelago publishes for a world and a client, read against this project, because the project follows
every standard and recommendation Archipelago mentions (`CLAUDE.md`; `apimplementation.md`, How it works §8).

**What was read, in full:** every file in Archipelago's `docs/` at the tag we target (0.6.7), each also diffed against
`main`; every generic player guide (`worlds/generic/docs/`); the reference world APQuest, file by file, at 0.6.7 and on
`main`; and MultiClient.Net's own docs and source (v6.7.1 and `main`). Seven readers, one area each, after the earlier
audit of what we rebuilt (2026-09-29). Every claim below cites both sides; **checked** means I read both sides myself.

**The order of the work is in `apimplementation.md`, "Where it stands", Next 43.** This page holds the evidence, in the
same numbering. Kinds: *bug*, *required* (the doc says must), *recommended* (should, recommended, encouraged, and the
conventions the docs and APQuest show), *main only* (not in 0.6.7 yet).

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
   says they are sent again after the next login (`:333-334`): the trap in `client-requirements.md` ("Clearing a check
   from the outbox on send"). Nothing is lost for good: the pickup comes back and can be taken again.

## Required

6. **The door shuffle belongs in `connect_entrances`** (checked): "entrance randomization is done exactly in
   `World.connect_entrances`" (`entrance randomization.md:371-378`); ours runs in `generate_early` (`world.py:56-59`).
   It can move now: `door_targets` is only read in `fill_slot_data`.
7. **`style.md`:** closing brackets on the line of the last element (`data_tables.py:36-38`,
   `logic/__init__.py:15-16, 18-19`; `style.md:22-23`); a blank line at the end of `data_tables.py` (`style.md:7`);
   11 Markdown lines over 120 characters in the player docs (`en_Bug Fables.md:10, 25`; `setup_en.md:61, 67, 72, 73,
   75, 78, 79, 85, 87`; `style.md:44`).

## Recommended: the apworld and the website

8. **The WebWorld** (`web_world.py`): option groups (`adding games.md:149-150`), presets (`:151-152`; CI's three option
   sets are a start), option texts in reST with `rich_text_options_doc = True` (`world api.md:59-61`, `options api.md:96-98`;
   the value lists at `options.py:87-90, 123-126, 156-161` need reformatting first), a bug report page
   (`adding games.md:148`), and `game` set as APQuest does (`apquest/web_world.py:9-11`).
9. **`topology_present = True`**: our exits are gated, and it puts the paths into the spoiler (`world api.md:487`,
   `AutoWorld.py:283-284`; ours is left False).
10. **Location and item groups** (`AutoWorld.py:294-298`): a "Shops" group for `exclude_locations`, groups for hints and
    plando (`plando_en.md:98, 219-239`).
11. **Archipelago's helpers:** `World.world_version` (not reading `archipelago.json` ourselves, `data_tables.py:15-19`);
    `Region.add_locations` (`locations.py:31-33`); option values in slot_data through `options.as_dict`
    (`apquest/world.py:83-86`; ours by hand at `slot_data.py:75-76`, and the mod checks bools, `SeedData.cs:126-129`).
12. **`start_inventory_from_pool`** (a convention: 32 of 88 worlds at 0.6.7; `world api.md:626-627`).
13. **The Rule Builder:** Jump's blanket rule as an `OptionFilter` (`rule builder.md:80, 92, 103-110`; ours an `if` in
    `rules.py:35-42`); `__str__` on our three rules, so they print their argument (`:501, 506`); `@override` on
    `_instantiate` (`:206`); benchmark `CachedRuleBuilderWorld` and record the decision (`:177`; `apquest/rules.py`,
    main only).
14. **Trackers:** Universal Tracker support (`interpret_slot_data`) and a PopTracker pack (`other_en.md:31-37`).
15. **slot_data: only what's necessary.** *Recommended:* "to not waste resources, it should be limited to data that
    is absolutely necessary"; for locations "it is preferable to use LocationScouts"; "the most common usage of slot
    data is sending option results" (`world api.md:878-887`). Ours sends seven entity tables, `ability_items` and
    `item_kinds`, the same in every seed (`slot_data.py:59-65, 78, 80`), and every location's detection data
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
    - `ability_items` goes: it is always true.

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
    Until it exists, the player docs point to the Launcher's Text Client for `!hint`, `!release` and `!collect`
    (`commands_en.md:13-15`).
19. **Connect:** a `uuid` kept in Archipelago's `common.json` (`network protocol.md:298`, `shared_cache.md:12`; the
    library makes a new one each time); the targeted Archipelago version, not the library's default 0.6.0
    (`network protocol.md:299`); the DeathLink tag in Connect, with `ConnectUpdate` only for changes (the room is told
    "changed tags" on every reconnect today); the event hooks attached before connecting (MultiClient.Net's
    `docfx/index.md:20-21`; ours after, `ApConnection.cs:443-457`).
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

23. **Archipelago's entrance randomizer** in place of `doors.py`, and what to prepare: a name for every door, as the
    doc advises (`entrance randomization.md:232-236`); one-way doors kept as doors, not map links, so they can be
    one-way entrances (`:238-242`); `force_creation` or `disconnect_entrance_for_randomization` for door exits
    (`rule builder.md:64`, `entrance randomization.md:228-230`); `pairings` turned into `door_targets` (`:381-384`);
    and Menu joined to the random start's region.

## Kept, because Archipelago has nothing for it

Closing a dead socket ourselves (the library's `Disconnect` closes only a live one); the offline record of checks
sent; reconnecting with a backoff; the compression switch (the net40 library never turns it on); the received count
in the save, read against `AllItemsReceived` (the library's index resets each session, so its queue goes unread);
the Harmony fix for the library's cache file names (a library bug, still on `main`); the enemy shuffle; the pool's
make-room step; the DeathLink panel row (a switch mid-seed, the user's choice; no doc asks for a yaml option); our
slot_data reader (it tolerates missing keys).

## Doesn't apply, and why

- `settings api.md`: host.yaml settings such as a ROM's path; the world needs none.
- `webhost api.md`: the website's HTTP API; nothing here calls it. Worth knowing: anyone with a room's tracker link can
  read its slot_data (`/slot_data_tracker`), the door shuffle included.
- `deploy using containers.md`, `webhost configuration sample.yaml`: running a WebHost; we host nothing.
- `running from source.md`: the Enemizer, SNI and the Linux build are for other games; the rest is item 22.
- `network diagram`: a picture; our client is its ".NET / MultiClient.Net / BepInEx" path.
- `mac_en.md`: the client is a Windows BepInEx mod; generation is pure Python and runs anywhere.
- Text plando and boss plando (`plando_en.md`): no texts or bosses shuffled; connection plando comes with item 23.
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
- `apworld specification.md` adds minimum and maximum version guidance; ours sets the minimum only.
