from random import Random

from . import BugFablesTestBase
from ..data_tables import DOORS
from ..doors import arrivals, shuffle_coupled


def _reachable_maps(connections, fixed, targets) -> tuple[set[str], set[str]]:
    """(maps reached from one map through doors and fixed links, every map in the table)."""
    links: dict[str, set[str]] = {}
    for (m, _), (to, _) in arrivals(connections, targets).items():
        links.setdefault(m, set()).add(to)
        links.setdefault(to, set()).add(m)
    for a, b in fixed:
        links.setdefault(a, set()).add(b)
        links.setdefault(b, set()).add(a)
    maps = {c[side]["map"] for c in connections for side in ("a", "b")}
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
        self.assertGreater(len(targets), len(DOORS["connections"]))

    def test_every_way_back_leads_back(self) -> None:
        # Coupled: through a door, then through the door you arrive next to, is where you started.
        arrive = arrivals(DOORS["connections"], self.world.fill_slot_data()["door_targets"])
        for door, there in arrive.items():
            self.assertEqual(arrive[there], door, f"{door} leads to {there}, which leads to {arrive[there]}")

    def test_targets_are_table_doors(self) -> None:
        doors = {(c[s]["map"], c[s]["door"]) for c in DOORS["connections"] for s in ("a", "b")}
        for t in self.world.fill_slot_data()["door_targets"]:
            self.assertIn((t["map"], t["door"]), doors)
            self.assertIn((t["like_map"], t["like_door"]), doors)

    def test_every_map_reachable(self) -> None:
        reached, maps = _reachable_maps(DOORS["connections"], DOORS["fixed"], self.world.fill_slot_data()["door_targets"])
        self.assertEqual(maps - reached, set())


class TestDoorShuffleConnects(BugFablesTestBase):
    # A hub with dead ends: paired at random, two dead ends joined to each other are cut off from the rest. The shuffle
    # must never do that, whatever the seed.
    def test_dead_ends_never_stranded(self) -> None:
        connections = []
        for n in range(6):
            connections.append({"a": {"map": "Hub", "door": f"to{n}"}, "b": {"map": f"Room{n}", "door": "out"}})
        connections.append({"a": {"map": "Hub", "door": "east"}, "b": {"map": "Hall", "door": "west"}})
        connections.append({"a": {"map": "Hall", "door": "east"}, "b": {"map": "Hub", "door": "west"}})
        for seed in range(300):
            targets = shuffle_coupled(connections, [], Random(seed))
            reached, maps = _reachable_maps(connections, [], targets)
            self.assertEqual(maps - reached, set(), f"seed {seed}")

    def test_fixed_links_join_areas(self) -> None:
        # Room0 and Room1 are joined by a fixed door, so they're one area with two doors: never counted as dead ends.
        connections = [{"a": {"map": "Hub", "door": f"to{n}"}, "b": {"map": f"Room{n}", "door": "out"}} for n in range(4)]
        for seed in range(100):
            targets = shuffle_coupled(connections, [["Room0", "Room1"]], Random(seed))
            reached, maps = _reachable_maps(connections, [["Room0", "Room1"]], targets)
            self.assertEqual(maps - reached, set(), f"seed {seed}")
