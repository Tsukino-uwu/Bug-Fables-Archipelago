# References: other projects read for their approach

Each one's licence is in `licensing.md`. **We take facts and approach only, never code.**

## Tevi_Randomizer: a Unity Mono BepInEx Archipelago mod (read 2026-09-24, last commit 2026-07-01)

Taken as the closest precedent (same engine family, same loader):
- `netstandard2.0`, `Archipelago.MultiClient.Net` 6.7.1 and `BepInEx.Core` 5 from NuGet, with the game DLLs
  referenced through `HintPath` (`Tevi_Randomizer.csproj`).
- Every way the game hands out an item funnels into one tracker that sends the check. A local collected list
  is resent on sync (`LocationTracker.cs`).
- Received items are applied in `Update()`, **one per frame, only while play is safe** (no map change, no
  pause, no cutscene) (`ArchipelagoInterface.cs`).
- The received-item index is saved in the game's save, and autosaves get their own snapshot
  (`SaveGamePatch.cs`).

Where we differ:
- It gives your own items locally. **We are remote only.**
- By its own header comment it lacks automatic reconnect and a clean disconnect, and it blocks the main
  thread with `.Wait()`/`.Result` while scouting at connect. We need all three done properly.

## Pokémon Emerald's apworld `remote_items` option (read 2026-09-24, local checkout at `0.6.7`)

- With it off, your own items are patched into the ROM at their locations (`rom.py`). With it on, the
  client adds your own world's items to `items_handling` via `ConnectUpdate` (`client.py`), and every item
  comes from the server. Race mode forces it on.
- **The received count lives in the save** (`client.py`, `handle_received_items`). The client hands the game
  one item plus the new count, and the game keeps both. That's why the option's description promises
  recovery after a lost save and co-op on one slot.
- Why remote only fits a mod better: a mod has no patched placements, so local mode would first need
  scouts cached per seed plus a second code path.

## The author's earlier Godot Archipelago project (2026-07, their own work, MIT)

Its implementation notes (all 14 client-to-server packets, all 12 server-to-client packets, and 9 of 9 client
requirements, each verified against a live server) are the design reference for the later stages:
DeathLink, TrapLink and traps, DataStorage, hints, options and entrance shuffle. The failure modes in
`client-requirements.md` come from it. One thing that doesn't carry over: it keeps no save, so its applied
item count is memory-only by design. Bug Fables saves items, so ours must be saved.
