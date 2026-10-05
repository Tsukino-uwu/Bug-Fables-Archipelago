"""The regions: Menu, the origin, then one per map and one per part of a map a roadblock cuts off (MAP_AREAS). Every door
is an entrance of the region it stands in, named after where it is, a one-way door too; fixed doors, the transfers that
aren't doors (logic/) and each map area's way across join regions too."""
from __future__ import annotations

from typing import TYPE_CHECKING, NamedTuple

from BaseClasses import Region

from .custom_rules import one_way
from .data_tables import (DOOR_RULES, DOORS, MAP_AREAS, MAPS, ONE_WAYS, REGIONS, TRANSFERS, door_name,
                          door_region)

if TYPE_CHECKING:
    from rule_builder.rules import Rule

    from .world import BugFablesWorld

# The origin region (World.origin_region_name).
MENU = "Menu"
# A new game begins outside the city.
START_MAP = "BugariaOutskirtsOutsideCity"


def create_and_connect_regions(world: BugFablesWorld) -> None:
    menu = Region(MENU, world.player, world.multiworld)
    regions = {name: Region(name, world.player, world.multiworld) for name in REGIONS}
    world.multiworld.regions += [menu, *regions.values()]
    regions[MENU] = menu
    for entrance in logic_entrances():
        world.create_entrance(regions[entrance.from_map], regions[entrance.to_map], entrance.rule, name=entrance.name)


class LogicEntrance(NamedTuple):
    name: str
    from_map: str  # a region: a map, or a map area
    to_map: str  # as the game has it; the entrance randomizer may send a door elsewhere
    rule: Rule | None


def logic_entrances() -> list[LogicEntrance]:
    """Every entrance the logic makes, the same in every seed (the options resolve its rules), so the PopTracker pack
    exports these: Menu to the start, the doors, the one-way doors, the fixed doors and the transfers."""
    entrances = [LogicEntrance(f"{MENU} -> {START_MAP}", MENU, START_MAP, None)]
    gates = {(gate.map, gate.door): gate.rule for gate in DOOR_RULES}
    for connection in DOORS.connections:
        for end, other in ((connection.a, connection.b), (connection.b, connection.a)):
            entrances.append(LogicEntrance(door_name(end.map, end.door), door_region(end.map, end.door),
                                           door_region(other.map, other.door), gates.get((end.map, end.door))))
    for door in ONE_WAYS:
        entrances.append(LogicEntrance(door_name(door.map, door.door), door_region(door.map, door.door), door.to,
                                       gates.get((door.map, door.door))))
    for a, b in dict.fromkeys(DOORS.fixed):
        if a != b and a in MAPS and b in MAPS:
            entrances.append(LogicEntrance(f"{a} to {b}", a, b, None))
    for transfer in TRANSFERS:
        start = transfer.from_map if transfer.from_area is None else f"{transfer.from_map} ({transfer.from_area})"
        ways = ((start, transfer.to_map), (transfer.to_map, start))
        rule = transfer.rule if transfer.way_back is None else one_way(transfer.rule, transfer.way_back)
        for a, b in ways if transfer.two_way else ways[:1]:
            entrances.append(LogicEntrance(f"{a} to {b} ({transfer.name})", a, b, rule))
    for area in MAP_AREAS:
        joined = area.to or area.map
        entrances.append(LogicEntrance(f"{joined} to {area.region}", joined, area.region, area.rule))
        out = area.rule if area.out is None else area.out
        entrances.append(LogicEntrance(f"{area.region} to {joined}", area.region, joined, out))
    return entrances
