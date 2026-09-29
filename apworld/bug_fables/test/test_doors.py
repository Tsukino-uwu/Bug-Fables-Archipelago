from collections import Counter
from random import Random

from . import BugFablesTestBase
from ..data_tables import DOORS
from ..data_types import DoorConnection, DoorEnd
from ..doors import arrivals, shuffle_coupled, shuffle_rooms


def _link(a_map: str, a_door: str, b_map: str, b_door: str) -> DoorConnection:
    return DoorConnection(a=DoorEnd(map=a_map, door=a_door), b=DoorEnd(map=b_map, door=b_door))


def _links(connections, fixed, targets) -> dict[str, set[str]]:
    """The maps each map is joined to, by its doors as the targets lead and by fixed links."""
    links: dict[str, set[str]] = {end.map: set() for c in connections for end in (c.a, c.b)}
    for (m, _), (to, _) in arrivals(connections, targets).items():
        links[m].add(to)
        links[to].add(m)
    for a, b in fixed:
        links.setdefault(a, set()).add(b)
        links.setdefault(b, set()).add(a)
    return links


def _groups(links: dict[str, set[str]]) -> set[frozenset[str]]:
    """The groups of maps that can reach each other through the links."""
    groups: set[frozenset[str]] = set()
    seen: set[str] = set()
    for start in sorted(links):
        if start in seen:
            continue
        group, todo = {start}, [start]
        while todo:
            for n in links[todo.pop()]:
                if n not in group:
                    group.add(n)
                    todo.append(n)
        seen |= group
        groups.add(frozenset(group))
    return groups


def _reachable_maps(connections, fixed, targets) -> tuple[set[str], set[str]]:
    """(maps reached from one map, every map in the table)."""
    maps = {end.map for c in connections for end in (c.a, c.b)}
    reached = next(g for g in _groups(_links(connections, fixed, targets)) if min(maps) in g)
    return reached & maps, maps


def _shape(connections, fixed, targets) -> Counter:
    """For each area (maps joined by fixed links): its part of the world, its door count, and the door counts of the
    areas its doors lead into. A room swap keeps this; any other shuffle almost never does."""
    maps = {end.map for c in connections for end in (c.a, c.b)}
    area_of = {m: g for g in _groups(_links([], fixed, [])) for m in g}
    area_of.update({m: frozenset([m]) for m in maps if m not in area_of})
    part_of = {m: g for g in _groups(_links(connections, fixed, [])) for m in g}
    doors = Counter(area_of[end.map] for c in connections for end in (c.a, c.b))
    leads: dict[frozenset[str], list[int]] = {a: [] for a in doors}
    for (m, _), (to, _) in arrivals(connections, targets).items():
        leads[area_of[m]].append(doors[area_of[to]])
    return Counter((part_of[min(a)], doors[a], tuple(sorted(leads[a]))) for a in doors)


class DoorPairTests:
    """Every mode: doors are rewritten, only to the table's doors, and each way back leads back."""

    def test_doors_are_shuffled(self) -> None:
        targets = self.world.fill_slot_data()["door_targets"]
        self.assertGreater(len(targets), len(DOORS.connections))

    def test_every_way_back_leads_back(self) -> None:
        # Coupled: through a door, then through the door you arrive next to, is where you started.
        arrive = arrivals(DOORS.connections, self.world.fill_slot_data()["door_targets"])
        for door, there in arrive.items():
            self.assertEqual(arrive[there], door, f"{door} leads to {there}, which leads to {arrive[there]}")

    def test_targets_are_table_doors(self) -> None:
        doors = {(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)}
        for t in self.world.fill_slot_data()["door_targets"]:
            self.assertIn((t["map"], t["door"]), doors)
            self.assertIn((t["like_map"], t["like_door"]), doors)


class TestDoorsOffByDefault(BugFablesTestBase):
    def test_no_door_rewritten(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["door_targets"], [])


class TestDoorsCoupled(DoorPairTests, BugFablesTestBase):
    options = {"entrance_randomizer": "coupled"}

    def test_every_map_reachable(self) -> None:
        reached, maps = _reachable_maps(DOORS.connections, DOORS.fixed, self.world.fill_slot_data()["door_targets"])
        self.assertEqual(maps - reached, set())


class TestDoorsRoomSwap(DoorPairTests, BugFablesTestBase):
    options = {"entrance_randomizer": "room_swap"}

    def test_parts_stay_whole(self) -> None:
        # Parts the game joins only by boats and scenes keep their own rooms, each still reachable within its part.
        targets = self.world.fill_slot_data()["door_targets"]
        self.assertEqual(_groups(_links(DOORS.connections, DOORS.fixed, targets)),
                         _groups(_links(DOORS.connections, DOORS.fixed, [])))

    def test_map_keeps_its_shape(self) -> None:
        targets = self.world.fill_slot_data()["door_targets"]
        self.assertEqual(_shape(DOORS.connections, DOORS.fixed, targets),
                         _shape(DOORS.connections, DOORS.fixed, []))


class TestDoorShuffleConnects(BugFablesTestBase):
    # Two dead ends paired with each other would be cut off; the shuffle must never do that.
    def test_dead_ends_never_stranded(self) -> None:
        connections = [_link("Hub", f"to{n}", f"Room{n}", "out") for n in range(6)]
        connections.append(_link("Hub", "east", "Hall", "west"))
        connections.append(_link("Hall", "east", "Hub", "west"))
        for seed in range(300):
            targets = shuffle_coupled(connections, [], Random(seed))
            reached, maps = _reachable_maps(connections, [], targets)
            self.assertEqual(maps - reached, set(), f"seed {seed}")

    def test_fixed_links_join_areas(self) -> None:
        # Room0 and Room1 share a fixed link, so they're one area with two doors, not two dead ends.
        connections = [_link("Hub", f"to{n}", f"Room{n}", "out") for n in range(4)]
        for seed in range(100):
            targets = shuffle_coupled(connections, [("Room0", "Room1")], Random(seed))
            reached, maps = _reachable_maps(connections, [("Room0", "Room1")], targets)
            self.assertEqual(maps - reached, set(), f"seed {seed}")


class TestRoomSwapParts(BugFablesTestBase):
    def test_parts_never_trade_rooms(self) -> None:
        # Two hubs of three doors, joined by no door (a boat): a hub or room crossing over would strand a part.
        connections = [_link(f"Hub{h}", f"to{n}", f"Room{h}{n}", "out") for h in range(2) for n in range(3)]
        vanilla = _groups(_links(connections, [], []))
        for seed in range(100):
            targets = shuffle_rooms(connections, [], Random(seed))
            self.assertEqual(_groups(_links(connections, [], targets)), vanilla, f"seed {seed}")

    def test_rooms_move_whole(self) -> None:
        # A two-door room joined to its neighbour by a fixed link moves as one area, so the shape holds.
        connections = [_link("Hub", f"to{n}", f"Room{n}", "out") for n in range(4)]
        connections += [_link("Room0", "east", "Hall", "west"), _link("Room1", "east", "Hall2", "west")]
        fixed = [("Hall", "Hall2")]
        for seed in range(100):
            targets = shuffle_rooms(connections, fixed, Random(seed))
            self.assertEqual(_shape(connections, fixed, targets), _shape(connections, fixed, []), f"seed {seed}")
