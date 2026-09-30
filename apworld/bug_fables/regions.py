"""The regions: Menu, the origin, then one per map. Every door is an entrance of its map's region, named after where it
is; fixed doors and the transfers that aren't doors (logic/) join maps too."""
from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from .custom_rules import one_way
from .data_tables import DOOR_RULES, DOORS, MAPS, TRANSFERS, door_name

if TYPE_CHECKING:
    from .world import BugFablesWorld

# A new game begins outside the city.
START_MAP = "BugariaOutskirtsOutsideCity"


def create_and_connect_regions(world: BugFablesWorld) -> None:
    menu = Region(world.origin_region_name, world.player, world.multiworld)
    regions = {name: Region(name, world.player, world.multiworld) for name in MAPS}
    world.multiworld.regions += [menu, *regions.values()]
    world.create_entrance(menu, regions[START_MAP])

    gates = {(gate.map, gate.door): gate.rule for gate in DOOR_RULES}
    for connection in DOORS.connections:
        for end, other in ((connection.a, connection.b), (connection.b, connection.a)):
            world.create_entrance(regions[end.map], regions[other.map], gates.get((end.map, end.door)),
                                  name=door_name(end.map, end.door))
    for a, b in dict.fromkeys(DOORS.fixed):
        if a != b and a in regions and b in regions:
            world.create_entrance(regions[a], regions[b], name=f"{a} to {b}")
    for transfer in TRANSFERS:
        ways = ((transfer.from_map, transfer.to_map), (transfer.to_map, transfer.from_map))
        rule = transfer.rule if transfer.way_back is None else one_way(transfer.rule, transfer.way_back)
        for a, b in ways if transfer.two_way else ways[:1]:
            world.create_entrance(regions[a], regions[b], rule, name=f"{a} to {b} ({transfer.name})")
