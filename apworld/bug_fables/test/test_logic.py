from . import BugFablesTestBase


class TestPermitGate(BugFablesTestBase):
    def test_first_artifact_needs_the_permit(self) -> None:
        # The gate is the Explorer Permit's whole purpose: if this passes with no items,
        # the rule is attached to nothing and fill could put the permit behind its own gate.
        self.assertFalse(self.can_reach_location("Artifact 1"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Artifact 1"))

    def test_goal_needs_the_permit(self) -> None:
        self.assertBeatable(False)
        self.collect_by_name("Explorer Permit")
        self.assertBeatable(True)

    def test_outskirts_locations_open_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: Maki and Eetl's Gift"))
        self.assertTrue(self.can_reach_location("Outskirts: Favor Reward"))
        self.assertTrue(self.can_reach_location("Outskirts: Artis's Gift"))

    def test_every_non_filler_item_is_in_the_pool(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Explorer Permit", pool)
        self.assertIn("G-Bug Ranger Plushie", pool)

    def test_pool_matches_locations(self) -> None:
        pool = [item for item in self.multiworld.itempool if item.player == self.player]
        locations = [loc for loc in self.multiworld.get_locations(self.player) if loc.address is not None]
        self.assertEqual(len(pool), len(locations))


class TestArtifactsCapped(BugFablesTestBase):
    # Asking for more artifacts than the world includes must lower the goal, not make the seed unwinnable.
    options = {"artifacts_required": 7}

    def test_goal_lowered_to_what_exists(self) -> None:
        self.assertEqual(self.world.artifacts_required, 1)
        self.assertEqual(self.world.fill_slot_data()["artifacts_required"], 1)

    def test_still_beatable(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertBeatable(True)


class TestSlotData(BugFablesTestBase):
    # The client watches exactly the flags in location_flags and sends those checks. A location missing here
    # could never be sent; a flag that isn't the location's own would send the wrong check.
    def test_every_location_has_its_flag(self) -> None:
        data = self.world.fill_slot_data()
        flags, variables = data["location_flags"], data["location_vars"]
        ids = {str(loc.address) for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        # Each location is watched one way or the other, never both, never neither.
        self.assertEqual(set(flags) | set(variables), ids)
        self.assertFalse(set(flags) & set(variables))
        self.assertEqual(flags[str(self.world.location_name_to_id["Outskirts: Maki and Eetl's Gift"])], 15)
        self.assertEqual(flags[str(self.world.location_name_to_id["Outskirts: Artis's Gift"])], 32)

    def test_world_version_is_the_manifest_one(self) -> None:
        import json
        import pkgutil
        manifest = json.loads(pkgutil.get_data("worlds.bug_fables", "archipelago.json").decode("utf-8"))
        self.assertEqual(self.world.fill_slot_data()["world_version"], manifest["world_version"])

    def test_gives_name_the_vanilla_item(self) -> None:
        # The client suppresses exactly the giveitem named here. A wrong one would let the vanilla item through,
        # or swallow an unrelated grant on the same map.
        gives = self.world.fill_slot_data()["location_gives"]
        medal = gives[str(self.world.location_name_to_id["Outskirts: Artis's Gift"])]
        self.assertEqual(medal, {"map": "BugariaOutskirtsOutsideCity", "type": 2, "item": 11})
        permit = gives[str(self.world.location_name_to_id["Outskirts: Maki and Eetl's Gift"])]
        self.assertEqual(permit, {"map": "BugariaOutskirtsOutsideCity", "type": 1, "item": 27})

    def test_item_kinds_cover_every_item(self) -> None:
        kinds = self.world.fill_slot_data()["item_kinds"]
        self.assertEqual(set(kinds), {str(i) for i in self.world.item_name_to_id.values()})
        self.assertEqual(kinds[str(self.world.item_name_to_id["Explorer Permit"])], 1)


class TestPickups(BugFablesTestBase):
    # The client knows a pickup location only by its map and its own activationflag. A missing entry would give
    # the vanilla item at that spot; a wrong flag would swap an unrelated pickup.
    def test_pickups_are_in_slot_data(self) -> None:
        pickups = self.world.fill_slot_data()["location_pickups"]
        medal = str(self.world.location_name_to_id["Snakemouth Den: Underground Door Room"])
        self.assertEqual(pickups[medal], {"map": "SnakemouthUndergrondDoor", "flag": 60})

    def test_pickups_are_not_gives(self) -> None:
        # A location is one or the other: the client would otherwise try to swap it twice.
        data = self.world.fill_slot_data()
        self.assertFalse(set(data["location_pickups"]) & set(data["location_gives"]))

    def test_pickup_flag_is_its_location_flag(self) -> None:
        data = self.world.fill_slot_data()
        for location, pickup in data["location_pickups"].items():
            self.assertEqual(data["location_flags"][location], pickup["flag"])


class TestMedals(BugFablesTestBase):
    # Medal ids overlap item ids in the game, so an id collision would give the wrong thing.
    def test_medals_have_their_own_ids(self) -> None:
        from ..data_tables import ITEM_ID_BASE, MEDAL_ID_OFFSET
        self.assertEqual(self.world.item_name_to_id["Poison Defender"], ITEM_ID_BASE + MEDAL_ID_OFFSET + 9)
        self.assertEqual(self.world.item_name_to_id["Hard Mode"], ITEM_ID_BASE + MEDAL_ID_OFFSET + 11)
        kinds = self.world.fill_slot_data()["item_kinds"]
        self.assertEqual(kinds[str(self.world.item_name_to_id["Poison Defender"])], 2)

    def test_filler_that_isnt_padding_is_in_the_pool_once(self) -> None:
        # The Hard Mode medal is filler, but a real item: exactly one copy, never used to pad the pool.
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertEqual(pool.count("Hard Mode"), 1)
        self.assertEqual(pool.count("Poison Defender"), 1)
        padding = {self.world.get_filler_item_name() for _ in range(50)}
        self.assertEqual(padding, {"Crunchy Leaf"})


class TestPool(BugFablesTestBase):
    # The pool is each location's own vanilla item, so an item found at two spots is in it twice. A location whose
    # vanilla item isn't in items.json would silently lose that item from the game.
    def test_every_location_item_is_known(self) -> None:
        from ..data_tables import LOCATIONS, vanilla_item
        for loc in LOCATIONS:
            if "give" in loc["source"] or "pickup" in loc["source"]:
                with self.subTest(location=loc["name"]):
                    self.assertIsNotNone(vanilla_item(loc))

    def test_each_location_puts_its_item_in_the_pool(self) -> None:
        from ..data_tables import LOCATIONS, vanilla_item
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        for name in {vanilla_item(loc) for loc in LOCATIONS} - {None}:
            expected = sum(1 for loc in LOCATIONS if vanilla_item(loc) == name)
            with self.subTest(item=name):
                self.assertGreaterEqual(pool.count(name), expected)


class TestLeif(BugFablesTestBase):
    # Rooms with water droplets need Leif to freeze them (the user, 2026-09-24). Without the rule, fill could put
    # something there that the player can't reach before Leif joins.
    def test_droplet_room_needs_leif(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        location = self.world.get_location("Snakemouth Den: Underground Door Room")
        state = CollectionState(self.multiworld)
        state.collect(self.world.create_item("Explorer Permit"), prevent_sweep=True)
        self.assertFalse(location.can_reach(state))
        state.collect(BugFablesItem("Leif", ItemClassification.progression, None, self.player), prevent_sweep=True)
        self.assertTrue(location.can_reach(state))

    def test_first_artifact_needs_leif(self) -> None:
        # The first boss is in the treasure room, reached only through rooms with droplets.
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        artifact = self.world.get_location("Artifact 1")
        state = CollectionState(self.multiworld)
        state.collect(self.world.create_item("Explorer Permit"), prevent_sweep=True)
        self.assertFalse(artifact.can_reach(state))
        state.collect(BugFablesItem("Leif", ItemClassification.progression, None, self.player), prevent_sweep=True)
        self.assertTrue(artifact.can_reach(state))

    def test_leif_joins_before_the_droplet_rooms(self) -> None:
        # Leif's event is reachable with only what the Snakemouth Den needs, so the seed stays completable.
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Leif Joins"))
        self.assertTrue(self.can_reach_location("Snakemouth Den: Underground Door Room"))


class TestInRoomRules(BugFablesTestBase):
    # What a spot needs once you're in its room is written on the location itself, even when the region already
    # implies it, so entrance rando can change how a room is reached without losing it (the user, 2026-09-24).
    def test_gummies_need_leif_in_the_room(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        location = self.world.get_location("Snakemouth Den: Mushroom Pit, Droplets")
        region = location.parent_region
        state = CollectionState(self.multiworld)
        state.collect(self.world.create_item("Explorer Permit"), prevent_sweep=True)
        # Pretend the room was reached another way: the location's own rule must still ask for Leif.
        self.assertFalse(location.access_rule(state))
        state.collect(BugFablesItem("Leif", ItemClassification.progression, None, self.player), prevent_sweep=True)
        self.assertTrue(location.access_rule(state))
        self.assertEqual(region.name, "Snakemouth Den Underground")

    def test_pit_medal_needs_nothing_in_the_room(self) -> None:
        from BaseClasses import CollectionState
        location = self.world.get_location("Snakemouth Den: Mushroom Pit, Floor")
        self.assertTrue(location.access_rule(CollectionState(self.multiworld)))


class TestLostKid(BugFablesTestBase):
    # The lost kid at the lake only appears after the first boss, and his cutscene moves all three party members, so
    # it needs Leif (EventControl.Event31). Without the rule, fill could put progression there that isn't reachable.
    def test_reward_needs_the_first_boss_and_leif(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        location = self.world.get_location("Snakemouth Den: Lake, Ladybug Kid's Reward")
        state = CollectionState(self.multiworld)
        state.collect(self.world.create_item("Explorer Permit"), prevent_sweep=True)
        state.collect(BugFablesItem("Leif", ItemClassification.progression, None, self.player), prevent_sweep=True)
        self.assertFalse(location.can_reach(state))
        state.collect(BugFablesItem("Snakemouth Den Cleared", ItemClassification.progression, None, self.player),
                      prevent_sweep=True)
        self.assertTrue(location.can_reach(state))


class TestQuestsOff(BugFablesTestBase):
    # With quests off, a quest's reward isn't a location and its item isn't in the pool: the game hands it out as
    # usual. Were it still in slot_data, the client would swap the reward for something the seed never placed.
    options = {"shuffle_quests": False}

    def test_quest_locations_left_out(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Snakemouth Den: Lake, Ladybug Kid's Reward", names)
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertNotIn("Lore Book", pool)
        gives = self.world.fill_slot_data()["location_gives"]
        self.assertNotIn(str(self.world.location_name_to_id["Snakemouth Den: Lake, Ladybug Kid's Reward"]), gives)


class TestQuestsOnByDefault(BugFablesTestBase):
    def test_quest_location_included(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertIn("Snakemouth Den: Lake, Ladybug Kid's Reward", names)


class TestStoryPickup(BugFablesTestBase):
    # A story pickup has no flag of its own: the client knows it by the story event picking it up starts. Without it
    # in slot_data, the vanilla item would be handed out and the seed's item lost.
    def test_story_pickup_known_by_its_event(self) -> None:
        pickups = self.world.fill_slot_data()["location_pickups"]
        trapdoor = str(self.world.location_name_to_id["Snakemouth Den: Door Room, Trapdoor"])
        self.assertEqual(pickups[trapdoor], {"map": "SnakemouthDoorRoom", "flag": 14, "event": 5})

    def test_ordinary_pickups_have_no_event(self) -> None:
        pickups = self.world.fill_slot_data()["location_pickups"]
        medal = str(self.world.location_name_to_id["Snakemouth Den: Underground Door Room"])
        self.assertNotIn("event", pickups[medal])


class TestGoldenPath(BugFablesTestBase):
    # The Golden Path's door opens with the first boss (flag 41). Without the rule, fill could put the permit there.
    def test_golden_path_needs_the_first_boss(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        location = self.world.get_location("Outskirts: Golden Path, Grass")
        state = CollectionState(self.multiworld)
        self.assertFalse(location.can_reach(state))
        state.collect(BugFablesItem("Snakemouth Den Cleared", ItemClassification.progression, None, self.player),
                      prevent_sweep=True)
        self.assertTrue(location.can_reach(state))


class TestKeptOpen(BugFablesTestBase):
    # Eetl's blocker closes the way back to Snakemouth Den after the first boss; the logic assumes the den stays
    # reachable, so the client must be told to keep it out of the way.
    def test_eetls_blocker_is_kept_open(self) -> None:
        kept = self.world.fill_slot_data()["kept_open"]
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "eetlblocker1 - Duplicate"}, kept)


class TestBossPrize(BugFablesTestBase):
    # The first boss's prize is handed over by Artis; the client knows it's done when its prize slot reaches 3.
    def test_prize_watched_by_its_slot(self) -> None:
        variables = self.world.fill_slot_data()["location_vars"]
        prize = str(self.world.location_name_to_id["Outskirts: Artis's Prize for Snakemouth Den"])
        self.assertEqual(variables[prize], {"var": 13, "at_least": 3})

    def test_prize_needs_the_boss(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        location = self.world.get_location("Outskirts: Artis's Prize for Snakemouth Den")
        state = CollectionState(self.multiworld)
        self.assertFalse(location.can_reach(state))
        state.collect(BugFablesItem("Snakemouth Den Cleared", ItemClassification.progression, None, self.player),
                      prevent_sweep=True)
        self.assertTrue(location.can_reach(state))
