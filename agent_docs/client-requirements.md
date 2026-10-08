# Archipelago requirements and known failure modes

The hard requirements come from Archipelago's `docs/adding games.md` at tag `0.6.7` (read 2026-09-24; re-read at
`0.6.8` on 2026-10-08: the same list, reworded, now pointing to APQuest as the example world); since
2026-09-29 the lines below also carry what the full review of every Archipelago doc found
([archipelago-review.md](archipelago-review.md), whose numbers they cite). **Tick a box only with its evidence and
date.** "It should work" doesn't count, and neither does a green build for anything that happens in the game.

## Client (the mod)

- [x] Secure (`wss://`) and insecure (`ws://`) connections (2026-09-24: `wss` to archipelago.gg, `ws` to a local
  server, `apimplementation.md` build step 5)
- [x] Reconnects when the connection is lost mid-play (2026-09-24: drop, retry and reconnect on a local server, build
  step 4)
- [x] The port in saved connection info can be changed (hosted rooms can lose their reserved port) (2026-09-24: a
  Port row of its own in the Archipelago panel, seen on screen; `documentation.md` step 8)
- [x] Sends `StatusUpdate` (goal) when the player completes their goal. Use StatusUpdate, not an event (2026-09-26:
  the first artifact reached on a dev file, the spider fight ended with the console's `killall`, `[goal] sent: 1 of 1
  artifacts`; the server logged the release and "Team #1 has
  completed all of their games")
- [x] Sends a location check when one is detected in the game (2026-09-24: seen by the user, build step 6)
- [x] Checks made while offline are sent on connect, recovered from the save's own flags (2026-09-24: a save with
  flag 15 already set sent its check on load, seen, and the server logged it; build step 6)
- [x] Items can be given on demand, at any time (2026-10-04: the server console's `/send` and `/send_multiple`
  mid-play, each arriving at once, seen on screen; build step 7)
- [ ] Any item can be received any number of times, beyond the game's normal quantities (2026-10-04: the same item
  three times in a row arrived, seen; beyond the bag's limit waits on the full bag, Next 6)
- [x] Items with no player or location attached (admin or server commands) are handled (2026-10-04: a Crystal Berry
  and three Mushrooms from the cheat console arrived, each with its hold-up, seen on screen; build step 7)
- [x] Keeps a received-item index for resyncing (in the save, since items are remote only) (2026-09-24: slot 60
  holds the count, seen by the user, build step 7)
- [x] Items sent while disconnected are received on connect (2026-09-24: two items the server already held arrived
  at the next login, seen on screen; build step 7)
- [ ] Room messages (`PrintJSON`) are shown to the player, or `NoText` is sent (review 18)
- [ ] Connect carries the `uuid` kept in Archipelago's `common.json` and the targeted Archipelago version (review 19;
  the version built 2026-10-08, 0.6.8, not yet seen; the `uuid` still the library's new one each time)
- [ ] A refusal without error codes stops the retries; `InvalidPacket` is logged (review 20)
- [ ] A failed or refused attempt closes its connection (review 2)

## World (the apworld)

- [x] `worlds/bug_fables/` with `__init__.py`, and an `__init__.py` in every subfolder holding `.py` files
      (2026-09-29: `logic/` and `test/`, each with its `__init__.py`; 2026-10-08, against `apworld specification.md`
      at 0.6.8, which asks it of every imported subfolder: still those two, and `data/` and `docs/` hold no Python)
- [x] A game info doc `en_Bug Fables.md`, found through the `WebWorld`'s `game_info_languages`, and a setup doc
      listed in its tutorials (2026-09-29: read in `web_world.py`)
- [x] A `World` subclass with a unique `game`, and a `WebWorld` instance (2026-09-27: read in `world.py`; 384 tests pass
  at 0.6.7)
- [x] `item_name_to_id`, `location_name_to_id` and `create_item` (2026-09-27: read in `world.py` and `items.py`; 384
  tests pass at 0.6.7)
- [x] An origin region ("Menu" by default), always reachable (2026-09-29: named in `world.py`, made in `regions.py`;
      a random start isn't in the logic yet, labelled experimental, build step 15)
- [x] No pool item placed by hand, no `eval`, no `yaml.load` (2026-09-29: none in the apworld, searched)
- [x] An item any rule uses is progression (2026-09-29: `TestClassifications`, which reads each rule's
      `item_dependencies()`, passes)
- [ ] One id per item name (the story's "Leif" event shares its name with the real item; review 3)
- [ ] The encouraged features: option groups, presets, a bug report page, rich-text option texts (review 8)
- [x] At least one location, and **an item pool exactly equal in size to the location count** (2026-09-27:
  asserted by `test_logic.py`, which passes)
- [x] `multiworld.completion_condition[player]` is set (2026-09-27: through `set_completion_rule`, read in `rules.py`)
- [x] Items and regions are added with `append`/`extend`/`+=`, never `=` (2026-09-27: read
  in `regions.py`, `locations.py` and `items.py`; 384 tests pass at 0.6.7)
- [x] Only `self.random` is used, never Python's `random` (2026-09-27: the door and enemy shuffles take it as an
  argument; no module-level `random` call)
- [x] Packaged with the "Build APWorlds" launcher component into a lowercase `bug_fables.apworld` (2026-09-26: CI
  builds it for every release; v0.1.0's loaded from `custom_worlds` and generated, `apimplementation.md` build step 17)

## Known failure modes

Learned in the author's earlier Archipelago project (2026-07, a Godot game with its own client). **Almost
all of them fail silently or report success.** Where MultiClient.Net may already handle one, that's for
its docs or source to settle; don't assume it.

| Trap | Symptom |
|---|---|
| Treating "socket open" as "in a seed" | Offline play leaks into a real seed, or pickups grant their vanilla item after a disconnect |
| `"Tracker"` (also `HintGame`, `TextOnly`) in the connect tags | Every check is refused, and you only see it if `InvalidPacket` is handled |
| Treating item/location ids as global across games | Wrong item names, but only in a room with two different games |
| Scouting with `create_as_hint` other than 0 | Hints spammed to the room and the seed spoiled |
| Reading `NetworkItem.player` in `LocationInfo` as the finder | It's the receiver there, so ids resolve in the wrong game's table |
| Scouting an id the server doesn't know | **The server disconnects the client**; the only evidence is in the host's log |
| Deciding the DeathLink tag before `slot_data` has arrived | DeathLink sends fine but never receives. Fix it with `ConnectUpdate` |
| Spotting your own DeathLink echo by player name | Two clients on one slot swallow each other's deaths. Use the timestamp |
| A received-item index mismatch with no recovery | Items silently stop arriving for the rest of the session. Request `Sync` |
| Clearing a check from the outbox on send, not on server confirmation | A send that never lands is lost |
| An outbox not tagged with its seed | An offline check from one seed gets sent into a different seed |
| An option deciding whether a location id EXISTS | The build knows ids the seed doesn't, and scouting them hits the disconnect above |
| An access rule silently attached to nothing | A valid-looking seed where the gate doesn't exist. Check the spoiler's playthrough spheres |
| Only `DisconnectAsync` on a dead MultiClient.Net socket (this game's Mono, 2026-09-24) | `State` stays `Open`, so the library's receive loop spins: lag and ~2.5 MB/s of memory, no error. Close the socket itself: `ClientWebSocket.Abort` on the netstandard build, websocket-sharp's `Close` on the net40 build we ship (build steps 4 and 5) |
| websocket-sharp compression switched on as it stands (MultiClient.Net 6.7.1 net40) | Every handshake refused: MultiServer always answers `server_max_window_bits=11`. Strip it before websocket-sharp's check (upstream #141) |
| MultiClient.Net's net40 Newtonsoft.Json in a Mono without Reflection.Emit (`Supports SRE: False`) | Login times out silently; the socket's error event shows `PlatformNotSupportedException` in `DynamicMethod`. Ship the netstandard2.0 Newtonsoft.Json |
| Sending on the game thread with websocket-sharp | Every send pings first and waits for the pong, up to 5 s: stutter, or a freeze while the server is gone |
| `TryConnectAndLogin` trusted to time out (MultiClient.Net 6.7.1) | Its login step waits on `SendPacket` with no timeout; one stuck attempt stops every retry. Give each attempt a deadline |
