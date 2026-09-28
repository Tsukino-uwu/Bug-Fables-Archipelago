from random import Random

from . import BugFablesTestBase
from ..data_tables import DOORS
from ..data_types import DoorConnection, DoorEnd
from ..doors import arrivals, shuffle_coupled


def _link(a_map: str, a_door: str, b_map: str, b_door: str) -> DoorConnection:
    return DoorConnection(a=DoorEnd(map=a_map, door=a_door), b=DoorEnd(map=b_map, door=b_door))


def _reachable_maps(connections, fixed, targets) -> tuple[set[str], set[str]]:
    """(maps reached from one map, every map in the table)."""
    links: dict[str, set[str]] = {}
    for (m, _), (to, _) in arrivals(connections, targets).items():
        links.setdefault(m, set()).add(to)
        links.setdefault(to, set()).add(m)
    for a, b in fixed:
        links.setdefault(a, set()).add(b)
        links.setdefault(b, set()).add(a)
    maps = {end.map for c in connections for end in (c.a, c.b)}
    start = min(maps)
    seen, todo = {start}, [start]
    while todo:
        for n in links.get(todo.pop(), ()):
            if n not in seen:
                seen.add(n)
                todo.append(n)
    return seen & maps, maps


class TestDoorsOffByDefault(BugFablesTestBase):
    def test_no_door_rewritten(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["door_targets"], [])


class TestDoorsCoupled(BugFablesTestBase):
    options = {"entrance_randomizer": "coupled"}

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

    def test_every_map_reachable(self) -> None:
        reached, maps = _reachable_maps(DOORS.connections, DOORS.fixed, self.world.fill_slot_data()["door_targets"])
        self.assertEqual(maps - reached, set())


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
