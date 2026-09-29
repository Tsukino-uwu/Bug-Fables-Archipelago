"""The entrance randomizer on the region graph: Archipelago's own (entrance_rando.py) for Coupled, and the room swap,
a mode Archipelago has none of, connecting the same split entrances by hand. Each pairing becomes a door_targets entry,
the doors the mod rewrites.
"""
from __future__ import annotations

from collections.abc import Sequence
from random import Random
from typing import TYPE_CHECKING

from BaseClasses import CollectionState, Entrance, EntranceType, Region
from entrance_rando import disconnect_entrance_for_randomization, randomize_entrances

from .data_tables import DOORS
from .data_types import DoorConnection
from .options import EntranceRandomizer
from .regions import door_name

if TYPE_CHECKING:
    from .world import BugFablesWorld

Door = tuple[str, str]  # (map, door entity name)


def _partners(connections: Sequence[DoorConnection]) -> dict[Door, Door]:
    partner: dict[Door, Door] = {}
    for c in connections:
        a, b = (c.a.map, c.a.door), (c.b.map, c.b.door)
        partner[a], partner[b] = b, a
    return partner


def _areas(maps: set[str], links: Sequence[tuple[str, str]]) -> dict[str, str]:
    """Each map's group: maps joined by the links share one."""
    parent = {m: m for m in maps}

    def find(m: str) -> str:
        while parent[m] != m:
            parent[m] = parent[parent[m]]
            m = parent[m]
        return m

    for a, b in links:
        parent.setdefault(a, a)
        parent.setdefault(b, b)
        parent[find(a)] = find(b)
    return {m: find(m) for m in parent}


def room_pairs(connections: Sequence[DoorConnection], fixed: Sequence[tuple[str, str]],
               random: Random) -> list[tuple[Door, Door]]:
    """Whole areas (maps joined by fixed doors both ways) trade places with areas of as many doors in their part of the
    world; each of the game's door pairs becomes the pair of doors now standing where its two doors stood."""
    partner = _partners(connections)
    doors = sorted(partner)
    maps = {m for m, _ in doors}
    # A one-way fixed door (a drop) joins nothing: an area moved whole could otherwise be entered on its far side.
    both_ways = [(a, b) for a, b in fixed if (b, a) in set(fixed)]
    area = _areas(maps, both_ways)
    # The doors alone split the world into parts that boats and scenes join; a swap across parts would strand rooms.
    part = _areas(maps, [*both_ways, *((a[0], b[0]) for a, b in partner.items())])
    by_area: dict[str, list[Door]] = {}
    for d in doors:
        by_area.setdefault(area[d[0]], []).append(d)
    alike: dict[tuple[str, int], list[str]] = {}
    for a in sorted(by_area):
        alike.setdefault((part[a], len(by_area[a])), []).append(a)

    # placed[d]: the door that now stands where d stood, a door of the area that took d's area's place.
    placed: dict[Door, Door] = {}
    for slots in alike.values():
        rooms = list(slots)
        random.shuffle(rooms)
        for slot, room in zip(slots, rooms):
            moved = list(by_area[room])
            random.shuffle(moved)
            placed.update(zip(by_area[slot], moved))
    return [(placed[a], placed[b]) for a, b in partner.items() if a < b]


def door_targets(pairings: Sequence[tuple[Door, Door]], connections: Sequence[DoorConnection]) -> list[dict[str, str]]:
    """Each pairing (x, y), x leading to y's side, as a door_targets entry: x leads where y's partner leads in the game,
    so you arrive next to y. Unchanged doors are left out."""
    partner = _partners(connections)
    targets = []
    for x, y in pairings:
        like = partner[y]
        if like != x:
            targets.append({"map": x[0], "door": x[1], "like_map": like[0], "like_door": like[1]})
    return targets


def shuffle(world: BugFablesWorld) -> list[tuple[Door, Door]]:
    """Splits every door entrance and connects them anew as the option says; returns the pairings, each door
    (exit) with the door it now leads to."""
    mode = world.options.entrance_randomizer
    if mode == EntranceRandomizer.option_off:
        return []
    names = {(end.map, end.door): door_name(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)}
    for name in names.values():
        entrance = world.get_entrance(name)
        entrance.randomization_type = EntranceType.TWO_WAY
        disconnect_entrance_for_randomization(entrance)
    if mode == EntranceRandomizer.option_room_swap:
        return _swap_rooms(world, names)
    doors = {name: door for door, name in names.items()}
    placed = randomize_entrances(world, coupled=True, target_group_lookup={0: [0]})
    return [(doors[x], doors[y]) for x, y in placed.pairings]


ROOM_SWAP_TRIES = 20


def _swap_rooms(world: BugFablesWorld, names: dict[Door, str]) -> list[tuple[Door, Door]]:
    """A room swap the logic can finish: unlike Archipelago's randomizer, it doesn't follow the logic while placing (a
    gated door moves with its room and can close the only way on), so each try is checked and undone if it fails."""
    for _ in range(ROOM_SWAP_TRIES):
        pairs = room_pairs(DOORS.connections, DOORS.fixed, world.random)
        pairings = [p for x, y in pairs for p in ((x, y), (y, x))]
        made = [_connect(world, names[x], y[0], names[y]) for x, y in pairings]
        if _every_region_reached(world):
            return pairings
        for source, region, target in made:
            region.entrances.remove(source)
            source.connected_region = None
            region.entrances.append(target)
    raise RuntimeError(f"Bug Fables: no room swap in {ROOM_SWAP_TRIES} tries kept every room reachable")


def _every_region_reached(world: BugFablesWorld) -> bool:
    """With everything the seed holds, as Archipelago's randomizer checks it: every region reached."""
    state = CollectionState(world.multiworld)
    for item in world.multiworld.itempool:
        if item.player == world.player:
            world.collect(state, item)
    state.sweep_for_advancements(world.get_locations())
    return all(state.can_reach_region(region.name, world.player) for region in world.get_regions())


def write_spoiler(world: BugFablesWorld) -> None:
    """Each shuffled door in the spoiler's Entrances section, the way it now leads; a pair both ways only once."""
    listed: set[tuple[Door, Door]] = set()
    for x, y in world.door_pairings:
        if (y, x) not in listed:
            listed.add((x, y))
            world.multiworld.spoiler.set_entrance(door_name(*x), door_name(*y), "both", world.player)


def _connect(world: BugFablesWorld, exit_name: str, target_map: str,
             target_name: str) -> tuple[Entrance, Region, Entrance]:
    """Connects a split door to the target named after another door, as Archipelago's randomizer does; returns what
    it took to undo it."""
    source = world.get_entrance(exit_name)
    region = world.get_region(target_map)
    target = next(e for e in region.entrances if e.name == target_name and e.parent_region is None)
    region.entrances.remove(target)
    source.connect(region)
    return source, region, target
