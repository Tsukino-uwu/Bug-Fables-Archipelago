"""slot_data: everything the client acts on. Its keys are the contract with the mod (ApConnection.cs)."""
from __future__ import annotations

from collections.abc import Callable, Iterable, Mapping
from typing import TYPE_CHECKING, Any

from .data_tables import (DIALOGUE_FLAGS, HELD_UNTIL, ITEM_NAME_TO_ID, ITEMS, KEPT_OPEN, KEPT_PRESENT, LOCATION_NAME_TO_ID,
                          PRESENT_FROM, SCENERY_HIDDEN, SCENERY_PRESENT, WORLD_VERSION)
from .data_types import DialogueFlag, EntityRef, FlagEntity, Source

if TYPE_CHECKING:
    from .world import BugFablesWorld


def _by_location(world: BugFablesWorld, key: str, value: Callable[[Source], Any] | None = None) -> dict[str, Any]:
    """{location id: what its source says}, for each included location whose source has key. JSON keys are strings."""
    return {str(LOCATION_NAME_TO_ID[loc.name]): (value(loc.source) if value else getattr(loc.source, key))
            for loc in world.included_locations if getattr(loc.source, key) is not None}


def _pickup(source: Source) -> dict[str, Any]:
    pickup = {"map": source.pickup.map, "flag": -1 if source.flag is None else source.flag}
    if source.pickup.story:
        pickup["event"] = source.event
    if source.berry is not None:
        pickup["berry"] = source.berry
    # A respawning pickup: its regional flag is wiped on area change.
    if source.regional is not None:
        pickup["regional"] = source.regional
    return pickup


def _entities(entries: Iterable[EntityRef | FlagEntity | DialogueFlag]) -> list[dict[str, Any]]:
    return [entry.to_slot() for entry in entries]


def build_slot_data(world: BugFablesWorld) -> Mapping[str, Any]:
    return {
        "world_version": WORLD_VERSION,
        "artifacts_required": world.artifacts_required,
        "location_flags": _by_location(world, "flag"),
        "location_berries": _by_location(world, "berry"),
        "location_discoveries": _by_location(world, "discovery"),
        # One location per copy a shop ever stocks; a shop's copies are its locations in id order.
        "location_shops": _by_location(world, "shop", lambda source: {"shop": source.shop, "medal": source.medal}),
        "location_item_shops": _by_location(world, "item_shop", lambda source: source.item_shop.to_slot()),
        # Done when a number slot reaches a value, not a flag (a boss prize handed over).
        "location_vars": _by_location(world, "var", lambda source: {"var": source.var, "at_least": source.at_least}),
        # Checks that show no item of their own (only a story flag): the client shows the player's own item there.
        "silent_locations": sorted(LOCATION_NAME_TO_ID[loc.name] for loc in world.included_locations
                                   if loc.source.present() <= {"event", "flag", "added"}),
        # The opening's checks: their items arrive with no hold-up, so a new file doesn't start with a string of boxes.
        "quiet_locations": sorted(LOCATION_NAME_TO_ID[loc.name] for loc in world.included_locations if loc.quiet),
        # Items the story puts straight into the bag at a location: the client leaves them out.
        "location_added": _by_location(world, "added", lambda source: source.added.to_slot()),
        "location_gives": _by_location(world, "give", lambda source: source.give.to_slot()),
        "location_pickups": _by_location(world, "pickup", _pickup),
        # Story blockers the client keeps away, so an area the logic counts as reachable never closes.
        "kept_open": _entities(KEPT_OPEN),
        "kept_present": _entities(KEPT_PRESENT),
        "scenery_hidden": _entities(SCENERY_HIDDEN),
        "scenery_present": _entities(SCENERY_PRESENT),
        "held_until": _entities(HELD_UNTIL),
        "present_from": _entities(PRESENT_FROM),
        "dialogue_flags": _entities(DIALOGUE_FLAGS),
        "door_targets": world.door_targets,
        # {"map:entity": [enemy ids]}: the fight a map enemy starts instead of its own.
        "enemy_swaps": world.enemy_swaps,
        # {"map", "from"}: the room a new file begins in, as if entering through the door from "from"; empty for the
        # game's own start.
        "start": world.start,
        # The one member a new file starts with (0 Vi, 1 Kabbu, 2 Leif); 3 all three; -1 is the story's party.
        "starting_member": world.starting_member,
        # Field moves as items: the three attacks, and Jump (the mod then keeps the Warp on).
        "shuffle_moves": world.moves_shuffled(),
        "shuffle_jump": world.jump_shuffled(),
        # Every learned ability is an item: the mod answers the game's ability checks from the items received.
        "ability_items": True,
        # An item's kind, as data_tables names them (ITEM_KIND and the rest).
        "item_kinds": {str(ITEM_NAME_TO_ID[item.name]): item.kind for item in ITEMS},
    }
