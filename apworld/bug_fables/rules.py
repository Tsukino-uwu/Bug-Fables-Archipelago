"""Each spot's rule, the shop policy and the goal."""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from BaseClasses import Item, LocationProgressType
from rule_builder.field_resolvers import FromWorldAttr
from rule_builder.options import OptionFilter
from rule_builder.rules import CanReachRegion, Has, HasFromListUnique, Rule
from worlds.generic.Rules import add_item_rule

from .data_tables import ARTIFACTS, LOCATIONS, vanilla_item
from .data_types import Location, StoryEvent
from .options import ShopContents, ShuffleJump

if TYPE_CHECKING:
    from .world import BugFablesWorld

SHOP_CATEGORIES = ("shop", "item_shop")
# The last fight (chapter 7's artifact scene): the goal, and held by the client until the artifacts are in.
FINAL_FIGHT = "Giant's Lair: Sapling Plains, Final Fight"
# Jump's blanket rule: with Shuffle Jump on, a spot needs Jump unless it was seen reachable without (no_jump).
JUMP = Has("Jump", options=[OptionFilter(ShuffleJump, 1)], filtered_resolution=True)
# The last fight, held by the client until the seed's count of artifact items have arrived (none needed at 0).
ARTIFACT_GATE = HasFromListUnique(*(vanilla_item(loc) for loc in ARTIFACTS),
                                  count=FromWorldAttr("artifacts_required"))


def _no_progression(item: Item) -> bool:
    return not item.advancement


def _no_trap(item: Item) -> bool:
    return not item.trap


def set_all_rules(world: BugFablesWorld) -> None:
    # Filler Starting Checks: the opening's own checks are excluded (filler first, never progression or useful), and
    # excluded still admits traps.
    if world.filler_starting_checks:
        for loc in world.included_locations:
            if loc.quiet:
                location = world.get_location(loc.name)
                location.progress_type = LocationProgressType.EXCLUDED
                add_item_rule(location, _no_trap)
    # The Termacade's gift and prizes only ever hold filler: excluded, as Archipelago keeps them.
    for loc in world.included_locations:
        if loc.filler:
            world.get_location(loc.name).progress_type = LocationProgressType.EXCLUDED
    # Minigame Prizes off (the default): a high score's prize holds filler only.
    if not world.options.minigame_prizes:
        for loc in world.included_locations:
            if loc.category == "minigame":
                world.get_location(loc.name).progress_type = LocationProgressType.EXCLUDED
    for loc in world.included_locations:
        if loc.category not in SHOP_CATEGORIES:
            continue
        location = world.get_location(loc.name)
        if world.options.shop_contents == ShopContents.option_filler_only:
            location.progress_type = LocationProgressType.EXCLUDED
        elif world.options.shop_contents == ShopContents.option_no_progression:
            location.item_rule = _no_progression
    for spot in (*world.included_locations, *world.included_events):
        rule = spot_rule(spot)
        if rule is not None:
            world.set_rule(world.get_location(spot.name), rule)
    # The goal: the last fight won, its artifacts in hand: its region and its rule.
    fight = next(loc for loc in LOCATIONS if loc.name == FINAL_FIGHT)
    world.set_completion_rule(_joined(CanReachRegion(f"{fight.region} ({fight.area})"), spot_rule(fight)))


def spot_rule(spot: Location | StoryEvent) -> Rule | None:
    """A spot's whole rule, the same in every seed (the options and the world resolve it), so the PopTracker pack
    exports this one: its reach, its own rule, JUMP, and the last fight's artifacts."""
    return _joined(spot.reach, spot.rule, None if spot.no_jump else JUMP,
                   ARTIFACT_GATE if spot.name == FINAL_FIGHT else None)


def _joined(*rules: Rule | None) -> Rule | None:
    """The rules given, all needed; None when there are none."""
    present = [rule for rule in rules if rule is not None]
    if not present:
        return None
    joined = present[0]
    for rule in present[1:]:
        joined = joined & rule
    return joined


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
    # The player's own exclusions stay, and the rule joins the item rules already there (local and non-local items).
    for location in shops:
        if location.name not in world.options.exclude_locations.value:
            location.progress_type = LocationProgressType.DEFAULT
        add_item_rule(location, _no_progression)
    world.shops_fell_back = True
