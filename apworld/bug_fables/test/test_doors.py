from collections import Counter
from random import Random

from . import BugFablesTestBase
from ..data_tables import DOORS, MAPS
from ..data_types import DoorConnection, DoorEnd
from ..entrances import door_targets, room_pairs
from ..regions import door_name

Door = tuple[str, str]


def _link(a_map: str, a_door: str, b_map: str, b_door: str) -> DoorConnection:
    return DoorConnection(a=DoorEnd(map=a_map, door=a_door), b=DoorEnd(map=b_map, door=b_door))


def _both_ways(pairs: list[tuple[Door, Door]]) -> list[tuple[Door, Door]]:
    return [p for x, y in pairs for p in ((x, y), (y, x))]


def arrivals(connections, targets) -> dict[Door, Door]:
    """Where each door leads once the mod applies the targets: the door the party arrives next to."""
    partner: dict[Door, Door] = {}
    for c in connections:
        a, b = (c.a.map, c.a.door), (c.b.map, c.b.door)
        partner[a], partner[b] = b, a
    like = {(t["map"], t["door"]): (t["like_map"], t["like_door"]) for t in targets}
    return {d: partner[like.get(d, d)] for d in partner}


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


def _shape(connections, fixed, targets) -> Counter:
    """For each area (maps joined by fixed links both ways): its part of the world, its door count, and the door counts
    of the areas its doors lead into. A room swap keeps this; any other shuffle almost never does."""
    maps = {end.map for c in connections for end in (c.a, c.b)}
    both_ways = [(a, b) for a, b in fixed if (b, a) in set(fixed)]
    area_of = {m: g for g in _groups(_links([], both_ways, [])) for m in g}
    area_of.update({m: frozenset([m]) for m in maps if m not in area_of})
    part_of = {m: g for g in _groups(_links(connections, fixed, [])) for m in g}
    doors = Counter(area_of[end.map] for c in connections for end in (c.a, c.b))
    leads: dict[frozenset[str], list[int]] = {a: [] for a in doors}
    for (m, _), (to, _) in arrivals(connections, targets).items():
        leads[area_of[m]].append(doors[area_of[to]])
    return Counter((part_of[min(a)], doors[a], tuple(sorted(leads[a]))) for a in doors)


class DoorPairTests:
    """Every mode that shuffles: doors rewritten, only to the table's doors, the logic and the mod agreeing."""

    def test_doors_are_shuffled(self) -> None:
        self.assertGreater(len(self.world.door_targets), len(DOORS.connections))

    def test_targets_are_table_doors(self) -> None:
        doors = {(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)}
        for t in self.world.door_targets:
            self.assertIn((t["map"], t["door"]), doors)
            self.assertIn((t["like_map"], t["like_door"]), doors)

    def test_the_mod_does_what_the_logic_proved(self) -> None:
        # Each door, rewritten as door_targets says, arrives where its entrance leads in the region graph.
        arrive = arrivals(DOORS.connections, self.world.door_targets)
        for x, y in self.world.door_pairings:
            self.assertEqual(arrive[x], y)
        for (m, d), (to_map, _) in arrive.items():
            with self.subTest(door=door_name(m, d)):
                entrance = self.multiworld.get_entrance(door_name(m, d), self.player)
                self.assertEqual(entrance.connected_region.name, to_map)

    def test_every_region_reachable_with_everything(self) -> None:
        state = self.multiworld.get_all_state()
        for name in MAPS:
            with self.subTest(region=name):
                self.assertTrue(state.can_reach_region(name, self.player))

    def spoiler_entries(self) -> list[tuple[str, str, int]]:
        self.world.write_spoiler_header(None)
        return [key for key in self.multiworld.spoiler.entrances if key[2] == self.player]


class CoupledTests(DoorPairTests):
    """Room Swap and Coupled: a door and its way back stay a pair."""

    def test_every_way_back_leads_back(self) -> None:
        # Through a door, then through the door you arrive next to, is where you started.
        arrive = arrivals(DOORS.connections, self.world.door_targets)
        for door, there in arrive.items():
            self.assertEqual(arrive[there], door, f"{door} leads to {there}, which leads to {arrive[there]}")

    def test_spoiler_lists_each_pair_once(self) -> None:
        self.assertEqual(len(self.spoiler_entries()), len(DOORS.connections))


class TestDoorsOffByDefault(BugFablesTestBase):
    def test_no_door_rewritten(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["door_targets"], [])
        self.assertEqual(self.world.door_pairings, [])


class TestDoorsCoupled(CoupledTests, BugFablesTestBase):
    options = {"entrance_randomizer": "coupled"}


class TestDoorsDecoupled(DoorPairTests, BugFablesTestBase):
    options = {"entrance_randomizer": "decoupled"}

    def test_the_way_back_is_shuffled_too(self) -> None:
        arrive = arrivals(DOORS.connections, self.world.door_targets)
        self.assertTrue(any(arrive[there] != door for door, there in arrive.items()))

    def test_spoiler_lists_every_door(self) -> None:
        self.assertEqual(len(self.spoiler_entries()), 2 * len(DOORS.connections))


class TestDoorsRoomSwap(CoupledTests, BugFablesTestBase):
    options = {"entrance_randomizer": "room_swap"}

    def test_parts_stay_whole(self) -> None:
        # Parts the game joins only by boats and scenes keep their own rooms, each still reachable within its part.
        targets = self.world.door_targets
        self.assertEqual(_groups(_links(DOORS.connections, DOORS.fixed, targets)),
                         _groups(_links(DOORS.connections, DOORS.fixed, [])))

    def test_map_keeps_its_shape(self) -> None:
        self.assertEqual(_shape(DOORS.connections, DOORS.fixed, self.world.door_targets),
                         _shape(DOORS.connections, DOORS.fixed, []))


class TestRoomSwapParts(BugFablesTestBase):
    def test_parts_never_trade_rooms(self) -> None:
        # Two hubs of three doors, joined by no door (a boat): a hub or room crossing over would strand a part.
        connections = [_link(f"Hub{h}", f"to{n}", f"Room{h}{n}", "out") for h in range(2) for n in range(3)]
        vanilla = _groups(_links(connections, [], []))
        for seed in range(100):
            targets = door_targets(_both_ways(room_pairs(connections, [], Random(seed))), connections)
            self.assertEqual(_groups(_links(connections, [], targets)), vanilla, f"seed {seed}")

    def test_rooms_move_whole(self) -> None:
        # Two halls joined by a fixed door both ways move as one area, so the shape holds.
        connections = [_link("Hub", f"to{n}", f"Room{n}", "out") for n in range(4)]
        connections += [_link("Room0", "east", "Hall", "west"), _link("Room1", "east", "Hall2", "west")]
        fixed = [("Hall", "Hall2"), ("Hall2", "Hall")]
        for seed in range(100):
            targets = door_targets(_both_ways(room_pairs(connections, fixed, Random(seed))), connections)
            self.assertEqual(_shape(connections, fixed, targets), _shape(connections, fixed, []), f"seed {seed}")
