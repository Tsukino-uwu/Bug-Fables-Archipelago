from . import BugFablesTestBase


class TestPermitGate(BugFablesTestBase):
    options = {"starting_party_member": "off"}

    def test_first_artifact_needs_the_permit(self) -> None:
        # With no items the permit's gate must hold, or fill could put the permit behind it.
        self.assertFalse(self.can_reach_location("Artifact 1"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Artifact 1"))

    def test_goal_needs_the_permit(self) -> None:
        self.assertBeatable(False)
        self.collect_by_name("Explorer Permit")
        self.assertBeatable(True)

    def test_outskirts_locations_open_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: Maki and Eetl's Gift"))
        self.assertTrue(self.can_reach_location("Outskirts: Artis's Gift"))

    def test_only_what_play_showed_before_the_gate(self) -> None:
        # Exactly what play reached before the permit; a wrong spot here let a seed lock the permit behind itself.
        reachable = {loc.name for loc in self.multiworld.get_reachable_locations(self.multiworld.state, self.player)
                     if loc.address is not None}
        self.assertEqual(reachable, {"Outskirts: Maki and Eetl's Gift", "Outskirts: Outside the City, Tutorial Battle",
                                     "Outskirts: Artis's Gift",
                                     "Outskirts: Ladybug Siblings' House", "Outskirts: East Road, Boulder",
                                     "Outskirts: Pier, Behind the Dock", "Bugaria City: Residential District, Rooftop",
                                     "Outskirts: Golden Path, Grass by the Dirt Spot",
                                     "Outskirts: Madeleine's House, Table Right",
                                     "Outskirts: Madeleine's House, Table Left"}
                         | {f"Bugaria City: Commercial District, Medal Shop {n}" for n in range(1, 23)}
                         | {f"Bugaria City: Commercial District, Item Shop {n}" for n in range(1, 6)}
                         | {f"Outskirts: Caravan, Item Shop {n}" for n in range(1, 4)})

    def test_reward_near_snakemouth_needs_the_permit(self) -> None:
        self.assertFalse(self.can_reach_location("Outskirts: Near Snakemouth Den, Horn Tutorial"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Near Snakemouth Den, Horn Tutorial"))

    def test_pool_is_the_locations_items(self) -> None:
        # An item whose vanilla spot isn't a location yet stays out of the pool.
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Explorer Permit", pool)
        self.assertNotIn("G-Bug Ranger Plushie", pool)

    def test_pool_matches_locations(self) -> None:
        pool = [item for item in self.multiworld.itempool if item.player == self.player]
        locations = [loc for loc in self.multiworld.get_locations(self.player) if loc.address is not None]
        self.assertEqual(len(pool), len(locations))


class TestArtifactsCapped(BugFablesTestBase):
    # Asking for more artifacts than the world includes must lower the goal, not make the seed unwinnable.
    options = {"artifacts_required": 7}

    def test_goal_lowered_to_what_exists(self) -> None:
        self.assertEqual(self.world.artifacts_required, 1)
        self.assertEqual(self.world.fill_slot_data()["options"]["artifacts_required"], 1)

    def test_still_beatable(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertBeatable(True)


class TestLeif(BugFablesTestBase):
    options = {"starting_party_member": "off"}
    # Rooms with water droplets need Leif to freeze them.
    def test_droplet_room_needs_leif(self) -> None:
        location = self.world.get_location("Snakemouth Den: Underground Door Room, Behind the Wall")
        state = self.state_with("Explorer Permit")
        self.assertFalse(location.can_reach(state))
        self.add(state, "Leif")
        self.assertTrue(location.can_reach(state))

    def test_first_artifact_needs_leif(self) -> None:
        artifact = self.world.get_location("Artifact 1")
        state = self.state_with("Explorer Permit")
        self.assertFalse(artifact.can_reach(state))
        self.add(state, "Leif")
        self.assertTrue(artifact.can_reach(state))

    def test_leif_joins_before_the_droplet_rooms(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Leif Joins"))
        self.assertTrue(self.can_reach_location("Snakemouth Den: Underground Door Room, Behind the Wall"))


class TestInRoomRules(BugFablesTestBase):
    options = {"starting_party_member": "off"}
    # A spot's own needs are written on the location, so entrance rando can't lose them.
    def test_gummies_need_leif_in_the_room(self) -> None:
        location = self.world.get_location("Snakemouth Den: Mushroom Pit, Mushroom by the Droplets")
        state = self.state_with("Explorer Permit")
        self.assertFalse(location.access_rule(state))
        self.add(state, "Leif")
        self.assertTrue(location.access_rule(state))
        self.assertEqual(location.parent_region.name, "SnakemouthMushroomPit")

    def test_pit_medal_needs_nothing_in_the_room(self) -> None:
        # Nothing of its own: only the underground's stand-in (reach), until the room is mapped.
        from ..data_tables import LOCATIONS
        spot = next(loc for loc in LOCATIONS if loc.name == "Snakemouth Den: Mushroom Pit, Mushroom by the Ledge")
        self.assertIsNone(spot.rule)
        location = self.world.get_location(spot.name)
        self.assertTrue(location.access_rule(self.state_with("Explorer Permit", "Leif")))


class TestGoldenPath(BugFablesTestBase):
    # The game makes the door only after the first boss (flag 41); the seed opens it from the start.
    def test_door_present_from_the_start(self) -> None:
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "LoadZoneGoldenPath"},
                      self.world.fill_slot_data()["kept_present"])

    def test_grass_reachable_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: Golden Path, Grass by the Dirt Spot"))


class TestBossPrize(BugFablesTestBase):
    # The first boss's prize is done when its prize slot reaches 3.
    def test_prize_watched_by_its_slot(self) -> None:
        variables = self.world.fill_slot_data()["location_vars"]
        prize = str(self.world.location_name_to_id["Outskirts: Artis's Prize for Snakemouth Den"])
        self.assertEqual(variables[prize], {"var": 13, "at_least": 3})

    def test_prize_needs_the_boss(self) -> None:
        location = self.world.get_location("Outskirts: Artis's Prize for Snakemouth Den")
        state = self.state_with()
        self.assertFalse(location.can_reach(state))
        self.add(state, "Snakemouth Den Cleared")
        self.assertTrue(location.can_reach(state))


class TestChapterTwo(BugFablesTestBase):
    # The library needs flag 67, whose palace scene needs the companion who joins after the first boss.
    def test_library_needs_the_city_and_chapter_two(self) -> None:
        library = self.world.get_location("Ant Palace: Library, Behind the Bookshelf")
        state = self.state_with()
        self.assertFalse(library.can_reach(state))
        self.add(state, "Chapter 2 Started")
        self.assertTrue(library.can_reach(state))

    def test_the_city_is_open_from_the_start(self) -> None:
        self.assertTrue(self.multiworld.get_region("BugariaMainPlaza", self.player).can_reach(self.state_with()))

    def test_chapter_two_needs_the_first_boss(self) -> None:
        start = self.world.get_location("Chapter 2 Start")
        state = self.state_with()
        self.assertFalse(start.can_reach(state))
        self.add(state, "Snakemouth Den Cleared")
        self.assertTrue(start.can_reach(state))


class TestMidQuestItem(BugFablesTestBase):
    # Mid-quest items are shuffled, so the delivery's reward needs the old book.
    def test_reward_needs_the_quest_book(self) -> None:
        reward = self.world.get_location("Bugaria City: Residential District, Old Book Delivery Reward 1")
        state = self.state_with("Chapter 2 Started")
        self.assertFalse(reward.can_reach(state))
        # The sweep takes it to the library step.
        state.collect(self.world.create_item("Quest Book"), prevent_sweep=False)
        self.assertTrue(reward.can_reach(state))


class TestOldBookChain(BugFablesTestBase):
    # The reward waits for the library step, not just the book, so a room-level world can't skip the library.
    def test_reward_needs_the_library_delivery(self) -> None:
        reward = self.world.get_location("Bugaria City: Residential District, Old Book Delivery Reward 1")
        self.assertTrue(reward.can_reach(self.state_with("Chapter 2 Started", "Old Book Delivered")))
        self.assertEqual(self.world.get_location("Old Book Delivered").parent_region.name, "AntPalaceLibrary")


class TestClassifications(BugFablesTestBase):
    # Progression exactly when a rule needs it: too few locks items behind themselves, too many skews fill. Read from
    # the resolved rules (Archipelago's item_dependencies) in a seed where every member, move and Jump is an item and
    # every category is in, so each rule names everything it can ever need.
    options = {"starting_party_member": "vi", "shuffle_field_moves": True, "shuffle_jump": True,
               "shuffle_discoveries": True}

    def test_items_rules_use_are_progression_and_only_those(self) -> None:
        from ..data_tables import ITEMS, STORY_EVENTS
        from ..items import own_copies
        used: set[str] = set()
        spots = (*self.multiworld.get_locations(self.player), *self.multiworld.get_entrances(self.player))
        for spot in spots:
            if hasattr(spot.access_rule, "item_dependencies"):
                used.update(spot.access_rule.item_dependencies())
        # Leif is both: the story's event with Starting Party Member off, an item with it on.
        event_items = {event.item for event in STORY_EVENTS} - {item.name for item in ITEMS} | {"Artifact"}
        real_items_used = used - event_items
        for item in ITEMS:
            if item.always and not own_copies(self.world, item.name):
                continue  # the boat's items of the other Progressive Boat value (TestClassificationsSplitBoat)
            with self.subTest(item=item.name):
                if item.name in real_items_used:
                    self.assertEqual(item.classification, "progression")
                else:
                    self.assertNotEqual(item.classification, "progression")


class TestClassificationsSplitBoat(TestClassifications):
    options = {**TestClassifications.options, "progressive_boat": False}


class TestTownMedal(BugFablesTestBase):
    options = {"starting_party_member": "off"}
    # The Bug Me Not! medal needs Leif's ice; the town itself is open.
    def test_needs_leif(self) -> None:
        name = "Bugaria City: Residential District, Fountain Rooftop"
        self.assertFalse(self.can_reach_location(name))
        self.collect_by_name(["Explorer Permit", "Leif"])
        self.assertTrue(self.can_reach_location(name))
