# Archipelago requirements and known failure modes

The hard requirements come from Archipelago's `docs/adding games.md` at tag `0.6.7` (read 2026-09-24). **Tick
a box only with its evidence and date.** "It should work" doesn't count, and neither does a green build for
anything that happens in the game.

## Client (the mod)

- [ ] Secure (`wss://`) and insecure (`ws://`) connections
- [ ] Reconnects when the connection is lost mid-play
- [ ] The port in saved connection info can be changed (hosted rooms can lose their reserved port)
- [ ] Sends `StatusUpdate` (goal) when the player completes their goal. Use StatusUpdate, not an event
- [ ] Sends a location check when one is detected in the game
- [ ] Checks made while offline are sent on connect, recovered from the save's own flags
- [ ] Items can be given on demand, at any time
- [ ] Any item can be received any number of times, beyond the game's normal quantities
- [ ] Items with no player or location attached (admin or server commands) are handled
- [ ] Keeps a received-item index for resyncing (in the save, since items are remote only)
- [ ] Items sent while disconnected are received on connect

## World (the apworld)

- [ ] `worlds/bug_fables/` with `__init__.py`, and an `__init__.py` in every subfolder holding `.py` files
      (including `test/`)
- [ ] A game info doc `en_Bug Fables.md` and a setup doc, both listed in the `WebWorld`'s tutorials
- [ ] A `World` subclass with a unique `game`, and a `WebWorld` instance
- [ ] `item_name_to_id`, `location_name_to_id` and `create_item`
- [ ] An origin region ("Menu" by default), always reachable
- [ ] At least one location, and **an item pool exactly equal in size to the location count**
- [ ] `multiworld.completion_condition[player]` is set
- [ ] Items and regions are added with `append`/`extend`/`+=`, never `=`
- [ ] Only `self.random` is used, never Python's `random`
- [ ] Packaged with the "Build APWorlds" launcher component into a lowercase `bug_fables.apworld`

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
| Only `DisconnectAsync` on a dead MultiClient.Net socket (this game's Mono, 2026-09-24) | `State` stays `Open`, so the library's receive loop spins: lag and ~2.5 MB/s of memory, no error. Abort the `ClientWebSocket` |
| `TryConnectAndLogin` trusted to time out (MultiClient.Net 6.7.1) | Its login step waits on `SendPacket` with no timeout; one stuck attempt stops every retry. Give each attempt a deadline |
