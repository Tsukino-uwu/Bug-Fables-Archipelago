"""The entrance randomizer on the region graph: Archipelago's own (entrance_rando.py) for Coupled and Decoupled, drawn
again when it runs out of spare doors or leaves the start too small, and the room swap, a mode Archipelago has none of,
connecting the same split entrances by hand. Each pairing becomes a door_targets entry, the doors the mod rewrites.
One-way doors (a fog maze's wrong turns, drops) are Archipelago's one-way entrances, paired only with one another; the
room swap leaves them as they are. replay reads a seed's door_targets back into the same graph with no roll, for
Universal Tracker.
"""
from __future__ import annotations

import logging
from collections.abc import Mapping, Sequence
from random import Random
from typing import TYPE_CHECKING

from BaseClasses import CollectionState, Entrance, EntranceType, Item, LocationProgressType, Region
from entrance_rando import EntranceRandomizationError, disconnect_entrance_for_randomization, randomize_entrances

from .data_tables import (
    DOORS,
    KEPT_DOORS,
    ROOM_STARTS,
    SHUFFLED_CONNECTIONS,
    SHUFFLED_ONE_WAYS,
    door_name,
    door_region,
    landing_region,
    one_way_landing,
)
from .data_types import DoorConnection, OneWayDoor
from .options import EntranceRandomizer
from .regions import START_ENTRANCE, START_MAP

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
            for slot, room in zip(slots, rooms, strict=True):
                self.room_at[slot] = room
                self.order[room] = list(self.by_area[room])
                random.shuffle(self.order[room])

    def pairs(self) -> list[tuple[Door, Door]]:
        """Each of the game's door pairs as the pair of doors now standing where its two doors stood."""
        # placed[d]: the door that now stands where d stood, a door of the area that took d's area's place.
        placed: dict[Door, Door] = {}
        for slot, room in self.room_at.items():
            placed.update(zip(self.by_area[slot], self.order[room], strict=True))
        return [(placed[a], placed[b]) for a, b in self.partner.items() if a < b]


def room_pairs(connections: Sequence[DoorConnection], fixed: Sequence[tuple[str, str]],
               random: Random) -> list[tuple[Door, Door]]:
    """A random room swap: whole areas trade places with areas of as many doors in their part of the world; each of
    the game's door pairs becomes the pair of doors now standing where its two doors stood."""
    return _RoomLayout(connections, fixed, random).pairs()


# Room Swap moves rooms joined by a kept door together, as it does rooms joined by fixed doors.
_KEPT_FIXED = (*DOORS.fixed, *((end.map, other.map) for c in DOORS.connections
                               for end, other in ((c.a, c.b), (c.b, c.a))
                               if (c.a.map, c.a.door) in KEPT_DOORS or (c.b.map, c.b.door) in KEPT_DOORS))


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
        targets.extend({"map": w.map, "door": copy, "like_map": like[0], "like_door": like[1]} for copy in w.copies)
    return targets


def shuffle(world: BugFablesWorld, wanted: int) -> list[tuple[Door, Door]]:
    """Splits every door entrance and connects them anew as the option says, leaving the start at least wanted spots
    (start_wanted) where Archipelago's randomizer places them; returns the pairings, each door (exit) with the door it
    now leads to, or for a one-way, the one-way whose landing it now takes."""
    mode = world.options.entrance_randomizer
    if world.options.plando_connections and mode in (EntranceRandomizer.option_off,
                                                     EntranceRandomizer.option_room_swap):
        logging.warning("Bug Fables: player %s (%s) has plando connections, which apply only with the Entrance "
                        "Randomizer on Coupled or Decoupled; they're ignored.", world.player, world.player_name)
    if mode == EntranceRandomizer.option_off:
        return []
    if mode == EntranceRandomizer.option_room_swap:
        return _swap_rooms(world, _split(world), wanted)
    measure = _Measure(world)
    names = _split(world)
    doors = {name: door for door, name in names.items()}
    doors |= {name: (w.map, w.door) for w in SHUFFLED_ONE_WAYS
              for name in (door_name(w.map, w.door), one_way_landing(w))}
    coupled = mode != EntranceRandomizer.option_decoupled
    planned = _plando(world, names, coupled)
    pins, placed = _randomize(world, coupled, measure, wanted)
    return planned + pins + [(doors[x], doors[y]) for x, y in placed]


# Archipelago's randomizer gives up when the dead ends outnumber the spare doors it has reached, with no retry of its
# own (entrance_rando.py): each try is a new draw from the world's random.
DOOR_TRIES = 10
# Draws refused, over all tries, because the start opens fewer spots than wanted (start_wanted): up to 71% of draws
# for some options (measured 2026-10-13), and a start kept that small can fail the fill.
START_RESHUFFLES = 50
# Then the doors a failure left waiting lead where the game has them, and the rest are drawn again, round by round.
PIN_ROUNDS = 10
PIN_ROUND_TRIES = 5
# How Archipelago's randomizer says it ran out; its setup errors say other things.
_RAN_OUT = "None of the available entrances are valid targets"


def _randomize(world: BugFablesWorld, coupled: bool, measure: _Measure,
               wanted: int) -> tuple[list[tuple[Door, Door]], list[tuple[str, str]]]:
    """Archipelago's randomizer on the split doors, drawn again when it runs out of spare doors or opens fewer than
    wanted spots from the start; after DOOR_TRIES failures, the doors a failure left waiting are pinned where the game
    has them, a round at a time. Returns the pins and the randomizer's pairings."""
    free = _FreeDoors(world)
    pins: list[tuple[Door, Door]] = []
    # The doors the last failed draw left waiting: every round after the first follows a failed one.
    left: list[Entrance] = []
    reshuffles = 0
    for round_ in range(PIN_ROUNDS + 1):
        if round_:
            pins += _pin(world, left)
            # Pinned doors are connected for good: a new snapshot leaves them out of every reset.
            free = _FreeDoors(world)
        tries = 0
        while tries < (PIN_ROUND_TRIES if round_ else DOOR_TRIES):
            try:
                placed = randomize_entrances(world, coupled=coupled, target_group_lookup={0: [0]})
            except EntranceRandomizationError as error:
                # One that placed nothing would fail the same way every time.
                if not str(error).startswith(_RAN_OUT) or not free.placed():
                    raise
                tries += 1
                failure, left = error, free.left()
                free.reset()
                logging.info("Bug Fables: player %s (%s)'s door shuffle ran out of spare doors on a draw.",
                             world.player, world.player_name)
                continue
            # A random start may move to another room start that suits these doors.
            if not (_fit_spawn(world, measure, wanted, SPAWN_TRIES) if world.start
                    else measure.start()[0] >= wanted):
                if reshuffles == START_RESHUFFLES:
                    raise EntranceRandomizationError(
                        f"Bug Fables: player {world.player_name}'s doors opened fewer than {wanted} spots "
                        f"from the start in {START_RESHUFFLES + 1} draws")
                reshuffles += 1
                free.reset()
                continue
            return pins, placed.pairings
    raise EntranceRandomizationError(
        f"Bug Fables: player {world.player_name}'s door shuffle ran out of spare doors in every try, with "
        f"{len(pins)} ways pinned where the game has them: {', '.join(door_name(*x) for x, _ in pins)}") from failure


class _FreeDoors:
    """The split doors nothing connects yet, the ones Archipelago's randomizer pairs, to put back as they were after a
    try: the randomizer connects each exit it places and takes the target it used off its region; nothing else of a
    try outlives it."""

    def __init__(self, world: BugFablesWorld):
        regions = world.get_regions()
        self.exits = [e for region in regions for e in region.exits if e.connected_region is None]
        self.targets = [e for region in regions for e in region.entrances if e.parent_region is None]
        # The randomizer counts only the total; a mismatch of kinds would fail every try the same way.
        for kind in (EntranceType.ONE_WAY, EntranceType.TWO_WAY):
            exits = sum(e.randomization_type == kind for e in self.exits)
            targets = sum(e.randomization_type == kind for e in self.targets)
            if exits != targets:
                raise ValueError(f"Bug Fables: player {world.player_name}'s doors split into {exits} {kind.name} exits "
                                 f"and {targets} {kind.name} targets")

    def placed(self) -> bool:
        return any(e.connected_region is not None for e in self.exits)

    def left(self) -> list[Entrance]:
        return sorted((e for e in self.exits if e.connected_region is None), key=lambda e: e.name)

    def reset(self) -> None:
        for exit_ in self.exits:
            if exit_.connected_region is not None:
                exit_.connected_region.entrances.remove(exit_)
                exit_.connected_region = None
        for target in self.targets:
            if target not in target.connected_region.entrances:
                target.connected_region.entrances.append(target)


def _pin(world: BugFablesWorld, doors: Sequence[Entrance]) -> list[tuple[Door, Door]]:
    """Each door connected where the game has it: a two-way door both ways with its partner, a one-way door to its own
    landing; a way a plando connection or an earlier pin took is left as it is. Returns the pins as pairings."""
    partner = _partners(SHUFFLED_CONNECTIONS)
    two_ways = {door_name(*door): door for door in partner}
    one_ways = {door_name(w.map, w.door): w for w in SHUFFLED_ONE_WAYS}
    pins: list[tuple[Door, Door]] = []
    for entrance in doors:
        if entrance.name in one_ways:
            w = one_ways[entrance.name]
            ways = [(entrance.name, landing_region(w), one_way_landing(w), ((w.map, w.door), (w.map, w.door)))]
        else:
            door = two_ways[entrance.name]
            other = partner[door]
            ways = [(door_name(*a), door_region(*b), door_name(*b), (a, b)) for a, b in ((door, other), (other, door))]
        for exit_name, region, target, pairing in ways:
            if (world.get_entrance(exit_name).connected_region is None
                    and _free_target(world, region, target) is not None):
                _connect(world, exit_name, region, target)
                pins.append(pairing)
    logging.warning("Bug Fables: player %s (%s)'s door shuffle kept running out of spare doors; %d ways now lead where "
                    "the game has them: %s", world.player, world.player_name, len(pins),
                    ", ".join(door_name(*x) for x, _ in pins) or "none free to pin")
    return pins


def _split(world: BugFablesWorld) -> dict[Door, str]:
    """Splits the door entrances the mode shuffles, as Archipelago's randomizer needs them: every two-way door, and the
    one-ways but a ride's way back (SHUFFLED_ONE_WAYS), none with Room Swap; the rest stay as they are. Returns each
    two-way door's entrance name."""
    names = {(end.map, end.door): door_name(end.map, end.door) for c in SHUFFLED_CONNECTIONS for end in (c.a, c.b)}
    for name in names.values():
        entrance = world.get_entrance(name)
        entrance.randomization_type = EntranceType.TWO_WAY
        disconnect_entrance_for_randomization(entrance)
    if world.options.entrance_randomizer != EntranceRandomizer.option_room_swap:
        for w in SHUFFLED_ONE_WAYS:
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
    copies = {(w.map, copy) for w in SHUFFLED_ONE_WAYS for copy in w.copies}
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
    one_ways = SHUFFLED_ONE_WAYS if mode != EntranceRandomizer.option_room_swap else ()
    pairings = pairings_from_targets(targets, SHUFFLED_CONNECTIONS, one_ways)
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
    one_ways = {door_name(w.map, w.door).lower(): w for w in SHUFFLED_ONE_WAYS}
    landings = {one_way_landing(w).lower(): w for w in SHUFFLED_ONE_WAYS}
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


# Few random layouts keep every room reachable; the repair below takes about 190 tries (measured in build step 30).
ROOM_SWAP_TRIES = 10000
# How often a move takes a room where the repair is needed (one the start reaches, or one around what is cut off),
# rather than any room.
_HOT_MOVES = 0.8
# How often a move trades two rooms, rather than turning one room's doors.
_TRADES = 0.7
# A layout that hasn't improved in this many tries is stuck (no one move helps): start again from a new random one.
_STUCK = 200
# The spots open from the start that can take progression (not excluded), which every door layout must leave: as many
# as the game's own layout has, up to this many. Fewer and the fill can't place the early items.
_START_SPOTS = 10


def _swap_rooms(world: BugFablesWorld, names: dict[Door, str], wanted: int) -> list[tuple[Door, Door]]:
    """A room swap the logic can finish: unlike Archipelago's randomizer, it doesn't follow the logic while placing (a
    gated door moves with its room and can close the only way on), so a random layout is repaired: two like rooms trade
    places, or a room's doors turn, most often among the rooms the start reaches while it opens fewer spots than wanted
    (the fill needs them), else around what is cut off, each move kept unless it cuts off more or opens fewer. A random
    start must suit the repaired layout too (_fit_spawn); a layout none suits is dropped for a new one."""
    random = world.random
    wiring = _Wiring(world, names)
    measure = _Measure(world)
    layout = _RoomLayout(SHUFFLED_CONNECTIONS, _KEPT_FIXED, random)
    group_of = {slot: slots for slots in layout.groups for slot in slots}
    movable = [g for g in layout.groups if len(g) > 1 or len(layout.by_area[g[0]]) > 1]
    region_of = {door: door_region(*door) for door in layout.partner}
    pairings = wiring.wire(layout.pairs())
    cut, start, opened = measure()
    since_better = 0
    for _ in range(ROOM_SWAP_TRIES):
        if since_better >= _STUCK:
            layout = _RoomLayout(SHUFFLED_CONNECTIONS, _KEPT_FIXED, random)
            pairings = wiring.wire(layout.pairs())
            cut, start, opened = measure()
            since_better = 0
        if not cut and start >= wanted:
            if not world.start or _fit_spawn(world, measure, wanted, SPAWN_TRIES):
                return pairings
            since_better = _STUCK
            continue
        slots = random.choice(movable)
        if random.random() < _HOT_MOVES:
            if start < wanted:
                # The start's way on is mostly gated inside the rooms it reaches, with no door to aim at: one of those
                # rooms trades places or turns its doors.
                hot = [s for s, room in layout.room_at.items()
                       if any(region_of[d] in opened for d in layout.by_area[room])]
            else:
                # The rooms on the edge of what is cut off (a door pair with one side reached) first: moving one of
                # them is what joins a cut-off block back; else any room standing where something is cut off.
                # The slot each door stands in now: a room's doors move with it.
                slot_of_door = {door: slot for slot, room in layout.room_at.items() for door in layout.by_area[room]}
                edge = {slot_of_door[d] for x, y in pairings
                        if (region_of[x] in cut) != (region_of[y] in cut) for d in (x, y)}
                cut_maps = {region.split(" (")[0] for region in cut}
                hot = sorted(edge) or [s for s, room in layout.room_at.items()
                                       if any(m in cut_maps for m, _ in layout.by_area[room])]
            if hot:
                slots = group_of[random.choice(hot)]
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
        now_pairings = wiring.wire(layout.pairs())
        now, now_start, now_opened = measure()
        score, now_score = (len(cut), max(0, wanted - start)), (len(now), max(0, wanted - now_start))
        since_better = 0 if now_score < score else since_better + 1
        if now_score <= score:
            cut, start, opened, pairings = now, now_start, now_opened, now_pairings
            continue
        if undo[0] is not None:
            layout.room_at[undo[0]], layout.room_at[undo[1]] = layout.room_at[undo[1]], layout.room_at[undo[0]]
        else:
            layout.order[undo[1]] = undo[2]
        wiring.wire(layout.pairs())
    raise RuntimeError(f"Bug Fables: no room swap in {ROOM_SWAP_TRIES} tries kept every room reachable")


class _Wiring:
    """The split doors as a layout connects them, kept connected between tries: a new layout rewires only the doors
    whose target changed (a move changes a few of the some 270 pairs)."""

    def __init__(self, world: BugFablesWorld, names: dict[Door, str]):
        self.world, self.names = world, names
        self.now: dict[Door, tuple[Door, Entrance, Region, Entrance]] = {}

    def wire(self, pairs: list[tuple[Door, Door]]) -> list[tuple[Door, Door]]:
        """Connects the layout's pairs, both ways; returns its pairings."""
        pairings = [p for x, y in pairs for p in ((x, y), (y, x))]
        wanted = dict(pairings)
        for x in [x for x, (y, *_) in self.now.items() if wanted.get(x) != y]:
            _, source, region, target = self.now.pop(x)
            region.entrances.remove(source)
            source.connected_region = None
            region.entrances.append(target)
        for x, y in pairings:
            if x not in self.now:
                self.now[x] = (y, *_connect(self.world, self.names[x], door_region(*y), self.names[y]))
        return pairings


class _Measure:
    """For the layout connected now: the regions it cuts off with everything the seed holds, as Archipelago's
    randomizer checks it; how many spots that can take progression (not excluded, nor kept from it by an item rule) it
    opens from the start, with only the start's items; and the regions it opens."""

    def __init__(self, world: BugFablesWorld):
        self.world = world
        self.locations = tuple(world.get_locations())
        self.regions = tuple(world.get_regions())
        # Everything the seed holds, collected once: each measure sweeps a copy.
        self.everything = CollectionState(world.multiworld)
        for item in world.multiworld.itempool:
            if item.player == world.player:
                world.collect(self.everything, item)
        # One of each of the seed's progression items, by name.
        self.kinds: dict[str, Item] = {}
        for item in world.multiworld.itempool:
            if item.player == world.player and item.advancement:
                self.kinds.setdefault(item.name, item)
        # The spots that can take some progression item: not excluded, and not kept from all of it by an item rule
        # (Shop Contents' No Progression keeps a shop's spots to other items; a rule against one item, a non-local one
        # say, leaves the rest).
        self.open_to_progression = frozenset(
            location for location in self.locations
            if location.address is not None and location.progress_type != LocationProgressType.EXCLUDED
            and (not self.kinds or any(location.item_rule(item) for item in self.kinds.values())))

    def with_items(self, names: Sequence[str]) -> tuple[int, int]:
        """With the start's items and one of each named: the spots that can take progression it opens, and regions."""
        state = CollectionState(self.world.multiworld)
        for name in names:
            state.collect(self.kinds[name], True)
        spots, regions = self.reach(state)
        return spots, len(regions)

    def start(self) -> tuple[int, set[str]]:
        """With only the start's items: the spots that can take progression the layout opens, and the regions."""
        # A new state already holds the start's items.
        return self.reach(CollectionState(self.world.multiworld))

    def reach(self, start: CollectionState) -> tuple[int, set[str]]:
        """With the items the state holds: the spots that can take progression the layout opens, and the regions."""
        player = self.world.player
        start.sweep_for_advancements(self.locations)
        opened_regions = [region for region in self.regions if start.can_reach_region(region.name, player)]
        spots = sum(1 for region in opened_regions for location in region.locations
                    if location in self.open_to_progression and location.access_rule(start))
        return spots, {region.name for region in opened_regions}

    def __call__(self) -> tuple[set[str], int, set[str]]:
        player = self.world.player
        spots, opened = self.start()
        state = self.everything.copy()
        state.sweep_for_advancements(self.locations)
        cut = {region.name for region in self.regions if not state.can_reach_region(region.name, player)}
        return cut, spots, opened


def start_wanted(world: BugFablesWorld) -> int:
    """Room Swap's rule for any start, from the game's own start on its own doors (before they're split): as many spots
    as it opens, up to _START_SPOTS."""
    measure = _Measure(world)
    if not world.start:
        return min(_START_SPOTS, measure.start()[0])
    entrance = world.get_entrance(START_ENTRANCE)
    spawn = entrance.connected_region
    _point(entrance, world.get_region(START_MAP))
    spots = measure.start()[0]
    _point(entrance, spawn)
    return min(_START_SPOTS, spots)


# Room starts tried on one door layout, the random start's own first, before the layout is drawn again.
SPAWN_TRIES = 20


def keep_spawn_open(world: BugFablesWorld, wanted: int) -> None:
    """A random start that can't begin a seed on the doors as they now are is replaced by one that can (_fit_spawn,
    every room start tried); none can is an error."""
    if not _fit_spawn(world, _Measure(world), wanted, len(ROOM_STARTS)):
        raise ValueError(f"Bug Fables: player {world.player_name}: no room start can begin this seed")


def _fit_spawn(world: BugFablesWorld, measure: _Measure, wanted: int, tries: int) -> bool:
    """Whether the random start, or failing it one of `tries` other room starts in the seed's random order, can begin
    a seed on the doors as they now are: it opens at least wanted spots, cuts no region off even with everything (the
    logic has no way out of some places), and if tight (fewer than _START_SPOTS) isn't restrictive: one item alone
    opens _START_SPOTS (Archipelago's restrictive start needs more than one to go on, docs/apworld_dev_faq.md).
    Menu's way in (and world.start) is left at the one that can, or where it was."""
    entrance = world.get_entrance(START_ENTRANCE)
    home = entrance.connected_region
    # The items early_items may place: never a non-local one.
    early = sorted(name for name in measure.kinds if name not in world.options.non_local_items.value)

    def fits() -> bool:
        spots = measure.start()[0]
        return (spots >= wanted and not measure()[0]
                and (spots >= _START_SPOTS or any(measure.with_items([name])[0] >= _START_SPOTS for name in early)))

    if fits():
        return True
    rooms = list(ROOM_STARTS)
    world.random.shuffle(rooms)
    for tried, room in enumerate(rooms[:tries], 1):
        _point(entrance, world.get_region(door_region(room.map, room.arrival)))
        if fits():
            logging.info("Bug Fables: player %s (%s)'s start couldn't begin a seed; %s (from %s) after %d more.",
                         world.player, world.player_name, room.map, room.from_map, tried)
            world.start = room.to_slot()
            return True
    _point(entrance, home)
    return False


# Items placed early for a tight start, at most: a start that needs more than this to go on isn't helped by them.
EARLY_ITEMS = 3


def early_items(world: BugFablesWorld) -> list[str]:
    """For a tight start, one opening fewer than _START_SPOTS spots with the start's items: the progression items for
    Archipelago to place within the start's reach (local_early_items, its answer to a restrictive start,
    docs/apworld_dev_faq.md). One picked with the seed's random among those that alone open _START_SPOTS; failing
    that, item by item the one opening most (spots, then regions) until the start opens more than it does alone,
    EARLY_ITEMS at most (leaving a start can take two at once: a member and their ability). None for a start that
    isn't tight, and never an item the player's own world mustn't hold (non_local_items) or one an item link took out
    of the player's pool."""
    measure = _Measure(world)
    alone = measure.start()[0]
    names = sorted(name for name in measure.kinds if name not in world.options.non_local_items.value)
    if alone >= _START_SPOTS or not names:
        return []
    single = {name: measure.with_items([name]) for name in names}
    enough = [name for name in single if single[name][0] >= _START_SPOTS]
    if enough:
        return [world.random.choice(enough)]
    chosen = [max(single, key=lambda name: single[name])]
    while measure.with_items(chosen)[0] <= alone and len(chosen) < min(EARLY_ITEMS, len(single)):
        more = {name: measure.with_items([*chosen, name]) for name in single if name not in chosen}
        chosen.append(max(more, key=lambda name: more[name]))
    return chosen


def _point(entrance: Entrance, region: Region) -> None:
    """Menu's way into the start, pointed at another region."""
    entrance.connected_region.entrances.remove(entrance)
    entrance.connect(region)


def write_spoiler(world: BugFablesWorld) -> None:
    """Each shuffled door in the spoiler's Entrances section, the way it now leads; a coupled pair only once, both
    ways; a one-way to the landing it takes."""
    coupled = world.options.entrance_randomizer != EntranceRandomizer.option_decoupled
    landing = {(w.map, w.door): one_way_landing(w) for w in SHUFFLED_ONE_WAYS}
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
