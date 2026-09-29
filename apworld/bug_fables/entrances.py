"""The entrance randomizer on the region graph: Archipelago's own (entrance_rando.py) for Coupled and Decoupled, and
the room swap, a mode Archipelago has none of, connecting the same split entrances by hand. Each pairing becomes a
door_targets entry, the doors the mod rewrites.
"""
from __future__ import annotations

import logging
from collections.abc import Sequence
from random import Random
from typing import TYPE_CHECKING

from BaseClasses import CollectionState, Entrance, EntranceType, Region
from entrance_rando import disconnect_entrance_for_randomization, randomize_entrances

from .data_tables import DOORS, door_name
from .data_types import DoorConnection
from .options import EntranceRandomizer

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
    if world.options.plando_connections and mode in (EntranceRandomizer.option_off,
                                                     EntranceRandomizer.option_room_swap):
        logging.warning("Bug Fables: player %s (%s) has plando connections, which apply only with the Entrance "
                        "Randomizer on Coupled or Decoupled; they're ignored.", world.player, world.player_name)
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
    coupled = mode != EntranceRandomizer.option_decoupled
    planned = _plando(world, names, coupled)
    placed = randomize_entrances(world, coupled=coupled, target_group_lookup={0: [0]})
    return planned + [(doors[x], doors[y]) for x, y in placed.pairings]


def _plando(world: BugFablesWorld, names: dict[Door, str], coupled: bool) -> list[tuple[Door, Door]]:
    """The player's plando connections, connected before the randomizer places the rest (as The Messenger does);
    Coupled joins each both ways."""
    by_name = {name.lower(): door for door, name in names.items()}
    pairings: list[tuple[Door, Door]] = []
    for connection in world.options.plando_connections:
        entrance, exit_ = by_name[connection.entrance.lower()], by_name[connection.exit.lower()]
        ways = []
        if coupled or connection.direction in ("entrance", "both"):
            ways.append((entrance, exit_))
        if coupled or connection.direction in ("exit", "both"):
            ways.append((exit_, entrance))
        for x, y in ways:
            if world.get_entrance(names[x]).connected_region is not None or _free_target(world, y[0], names[y]) is None:
                raise ValueError(f"Bug Fables: player {world.player_name}'s plando connection {connection.entrance} "
                                 f"to {connection.exit} uses a door another one already uses")
            _connect(world, names[x], y[0], names[y])
            pairings.append((x, y))
    return pairings


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
    """Each shuffled door in the spoiler's Entrances section, the way it now leads; a coupled pair only once, both
    ways."""
    coupled = world.options.entrance_randomizer != EntranceRandomizer.option_decoupled
    listed: set[tuple[Door, Door]] = set()
    for x, y in world.door_pairings:
        if not coupled:
            world.multiworld.spoiler.set_entrance(door_name(*x), door_name(*y), "entrance", world.player)
        elif (y, x) not in listed:
            listed.add((x, y))
            world.multiworld.spoiler.set_entrance(door_name(*x), door_name(*y), "both", world.player)


def _connect(world: BugFablesWorld, exit_name: str, target_map: str,
             target_name: str) -> tuple[Entrance, Region, Entrance]:
    """Connects a split door to the target named after another door, as Archipelago's randomizer does; returns what
    it took to undo it."""
    source = world.get_entrance(exit_name)
    region = world.get_region(target_map)
    target = _free_target(world, target_map, target_name)
    region.entrances.remove(target)
    source.connect(region)
    return source, region, target


def _free_target(world: BugFablesWorld, map_name: str, name: str) -> Entrance | None:
    """The split door's target in its map, while nothing is connected to it yet."""
    return next((e for e in world.get_region(map_name).entrances if e.name == name and e.parent_region is None), None)
