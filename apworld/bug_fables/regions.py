"""The regions and the exits between them: Menu, the origin, and every area's regions (logic/)."""
from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from .data_tables import REGIONS

if TYPE_CHECKING:
    from .world import BugFablesWorld

# A new game begins outside the city.
START_REGION = "Bugaria Outskirts"


def create_and_connect_regions(world: BugFablesWorld) -> None:
    menu = Region(world.origin_region_name, world.player, world.multiworld)
    regions = {data.name: Region(data.name, world.player, world.multiworld) for data in REGIONS}
    world.multiworld.regions += [menu, *regions.values()]

    world.create_entrance(menu, regions[START_REGION])
    for data in REGIONS:
        for exit_data in data.exits:
            world.create_entrance(regions[data.name], regions[exit_data.to], exit_data.rule)
