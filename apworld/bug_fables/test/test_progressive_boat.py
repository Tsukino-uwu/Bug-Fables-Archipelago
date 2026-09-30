from . import BugFablesTestBase

BOAT = "Progressive Boat"
TICKET = "Boat Ticket"
SUBMARINE = "Subaquatic Maritime Neotransport"
THRONE_ROOM = "Termite Capitol: Throne Room"
ICICLE = "Upper Snakemouth: Entrance"
DOCKS = {("TermitePier", "Fixedsub"), ("BugariaPier", "Fixedsub - Duplicate"), ("MetalIsland1", "Fixedsub - Duplicate"),
         ("FishingVillage", "Fixedsub - Duplicate - Duplicate"),
         ("RubberPrisonPier", "Fixedsub - Duplicate - Duplicate"), ("MysteryIsland", "Fixedsub")}


class TestProgressiveBoat(BugFablesTestBase):
    # Progressive Boat on (the default): one item found twice, the Boat Ticket, then the submarine.
    def _first_copy_only(self) -> None:
        self.collect_all_but([BOAT])
        self.collect(self.get_items_by_name(BOAT)[0])

    def test_in_the_pool_twice(self) -> None:
        # Two copies of one item, both progression, and neither level's own item.
        boats = [item for item in self.multiworld.itempool if item.player == self.player and item.name == BOAT]
        self.assertEqual(len(boats), 2)
        self.assertTrue(all(item.advancement for item in boats))
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertEqual([pool.count(TICKET), pool.count(SUBMARINE)], [0, 0])

    def test_metal_island_needs_one_copy(self) -> None:
        self.collect_all_but([BOAT])
        self.assertFalse(self.can_reach_region("MetalIsland1"))
        self.collect(self.get_items_by_name(BOAT)[0])
        self.assertTrue(self.can_reach_region("MetalIsland1"))

    def test_the_lake_needs_the_second_copy(self) -> None:
        # The docks' rule held only the first copy (inside the later chapters' stand-in) before the submarine was one.
        self._first_copy_only()
        self.assertFalse(self.can_reach_region("MetalLake"))
        self.collect(self.get_items_by_name(BOAT)[1])
        self.assertTrue(self.can_reach_region("MetalLake"))

    def test_the_prison_and_the_giants_lair_need_the_submarine(self) -> None:
        # The ant tunnel's prison door opens only once the prison has been reached, by the sub.
        regions = ("RubberPrisonPier", "RubberPrisonGiantLairBridge", "GiantLairEntrance")
        self._first_copy_only()
        for region in regions:
            with self.subTest(region=region):
                self.assertFalse(self.can_reach_region(region))
        self.collect(self.get_items_by_name(BOAT)[1])
        for region in regions:
            with self.subTest(region=region):
                self.assertTrue(self.can_reach_region(region))

    def test_the_throne_room_never_needs_the_submarine(self) -> None:
        # Where the king hands the submarine over.
        self._first_copy_only()
        self.assertTrue(self.can_reach_location(THRONE_ROOM))

    def test_icicle_comes_after_the_submarine(self) -> None:
        self._first_copy_only()
        self.assertFalse(self.can_reach_location(ICICLE))
        self.collect(self.get_items_by_name(BOAT)[1])
        self.assertTrue(self.can_reach_location(ICICLE))

    def test_the_throne_room_is_the_one_check_on_flag_379(self) -> None:
        from ..data_tables import LOCATIONS
        self.assertEqual([loc.name for loc in LOCATIONS if loc.source.flag == 379], [THRONE_ROOM])
        data = self.world.fill_slot_data()
        location = self.world.location_name_to_id[THRONE_ROOM]
        self.assertEqual(data["location_flags"][str(location)], 379)
        self.assertIn(location, data["silent_locations"])

    def test_the_docks_follow_the_submarine(self) -> None:
        data = self.world.fill_slot_data()
        self.assertTrue(data["submarine_item"])
        self.assertEqual({(e["map"], e["entity"]) for e in data["present_with_item"] if e["item"] == 212}, DOCKS)
        self.assertEqual({(e["map"], e["entity"], e["item"]) for e in data["held_until_item"]},
                         {("TermitePier", "FixedScientist", 212), ("TermitePier", "FixedQueen", 212)})
        self.assertIn({"map": "TermiteMainPlaza", "entity": "gate", "flag": 384}, data["held_until"])

    def test_the_termite_gate_opens_from_outside_only(self) -> None:
        # From inside, before it was opened from outside, the gate's scene stops (and the sub can land a party inside).
        plaza = self.multiworld.get_region("TermiteMainPlaza", self.player)
        self.assertFalse(any(exit_.connected_region.name == "TermiteOutside" and "(gate)" in exit_.name
                             for exit_ in plaza.exits))

    def test_hints_find_it_by_what_it_is(self) -> None:
        _assert_hints_find(self, {"submarine": BOAT, "boat": BOAT})


def _assert_hints_find(test: BugFablesTestBase, wanted: dict[str, str]) -> None:
    # !hint takes a group's name, whatever its case, and hints every item in it.
    groups = {name.lower(): items for name, items in test.world.item_name_groups.items()}
    for typed, item in wanted.items():
        with test.subTest(typed=typed):
            test.assertIn(item, groups[typed])
            test.assertIn(item, test.world.item_name_to_id)


class TestSplitBoat(BugFablesTestBase):
    # Progressive Boat off: the Boat Ticket and the submarine are two items, in any order.
    options = {"progressive_boat": False}

    def test_two_items_once_each(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertEqual([pool.count(name) for name in (TICKET, SUBMARINE, BOAT)], [1, 1, 0])

    def test_the_boat_needs_the_ticket(self) -> None:
        self.collect_all_but([TICKET, SUBMARINE])
        self.assertFalse(self.can_reach_region("MetalIsland1"))
        self.collect_by_name([TICKET])
        self.assertTrue(self.can_reach_region("MetalIsland1"))

    def test_the_lake_the_prison_and_the_giants_lair_need_the_submarine(self) -> None:
        regions = ("MetalLake", "RubberPrisonPier", "RubberPrisonGiantLairBridge", "GiantLairEntrance")
        self.collect_all_but([SUBMARINE])
        for region in regions:
            with self.subTest(region=region):
                self.assertFalse(self.can_reach_region(region))
        self.collect_by_name([SUBMARINE])
        for region in regions:
            with self.subTest(region=region):
                self.assertTrue(self.can_reach_region(region))

    def test_the_throne_room_never_needs_the_submarine(self) -> None:
        self.collect_all_but([SUBMARINE])
        self.assertTrue(self.can_reach_location(THRONE_ROOM))

    def test_hints_find_it_by_what_it_is(self) -> None:
        _assert_hints_find(self, {"submarine": SUBMARINE, "boat": TICKET})
