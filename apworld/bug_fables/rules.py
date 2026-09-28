"""What each spot needs, the shop policy and the goal."""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from BaseClasses import Item, LocationProgressType
from rule_builder.rules import Has, HasAllCounts

from .abilities import ABILITIES, item_count
from .data_tables import ARTIFACTS
from .data_types import Needs
from .options import ShopContents

if TYPE_CHECKING:
    from .world import BugFablesWorld

SHOP_CATEGORIES = ("shop", "item_shop")

# The one member the story's party lacks at first.
LATE_MEMBER = "Leif"


def requires(world: BugFablesWorld, data: Needs, location: bool = False) -> dict[str, int]:
    """What a spot or exit needs, as item counts: its own requires, members when members are items, and each ability's
    member (when members are items; with the story's party, only Leif, who joins late) and its item's copies. With Jump
    shuffled, a location or story event needs Jump unless it was seen reachable without (no_jump)."""
    needed: dict[str, int] = {}
    members_are_items = world.starting_member >= 0

    def add(name: str, count: int = 1) -> None:
        needed[name] = max(needed.get(name, 0), count)

    for name in data.requires:
        add(name)
    if members_are_items:
        for member in data.members:
            add(member)
    for ability in data.abilities:
        holder = ABILITIES[ability].holder
        if holder is not None and (members_are_items or holder == LATE_MEMBER):
            add(holder)
        if item_count(world, ability):
            add(ABILITIES[ability].item, item_count(world, ability))
    # A blanket rule for unmeasured ground: the abilities' items alone, not who does them.
    for ability in data.moves:
        if item_count(world, ability):
            add(ABILITIES[ability].item, item_count(world, ability))
    if location and world.jump_shuffled() and not data.no_jump:
        add("Jump")
    return needed


def _no_progression(item: Item) -> bool:
    return not item.advancement


def set_all_rules(world: BugFablesWorld) -> None:
    for loc in world.included_locations:
        if loc.category not in SHOP_CATEGORIES:
            continue
        location = world.get_location(loc.name)
        if world.options.shop_contents == ShopContents.option_filler_only:
            location.progress_type = LocationProgressType.EXCLUDED
        elif world.options.shop_contents == ShopContents.option_no_progression:
            location.item_rule = _no_progression
    for data in (*world.included_locations, *world.included_events):
        needed = requires(world, data, location=True)
        if needed:
            world.set_rule(world.get_location(data.name), HasAllCounts(needed))
    # Artifacts are events with no data of their own: with Jump shuffled they wait for it like every other spot.
    if world.jump_shuffled():
        for artifact in ARTIFACTS:
            world.set_rule(world.get_location(artifact.name), Has("Jump"))
    world.set_completion_rule(Has("Artifact", count=world.artifacts_required))


def fall_back_from_filler_only(world: BugFablesWorld) -> None:
    """A room with fewer excludable items than excluded spots fails to generate, so Filler Only falls back."""
    if world.options.shop_contents != ShopContents.option_filler_only:
        return
    shops = [world.get_location(loc.name) for loc in world.included_locations if loc.category in SHOP_CATEGORIES]
    excludable = sum(1 for item in world.multiworld.itempool if item.excludable)
    excluded = sum(1 for location in world.multiworld.get_unfilled_locations()
                   if location.progress_type == LocationProgressType.EXCLUDED)
    if excludable >= excluded:
        return
    logging.warning(
        "Bug Fables: player %s (%s) asked for Shop Contents: Filler Only, but the room has %d filler items for %d "
        "excluded locations; this seed's shops use No Progression instead.",
        world.player, world.player_name, excludable, excluded,
    )
    for location in shops:
        location.progress_type = LocationProgressType.DEFAULT
        location.item_rule = _no_progression
