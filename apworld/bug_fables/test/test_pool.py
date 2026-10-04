from . import BugFablesTestBase


class TestMedals(BugFablesTestBase):
    # Medal ids overlap item ids in the game, so an id collision would give the wrong thing.
    def test_medals_have_their_own_ids(self) -> None:
        from ..data_tables import ITEM_ID_BASE, MEDAL_ID_OFFSET
        self.assertEqual(self.world.item_name_to_id["Poison Defender"], ITEM_ID_BASE + MEDAL_ID_OFFSET + 9)
        self.assertEqual(self.world.item_name_to_id["Hard Mode"], ITEM_ID_BASE + MEDAL_ID_OFFSET + 11)
        kinds = self.world.fill_slot_data()["item_kinds"]
        self.assertEqual(kinds[str(self.world.item_name_to_id["Poison Defender"])], 2)

    def test_filler_that_isnt_padding_is_in_the_pool_once(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertEqual(pool.count("Hard Mode"), 1)
        self.assertEqual(pool.count("Poison Defender"), 1)
        padding = {self.world.get_filler_item_name() for _ in range(50)}
        self.assertEqual(padding, {"Crunchy Leaf"})


class TestPool(BugFablesTestBase):
    # An item at two spots is in the pool twice; one missing from items.json would be lost.
    def test_every_location_item_is_known(self) -> None:
        from ..data_tables import LOCATIONS, vanilla_item
        for loc in LOCATIONS:
            if loc.source.give or loc.source.pickup:
                with self.subTest(location=loc.name):
                    self.assertIsNotNone(vanilla_item(loc))

    def test_each_location_puts_its_item_in_the_pool(self) -> None:
        # Every location's item is in the pool once per location holding it, except the copies the mod's own items
        # (the Progressive Boat's two) take when the pool is full: one duplicated filler copy each (TestSmallPool: a
        # last copy).
        from ..data_tables import ITEMS, vanilla_item
        from ..items import own_copies
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        own = sum(own_copies(self.world, item.name) for item in ITEMS)
        short = 0
        included = self.world.included_locations
        for name in {vanilla_item(loc) for loc in included} - {None}:
            expected = sum(1 for loc in included if vanilla_item(loc) == name)
            with self.subTest(item=name):
                self.assertGreaterEqual(pool.count(name), 1)
                short += max(0, expected - pool.count(name))
        self.assertLessEqual(short, own)


class TestBerries(BugFablesTestBase):
    # Each berry reward puts its own amount in the pool.
    def test_reward_near_snakemouth_puts_its_berries_in_the_pool(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("10 Berries", pool)
        gives = self.world.fill_slot_data()["location_gives"]
        reward = str(self.world.location_name_to_id["Outskirts: Near Snakemouth Den, Reward"])
        self.assertEqual(gives[reward], {"map": "NearSnakemouth", "type": -1, "item": 10})

    def test_berries_have_their_own_ids(self) -> None:
        from ..data_tables import ITEM_ID_BASE, MONEY_ID_OFFSET
        self.assertEqual(self.world.item_name_to_id["10 Berries"], ITEM_ID_BASE + MONEY_ID_OFFSET + 10)
        kinds = self.world.fill_slot_data()["item_kinds"]
        self.assertEqual(kinds[str(self.world.item_name_to_id["10 Berries"])], 3)


class TestSmallPool(BugFablesTestBase):
    # With this few locations the move items outnumber the duplicate filler copies.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "starting_party_member": "vi",
               "shuffle_quests": False, "shuffle_crystal_berries": False, "shuffle_discoveries": False,
               "shuffle_medal_shops": False, "shuffle_item_shops": False}

    def test_only_ordinary_filler_gives_way(self) -> None:
        from ..data_tables import ITEM_KIND, MONEY_KIND, ITEMS, vanilla_item
        from ..items import ITEMS_BY_NAME, own_copies
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        for item in ITEMS:
            if item.always:
                self.assertEqual(pool.count(item.name), own_copies(self.world, item.name))
        for name in {vanilla_item(loc) for loc in self.world.included_locations} - {None}:
            data = ITEMS_BY_NAME[name]
            if data.classification != "filler" or data.kind not in (ITEM_KIND, MONEY_KIND):
                expected = sum(1 for loc in self.world.included_locations if vanilla_item(loc) == name)
                with self.subTest(item=name):
                    self.assertEqual(pool.count(name), expected)
