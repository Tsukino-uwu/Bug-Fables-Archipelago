from collections import Counter
from random import Random
from unittest import TestCase

from BaseClasses import EntranceType

from . import BugFablesTestBase
from ..data_tables import DOORS, MAPS, ONE_WAYS, door_name, one_way_landing
from ..data_types import DoorConnection, DoorEnd
from ..entrances import door_targets, room_pairs
from ..options import DoorPlando

Door = tuple[str, str]
ONE_WAY_DOORS = {(w.map, w.door) for w in ONE_WAYS}
# The game's one-way doors (MEASURED.md, the fog maze): the Forsaken Lands' wrong turns, the pink spider's room, the
# underground bar's exit, the wizard's basement drop and a Giant's Lair ladder.
EXPECTED_ONE_WAYS = {
    ("BarrenLandsEntrance", "returnloadzoneright"), ("BarrenLandsCD", "returnloadzoneleft"),
    ("BarrenLandsCD", "returnloadzone"), ("BarrenLandsBeefly", "returnloadzone"),
    ("BarrenLandsTanks", "returnloadzone"), ("BarrenLandsTanks", "returnzonenorth"),
    ("BarrenLandsPumpkins", "returnloadzone"), ("BarrenLandsCloud", "returnzone"),
    ("BarrenLandsRock", "returnzoneright"), ("BarrenLandsRock", "returnzonesouth"),
    ("BarrenLandsSideGPT", "returnzone left"), ("BarrenLandsSideGPT", "returnzone south"),
    ("BarrenLandsMushrooms", "loadzonepinkspider"), ("BarrenLandsPinkSpider", "loadzonepumpkin - Duplicate"),
    ("UndergroundBar", "LoadZone"), ("FarGrasslandsWizard", "loadzonebasement"),
    ("GiantLairBeforeBoss2", "loadzoneleft"),
}
# The fog maze's left edge in BarrenLandsCD: two copies at one spot, the second there once the Termite gate is open.
TWIN = ("BarrenLandsCD", "returnloadzoneleft")
TWIN_COPY = "returnloadzoneleft - Duplicate"


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


def one_way_lands(one_ways, targets) -> dict[Door, str]:
    """The map each one-way door, its story copies included, lands in once the mod applies the targets."""
    to = {(w.map, w.door): w.to for w in one_ways}
    like = {(t["map"], t["door"]): (t["like_map"], t["like_door"]) for t in targets}
    return {(w.map, name): to[like.get((w.map, name), (w.map, w.door))] for w in one_ways for name in (w.door, *w.copies)}


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

    # How many spoiler lines the one-way doors add: one each, unless the mode leaves them as they are.
    one_way_lines = len(ONE_WAYS)

    def test_doors_are_shuffled(self) -> None:
        self.assertGreater(len(self.world.door_targets), len(DOORS.connections))

    def test_targets_are_table_doors(self) -> None:
        doors = {(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)} | ONE_WAY_DOORS
        copies = {(w.map, copy) for w in ONE_WAYS for copy in w.copies}
        for t in self.world.door_targets:
            self.assertIn((t["map"], t["door"]), doors | copies)
            self.assertIn((t["like_map"], t["like_door"]), doors)

    def test_the_mod_does_what_the_logic_proved(self) -> None:
        # Each door, rewritten as door_targets says, arrives where its entrance leads in the region graph; a one-way
        # door, and its story copies, land where its entrance leads.
        targets = self.world.door_targets
        arrive = arrivals(DOORS.connections, targets)
        lands = one_way_lands(ONE_WAYS, targets)
        to = {(w.map, w.door): w.to for w in ONE_WAYS}
        for x, y in self.world.door_pairings:
            if x in ONE_WAY_DOORS:
                self.assertEqual(lands[x], to[y])
            else:
                self.assertEqual(arrive[x], y)
        for (m, d), (to_map, _) in arrive.items():
            with self.subTest(door=door_name(m, d)):
                entrance = self.multiworld.get_entrance(door_name(m, d), self.player)
                self.assertEqual(entrance.connected_region.name, to_map)
        for w in ONE_WAYS:
            entrance = self.multiworld.get_entrance(door_name(w.map, w.door), self.player)
            for name in (w.door, *w.copies):
                with self.subTest(door=door_name(w.map, name)):
                    self.assertEqual(lands[(w.map, name)], entrance.connected_region.name)

    def test_story_copies_follow_their_door(self) -> None:
        # The fog maze's left edge in BarrenLandsCD leads one place whatever the Termite gate: its copy always has an
        # entry, the same as its door's.
        like = {(t["map"], t["door"]): (t["like_map"], t["like_door"]) for t in self.world.door_targets}
        self.assertEqual(like[(TWIN[0], TWIN_COPY)], like.get(TWIN, TWIN))

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
        self.assertEqual(len(self.spoiler_entries()), len(DOORS.connections) + self.one_way_lines)


class OneWayTests:
    """Coupled and Decoupled: the one-way doors are Archipelago's one-way entrances, paired only with one another."""

    def test_one_ways_are_shuffled_among_themselves(self) -> None:
        taken = Counter(x for x, _ in self.world.door_pairings if x in ONE_WAY_DOORS)
        self.assertEqual(set(taken), ONE_WAY_DOORS)
        self.assertEqual(set(taken.values()), {1})
        for x, y in self.world.door_pairings:
            with self.subTest(door=door_name(*x)):
                self.assertEqual(x in ONE_WAY_DOORS, y in ONE_WAY_DOORS)
                if x in ONE_WAY_DOORS:
                    entrance = self.multiworld.get_entrance(door_name(*x), self.player)
                    self.assertEqual(entrance.randomization_type, EntranceType.ONE_WAY)


class TestDoorsOffByDefault(BugFablesTestBase):
    def test_no_door_rewritten(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["door_targets"], [])
        self.assertEqual(self.world.door_pairings, [])


class TestDoorsCoupled(OneWayTests, CoupledTests, BugFablesTestBase):
    options = {"entrance_randomizer": "coupled"}


class TestDoorsDecoupled(OneWayTests, DoorPairTests, BugFablesTestBase):
    options = {"entrance_randomizer": "decoupled"}

    def test_the_way_back_is_shuffled_too(self) -> None:
        arrive = arrivals(DOORS.connections, self.world.door_targets)
        self.assertTrue(any(arrive[there] != door for door, there in arrive.items()))

    def test_spoiler_lists_every_door(self) -> None:
        self.assertEqual(len(self.spoiler_entries()), 2 * len(DOORS.connections) + len(ONE_WAYS))


class TestDoorsRoomSwap(CoupledTests, BugFablesTestBase):
    options = {"entrance_randomizer": "room_swap"}
    one_way_lines = 0

    def test_one_ways_stay_as_they_are(self) -> None:
        # Rooms move whole; a one-way still leads into the room it did, and only its story copies are rewritten.
        self.assertFalse(any(x in ONE_WAY_DOORS for x, _ in self.world.door_pairings))
        for w in ONE_WAYS:
            with self.subTest(door=door_name(w.map, w.door)):
                entrance = self.multiworld.get_entrance(door_name(w.map, w.door), self.player)
                self.assertEqual(entrance.connected_region.name, w.to)
                self.assertNotIn((w.map, w.door), {(t["map"], t["door"]) for t in self.world.door_targets})

    def test_parts_stay_whole(self) -> None:
        # Parts the game joins only by boats and scenes keep their own rooms, each still reachable within its part.
        targets = self.world.door_targets
        self.assertEqual(_groups(_links(DOORS.connections, DOORS.fixed, targets)),
                         _groups(_links(DOORS.connections, DOORS.fixed, [])))

    def test_map_keeps_its_shape(self) -> None:
        self.assertEqual(_shape(DOORS.connections, DOORS.fixed, self.world.door_targets),
                         _shape(DOORS.connections, DOORS.fixed, []))


CITY_GATE, LAKE = ("BugariaOutskirtsOutsideCity", "DoorBugaria"), ("SnakemouthLake", "WarpMap5")
FIELDS, PALACE = ("NearSnakemouth", "loadingzonefields"), ("AntBridge", "loadzonepalace")
# A fog maze's wrong turn sent where another one lands.
TANKS_NORTH, ENTRANCE_RIGHT = ("BarrenLandsTanks", "returnzonenorth"), ("BarrenLandsEntrance", "returnloadzoneright")
ENTRANCE_RIGHT_LANDING = "BarrenLandsEntrance as from BarrenLandsEntrance: returnloadzoneright"
PLANDO = [
    # Written in another case on purpose: plando names match whatever the case.
    {"entrance": "bugariaoutskirtsoutsidecity: doorbugaria", "exit": "SnakemouthLake: WarpMap5", "direction": "entrance"},
    {"entrance": "NearSnakemouth: loadingzonefields", "exit": "AntBridge: loadzonepalace", "direction": "exit"},
    # No direction: both, which a one-way can't go; it goes its one way.
    {"entrance": "barrenlandstanks: returnzonenorth", "exit": ENTRANCE_RIGHT_LANDING},
]


class TestDoorPlandoCoupled(OneWayTests, CoupledTests, BugFablesTestBase):
    options = {"entrance_randomizer": "coupled", "plando_connections": PLANDO}

    def test_each_planned_door_both_ways(self) -> None:
        # Coupled joins a plando connection both ways, whatever its direction says.
        for pair in ((CITY_GATE, LAKE), (LAKE, CITY_GATE), (FIELDS, PALACE), (PALACE, FIELDS)):
            self.assertIn(pair, self.world.door_pairings)

    def test_planned_one_way(self) -> None:
        self.assertIn((TANKS_NORTH, ENTRANCE_RIGHT), self.world.door_pairings)


class TestDoorPlandoDecoupled(OneWayTests, DoorPairTests, BugFablesTestBase):
    options = {"entrance_randomizer": "decoupled", "plando_connections": PLANDO}

    def test_each_planned_way(self) -> None:
        # entrance: the entrance door leads to the exit door; exit: the exit door leads back to the entrance door.
        self.assertIn((CITY_GATE, LAKE), self.world.door_pairings)
        self.assertIn((PALACE, FIELDS), self.world.door_pairings)
        self.assertIn((TANKS_NORTH, ENTRANCE_RIGHT), self.world.door_pairings)


class TestDoorPlandoIgnoredWithRoomSwap(CoupledTests, BugFablesTestBase):
    options = {"entrance_randomizer": "room_swap", "plando_connections": PLANDO}
    one_way_lines = 0


class TestDoorPlandoNames(TestCase):
    def test_every_door_and_nothing_else(self) -> None:
        self.assertIn("antbridge: loadzonepalace", DoorPlando.entrances)
        self.assertIn("antbridge: loadzonepalace", DoorPlando.exits)
        self.assertIn("barrenlandstanks: returnzonenorth", DoorPlando.entrances)
        self.assertIn(ENTRANCE_RIGHT_LANDING.lower(), DoorPlando.exits)
        self.assertEqual(len(DoorPlando.entrances), 2 * len(DOORS.connections) + len(ONE_WAYS))
        self.assertEqual(len(DoorPlando.exits), 2 * len(DOORS.connections) + len(ONE_WAYS))
        with self.assertRaises(ValueError):
            DoorPlando.from_any([{"entrance": "Nowhere: door", "exit": "AntBridge: loadzonepalace"}])

    def test_one_way_and_two_way_never_mix(self) -> None:
        # A one-way door can't arrive next to a door, nor a door take a one-way's landing: Archipelago's can_connect.
        for entrance, exit_ in (("BarrenLandsTanks: returnzonenorth", "AntBridge: loadzonepalace"),
                                ("AntBridge: loadzonepalace", ENTRANCE_RIGHT_LANDING)):
            with self.subTest(entrance=entrance), self.assertRaises(ValueError):
                DoorPlando.from_any([{"entrance": entrance, "exit": exit_}])


class TestOneWayTable(TestCase):
    def test_the_game_s_one_way_doors(self) -> None:
        self.assertEqual(ONE_WAY_DOORS, EXPECTED_ONE_WAYS)
        copies = {(w.map, w.door): w.copies for w in ONE_WAYS if w.copies}
        self.assertEqual(copies, {TWIN: (TWIN_COPY,)})

    def test_landings_are_named_apart_from_their_doors(self) -> None:
        # Coupled, Archipelago never joins an exit to a target of its own name: apart, a one-way may keep its landing.
        for w in ONE_WAYS:
            self.assertNotEqual(one_way_landing(w), door_name(w.map, w.door))

    def test_ladder_pair_is_a_connection(self) -> None:
        # Its way back lands 11.3 from it, once just past the pairing distance.
        ends = {frozenset({(c.a.map, c.a.door), (c.b.map, c.b.door)}) for c in DOORS.connections}
        self.assertIn(frozenset({("GiantLairBeforeBoss", "loadzoneup"), ("GiantLairBeforeBoss2", "loadzoneright")}), ends)

    def test_parked_doors_are_in_no_list(self) -> None:
        # The Sand Castle's two right-hand basement doors sit at height 99, out of reach.
        parked = {("SandCastleBasement", "loadzoneright"), ("SandCastleMainRoom", "loadzonebasementright")}
        doors = {(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)} | ONE_WAY_DOORS
        self.assertFalse(parked & doors)
        self.assertNotIn(("SandCastleBasement", "SandCastleMainRoom"), DOORS.fixed)


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
