from unittest import TestCase

from worlds.AutoWorld import call_all
from worlds.generic.Rules import add_item_rule

from .. import entrances
from ..data_tables import DOORS, ROOM_STARTS, start_region
from ..data_types import EntityRef
from ..logic import KEPT_PRESENT
from ..regions import MENU, START_ENTRANCE, START_MAP
from ..world import BugFablesWorld
from . import BugFablesTestBase, generate_like_main


class TestStartOffByDefault(BugFablesTestBase):
    def test_game_start(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["start"], {})

    def test_logic_begins_at_the_game_start(self) -> None:
        menu = self.multiworld.get_region(MENU, self.player)
        self.assertEqual([e.connected_region.name for e in menu.exits], [START_MAP])


class TestStartAnywhere(BugFablesTestBase):
    options = {"starting_location": "anywhere"}

    def test_start_is_a_room_entered_through_a_door(self) -> None:
        start = self.world.fill_slot_data()["start"]
        self.assertIn(start, [room.to_slot() for room in ROOM_STARTS])
        self.assertIn({start["map"], start["from"]}, [{c.a.map, c.b.map} for c in DOORS.connections])
        self.assertIn((start["from"], start["door"]), {(end.map, end.door) for c in DOORS.connections
                                                       for end in (c.a, c.b)})

    def test_every_door_into_a_room_can_be_picked(self) -> None:
        # Each door into a room is a start of its own, unless its landing door is absent on a new file.
        starts = {(s.map, s.from_map, s.door, s.arrival) for s in ROOM_STARTS}
        for c in DOORS.connections:
            for end, other in ((c.a, c.b), (c.b, c.a)):
                if end.map != other.map and ((end.map, end.door) not in DOORS.gated
                                             or EntityRef(end.map, end.door) in KEPT_PRESENT):
                    self.assertIn((end.map, other.map, other.door, end.door), starts)

    def test_never_lands_at_a_door_the_game_hides(self) -> None:
        # The desert border's door to the Far Grasslands is made only in chapter 5 (flag 348): a start there fell
        # forever until the seed kept it present. Every landing door exists on a new file, or the seed keeps it.
        for start in ROOM_STARTS:
            with self.subTest(start=start):
                self.assertTrue((start.map, start.arrival) not in DOORS.gated
                                or EntityRef(start.map, start.arrival) in KEPT_PRESENT)
        self.assertIn(("DesertFGBorder", "FarGrasslands1"), {(s.map, s.from_map) for s in ROOM_STARTS})

    def test_same_seed_same_start(self) -> None:
        # generate_early ran once with the seed's random; asking twice gives what it chose, not a new pick.
        self.assertEqual(self.world.fill_slot_data()["start"], self.world.fill_slot_data()["start"])

    def test_logic_begins_at_the_start(self) -> None:
        # Menu leads only to the start's area, where a reset (the Warp to Start) takes the party back.
        menu = self.multiworld.get_region(MENU, self.player)
        self.assertEqual([(e.name, e.connected_region.name) for e in menu.exits],
                         [(START_ENTRANCE, start_region(self.world.fill_slot_data()["start"]))])

    def test_the_opening_checks_count_from_the_start(self) -> None:
        # The opening's checks are sent as a new file begins, wherever it starts.
        for loc in self.world.included_locations:
            if loc.quiet:
                self.assertEqual(self.world.get_location(loc.name).parent_region.name, MENU)

    def test_every_region_reachable_with_everything(self) -> None:
        state = self.multiworld.get_all_state()
        for region in self.world.get_regions():
            with self.subTest(region=region.name):
                self.assertTrue(state.can_reach_region(region.name, self.player))


class TestStartPickedAgain(TestCase):
    """A start the fill couldn't begin from is replaced by another (build step 96)."""

    def test_a_start_reaching_too_little(self) -> None:
        # Rubber Prison's cell block: every spot there needs a key a new file hasn't got (measured 2026-10-13).
        self._assert_picked_again(next(s for s in ROOM_STARTS if s.map == "RubberPrisonCells1").to_slot())

    def test_a_trapped_start(self) -> None:
        # Snakemouth Den's lake, from the fall room: 11 spots in reach, but the logic has no way out, so even with
        # every item the rest of the world is cut off (measured 2026-10-13); only the cut-off check refuses it.
        self._assert_picked_again(next(s for s in ROOM_STARTS if s.map == "SnakemouthLake"
                                       and s.from_map == "SnakemouthFallRoom").to_slot())

    def _assert_picked_again(self, start: dict[str, str]) -> None:
        world = _started_at(start)
        self.assertNotEqual(world.start, start)
        self.assertEqual(world.get_entrance(START_ENTRANCE).connected_region.name, start_region(world.start))
        cut, spots, _ = entrances._Measure(world)()
        self.assertFalse(cut)
        # The doors are the game's own, so the game's start is measured on them as connect_entrances measured it.
        self.assertGreaterEqual(spots, entrances.start_wanted(world))


class TestTightStart(TestCase):
    """A start that opens few spots gets its way on placed early (build step 97)."""

    HARD = {"starting_party_member": "vi", "shuffle_field_moves": True, "shuffle_jump": True}

    def test_none_with_the_defaults(self) -> None:
        # The game's start opens 16 spots with the defaults (measured 2026-10-13).
        world = generate_like_main({}, seed=1)
        self.assertEqual(world.multiworld.local_early_items[world.player], {})

    def test_one_that_opens_the_way(self) -> None:
        # Vi alone with the moves and Jump shuffled: the game's start opens 3 spots, one item alone opens 10 or more.
        world = generate_like_main(self.HARD, seed=1)
        early = list(world.multiworld.local_early_items[world.player])
        measure = entrances._Measure(world)
        self.assertLess(measure.start()[0], entrances._START_SPOTS)
        self.assertEqual(len(early), 1)
        self.assertGreaterEqual(measure.with_items(early)[0], entrances._START_SPOTS)

    def test_none_when_a_link_took_the_items(self) -> None:
        # An item link moves the linked items out of the player's pool before pre_fill: nothing left to place early.
        world = generate_like_main(self.HARD, seed=1, steps=BEFORE_FILL)
        world.multiworld.itempool[:] = [item for item in world.multiworld.itempool
                                        if item.player != world.player or not item.advancement]
        self.assertEqual(entrances.early_items(world), [])

    def test_a_start_item_from_the_pool_counts(self) -> None:
        # Archipelago gives Start Inventory from Pool's items before generation (Main.py, push_precollected): Jump in
        # hand, Vi's start opens 11 spots (measured 2026-10-13), so nothing is placed early.
        world = generate_like_main(self.HARD, seed=1, steps=("generate_early",))
        world.multiworld.push_precollected(world.create_item("Jump"))
        for step in BEFORE_FILL[1:]:
            call_all(world.multiworld, step)
        self.assertEqual(entrances.early_items(world), [])

    def test_never_a_non_local_item(self) -> None:
        # Jump is the one picked here (test_one_that_opens_the_way); kept out of the player's own world, it can't be.
        world = generate_like_main(self.HARD, seed=1, steps=BEFORE_FILL)
        world.options.non_local_items.value = {"Jump"}
        self.assertNotIn("Jump", entrances.early_items(world))

    def test_a_restrictive_spawn_is_picked_again(self) -> None:
        # Leif alone at the desert's DR East Entrance: 3 spots, and no item alone opens more than 4 (measured
        # 2026-10-13): its way on needs two at once.
        start = next(s for s in ROOM_STARTS
                     if s.map == "DesertDREastEntrance" and s.from_map == "DesertBadgeAlcove").to_slot()
        world = _started_at(start, {"starting_party_member": "leif", "shuffle_field_moves": True, "shuffle_jump": True})
        self.assertNotEqual(world.start, start)


class TestStartCount(TestCase):
    def test_a_rule_against_one_item_leaves_the_spot(self) -> None:
        # In a room of several players, a non-local item is kept out of every one of the player's spots: they still
        # take the rest of the progression, so the start's count stays (a review's find, 2026-10-13).
        world = generate_like_main({}, seed=1, steps=BEFORE_FILL)
        before = entrances._Measure(world).start()[0]
        for location in world.get_locations():
            add_item_rule(location, lambda item: item.name != "Explorer Permit")
        self.assertEqual(entrances._Measure(world).start()[0], before)


# Main.py's steps up to the fill, pre_fill (and its early items) left out.
BEFORE_FILL = ("generate_early", "create_regions", "create_items", "set_rules", "connect_entrances", "generate_basic")


def _started_at(start: dict[str, str], options: dict[str, object] | None = None) -> BugFablesWorld:
    """A world whose random start is the one given, built through its doors."""
    world = generate_like_main({"starting_location": "anywhere", **(options or {})}, seed=1,
                               steps=("generate_early",))
    world.start = start
    for step in ("create_regions", "create_items", "set_rules", "connect_entrances"):
        call_all(world.multiworld, step)
    return world
