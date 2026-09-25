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
        self.assertTrue(self.can_reach_location("Outskirts: Artis's Gift"))

    def test_only_what_play_showed_before_the_gate(self) -> None:
        # Before the permit, the user reached exactly these (2026-09-25): Maki and Eetl's gift and Artis's gift, then,
        # with the seed's rocks removed, the ladybug siblings' house, the stone on the east road and the pier's crystal
        # berry, and, with the town open from the start, the Bad Book in the residential district. A spot claimed open
        # here that isn't (the 10-berry reward near Snakemouth, past the gate) let a seed put
        # the permit behind its own gate: the user's impossible seed.
        reachable = {loc.name for loc in self.multiworld.get_reachable_locations(self.multiworld.state, self.player)
                     if loc.address is not None}
        self.assertEqual(reachable, {"Outskirts: Maki and Eetl's Gift", "Outskirts: Artis's Gift",
                                     "Outskirts: Ladybug Siblings' House", "Outskirts: East Road, Stone",
                                     "Outskirts: Pier", "Bugaria City: Residential District, Rooftop",
                                     "Outskirts: Madeleine's House, Floor", "Outskirts: Madeleine's House, Shelf"}
                         | {f"Bugaria City: Commercial District, Medal Shop {n}" for n in range(1, 11)})

    def test_reward_near_snakemouth_needs_the_permit(self) -> None:
        self.assertFalse(self.can_reach_location("Outskirts: Near Snakemouth Den, Reward"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Near Snakemouth Den, Reward"))

    def test_pool_is_the_locations_items(self) -> None:
        # The permit's vanilla spot is a location, so it's in the pool; the plushie's (the theater) isn't yet, so
        # the game hands it out there and it stays out of the pool.
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
        self.assertEqual(self.world.fill_slot_data()["artifacts_required"], 1)

    def test_still_beatable(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertBeatable(True)


class TestSlotData(BugFablesTestBase):
    # The client watches exactly the flags in location_flags and sends those checks. A location missing here
    # could never be sent; a flag that isn't the location's own would send the wrong check.
    def test_every_location_has_its_flag(self) -> None:
        data = self.world.fill_slot_data()
        flags, variables, berries = data["location_flags"], data["location_vars"], data["location_berries"]
        discoveries = data["location_discoveries"]
        shops = data["location_shops"]
        respawns = {loc for loc, pickup in data["location_pickups"].items() if "regional" in pickup}
        ids = {str(loc.address) for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        # Each location is watched exactly one way: a flag, a number slot, a crystal berry's index, a journal
        # discovery, or (a respawning pickup) the pickup itself.
        self.assertEqual(set(flags) | set(variables) | set(berries) | set(discoveries) | set(shops) | respawns, ids)
        self.assertEqual(len(flags) + len(variables) + len(berries) + len(discoveries) + len(shops) + len(respawns), len(ids))
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
            if "berry" in pickup or "regional" in pickup:
                continue  # a crystal berry is known by its index, a respawning pickup by its regional flag
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
        # The quest's Lore Book is out; Lore Books from other (non-quest) locations stay, one each.
        from ..data_tables import vanilla_item
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        expected = sum(1 for loc in self.world.included_locations if vanilla_item(loc) == "Lore Book")
        self.assertEqual(pool.count("Lore Book"), expected)
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


class TestChapterTwo(BugFablesTestBase):
    # The Ant Palace library is behind chapter 2's start (flag 67), whose palace scene needs the companion who joins
    # after the first boss. The city itself is open from the start (the user, 2026-09-25).
    def test_library_needs_the_city_and_chapter_two(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem

        def event(name: str) -> BugFablesItem:
            return BugFablesItem(name, ItemClassification.progression, None, self.player)

        library = self.world.get_location("Ant Palace: Library, Bookshelf")
        state = CollectionState(self.multiworld)
        self.assertFalse(library.can_reach(state))
        state.collect(event("Chapter 2 Started"), prevent_sweep=True)
        self.assertTrue(library.can_reach(state))

    def test_the_city_is_open_from_the_start(self) -> None:
        from BaseClasses import CollectionState
        self.assertTrue(self.multiworld.get_region("Bugaria City", self.player).can_reach(CollectionState(self.multiworld)))

    def test_chapter_two_needs_the_first_boss(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        start = self.world.get_location("Chapter 2 Start")
        state = CollectionState(self.multiworld)
        self.assertFalse(start.can_reach(state))
        state.collect(BugFablesItem("Snakemouth Den Cleared", ItemClassification.progression, None, self.player),
                      prevent_sweep=True)
        self.assertTrue(start.can_reach(state))


class TestMidQuestItem(BugFablesTestBase):
    # Mid-quest items are shuffled (the user, 2026-09-24): the old book can be anywhere, so the delivery's reward
    # needs it. Without the rule, the reward could hold something needed to reach the book.
    def test_reward_needs_the_quest_book(self) -> None:
        from BaseClasses import CollectionState, ItemClassification
        from ..world import BugFablesItem
        reward = self.world.get_location("Bugaria City: Residential District, Old Book Delivery Reward")
        state = CollectionState(self.multiworld)
        for name in ("Chapter 2 Started",):
            state.collect(BugFablesItem(name, ItemClassification.progression, None, self.player), prevent_sweep=True)
        self.assertFalse(reward.can_reach(state))
        state.collect(self.world.create_item("Quest Book"), prevent_sweep=True)
        self.assertTrue(reward.can_reach(state))


class TestMidQuestItemQuestsOff(BugFablesTestBase):
    # With quests off the whole quest stays vanilla: its start and reward aren't locations, and the old book stays
    # out of the pool (the game hands it out as usual).
    options = {"shuffle_quests": False}

    def test_quest_book_not_in_pool(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertNotIn("Quest Book", pool)


class TestClassifications(BugFablesTestBase):
    # An item is progression exactly when a rule needs it, even for one check (the user, 2026-09-24). Too few and
    # fill can lock an item behind itself; too many and it skews placement and playthroughs.
    def test_items_rules_use_are_progression_and_only_those(self) -> None:
        from ..data_tables import ITEMS, LOCATIONS, REGIONS, STORY_EVENTS
        used: set[str] = set()
        for region in REGIONS:
            for exit_data in region["exits"]:
                used.update(exit_data.get("requires", []))
        for spot in LOCATIONS + STORY_EVENTS:
            used.update(spot.get("requires", []))
        event_items = {event["item"] for event in STORY_EVENTS} | {"Artifact"}
        real_items_used = used - event_items
        for item in ITEMS:
            with self.subTest(item=item["name"]):
                if item["name"] in real_items_used:
                    self.assertEqual(item["classification"], "progression")
                else:
                    self.assertNotEqual(item["classification"], "progression")


class TestBerries(BugFablesTestBase):
    # Berry rewards are locations and berries are items (the user, 2026-09-24): each berry reward puts its own
    # amount in the pool, so a seed holds as much money as the game gives out.
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


class TestCrystalBerries(BugFablesTestBase):
    # A crystal berry spot is known by its index, not a flag; the client needs it both to recognise the pickup and to
    # see the check done. Its vanilla item is the one Crystal Berry item.
    def test_berry_zero_known_by_its_index(self) -> None:
        data = self.world.fill_slot_data()
        berry = str(self.world.location_name_to_id["Outskirts: Snakemouth Den Entrance"])
        self.assertEqual(data["location_berries"][berry], 0)
        self.assertEqual(data["location_pickups"][berry]["berry"], 0)
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Crystal Berry", pool)

    def test_berry_zero_needs_the_permit(self) -> None:
        self.assertFalse(self.can_reach_location("Outskirts: Snakemouth Den Entrance"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Snakemouth Den Entrance"))


class TestCrystalBerriesOff(BugFablesTestBase):
    # With crystal berries off their spots aren't locations, the item stays out of the pool, and the client isn't
    # told about them, so the game hands them out as usual.
    options = {"shuffle_crystal_berries": False}

    def test_berries_left_out(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Outskirts: Snakemouth Den Entrance", names)
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertNotIn("Crystal Berry", pool)
        self.assertEqual(self.world.fill_slot_data()["location_berries"], {})


class TestRespawningPickups(BugFablesTestBase):
    # A respawning pickup has no flag of its own: the client knows it by its map and regional flag, and sends the
    # check itself at the first pickup. Without "regional" it would never recognise the pickup and hand out the
    # vanilla item every time; with a flag entry it would wait for a flag the game never sets.
    def test_known_by_regional_flag(self) -> None:
        data = self.world.fill_slot_data()
        spot = str(self.world.location_name_to_id["Snakemouth Den: Underground Bridge Room, Behind Pillar"])
        self.assertEqual(data["location_pickups"][spot], {"map": "SnakemouthUndergroundRightB", "flag": -1, "regional": 28})
        self.assertNotIn(spot, data["location_flags"])

    def test_vanilla_item_in_pool(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Honey Drop", pool)

    def test_needs_the_underground(self) -> None:
        self.assertFalse(self.can_reach_location("Snakemouth Den: Underground Bridge Room, Behind Pillar"))
        self.collect_by_name(["Explorer Permit", "Leif"])
        self.assertTrue(self.can_reach_location("Snakemouth Den: Underground Bridge Room, Behind Pillar"))


class TestKeptPresent(BugFablesTestBase):
    # The trapdoor into the fall room must never be a dead end, and the big door to Upper Snakemouth stays open (the
    # Peculiar Gem slot behind it is the real gate). Without these entries the client leaves them to the story (flag
    # 41), and the chapter 1 blocker in the fall room still turns the party back.
    def test_ways_back_are_present(self) -> None:
        present = self.world.fill_slot_data()["kept_present"]
        self.assertIn({"map": "SnakemouthFallRoom", "entity": "JumpShroom"}, present)
        self.assertIn({"map": "SnakemouthFallRoom", "entity": "LoadingZoneDoorRoom"}, present)
        self.assertIn({"map": "SnakemouthDoorRoom", "entity": "DoorLoadZone"}, present)

    def test_way_back_down_from_the_trapdoor(self) -> None:
        # Going up the kept mushroom before the spider fight left no way back down (the user, 2026-09-25): the door room's
        # door to the fall room exists from the trapdoor (flag 14), not from the first boss.
        self.assertIn({"map": "SnakemouthDoorRoom", "entity": "LoadZoneFallRoom", "flag": 14},
                      self.world.fill_slot_data()["present_from"])

    def test_fall_room_blocker_is_kept_open(self) -> None:
        self.assertIn({"map": "SnakemouthFallRoom", "entity": "blocker"}, self.world.fill_slot_data()["kept_open"])


class TestOutskirtsRocks(BugFablesTestBase):
    # The rocks that cut the Outskirts off until the first boss are removed from the start, and the town's first-entry
    # scene they used to guard waits for the first boss instead (the user, 2026-09-25: rocks and house now, town later).
    # Without the hold, removing the rocks would let the chapter 2 city scene start in chapter 1.
    def test_rocks_are_removed(self) -> None:
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "Base/BlockingRocks"},
                      self.world.fill_slot_data()["scenery_hidden"])

    def test_town_open_from_the_start(self) -> None:
        # The arrival scene (Event60) is removed and the real door kept, so the city is a plain door from the start (the
        # user, 2026-09-25); the palace scene that starts chapter 2 waits for the companion who joins after the boss.
        data = self.world.fill_slot_data()
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "DoorBugaria - Duplicate"}, data["kept_open"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "DoorBugaria"}, data["kept_present"])
        self.assertIn({"map": "AntPalace1", "entity": "Chapter1StartEvent", "flag": 114}, data["held_until"])

    def test_plaza_blockers_removed(self) -> None:
        # The plaza's three blockers kept the party in the plaza until chapter 2 (the user, 2026-09-25: open the town).
        kept = self.world.fill_slot_data()["kept_open"]
        for entity in ("MM", "blockereetl2", "blockereetl2 - Duplicate"):
            with self.subTest(entity=entity):
                self.assertIn({"map": "BugariaMainPlaza", "entity": entity}, kept)
        data = self.world.fill_slot_data()
        for entity in ("LoadingZoneCommercial", "LoadingZoneResidential", "loadingzone theater"):
            with self.subTest(entity=entity):
                self.assertIn({"map": "BugariaMainPlaza", "entity": entity}, data["kept_present"])
        self.assertIn({"map": "BugariaMainPlaza", "entity": "Cube"}, data["scenery_hidden"])

    def test_boat_waits_for_leif(self) -> None:
        # The boat scene seats three; with the rocks gone a two-member party reached it and the scene threw (the user,
        # 2026-09-25). The sailor waits for Leif's joining flag.
        self.assertIn({"map": "BugariaPier", "entity": "boatsailor", "flag": 16},
                      self.world.fill_slot_data()["held_until"])


class TestDiscoveriesOffByDefault(BugFablesTestBase):
    # Shuffle Discoveries is opt-in: by default no discovery is a location and the client watches none.
    def test_no_discovery_locations(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Outskirts: Pier, Statue", names)
        self.assertEqual(self.world.fill_slot_data()["location_discoveries"], {})


class TestDiscoveriesOn(BugFablesTestBase):
    options = {"shuffle_discoveries": True}

    def test_pier_statue_is_discovery_49(self) -> None:
        # The statue's line runs |discovery,49| (ScriptDump, 2026-09-25); the check is that journal entry.
        pier = str(self.world.location_name_to_id["Outskirts: Pier, Statue"])
        self.assertEqual(self.world.fill_slot_data()["location_discoveries"][pier], 49)

    def test_pier_statue_open_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: Pier, Statue"))

    def test_snakemouth_arrival_needs_the_permit(self) -> None:
        self.assertFalse(self.can_reach_location("Outskirts: Snakemouth Den Entrance, Arrival"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Snakemouth Den Entrance, Arrival"))


class TestTownMedal(BugFablesTestBase):
    # The Bug Me Not! medal in the residential district needs Leif's ice (the user, 2026-09-25); the town itself is open.
    def test_needs_leif(self) -> None:
        name = "Bugaria City: Residential District, Fountain Rooftop"
        self.assertFalse(self.can_reach_location(name))
        self.collect_by_name(["Explorer Permit", "Leif"])
        self.assertTrue(self.can_reach_location(name))


class TestBarAndBoards(BugFablesTestBase):
    # The bar (Shades's shop, a bounty board) and the town and Outskirts quest boards are open from the start (the user,
    # 2026-09-25). The bar's entrance line answers to flag 691, set on every new game, not the story's flag 135.
    def test_bar_entrance_repointed(self) -> None:
        self.assertIn({"map": "BugariaCommercial", "entity": "HideoutEntrance", "flag": 135, "to": 691},
                      self.world.fill_slot_data()["dialogue_flags"])

    def test_quest_boards_present(self) -> None:
        present = self.world.fill_slot_data()["kept_present"]
        self.assertIn({"map": "BugariaMainPlaza", "entity": "QuestBoard"}, present)
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "QuestBoard"}, present)


class TestMedalShop(BugFablesTestBase):
    # Merab's ten starting medals are locations, open from the start (the town is), each known by its shop and medal.
    def test_stock_in_slot_data(self) -> None:
        data = self.world.fill_slot_data()
        first = str(self.world.location_name_to_id["Bugaria City: Commercial District, Medal Shop 1"])
        self.assertEqual(data["location_shops"][first], {"shop": 0, "medal": 0})
        self.assertEqual(data["location_gives"][first], {"map": "BugariaCommercial", "type": 2, "item": 0})
        self.assertEqual(len(data["location_shops"]), 10)


class TestMedalShopsOff(BugFablesTestBase):
    options = {"shuffle_medal_shops": False}

    def test_no_shop_locations(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["location_shops"], {})


class TestShopContentsDefault(BugFablesTestBase):
    # By default shops refuse progression items (the user, 2026-09-25: shops soak up the good items, as in Tevi).
    def test_shop_refuses_progression(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertFalse(shop.item_rule(self.world.create_item("Explorer Permit")))
        self.assertTrue(shop.item_rule(self.world.create_item("TP Plus")))

    def test_no_progression_placed_in_shops(self) -> None:
        for location in self.multiworld.get_locations(self.player):
            if "Medal Shop" in location.name and location.item is not None:
                with self.subTest(location=location.name):
                    self.assertFalse(location.item.advancement)


class TestShopContentsFillerOnly(BugFablesTestBase):
    options = {"shop_contents": "filler_only"}

    def test_shops_excluded(self) -> None:
        from BaseClasses import LocationProgressType
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertEqual(shop.progress_type, LocationProgressType.EXCLUDED)


class TestShopContentsAnything(BugFablesTestBase):
    options = {"shop_contents": "anything"}

    def test_shop_takes_progression(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertTrue(shop.item_rule(self.world.create_item("Explorer Permit")))


class TestMadeleinesHouse(BugFablesTestBase):
    # The house is open from the start (the user, 2026-09-25): door kept, lock and locked-door check removed.
    def test_house_opened(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "doormadeleine"}, data["kept_present"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "lockeddoor"}, data["kept_open"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "Base/lock (1)"}, data["scenery_hidden"])
