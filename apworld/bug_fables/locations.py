"""The location class, which locations a seed includes, and placing them in their regions."""
from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location

from .data_tables import ARTIFACTS, LOCATION_NAME_TO_ID
from .items import GAME, BugFablesItem
from .options import CATEGORY_OPTIONS

if TYPE_CHECKING:
    from .world import BugFablesWorld


class BugFablesLocation(Location):
    game = GAME


def category_on(world: BugFablesWorld, category: str | None) -> bool:
    if category in CATEGORY_OPTIONS:
        return bool(getattr(world.options, CATEGORY_OPTIONS[category]).value)
    if category == "party_member":
        return world.starting_member >= 0
    if category == "story_party":
        return world.starting_member < 0
    return True


def create_all_locations(world: BugFablesWorld) -> None:
    for loc in world.included_locations:
        region = world.get_region(loc.region if loc.area is None else f"{loc.region} ({loc.area})")
        region.locations.append(BugFablesLocation(world.player, loc.name, LOCATION_NAME_TO_ID[loc.name], region))

    for event in world.included_events:
        world.get_region(event.region).add_event(
            event.name, event.item, location_type=BugFablesLocation, item_type=BugFablesItem
        )

    # The game counts artifacts from flags, so these events hold no real item: they let fill prove the goal.
    for artifact in ARTIFACTS:
        world.get_region(artifact.region).add_event(
            artifact.name, "Artifact", location_type=BugFablesLocation, item_type=BugFablesItem
        )
