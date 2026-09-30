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
- Ours reconnects automatically, disconnects cleanly, and scouts at connect without blocking the game's main thread.

Its code style, read again 2026-09-28 (`main` at `0ef738f`), for a comparison with ours:
- **Hooks are Harmony attributes:** `[HarmonyPatch]` with `[HarmonyPrefix]`/`[HarmonyPostfix]` (about 213), installed
  with `PatchAll(typeof(X))` one class at a time and removed with `UnpatchSelf` when the mod is switched off.
- **A mismatch shows on the title screen:** an apworld version or a location list that doesn't match the client.
- **We take:** the attribute style (`documentation.md`, step 4).

## Tevi's apworld: `worlds/tevi` in BlackSoulKnight/Tevi_Archipelago (read 2026-09-28, world 0.7.5)

- **Data-driven:** 1 MB of JSON (areas, locations, items). Rules are strings such as `"A || (B && C)"`, turned
  into nested lambdas by its own parser.
- **Strengths:** entrances through Archipelago's generic entrance randomizer (`connect_entrances`), and Universal
  Tracker support (a generated location table, map pages, `interpret_slot_data`).
- **We take, later:** the generic entrance randomizer and Universal Tracker support, each as its own step.

## The Pokémon Crystal apworld: gerbiljames/Archipelago-Crystal (read 2026-09-28)

Two branches: `pokecrystal-develop` (world 0.20.1) and `future/6.0.0`, the next major version (at `55197f1a06`).
- **Typed data:** 36 to 41 frozen dataclasses (`LocationData`, `ItemData`, `RegionData`...) and a dozen enums,
  parsed once at import into one frozen data object. Per-seed changes go through `dataclasses.replace`. Raw
  `dict[str, Any]` appears 6 times in the whole world.
- **`future/6.0.0` moved its logic to `rule_builder`:** 12 custom `Rule` dataclasses (`CanUseHM`, `HasBadges`...)
  and a `LogicMixin` for its counters. Its lambdas fell from 363 to 49.
- **Its entrance randomizer is Archipelago's generic one,** with a retry ladder and pinning to vanilla as a last
  resort. Rules sit on named entrances, so they travel with the entrance.
- **Tests:** 95 methods on the old branch, 461 on the new one, plus a fuzzer in CI.
- **Style:** the new modules are typed, documented and within 120 columns.
- **We take:** typed frozen data (the apworld's data tables), the 120-column limit, and later option groups and
  Universal Tracker support.

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
