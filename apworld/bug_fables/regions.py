"""The regions and the exits between them."""
from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region
from rule_builder.rules import HasAllCounts

from .data_tables import REGIONS
from .rules import requires

if TYPE_CHECKING:
    from .world import BugFablesWorld


def create_and_connect_regions(world: BugFablesWorld) -> None:
    regions = {data.name: Region(data.name, world.player, world.multiworld) for data in REGIONS}
    world.multiworld.regions += regions.values()

    for data in REGIONS:
        for exit_data in data.exits:
            needed = requires(world, exit_data)
            world.create_entrance(regions[data.name], regions[exit_data.to], HasAllCounts(needed) if needed else None)
