from . import BugFablesTestBase


class TestQuestsOff(BugFablesTestBase):
    # With quests off, a quest reward must stay out of slot_data, or the client would swap it.
    options = {"shuffle_quests": False}

    def test_quest_locations_left_out(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn(QUEST_REWARD, names)
        from ..data_tables import vanilla_item
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        expected = sum(1 for loc in self.world.included_locations if vanilla_item(loc) == "Lore Book")
        self.assertEqual(pool.count("Lore Book"), expected)
        gives = self.world.fill_slot_data()["location_gives"]
        self.assertNotIn(str(self.world.location_name_to_id[QUEST_REWARD]), gives)


QUEST_REWARD = "Bugaria City: Residential District, Old Book Delivery Reward"
LOST_KID = "Snakemouth Den: Lake, Ladybug Kid's Reward"


class TestQuestsOnByDefault(BugFablesTestBase):
    def test_quest_location_included(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertIn(QUEST_REWARD, names)


class TestPendingQuest(BugFablesTestBase):
    # The lost kid's quest joins the board only in chapter 5 in the game; until it's opened from the start (build step
    # 44), its reward is out of every seed and stays vanilla.
    def test_lost_kid_left_out(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn(LOST_KID, names)
        gives = self.world.fill_slot_data()["location_gives"]
        self.assertNotIn(str(self.world.location_name_to_id[LOST_KID]), gives)


class TestMidQuestItemQuestsOff(BugFablesTestBase):
    # With quests off the whole quest stays vanilla.
    options = {"shuffle_quests": False}

    def test_quest_book_not_in_pool(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertNotIn("Quest Book", pool)

    def test_quest_steps_left_out(self) -> None:
        # A step event that needs the book would be unreachable without it.
        with self.assertRaises(KeyError):
            self.world.get_location("Old Book Delivered")


class TestCrystalBerries(BugFablesTestBase):
    # A crystal berry spot is known by its index, not a flag; its item is the one Crystal Berry item.
    def test_berry_zero_known_by_its_index(self) -> None:
        data = self.world.fill_slot_data()
        berry = str(self.world.location_name_to_id["Outskirts: Snakemouth Den Entrance, by the Cave"])
        self.assertEqual(data["location_berries"][berry], 0)
        self.assertEqual(data["location_pickups"][berry]["berry"], 0)
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Crystal Berry", pool)

    def test_berry_zero_needs_the_permit(self) -> None:
        self.assertFalse(self.can_reach_location("Outskirts: Snakemouth Den Entrance, by the Cave"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Snakemouth Den Entrance, by the Cave"))


class TestCrystalBerriesOff(BugFablesTestBase):
    # With crystal berries off, the game hands them out as usual.
    options = {"shuffle_crystal_berries": False}

    def test_berries_left_out(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Outskirts: Snakemouth Den Entrance, by the Cave", names)
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertNotIn("Crystal Berry", pool)
        self.assertEqual(self.world.fill_slot_data()["location_berries"], {})


class TestDiscoveriesOffByDefault(BugFablesTestBase):
    # Shuffle Discoveries is opt-in: by default no discovery is a location and the client watches none.
    def test_no_discovery_locations(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Outskirts: Pier, Statue", names)
        self.assertEqual(self.world.fill_slot_data()["location_discoveries"], {})


class TestDiscoveriesOn(BugFablesTestBase):
    options = {"shuffle_discoveries": True}

    def test_pier_statue_is_discovery_49(self) -> None:
        pier = str(self.world.location_name_to_id["Outskirts: Pier, Statue"])
        self.assertEqual(self.world.fill_slot_data()["location_discoveries"][pier], 49)

    def test_pier_statue_open_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: Pier, Statue"))

    def test_snakemouth_arrival_needs_the_permit(self) -> None:
        self.assertFalse(self.can_reach_location("Outskirts: Snakemouth Den Entrance, Arrival"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Snakemouth Den Entrance, Arrival"))
