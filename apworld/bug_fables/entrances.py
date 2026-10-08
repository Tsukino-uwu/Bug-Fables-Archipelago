"""The entrance randomizer on the region graph: Archipelago's own (entrance_rando.py) for Coupled and Decoupled, and
the room swap, a mode Archipelago has none of, connecting the same split entrances by hand. Each pairing becomes a
door_targets entry, the doors the mod rewrites. One-way doors (a fog maze's wrong turns, drops) are Archipelago's
one-way entrances, paired only with one another; the room swap leaves them as they are. replay reads a seed's
door_targets back into the same graph with no roll, for Universal Tracker.
"""
from __future__ import annotations

import logging
from collections.abc import Mapping, Sequence
from random import Random
from typing import TYPE_CHECKING

from BaseClasses import CollectionState, Entrance, EntranceType, Region
from entrance_rando import disconnect_entrance_for_randomization, randomize_entrances

from .data_tables import DOORS, ONE_WAYS, door_name, door_region, landing_region, one_way_landing
from .data_types import DoorConnection, OneWayDoor
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


class _RoomLayout:
    """Whole areas (maps joined by fixed doors both ways) trading places with areas of as many doors in their part of
    the world: room_at[slot] is the area standing in slot's place, order[area] its doors in the order they take the
    slot's doors."""

    def __init__(self, connections: Sequence[DoorConnection], fixed: Sequence[tuple[str, str]], random: Random):
        self.partner = _partners(connections)
        doors = sorted(self.partner)
        maps = {m for m, _ in doors}
        # A one-way fixed door (a drop) joins nothing: an area moved whole could otherwise be entered on its far side.
        both_ways = [(a, b) for a, b in fixed if (b, a) in set(fixed)]
        area = _areas(maps, both_ways)
        # The doors alone split the world into parts that boats and scenes join; a swap across parts would strand rooms.
        part = _areas(maps, [*both_ways, *((a[0], b[0]) for a, b in self.partner.items())])
        self.by_area: dict[str, list[Door]] = {}
        for d in doors:
            self.by_area.setdefault(area[d[0]], []).append(d)
        alike: dict[tuple[str, int], list[str]] = {}
        for a in sorted(self.by_area):
            alike.setdefault((part[a], len(self.by_area[a])), []).append(a)
        self.groups = list(alike.values())
        self.room_at: dict[str, str] = {}
        self.order: dict[str, list[Door]] = {}
        for slots in self.groups:
            rooms = list(slots)
            random.shuffle(rooms)
            for slot, room in zip(slots, rooms):
                self.room_at[slot] = room
                self.order[room] = list(self.by_area[room])
                random.shuffle(self.order[room])

    def pairs(self) -> list[tuple[Door, Door]]:
        """Each of the game's door pairs as the pair of doors now standing where its two doors stood."""
        # placed[d]: the door that now stands where d stood, a door of the area that took d's area's place.
        placed: dict[Door, Door] = {}
        for slot, room in self.room_at.items():
            placed.update(zip(self.by_area[slot], self.order[room]))
        return [(placed[a], placed[b]) for a, b in self.partner.items() if a < b]


def room_pairs(connections: Sequence[DoorConnection], fixed: Sequence[tuple[str, str]],
               random: Random) -> list[tuple[Door, Door]]:
    """A random room swap: whole areas trade places with areas of as many doors in their part of the world; each of
    the game's door pairs becomes the pair of doors now standing where its two doors stood."""
    return _RoomLayout(connections, fixed, random).pairs()


def door_targets(pairings: Sequence[tuple[Door, Door]], connections: Sequence[DoorConnection],
                 one_ways: Sequence[OneWayDoor] = ()) -> list[dict[str, str]]:
    """Each pairing (x, y) as a door_targets entry. A two-way x leads to y's side: where y's partner leads in the game,
    so you arrive next to y. A one-way x lands where y lands. Unchanged doors are left out, but a one-way's story copies
    always follow their door, so its spot leads one place whatever the story."""
    partner = _partners(connections)
    one_way = {(w.map, w.door) for w in one_ways}
    like_of: dict[Door, Door] = {}
    targets = []
    for x, y in pairings:
        like = y if x in one_way else partner[y]
        like_of[x] = like
        if like != x:
            targets.append({"map": x[0], "door": x[1], "like_map": like[0], "like_door": like[1]})
    for w in one_ways:
        like = like_of.get((w.map, w.door), (w.map, w.door))
        for copy in w.copies:
            targets.append({"map": w.map, "door": copy, "like_map": like[0], "like_door": like[1]})
    return targets


def shuffle(world: BugFablesWorld) -> list[tuple[Door, Door]]:
    """Splits every door entrance and connects them anew as the option says; returns the pairings, each door
    (exit) with the door it now leads to, or for a one-way, the one-way whose landing it now takes."""
    mode = world.options.entrance_randomizer
    if world.options.plando_connections and mode in (EntranceRandomizer.option_off,
                                                     EntranceRandomizer.option_room_swap):
        logging.warning("Bug Fables: player %s (%s) has plando connections, which apply only with the Entrance "
                        "Randomizer on Coupled or Decoupled; they're ignored.", world.player, world.player_name)
    if mode == EntranceRandomizer.option_off:
        return []
    names = _split(world)
    if mode == EntranceRandomizer.option_room_swap:
        return _swap_rooms(world, names)
    doors = {name: door for door, name in names.items()}
    doors |= {name: (w.map, w.door) for w in ONE_WAYS for name in (door_name(w.map, w.door), one_way_landing(w))}
    coupled = mode != EntranceRandomizer.option_decoupled
    planned = _plando(world, names, coupled)
    placed = randomize_entrances(world, coupled=coupled, target_group_lookup={0: [0]})
    return planned + [(doors[x], doors[y]) for x, y in placed.pairings]


def _split(world: BugFablesWorld) -> dict[Door, str]:
    """Splits the door entrances the mode shuffles, as Archipelago's randomizer needs them: every two-way door, and the
    one-ways except with Room Swap, which leaves them as they are. Returns each two-way door's entrance name."""
    names = {(end.map, end.door): door_name(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)}
    for name in names.values():
        entrance = world.get_entrance(name)
        entrance.randomization_type = EntranceType.TWO_WAY
        disconnect_entrance_for_randomization(entrance)
    if world.options.entrance_randomizer != EntranceRandomizer.option_room_swap:
        for w in ONE_WAYS:
            entrance = world.get_entrance(door_name(w.map, w.door))
            entrance.randomization_type = EntranceType.ONE_WAY
            disconnect_entrance_for_randomization(entrance, one_way_target_name=one_way_landing(w))
    return names


def pairings_from_targets(targets: Sequence[Mapping[str, str]], connections: Sequence[DoorConnection],
                          one_ways: Sequence[OneWayDoor] = ()) -> list[tuple[Door, Door]]:
    """door_targets read back into the pairings it was written from: each door with the door it leads to, a one-way
    with the one-way whose landing it takes. A door with no entry is as the game has it, and a one-way's story copies
    are never read. one_ways: those the mode shuffles (none with Room Swap)."""
    partner = _partners(connections)
    one_way = {(w.map, w.door) for w in one_ways}
    copies = {(w.map, copy) for w in ONE_WAYS for copy in w.copies}
    like_of = {(t["map"], t["door"]): (t["like_map"], t["like_door"]) for t in targets}
    unknown = set(like_of) - set(partner) - one_way - copies
    if unknown:
        raise ValueError(f"Bug Fables: door_targets names doors this world doesn't shuffle: {sorted(unknown)}")
    pairings: list[tuple[Door, Door]] = []
    taken: set[Door] = set()
    for x in (*sorted(partner), *sorted(one_way)):
        like = like_of.get(x, x)
        if (x in one_way) != (like in one_way) or (like not in one_way and like not in partner):
            raise ValueError(f"Bug Fables: door_targets sends {x} like {like}, a door of another kind")
        y = like if x in one_way else partner[like]
        if y in taken:
            raise ValueError(f"Bug Fables: door_targets sends two doors to {y}")
        taken.add(y)
        pairings.append((x, y))
    return pairings


def replay(world: BugFablesWorld, targets: Sequence[Mapping[str, str]]) -> list[tuple[Door, Door]]:
    """The seed's doors as its door_targets says, connected with no roll (Universal Tracker rebuilding a seed from its
    slot_data): split as shuffle() splits them, each pairing connected as the randomizer connected it. Raises if a door
    is left unconnected. Returns the pairings."""
    mode = world.options.entrance_randomizer
    if mode == EntranceRandomizer.option_off:
        if targets:
            raise ValueError("Bug Fables: door_targets rewrites doors, but the Entrance Randomizer is off")
        return []
    names = _split(world)
    one_ways = ONE_WAYS if mode != EntranceRandomizer.option_room_swap else ()
    pairings = pairings_from_targets(targets, DOORS.connections, one_ways)
    landing = {(w.map, w.door): w for w in one_ways}
    for x, y in pairings:
        if x in landing:
            _connect(world, door_name(*x), landing_region(landing[y]), one_way_landing(landing[y]))
        else:
            _connect(world, names[x], door_region(*y), names[y])
    left = [name for name in (*names.values(), *(door_name(*door) for door in landing))
            if world.get_entrance(name).connected_region is None]
    if left:
        raise ValueError(f"Bug Fables: door_targets leaves doors unconnected: {left}")
    return pairings


def _plando(world: BugFablesWorld, names: dict[Door, str], coupled: bool) -> list[tuple[Door, Door]]:
    """The player's plando connections, connected before the randomizer places the rest; Coupled joins each both
    ways. A one-way door takes the named one-way's landing, one way only (options.DoorPlando refuses mixing the
    two)."""
    by_name = {name.lower(): door for door, name in names.items()}
    one_ways = {door_name(w.map, w.door).lower(): w for w in ONE_WAYS}
    landings = {one_way_landing(w).lower(): w for w in ONE_WAYS}
    pairings: list[tuple[Door, Door]] = []
    for connection in world.options.plando_connections:
        if connection.entrance.lower() in one_ways:
            x, y = one_ways[connection.entrance.lower()], landings[connection.exit.lower()]
            if (world.get_entrance(door_name(x.map, x.door)).connected_region is not None
                    or _free_target(world, landing_region(y), one_way_landing(y)) is None):
                raise ValueError(f"Bug Fables: player {world.player_name}'s plando connection {connection.entrance} "
                                 f"to {connection.exit} uses a door another one already uses")
            _connect(world, door_name(x.map, x.door), landing_region(y), one_way_landing(y))
            pairings.append(((x.map, x.door), (y.map, y.door)))
            continue
        entrance, exit_ = by_name[connection.entrance.lower()], by_name[connection.exit.lower()]
        ways = []
        if coupled or connection.direction in ("entrance", "both"):
            ways.append((entrance, exit_))
        if coupled or connection.direction in ("exit", "both"):
            ways.append((exit_, entrance))
        for x, y in ways:
            if (world.get_entrance(names[x]).connected_region is not None
                    or _free_target(world, door_region(*y), names[y]) is None):
                raise ValueError(f"Bug Fables: player {world.player_name}'s plando connection {connection.entrance} "
                                 f"to {connection.exit} uses a door another one already uses")
            _connect(world, names[x], door_region(*y), names[y])
            pairings.append((x, y))
    return pairings


# Each try about 1.5 ms. With the rooms' one-way drops and gated parts mapped, about 4 random layouts in 1000 keep every
# room reachable; the repair below gets there in about 190 tries (2026-10-08, 180 seeds: 95% within 650, the worst
# 1870).
ROOM_SWAP_TRIES = 10000
# How often a move takes a room standing where something is cut off, rather than any room.
_HOT_MOVES = 0.8
# How often a move trades two rooms, rather than turning one room's doors.
_TRADES = 0.7
# The spots open from the start the swap must leave (as many as the game's own layout has, up to this many): fewer
# and the fill can't place the early items (2026-10-08: swaps left 3 to 7, the fuzzer's FillErrors; 15 filled).
_START_SPOTS = 15


def _swap_rooms(world: BugFablesWorld, names: dict[Door, str]) -> list[tuple[Door, Door]]:
    """A room swap the logic can finish: unlike Archipelago's randomizer, it doesn't follow the logic while placing (a
    gated door moves with its room and can close the only way on), so a random layout is repaired: two like rooms trade
    places, or a room's doors turn, most often around what is cut off, each move kept unless it cuts off more or opens
    fewer spots from the start than wanted (the fill needs them)."""
    random = world.random
    layout = _RoomLayout(DOORS.connections, DOORS.fixed, random)
    group_of = {slot: slots for slots in layout.groups for slot in slots}
    movable = [g for g in layout.groups if len(g) > 1 or len(layout.by_area[g[0]]) > 1]
    as_the_game = [(a, b) for a, b in layout.partner.items() if a < b]
    wanted = min(_START_SPOTS, _cut_off(world, names, as_the_game)[1])
    cut, start, pairings = _cut_off(world, names, layout.pairs())
    for _ in range(ROOM_SWAP_TRIES):
        if not cut and start >= wanted:
            _connect_all(world, names, pairings)
            return pairings
        cut_maps = {region.split(" (")[0] for region in cut}
        hot = [s for s, room in layout.room_at.items()
               if any(m in cut_maps for m, _ in layout.by_area[room]) and s in group_of]
        slots = group_of[random.choice(hot)] if hot and random.random() < _HOT_MOVES else random.choice(movable)
        if len(slots) == 1 and len(layout.by_area[slots[0]]) == 1:
            slots = random.choice(movable)
        if len(slots) > 1 and random.random() < _TRADES:
            a, b = random.sample(slots, 2)
            layout.room_at[a], layout.room_at[b] = layout.room_at[b], layout.room_at[a]
            undo = (a, b, None)
        else:
            room = layout.room_at[random.choice(slots)]
            undo = (None, room, list(layout.order[room]))
            random.shuffle(layout.order[room])
        now, now_start, now_pairings = _cut_off(world, names, layout.pairs())
        if (len(now), max(0, wanted - now_start)) <= (len(cut), max(0, wanted - start)):
            cut, start, pairings = now, now_start, now_pairings
        elif undo[0] is not None:
            layout.room_at[undo[0]], layout.room_at[undo[1]] = layout.room_at[undo[1]], layout.room_at[undo[0]]
        else:
            layout.order[undo[1]] = undo[2]
    raise RuntimeError(f"Bug Fables: no room swap in {ROOM_SWAP_TRIES} tries kept every room reachable")


def _cut_off(world: BugFablesWorld, names: dict[Door, str],
             pairs: list[tuple[Door, Door]]) -> tuple[set[str], int, list[tuple[Door, Door]]]:
    """For a layout: the regions it cuts off with everything the seed holds, as Archipelago's randomizer checks it; how
    many spots it opens from the start, with only the start's items; and its pairings both ways. The doors are left as
    they were."""
    pairings = [p for x, y in pairs for p in ((x, y), (y, x))]
    made = _connect_all(world, names, pairings)
    start = CollectionState(world.multiworld)
    for item in world.multiworld.precollected_items[world.player]:
        start.collect(item, True)
    start.sweep_for_advancements(world.get_locations())
    spots = sum(1 for location in world.get_locations()
                if location.address is not None and location.can_reach(start))
    state = CollectionState(world.multiworld)
    for item in world.multiworld.itempool:
        if item.player == world.player:
            world.collect(state, item)
    state.sweep_for_advancements(world.get_locations())
    cut = {region.name for region in world.get_regions() if not state.can_reach_region(region.name, world.player)}
    for source, region, target in made:
        region.entrances.remove(source)
        source.connected_region = None
        region.entrances.append(target)
    return cut, spots, pairings


def _connect_all(world: BugFablesWorld, names: dict[Door, str],
                 pairings: list[tuple[Door, Door]]) -> list[tuple[Entrance, Region, Entrance]]:
    return [_connect(world, names[x], door_region(*y), names[y]) for x, y in pairings]


def write_spoiler(world: BugFablesWorld) -> None:
    """Each shuffled door in the spoiler's Entrances section, the way it now leads; a coupled pair only once, both
    ways; a one-way to the landing it takes."""
    coupled = world.options.entrance_randomizer != EntranceRandomizer.option_decoupled
    landing = {(w.map, w.door): one_way_landing(w) for w in ONE_WAYS}
    listed: set[tuple[Door, Door]] = set()
    for x, y in world.door_pairings:
        if x in landing:
            world.multiworld.spoiler.set_entrance(door_name(*x), landing[y], "entrance", world.player)
        elif not coupled:
            world.multiworld.spoiler.set_entrance(door_name(*x), door_name(*y), "entrance", world.player)
        elif (y, x) not in listed:
            listed.add((x, y))
            world.multiworld.spoiler.set_entrance(door_name(*x), door_name(*y), "both", world.player)


def _connect(world: BugFablesWorld, exit_name: str, target_region: str,
             target_name: str) -> tuple[Entrance, Region, Entrance]:
    """Connects a split door to the target named after another door, in that door's region, as Archipelago's
    randomizer does; returns what it took to undo it."""
    source = world.get_entrance(exit_name)
    region = world.get_region(target_region)
    target = _free_target(world, target_region, target_name)
    region.entrances.remove(target)
    source.connect(region)
    return source, region, target


def _free_target(world: BugFablesWorld, region: str, name: str) -> Entrance | None:
    """The split door's target in its region, while nothing is connected to it yet."""
    return next((e for e in world.get_region(region).entrances if e.name == name and e.parent_region is None), None)
