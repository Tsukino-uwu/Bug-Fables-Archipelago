from BaseClasses import CollectionState

from . import BugFablesTestBase
from ..data_tables import ARTIFACTS, STORY_EVENTS


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
                                     "Outskirts: Ladybug Siblings' House",
                                     "Outskirts: Pier, Behind the Dock", "Bugaria City: Residential District, Rooftop",
                                     "Outskirts: Golden Path Tunnel, Behind the Boulder",
                                     "Ant Palace: Library, Behind the Bookshelf", "Ant Palace: War Room, Table",
                                     "Bugaria City: Residential District, Old Book Delivery Start",
                                     "Outskirts: Madeleine's House, Table Right",
                                     "Outskirts: Madeleine's House, Table Left"}
                         | {f"Bugaria City: Commercial District, Medal Shop {n}" for n in range(1, 23)}
                         | {f"Bugaria City: Commercial District, Item Shop {n}" for n in range(1, 6)}
                         | {f"Outskirts: Caravan, Item Shop {n}" for n in range(1, 4)}
                         # The Termacade and the theater, mapped 2026-10-06 in the commercial district's reach.
                         | {"Bugaria City: Termacade, Arcade Gift", "Bugaria City: Theater, Right Side Spinner",
                            "Bugaria City: Theater, Moth's Sale"}
                         | {f"Bugaria City: Termacade, Prize {n}" for n in range(1, 14)})

    def test_reward_near_snakemouth_needs_the_permit(self) -> None:
        self.assertFalse(self.can_reach_location("Outskirts: Near Snakemouth Den, Horn Tutorial"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Near Snakemouth Den, Horn Tutorial"))

    def test_pool_is_the_locations_items(self) -> None:
        # The pool is the locations' items (test_pool_matches_locations counts them); since the theater's moth sale
        # (2026-10-06) every key item and medal in the table has a spot, so there is no item left out to name here.
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Explorer Permit", pool)

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
        state = self._every_story_step_but_leif()
        self.assertFalse(location.can_reach(state))
        self.add(state, "Leif")
        self.assertTrue(location.can_reach(state))

    def test_first_artifact_needs_leif(self) -> None:
        artifact = self.world.get_location("Artifact 1")
        state = self._every_story_step_but_leif()
        self.assertFalse(artifact.can_reach(state))
        self.add(state, "Leif")
        self.assertTrue(artifact.can_reach(state))

    def _every_story_step_but_leif(self) -> CollectionState:
        # Unswept, so Leif's own story event stays out; the den's other story steps (mapped since) are in.
        return self.state_with("Explorer Permit", *sorted({event.item for event in STORY_EVENTS} - {"Leif"}))

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
    # The game makes the door only after the first boss (flag 41); the seed opens it from the start. The grass spot is
    # a hidden item.
    options = {"shuffle_hidden_items": True}
    def test_door_present_from_the_start(self) -> None:
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "LoadZoneGoldenPath"},
                      self.world.fill_slot_data()["kept_present"])

    def test_tunnel_present_from_the_start(self) -> None:
        self.assertIn({"map": "BOGoldenPath", "entity": "Loadzonetunnel"}, self.world.fill_slot_data()["kept_present"])

    def test_blocker_kept_away(self) -> None:
        self.assertIn({"map": "BOGoldenPath", "entity": "blocker"}, self.world.fill_slot_data()["kept_open"])

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
    # The game makes the library door only from flag 67; the seed opens it from the start.
    def test_library_open_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Ant Palace: Library, Behind the Bookshelf"))

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


class TestBagItemsAreFiller(BugFablesTestBase):
    # With the bag and storage full the client drops a received bag item at the party's feet, where the game's own
    # throw-away prompt may lose it: only filler may ever go in the bag.
    def test_bag_items_are_filler(self) -> None:
        from ..data_tables import ITEM_KIND, ITEMS
        for item in ITEMS:
            if item.kind == ITEM_KIND:
                with self.subTest(item=item.name):
                    self.assertEqual(item.classification, "filler")


class TestKeyItemsAndMedalsAreUseful(BugFablesTestBase):
    # Filler is only what's useless: a player wants every key item and every medal (Hard Mode and the drawback medals
    # too), so each is useful, or progression when a rule needs it. Crystal berries buy Shades's medals: useful while
    # her shop isn't in the seed.
    def test_key_items_and_medals_are_useful(self) -> None:
        from ..data_tables import ITEMS, KEY_ITEM_KIND, MEDAL_KIND
        for item in ITEMS:
            if item.kind in (KEY_ITEM_KIND, MEDAL_KIND):
                with self.subTest(item=item.name):
                    self.assertIn(item.classification, ("useful", "progression"))

    def test_crystal_berries_are_useful(self) -> None:
        from ..data_tables import CRYSTAL_KIND, ITEMS
        berries = [item for item in ITEMS if item.kind == CRYSTAL_KIND]
        self.assertTrue(berries)
        for item in berries:
            self.assertEqual(item.classification, "useful")


class TestTownMedal(BugFablesTestBase):
    options = {"starting_party_member": "off"}
    # The Bug Me Not! medal needs Leif's ice, or the Bee Fly item; the town itself is open.
    def test_needs_leif(self) -> None:
        name = "Bugaria City: Residential District, Fountain Rooftop"
        self.assertFalse(self.can_reach_location(name))
        self.collect_by_name(["Explorer Permit", "Leif"])
        self.assertTrue(self.can_reach_location(name))


class TestMainPlaza(BugFablesTestBase):
    # The red house's roof takes only the Flower Key (its bounce pads work without Jump); the mound, only Beetle Dig.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_crystal_berries": True}

    def test_rooftop_needs_the_flower_key(self) -> None:
        spot = "Bugaria City: Main Plaza, Red House Rooftop"
        self.collect_by_name("Explorer Permit")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Flower Key")
        self.assertTrue(self.can_reach_location(spot))

    def test_break_room_dig_spot_needs_beetle_dig(self) -> None:
        spot = "Bugaria City: Ant Tunnels, Break Room Dig Spot"
        self.collect_by_name("Explorer Permit")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Beetle Dig")
        self.assertTrue(self.can_reach_location(spot))

    def test_dig_spot_needs_beetle_dig(self) -> None:
        spot = "Bugaria City: Main Plaza, Dig Spot"
        self.collect_by_name("Explorer Permit")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Beetle Dig")
        self.assertTrue(self.can_reach_location(spot))


class TestResidentialRooftops(BugFablesTestBase):
    # The Bad Book's rooftop takes only the horn (no Jump); the fountain's takes Jump and Freeze, or Bee Fly alone; the
    # banker takes Jump.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_rooftop_needs_only_the_horn(self) -> None:
        spot = "Bugaria City: Residential District, Rooftop"
        self.collect_by_name("Explorer Permit")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Horn Slash")
        self.assertTrue(self.can_reach_location(spot))

    def test_fountain_rooftop_needs_jump_and_freeze(self) -> None:
        spot = "Bugaria City: Residential District, Fountain Rooftop"
        self.collect_by_name(["Explorer Permit", "Jump"])
        self.assertFalse(self.can_reach_location(spot))
        self.collect(self.get_items_by_name("Progressive Freeze"))
        self.assertTrue(self.can_reach_location(spot))

    def test_fountain_rooftop_with_bee_fly_alone(self) -> None:
        spot = "Bugaria City: Residential District, Fountain Rooftop"
        self.collect_by_name("Explorer Permit")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Bee Fly")
        self.assertTrue(self.can_reach_location(spot))

    def test_banker_needs_jump(self) -> None:
        spot = "Bugaria City: Residential District, Banker"
        self.collect_by_name("Explorer Permit")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Jump")
        self.assertTrue(self.can_reach_location(spot))


class TestTheater(BugFablesTestBase):
    # The spinner gives its crystal berry only to the horn; the plushie is a plain sale.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_crystal_berries": True}

    def test_spinner_needs_the_horn(self) -> None:
        spot = "Bugaria City: Theater, Right Side Spinner"
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Bugaria City: Theater, Moth's Sale"))
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Horn Slash")
        self.assertTrue(self.can_reach_location(spot))


class TestBadlands(BugFablesTestBase):
    # The center pillar's medal takes Jump and Bee Fly or the Beemerang, the rock ledge's yam Jump and the Beemerang,
    # Bee Fly, or Freeze and the horn.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_center_pillar_needs_bee_fly_or_the_beemerang(self) -> None:
        # The spot's own rule: the room is also reached by doors that may need either.
        pillar = self.multiworld.get_location("Lost Sands: Badlands, Center Pillar", self.player)
        self.collect_all_but(["Bee Fly", "Progressive Beemerang"])
        self.assertFalse(pillar.access_rule(self.multiworld.state))
        self.collect_by_name("Bee Fly")
        self.assertTrue(pillar.access_rule(self.multiworld.state))

    def test_center_pillar_by_the_beemerang(self) -> None:
        pillar = self.multiworld.get_location("Lost Sands: Badlands, Center Pillar", self.player)
        self.collect_all_but(["Bee Fly"])
        self.assertTrue(pillar.access_rule(self.multiworld.state))

    def test_rock_ledge_needs_one_of_three(self) -> None:
        ledge = self.multiworld.get_location("Lost Sands: Badlands, Rock Ledge", self.player)
        self.collect_all_but(["Progressive Beemerang", "Bee Fly", "Progressive Freeze"])
        self.assertFalse(ledge.access_rule(self.multiworld.state))
        self.collect(self.get_items_by_name("Progressive Beemerang"))
        self.assertTrue(ledge.access_rule(self.multiworld.state))

    def test_rock_ledge_by_bee_fly(self) -> None:
        ledge = self.multiworld.get_location("Lost Sands: Badlands, Rock Ledge", self.player)
        self.collect_all_but(["Progressive Beemerang", "Progressive Freeze"])
        self.assertTrue(ledge.access_rule(self.multiworld.state))

    def test_rock_ledge_by_freeze_and_the_horn(self) -> None:
        ledge = self.multiworld.get_location("Lost Sands: Badlands, Rock Ledge", self.player)
        self.collect_all_but(["Progressive Beemerang", "Bee Fly", "Horn Slash"])
        self.assertFalse(ledge.access_rule(self.multiworld.state))
        self.collect_by_name("Horn Slash")
        self.assertTrue(ledge.access_rule(self.multiworld.state))


class TestBookArea(BugFablesTestBase):
    # The north half takes Horn Dash or Bee Fly from the south, either one.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_either_crosses(self) -> None:
        # The crossing itself: the north half is also reached by its own doors from rooms mapped since.
        crossing = self.multiworld.get_entrance("DesertBookArea to DesertBookArea (North)", self.player)
        self.collect_all_but(["Progressive Dash", "Bee Fly"])
        self.assertFalse(crossing.access_rule(self.multiworld.state))
        self.collect_by_name("Bee Fly")
        self.assertTrue(crossing.access_rule(self.multiworld.state))
        self.remove_by_name("Bee Fly")
        self.assertFalse(crossing.access_rule(self.multiworld.state))
        self.collect(self.get_items_by_name("Progressive Dash"))
        self.assertTrue(crossing.access_rule(self.multiworld.state))


class TestTardigradeIdol(BugFablesTestBase):
    # Jump, Freeze and the horn, each needed.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_needs_each(self) -> None:
        spot = "Lost Sands: Rock Formation, Tardigrade Idol"
        for missing in ("Jump", "Progressive Freeze", "Horn Slash"):
            with self.subTest(missing=missing):
                state = CollectionState(self.multiworld)
                self.collect_all_but([missing], state)
                self.assertFalse(state.can_reach(spot, "Location", self.player))
        self.collect_all_but([])
        self.assertTrue(self.can_reach_location(spot))


class TestSouthTrenchBridge(BugFablesTestBase):
    # The south trench's bridge falls only to the horn.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_needs_horn(self) -> None:
        event = "Lost Sands: South Trench, Bridge Knocked Down"
        self.collect_all_but(["Horn Slash"])
        self.assertFalse(self.can_reach_location(event))
        self.collect_by_name("Horn Slash")
        self.assertTrue(self.can_reach_location(event))


class TestDefiantRootEntrance(BugFablesTestBase):
    # The dig spot behind the rock: Horn Dash and Beetle Dig, each needed.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_dig_spots": True}

    def test_needs_each(self) -> None:
        spot = "Lost Sands: Defiant Root Entrance, Dig Spot"
        # By item: Horn Dash is Progressive Dash's second level.
        for missing in ("Progressive Dash", "Beetle Dig"):
            with self.subTest(missing=missing):
                state = CollectionState(self.multiworld)
                self.collect_all_but([missing], state)
                self.assertFalse(state.can_reach(spot, "Location", self.player))
        self.collect_all_but([])
        self.assertTrue(self.can_reach_location(spot))


class TestBadgeAlcove(BugFablesTestBase):
    # The ledge's medal takes Jump, the grass by the right door the horn.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_hidden_items": True}

    def test_needs(self) -> None:
        for spot, missing in (("Lost Sands: Badge Alcove, Platform on the Upper Left", "Jump"),
                              ("Lost Sands: Badge Alcove, Grass by the Right Door", "Horn Slash")):
            with self.subTest(spot=spot):
                state = CollectionState(self.multiworld)
                self.collect_all_but([missing], state)
                self.assertFalse(state.can_reach(spot, "Location", self.player))
        self.collect_all_but([])
        self.assertTrue(self.can_reach_location("Lost Sands: Badge Alcove, Platform on the Upper Left"))
        self.assertTrue(self.can_reach_location("Lost Sands: Badge Alcove, Grass by the Right Door"))


class TestCaravanCampDig(BugFablesTestBase):
    # Jump, Bee Fly and Beetle Dig, each needed.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_needs_each(self) -> None:
        spot = "Lost Sands: Caravan Camp, Dig Spot"
        for missing in ("Jump", "Bee Fly", "Beetle Dig"):
            with self.subTest(missing=missing):
                state = CollectionState(self.multiworld)
                self.collect_all_but([missing], state)
                self.assertFalse(state.can_reach(spot, "Location", self.player))
        self.collect_all_but([])
        self.assertTrue(self.can_reach_location(spot))


class TestSandPitBridges(BugFablesTestBase):
    # Every bridge falls only to the horn; the middle's also need Jump to reach.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_needs(self) -> None:
        bridges = ("Bottom Bridge", "Middle Bridges", "Left Bridges", "Right Bridge", "Top Right Bridge")
        for bridge in bridges:
            event = f"Lost Sands: Sand Pit, {bridge} Knocked Down"
            for missing in ("Horn Slash", "Jump") if bridge == "Middle Bridges" else ("Horn Slash",):
                with self.subTest(event=event, missing=missing):
                    state = CollectionState(self.multiworld)
                    self.collect_all_but([missing], state)
                    self.assertFalse(state.can_reach(event, "Location", self.player))
        self.collect_all_but([])
        for bridge in bridges:
            self.assertTrue(self.can_reach_location(f"Lost Sands: Sand Pit, {bridge} Knocked Down"))


class TestGoldenHillsBorderLedge(BugFablesTestBase):
    # The horn and Jump, each needed.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_needs_each(self) -> None:
        spot = "Lost Sands: Golden Hills Border, Left Ledge"
        for missing in ("Horn Slash", "Jump"):
            with self.subTest(missing=missing):
                state = CollectionState(self.multiworld)
                self.collect_all_but([missing], state)
                self.assertFalse(state.can_reach(spot, "Location", self.player))
        self.collect_all_but([])
        self.assertTrue(self.can_reach_location(spot))


class TestOasis(BugFablesTestBase):
    # The sandpile's way back up takes Jump and a switch hit.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_needs(self) -> None:
        # The top right is reached by a drop or its own cave door; the way up on the platform takes the switch (hit
        # from up there, any attack) and Jump.
        up = self.multiworld.get_entrance("DesertOasis to DesertOasis (Top Right)", self.player)
        state = CollectionState(self.multiworld)
        self.collect_all_but(["Jump"], state)
        self.assertFalse(up.access_rule(state))
        switch = self.multiworld.get_location("Lost Sands: Oasis, Platform Switch Hit", self.player)
        state = CollectionState(self.multiworld)
        self.collect_all_but(["Progressive Beemerang", "Horn Slash", "Progressive Freeze"], state)
        self.assertFalse(switch.access_rule(state))
        self.collect_all_but([])
        self.assertTrue(up.access_rule(self.multiworld.state))
        self.assertTrue(self.can_reach_location("Lost Sands: Oasis, On Top of the Sandpile"))
        self.assertTrue(self.can_reach_location("Lost Sands: Oasis, Crimson Cave"))


class TestLeftHallBerry(BugFablesTestBase):
    # Crystal berry #9 takes Jump.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_crystal_berries": True}

    def test_needs_jump(self) -> None:
        spot = "Golden Hills: Left Hall, above the Flytraps"
        state = CollectionState(self.multiworld)
        self.collect_all_but(["Jump"], state)
        self.assertFalse(state.can_reach(spot, "Location", self.player))


class TestBarCorner(BugFablesTestBase):
    # The way down to the underground bar is behind the commercial district's grass, and the bar's door comes back up
    # behind it too: the horn both ways.
    options = {"shuffle_field_moves": True}

    def test_the_bar_needs_the_horn(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_region("BugariaCommercial"))
        self.assertFalse(self.can_reach_region("UndergroundBar"))
        self.collect_by_name("Horn Slash")
        self.assertTrue(self.can_reach_region("UndergroundBar"))

    def test_the_bar_door_lands_behind_the_grass(self) -> None:
        entrance = self.multiworld.get_entrance("UndergroundBar: LoadZone", self.player)
        self.assertEqual(entrance.connected_region.name, "BugariaCommercial (Bar Corner)")


class TestAntTunnels(BugFablesTestBase):
    # A far end's free miner opens the way to the tunnel hub, so reaching the far end reaches the hub.
    def test_far_ends_lead_to_the_hub(self) -> None:
        names = {e.name for e in self.multiworld.get_region("AntTunnels", self.player).entrances}
        # The border cave's miner stands in its top part, past grass (mapped 2026-10-08).
        for far in ("GoldenSettlementEntrance", "DefiantRoot2", "BarrenLandsAntTunnel", "FGCave (Tunnel)"):
            self.assertIn(f"{far} to AntTunnels (ant tunnel)", names)

    def test_miners_free(self) -> None:
        self.assertTrue(self.world.fill_slot_data()["free_ant_tunnels"])

    def test_palace_mine_open(self) -> None:
        # Coming up from the tunnels, the railing and the missing door boxed the party in until chapter 2.
        data = self.world.fill_slot_data()
        self.assertIn({"map": "AntPalace1", "entity": "MineLoadZone"}, data["kept_present"])
        self.assertIn({"map": "AntPalace1", "entity": "Base/mineblock"}, data["scenery_hidden"])

    def test_palace_library_open(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "AntPalace1", "entity": "Loadzonelibrary"}, data["kept_present"])
        self.assertIn({"map": "AntPalace1", "entity": "makiblocker1"}, data["kept_open"])
        self.assertIn({"map": "AntPalace1", "entity": "loadzonewarroom"}, data["kept_present"])
        self.assertIn({"map": "AntPalace1", "entity": "makiblocker2"}, data["kept_open"])

    def test_war_room_medal_from_the_start(self) -> None:
        # The game makes it only after the ending; the seed puts it on the table from the start, as a location.
        self.assertIn({"map": "AntPalaceWarRoom", "entity": "royal medal"}, self.world.fill_slot_data()["kept_present"])
        self.assertTrue(self.can_reach_location("Ant Palace: War Room, Table"))


class TestPrisonCorridor(BugFablesTestBase):
    # Switch gates: across to the yard with an attack; back through its prison door with the permit.
    options = {"shuffle_field_moves": True}

    def test_corridor_rules(self) -> None:
        from ..data_tables import door_name
        def entrance(map_name: str, door: str):
            return self.multiworld.get_entrance(door_name(map_name, door), self.player)
        across = entrance("RubberPrisonCheckpointCorridor", "loadzoneexit")
        self.assertFalse(across.access_rule(self.state_with()))
        self.assertTrue(across.access_rule(self.state_with("Progressive Beemerang")))
        back = entrance("RubberPrisonCheckpointCorridor", "loadzoneforward")
        self.assertFalse(back.access_rule(self.state_with()))
        self.assertTrue(back.access_rule(self.state_with("Explorer Permit")))


class TestWizardTower(BugFablesTestBase):
    # Open in a seed: the fall's scene and the basement's wizard kept away, the hole's door and the front door kept
    # present, the front door's model and lock hidden, and the front door free both ways.

    def test_tower_open(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "FarGrasslandsWizard", "entity": "basementevent"}, data["kept_open"])
        self.assertIn({"map": "WizardTowerBasement", "entity": "wizard"}, data["kept_open"])
        self.assertIn({"map": "FarGrasslandsWizard", "entity": "loadzonebasement"}, data["kept_present"])
        self.assertIn({"map": "FarGrasslandsWizard", "entity": "loadzonetower"}, data["kept_present"])
        self.assertIn({"map": "FarGrasslandsWizard", "entity": "Base/Tower/Door"}, data["scenery_hidden"])
        self.assertIn({"map": "WizardTowerStairs", "entity": "Base/DoorLock"}, data["scenery_hidden"])
        self.assertIn({"map": "WizardTowerAttic", "entity": "wizard", "flag": 450, "to": 691}, data["dialogue_flags"])

    def test_front_door_free(self) -> None:
        from ..data_tables import door_name
        for map_name, door in (("FarGrasslandsWizard", "loadzonetower"), ("WizardTowerStairs", "loadzoneoutside")):
            with self.subTest(door=door):
                entrance = self.multiworld.get_entrance(door_name(map_name, door), self.player)
                self.assertTrue(entrance.access_rule(self.state_with()))


class TestSwampBridge(BugFablesTestBase):
    # Kept up in a seed (the user, 2026-10-04): the collapse and its leafbugs away, the bottom's bounce pad always
    # there.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_kept_up(self) -> None:
        data = self.world.fill_slot_data()
        for entity in ("eventtrigger", "leafbug", "leafbug - Duplicate"):
            self.assertIn({"map": "SwamplandsBridge", "entity": entity}, data["kept_open"])
        self.assertIn({"map": "SwamplandsBridge", "entity": "spring - Duplicate - Duplicate"}, data["kept_present"])
        self.assertIn({"map": "SwamplandsBridge", "entity": "Base/BridgeWalls"}, data["scenery_hidden"])

    def test_top_door_from_the_right_side(self) -> None:
        # Bee Fly, or Jump once the small bridge is knocked down; back, the small bridge and Jump.
        up = self.multiworld.get_entrance("SwamplandsBridge (Top Right) to SwamplandsBridge (Top)", self.player)
        down = self.multiworld.get_entrance("SwamplandsBridge (Top) to SwamplandsBridge (Top Right)", self.player)
        bridge = "Swamp Lower Bridge Knocked Down"
        self.assertFalse(up.access_rule(self.state_with("Jump")))
        self.assertTrue(up.access_rule(self.state_with("Bee Fly")))
        self.assertTrue(up.access_rule(self.state_with("Jump", bridge)))
        self.assertFalse(down.access_rule(self.state_with("Bee Fly")))
        self.assertTrue(down.access_rule(self.state_with("Jump", bridge)))

    def test_red_pad_from_the_bridge(self) -> None:
        # The left end's boulder, Horn Dash; the pad itself sends the party back up with nothing.
        down = self.multiworld.get_entrance("SwamplandsBridge to SwamplandsBridge (Red Bounce Pad)", self.player)
        up = self.multiworld.get_entrance("SwamplandsBridge (Red Bounce Pad) to SwamplandsBridge", self.player)
        self.assertFalse(down.access_rule(self.state_with("Progressive Dash")))
        self.assertTrue(down.access_rule(self.state_with("Progressive Dash", "Progressive Dash")))
        self.assertTrue(up.access_rule(self.state_with()))

    def test_bottom_free_both_ways(self) -> None:
        # Dropped down to from the bridge, its walls taken away; back up by the bottom's bounce pad.
        for name in ("SwamplandsBridge to SwamplandsBridge (Bottom)", "SwamplandsBridge (Bottom) to SwamplandsBridge"):
            with self.subTest(entrance=name):
                self.assertTrue(self.multiworld.get_entrance(name, self.player).access_rule(self.state_with()))

    def test_boulder_needs_nothing(self) -> None:
        boulder = self.multiworld.get_location("Wild Swamplands: Bridge, Boulder", self.player)
        self.assertTrue(boulder.access_rule(self.state_with()))
        self.assertEqual(boulder.parent_region.name, "SwamplandsBridge (Bottom)")


class TestLongSwampRoom(BugFablesTestBase):
    # Swamplands4's crossing: Jump and Freeze toward the middle; back toward the left door Freeze isn't needed, but
    # without it there's no way back, so the logic counts it only with a way back (Points of No Return off).
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_freeze_toward_the_middle(self) -> None:
        east = self.multiworld.get_entrance("Swamplands4 (West) to Swamplands4", self.player)
        self.assertFalse(east.access_rule(self.state_with("Jump")))
        self.assertTrue(east.access_rule(self.state_with("Jump", "Progressive Freeze")))
        self.assertTrue(east.access_rule(self.state_with("Bee Fly")))

    def test_back_west_counts_only_with_a_way_back(self) -> None:
        west = self.multiworld.get_entrance("Swamplands4 to Swamplands4 (West)", self.player)
        self.assertFalse(west.access_rule(self.state_with("Jump")))
        self.assertTrue(west.access_rule(self.state_with("Jump", "Progressive Freeze")))


class TestLongSwampRoomNoReturn(BugFablesTestBase):
    # With Points of No Return, the crossing back toward the left door takes Jump alone.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "points_of_no_return": True}

    def test_back_west_with_jump_alone(self) -> None:
        west = self.multiworld.get_entrance("Swamplands4 to Swamplands4 (West)", self.player)
        self.assertTrue(west.access_rule(self.state_with("Jump")))
        self.assertFalse(west.access_rule(self.state_with()))


class TestJunction(BugFablesTestBase):
    # Swamplands5 (the user, 2026-10-09): its centipede scene never plays; up to its top right, the lift and Jump; its
    # left side to the right by Bee Fly or the long way round to the lever; the vine's Clear Bomb, Jump and the Halt.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_centipede_scene_kept_away(self) -> None:
        scenes = self.world.fill_slot_data()["scenes_kept_away"]
        self.assertIn({"map": "Swamplands5", "event": 147, "flag": 383}, scenes)

    def test_lift_up_to_the_top_right(self) -> None:
        up = self.multiworld.get_entrance("Swamplands5 to Swamplands5 (Top Right)", self.player)
        self.assertFalse(up.access_rule(self.state_with("Jump")))
        self.assertFalse(up.access_rule(self.state_with("Swamp Lift Running")))
        self.assertTrue(up.access_rule(self.state_with("Jump", "Swamp Lift Running")))

    def test_left_to_right(self) -> None:
        across = self.multiworld.get_entrance("Swamplands5 (Left) to Swamplands5", self.player)
        self.assertTrue(across.access_rule(self.state_with("Bee Fly")))
        self.assertFalse(across.access_rule(self.state_with("Jump", "Progressive Dash", "Progressive Dash",
                                                            "Progressive Beemerang")))
        self.assertTrue(across.access_rule(self.state_with("Jump", "Progressive Dash", "Progressive Dash",
                                                           "Progressive Beemerang", "Shield")))

    def test_vine_needs_jump_and_the_halt(self) -> None:
        vine = self.multiworld.get_location("Wild Swamplands: Junction, Vine above the Platform", self.player)
        self.assertFalse(vine.access_rule(self.state_with("Jump", "Progressive Beemerang")))
        self.assertTrue(vine.access_rule(self.state_with("Jump", "Progressive Beemerang", "Progressive Beemerang")))


class TestCrankPond(BugFablesTestBase):
    # Swamplands6 (the user, 2026-10-09): the lower right by the middle's crank and Jump, back by the lily pad (the
    # horn); its right door up the lift, Horn Dash, the Halt and Jump.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_lower_right_by_the_crank(self) -> None:
        there = self.multiworld.get_entrance("Swamplands6 (Middle) to Swamplands6 (Lower Right)", self.player)
        back = self.multiworld.get_entrance("Swamplands6 (Lower Right) to Swamplands6 (Middle)", self.player)
        self.assertFalse(there.access_rule(self.state_with("Progressive Beemerang", "Progressive Beemerang")))
        self.assertTrue(there.access_rule(self.state_with("Progressive Beemerang", "Progressive Beemerang", "Jump")))
        self.assertTrue(back.access_rule(self.state_with("Horn Slash")))
        self.assertFalse(back.access_rule(self.state_with("Jump")))

    def test_right_door_up_the_lift(self) -> None:
        up = self.multiworld.get_entrance("Swamplands6 (Lower Right) to Swamplands6 (Upper Right)", self.player)
        self.assertFalse(up.access_rule(self.state_with("Progressive Beemerang", "Progressive Beemerang", "Jump")))
        self.assertTrue(up.access_rule(self.state_with("Progressive Beemerang", "Progressive Beemerang", "Jump",
                                                       "Progressive Dash", "Progressive Dash")))


class TestIceBlockClimb(BugFablesTestBase):
    # Swamplands7 (the user, 2026-10-09): ice blocks frozen from droplets, knocked with the horn and jumped on; one
    # brought up past a boulder (Horn Dash) opens the middle's ways up and across; Bee Fly for some.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}
    ICE = ("Progressive Freeze", "Horn Slash", "Jump")
    ICE_UP = ICE + ("Progressive Dash", "Progressive Dash")

    def test_bottom_right_to_the_bottom_middle(self) -> None:
        across = self.multiworld.get_entrance("Swamplands7 (Bottom Right) to Swamplands7 (Bottom Middle)", self.player)
        self.assertFalse(across.access_rule(self.state_with("Progressive Freeze", "Jump")))
        self.assertTrue(across.access_rule(self.state_with(*self.ICE)))
        self.assertTrue(across.access_rule(self.state_with("Bee Fly")))

    def test_up_to_the_middle_needs_the_horn_dash(self) -> None:
        up = self.multiworld.get_entrance("Swamplands7 (Bottom Middle) to Swamplands7", self.player)
        self.assertFalse(up.access_rule(self.state_with(*self.ICE)))
        self.assertFalse(up.access_rule(self.state_with("Bee Fly")))
        self.assertTrue(up.access_rule(self.state_with(*self.ICE_UP)))

    def test_upper_left_back_only_by_bee_fly(self) -> None:
        back = self.multiworld.get_entrance("Swamplands7 (Upper Left) to Swamplands7 (Upper Middle)", self.player)
        self.assertFalse(back.access_rule(self.state_with(*self.ICE_UP)))
        self.assertTrue(back.access_rule(self.state_with("Bee Fly")))

    def test_left_to_the_bottom_right_counts_with_a_way_back(self) -> None:
        ledges = self.multiworld.get_entrance("Swamplands7 (Left) to Swamplands7 (Bottom Right) (ledges)", self.player)
        self.assertFalse(ledges.access_rule(self.state_with("Jump")))
        self.assertTrue(ledges.access_rule(self.state_with(*self.ICE_UP)))

    def test_medal_needs_nothing_there(self) -> None:
        medal = self.multiworld.get_location("Wild Swamplands: Ice Block Climb, On Top of the Stump", self.player)
        self.assertTrue(medal.access_rule(self.state_with()))
        self.assertEqual(medal.parent_region.name, "Swamplands7 (Upper Left)")


class TestFencedPond(BugFablesTestBase):
    # Swamplands8 (the user, 2026-10-09): a double fence splits the middle from the right side and the top door until
    # the lever on the ledge takes it down; Bee Fly goes everywhere.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_dig_spots": True}

    def test_lever_ledge(self) -> None:
        up = self.multiworld.get_entrance("Swamplands8 to Swamplands8 (Upper Middle)", self.player)
        self.assertFalse(up.access_rule(self.state_with("Jump", "Horn Slash")))
        self.assertTrue(up.access_rule(self.state_with("Jump", "Horn Slash", "Progressive Dash", "Progressive Dash")))
        self.assertTrue(up.access_rule(self.state_with("Jump", "Bee Fly")))

    def test_fence_splits_the_room(self) -> None:
        across = self.multiworld.get_entrance("Swamplands8 to Swamplands8 (Right)", self.player)
        self.assertFalse(across.access_rule(self.state_with("Jump")))
        self.assertTrue(across.access_rule(self.state_with("Jump", "Swamp Fence Down")))
        self.assertTrue(across.access_rule(self.state_with("Bee Fly")))

    def test_top_door_side_needs_no_lever(self) -> None:
        down = self.multiworld.get_entrance("Swamplands8 (Top) to Swamplands8 (Right)", self.player)
        self.assertFalse(down.access_rule(self.state_with()))
        self.assertTrue(down.access_rule(self.state_with("Jump")))
        dig = self.multiworld.get_location("Wild Swamplands: Fenced Pond, Dig Spot", self.player)
        self.assertEqual(dig.parent_region.name, "Swamplands8 (Right)")
        self.assertTrue(dig.access_rule(self.state_with("Beetle Dig")))


class TestDefiantRootSquare(BugFablesTestBase):
    # DefiantRoot1 (the user, 2026-10-09): the ground and its doors need nothing; the rooftops are up with Jump, the
    # mayor's storage behind them locked until the Desert Key.
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_crystal_berries": True}

    def test_ground_spots_need_nothing(self) -> None:
        for name in ("Behind the Box", "Morty's Gift"):
            spot = self.multiworld.get_location(f"Defiant Root: Square, {name}", self.player)
            self.assertEqual(spot.parent_region.name, "DefiantRoot1")
            self.assertTrue(spot.access_rule(self.state_with()), name)

    def test_rooftops_need_jump(self) -> None:
        up = self.multiworld.get_entrance("DefiantRoot1 to DefiantRoot1 (Rooftops)", self.player)
        self.assertFalse(up.access_rule(self.state_with()))
        self.assertTrue(up.access_rule(self.state_with("Jump")))
        for name in ("Right Rooftop", "Left Rooftop"):
            spot = self.multiworld.get_location(f"Defiant Root: Square, {name}", self.player)
            self.assertEqual(spot.parent_region.name, "DefiantRoot1 (Rooftops)", name)

    # The Desert Key exists only once quest 46 is done, from chapter 5's start: the later chapters' stand-in claimed
    # less than the game, so the storage waits for the quest pass.
    def test_storage_waits_for_the_quest_pass(self) -> None:
        names = {spot.name for spot in self.multiworld.get_locations(self.player)}
        for name in ("Mayor's Storage 1", "Mayor's Storage 2"):
            self.assertNotIn(f"Defiant Root: Square, {name}", names)

    # Morty rents the Bed Bug out again for 30 berries (line 28) once it's used up: his first lend alone is the check,
    # and a re-rental after it is the game's own (build step 66).
    def test_mortys_gift_is_his_first_lend(self) -> None:
        gives = self.world.fill_slot_data()["location_gives"].values()
        self.assertIn({"map": "DefiantRoot1", "type": 1, "item": 89, "npc": "Morty", "again": True}, gives)

    # Flag 201 (the desert's south entrance, the caravan robbery) took crystal berry #15 away for good (build step 64).
    def test_berry_kept_until_taken(self) -> None:
        self.assertIn({"map": "DefiantRoot1", "entity": "crystalberryskip", "flag": 201, "to": -1},
                      self.world.fill_slot_data()["limit_flags"])


class TestDefiantRootWell(BugFablesTestBase):
    # DefiantRootWell (the user, 2026-10-09): the landing and its bounce pad up to the town on the left; the right side
    # (the hideout's door, a Leaf Croissant on boxes) by burrowing, both ways.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_right_side_needs_dig(self) -> None:
        for entrance in ("DefiantRootWell to DefiantRootWell (Right)", "DefiantRootWell (Right) to DefiantRootWell"):
            way = self.multiworld.get_entrance(entrance, self.player)
            self.assertFalse(way.access_rule(self.state_with()), entrance)
            self.assertTrue(way.access_rule(self.state_with("Beetle Dig")), entrance)

    def test_croissant_needs_jump(self) -> None:
        spot = self.multiworld.get_location("Defiant Root: Well, By the Boxes", self.player)
        self.assertEqual(spot.parent_region.name, "DefiantRootWell (Right)")
        self.assertFalse(spot.access_rule(self.state_with()))
        self.assertTrue(spot.access_rule(self.state_with("Jump")))


class TestBeehiveLift(BugFablesTestBase):
    # DefiantRoot2 (the user, 2026-10-09): the ground free; the elevator's platform and the inn's upstairs each up with
    # Jump or Bee Fly; the medal on the inn's roof flown around to from upstairs.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_platform_and_upstairs_need_jump_or_bee_fly(self) -> None:
        for area in ("Elevator", "Upstairs"):
            up = self.multiworld.get_entrance(f"DefiantRoot2 to DefiantRoot2 ({area})", self.player)
            self.assertFalse(up.access_rule(self.state_with()), area)
            self.assertTrue(up.access_rule(self.state_with("Jump")), area)
            self.assertTrue(up.access_rule(self.state_with("Bee Fly")), area)

    def test_elevator_leaves_from_the_platform(self) -> None:
        self.multiworld.get_entrance("DefiantRoot2 (Elevator) to BeehiveOutside (elevator)", self.player)
        self.multiworld.get_entrance("BeehiveOutside to DefiantRoot2 (Elevator) (elevator)", self.player)

    def test_upstairs_spots(self) -> None:
        book = self.multiworld.get_location("Defiant Root: Beehive Lift, Above the Inn", self.player)
        medal = self.multiworld.get_location("Defiant Root: Beehive Lift, Inn Rooftop", self.player)
        for spot in (book, medal):
            self.assertEqual(spot.parent_region.name, "DefiantRoot2 (Upstairs)")
        self.assertFalse(book.access_rule(self.state_with()))
        self.assertTrue(book.access_rule(self.state_with("Jump")))
        self.assertTrue(book.access_rule(self.state_with("Bee Fly")))
        self.assertFalse(medal.access_rule(self.state_with("Jump")))
        self.assertTrue(medal.access_rule(self.state_with("Bee Fly")))

    # Its lock stood until the innkeeper's daughter, in the Termite Capitol, was talked to (build step 65).
    def test_inn_door_kept_open(self) -> None:
        self.assertIn({"map": "DefiantRoot2", "entity": "Base/DoorLock"}, self.world.fill_slot_data()["scenery_hidden"])


class TestMarketShops(BugFablesTestBase):
    # DefiantRoot3, the Market (the user, 2026-10-10): its three shops' slots, on the ground, nothing needed.
    def test_three_shops(self) -> None:
        shops = self.world.fill_slot_data()["location_item_shops"]
        for shop, keeper, stock in (("Item Shop", "shopkeeper", (72, 0, 12, 96, 162)),
                                    ("Poison Shop", "poisonguyshop - Duplicate", (64, 26, 31, 88)),
                                    ("Bakery Shop", "sirfy", (1, 68, 73))):
            for slot, item in enumerate(stock, start=1):
                spot = self.multiworld.get_location(f"Defiant Root: Market, {shop} {slot}", self.player)
                self.assertEqual(spot.parent_region.name, "DefiantRoot3")
                self.assertTrue(spot.access_rule(self.state_with()))
                self.assertEqual(shops[str(spot.address)], {"map": "DefiantRoot3", "keeper": keeper, "item": item})


class TestCastleEntrance(BugFablesTestBase):
    # SandCastleEntrance (the user, 2026-10-09): the middle's bridge shows only while the crystal is lit, which the
    # Beemerang Toss does from either side; Bee Fly crosses without it.
    options = {"shuffle_field_moves": True}

    def test_middle_needs_the_toss_or_bee_fly(self) -> None:
        for entrance in ("SandCastleEntrance to SandCastleEntrance (Right)",
                         "SandCastleEntrance (Right) to SandCastleEntrance"):
            way = self.multiworld.get_entrance(entrance, self.player)
            self.assertFalse(way.access_rule(self.state_with("Horn Slash", "Progressive Freeze")), entrance)
            self.assertTrue(way.access_rule(self.state_with("Progressive Beemerang")), entrance)
            self.assertTrue(way.access_rule(self.state_with("Bee Fly")), entrance)


class TestSlidePuzzle(BugFablesTestBase):
    # SandCastleSlidePuzzle (the user, 2026-10-09): the puzzle's floor is a drop from either upper side and from the
    # bottom right door, Jump back up to that door; the block knocked into place (the horn) fills the upper gap and
    # opens the upper left door; the medal burrowed to, Beetle Dig.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_floor_needs_jump_or_bee_fly_back_up(self) -> None:
        up = self.multiworld.get_entrance("SandCastleSlidePuzzle (Bottom) to SandCastleSlidePuzzle", self.player)
        self.assertFalse(up.access_rule(self.state_with()))
        self.assertTrue(up.access_rule(self.state_with("Jump")))
        self.assertTrue(up.access_rule(self.state_with("Bee Fly")))

    def test_upper_gap_and_door_wait_for_the_puzzle(self) -> None:
        gap = self.multiworld.get_entrance("SandCastleSlidePuzzle (Upper Right) to SandCastleSlidePuzzle (Upper Left)",
                                           self.player)
        self.assertFalse(gap.access_rule(self.state_with()))
        self.assertTrue(gap.access_rule(self.state_with("Sand Castle Slide Puzzle Solved")))
        self.assertTrue(gap.access_rule(self.state_with("Bee Fly")))
        door = self.multiworld.get_entrance(
            next(e.name for e in self.multiworld.get_region("SandCastleSlidePuzzle (Upper Left)", self.player).exits
                 if "SandCastlePressurePuzzle" in e.connected_region.name), self.player)
        self.assertFalse(door.access_rule(self.state_with("Bee Fly")))
        self.assertTrue(door.access_rule(self.state_with("Sand Castle Slide Puzzle Solved")))

    def test_the_puzzle(self) -> None:
        puzzle = self.multiworld.get_location("Ancient Castle: Slide Puzzle, Block Knocked into Place", self.player)
        self.assertEqual(puzzle.parent_region.name, "SandCastleSlidePuzzle (Bottom)")
        self.assertFalse(puzzle.access_rule(self.state_with()))
        self.assertTrue(puzzle.access_rule(self.state_with("Horn Slash")))

    # The castle's spots wait until the Sand Castle Key's chain is in the logic (build step 67).
    def test_the_medal_waits(self) -> None:
        names = {spot.name for spot in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Ancient Castle: Slide Puzzle, Behind the Cracked Wall", names)


class TestStatueRoom(BugFablesTestBase):
    # SandCastleStatueRoom (the user, 2026-10-09): left to right over the middle's block, Icicle and Jump or Bee Fly;
    # back, Jump or Bee Fly.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_left_to_right_needs_icicle_and_jump_or_bee_fly(self) -> None:
        right = self.multiworld.get_entrance("SandCastleStatueRoom to SandCastleStatueRoom (Right)", self.player)
        self.assertFalse(right.access_rule(self.state_with("Jump", "Progressive Freeze")))
        self.assertTrue(right.access_rule(self.state_with("Jump", "Progressive Freeze", "Progressive Freeze")))
        self.assertTrue(right.access_rule(self.state_with("Bee Fly")))

    def test_right_to_left_needs_jump_or_bee_fly(self) -> None:
        left = self.multiworld.get_entrance("SandCastleStatueRoom (Right) to SandCastleStatueRoom", self.player)
        self.assertFalse(left.access_rule(self.state_with()))
        self.assertTrue(left.access_rule(self.state_with("Jump")))
        self.assertTrue(left.access_rule(self.state_with("Bee Fly")))


class TestDashScene(BugFablesTestBase):
    # Location 69's trigger needs chapter 2's boss beaten (flag 88, behind both offerings): its stand-in claimed less.
    def test_needs_both_offerings(self) -> None:
        for offering in ("Sun Offering", "Moon Offering"):
            with self.subTest(missing=offering):
                state = CollectionState(self.multiworld)
                self.collect_all_but([offering], state)
                self.assertFalse(self.multiworld.get_location("Lost Sands: Entrance", self.player).can_reach(state))
        self.collect_all_but([])
        self.assertTrue(self.can_reach_location("Lost Sands: Entrance"))


class TestCastleBasement(BugFablesTestBase):
    # SandCastleBasement (the user, 2026-10-09): its door on an isolated ledge, the middle across with Jump or Bee Fly;
    # its three spots wait with the rest of the castle (build step 67).
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "shuffle_crystal_berries": True}

    def test_middle_needs_jump_or_bee_fly(self) -> None:
        for entrance in ("SandCastleBasement to SandCastleBasement (Middle)",
                         "SandCastleBasement (Middle) to SandCastleBasement"):
            way = self.multiworld.get_entrance(entrance, self.player)
            self.assertFalse(way.access_rule(self.state_with()), entrance)
            self.assertTrue(way.access_rule(self.state_with("Jump")), entrance)
            self.assertTrue(way.access_rule(self.state_with("Bee Fly")), entrance)

    def test_spots_wait(self) -> None:
        names = {spot.name for spot in self.multiworld.get_locations(self.player)}
        for spot in ("Switch Puzzle", "Tiny Platform", "Behind the Barrier"):
            self.assertNotIn(f"Ancient Castle: Basement, {spot}", names)


class TestCastleMainRoom(BugFablesTestBase):
    # SandCastleMainRoom (the user, 2026-10-09): a hub of five parts; its two lifts each started by a switch at its top
    # end (any attack), boarded from the bottom with Jump; its two locks each using up an Ancient Key.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def _up(self, area: str):
        return self.multiworld.get_entrance(f"SandCastleMainRoom to SandCastleMainRoom ({area})", self.player)

    def test_middle_right_on_either_lift(self) -> None:
        up = self._up("Middle Right")
        self.assertFalse(up.access_rule(self.state_with("Jump")))
        self.assertTrue(up.access_rule(self.state_with("Jump", "Sand Castle Lower Lift Running")))
        self.assertTrue(up.access_rule(self.state_with("Jump", "Sand Castle Upper Lift Running")))
        self.assertFalse(up.access_rule(self.state_with("Sand Castle Lower Lift Running")))

    def test_top_right_on_the_upper_lift(self) -> None:
        up = self._up("Top Right")
        self.assertFalse(up.access_rule(self.state_with("Jump", "Sand Castle Lower Lift Running")))
        self.assertTrue(up.access_rule(self.state_with("Jump", "Sand Castle Upper Lift Running")))

    def test_the_left_parts_are_cut_off_from_the_bottom(self) -> None:
        everything = self.multiworld.get_all_state()
        for area in ("Middle Left", "Top Left"):
            self.assertFalse(self._up(area).access_rule(everything), area)

    def test_switches_take_any_attack(self) -> None:
        for name, area in (("Lower", "Middle Right"), ("Upper", "Top Right")):
            switch = self.multiworld.get_location(f"Ancient Castle: Main Room, {name} Lift Switch Hit", self.player)
            self.assertEqual(switch.parent_region.name, f"SandCastleMainRoom ({area})")
            self.assertFalse(switch.access_rule(self.state_with()))
            self.assertTrue(switch.access_rule(self.state_with("Horn Slash")))

    # The way up to the castle's top: a ledge outside behind the windows, Jump, or Bee Fly across.
    def test_middle_ledge_both_ways(self) -> None:
        for a, b in (("Middle Right", "Middle Left"), ("Middle Left", "Middle Right")):
            ledge = self.multiworld.get_entrance(
                f"SandCastleMainRoom ({a}) to SandCastleMainRoom ({b}) (ledge outside)", self.player)
            self.assertFalse(ledge.access_rule(self.state_with()), a)
            self.assertTrue(ledge.access_rule(self.state_with("Jump")), a)
            self.assertTrue(ledge.access_rule(self.state_with("Bee Fly")), a)

    # The statue room's lock is the first one reached with a key: the Basement's opens it, the other key not needed.
    def test_statue_lock_takes_the_basement_key(self) -> None:
        bottom = self.multiworld.get_region("SandCastleMainRoom", self.player)
        door = next(e for e in bottom.exits if "SandCastleStatueRoom" in e.connected_region.name)
        self.assertFalse(door.access_rule(self.state_with()))
        state = CollectionState(self.multiworld)
        self.collect_all_but(["Progressive Freeze"], state)
        self.assertTrue(door.access_rule(state))


class TestPressurePuzzle(BugFablesTestBase):
    # SandCastlePressurePuzzle (the user, 2026-10-09): its plates (Freeze and the horn) open its door to the main room
    # for good, or raise platforms to its Ancient Key (then Jump or Bee Fly); that door is shut from inside until then.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_door_plates_need_freeze_and_the_horn(self) -> None:
        plates = self.multiworld.get_location("Ancient Castle: Pressure Puzzle, Door Puzzle Solved", self.player)
        self.assertFalse(plates.access_rule(self.state_with("Jump", "Horn Slash")))
        self.assertFalse(plates.access_rule(self.state_with("Jump", "Progressive Freeze")))
        self.assertTrue(plates.access_rule(self.state_with("Progressive Freeze", "Horn Slash")))

    def test_door_to_the_main_room_waits_for_the_plates(self) -> None:
        room = self.multiworld.get_region("SandCastlePressurePuzzle", self.player)
        door = next(e for e in room.exits if "SandCastleMainRoom" in e.connected_region.name)
        self.assertFalse(door.access_rule(self.state_with("Jump", "Progressive Freeze", "Horn Slash")))
        self.assertTrue(door.access_rule(self.state_with("Sand Castle Pressure Puzzle Door Open")))

    def test_key_waits(self) -> None:
        names = {spot.name for spot in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Ancient Castle: Pressure Puzzle, By the Statue", names)


class TestRockRoom(BugFablesTestBase):
    # SandCastleRockRoom (the user, 2026-10-09): four parts; the bottom's platform from its switch, the top right past a
    # rolling rock and thorns, the top left across once the boulder is broken (Horn Dash, or the rolling rock).
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    @staticmethod
    def _part(area: str) -> str:
        return "SandCastleRockRoom" + (f" ({area})" if area else "")

    def _way(self, a: str, b: str):
        return self.multiworld.get_entrance(f"{self._part(a)} to {self._part(b)}", self.player)

    def test_bottom_right_on_the_platform(self) -> None:
        for way in (self._way("", "Bottom Right"), self._way("Bottom Right", "")):
            self.assertFalse(way.access_rule(self.state_with("Jump", "Bee Fly")))
            self.assertTrue(way.access_rule(self.state_with("Sand Castle Rock Room Platform Running")))

    def test_top_right_past_the_rock_and_the_thorns(self) -> None:
        up = self._way("Bottom Right", "Top Right")
        self.assertFalse(up.access_rule(self.state_with("Shield")))
        self.assertFalse(up.access_rule(self.state_with("Beetle Dig")))
        self.assertTrue(up.access_rule(self.state_with("Beetle Dig", "Shield")))
        self.assertTrue(up.access_rule(self.state_with("Progressive Dash", "Shield")))
        self.assertTrue(up.access_rule(self.state_with("Bee Fly")))
        down = self._way("Top Right", "Bottom Right")
        self.assertTrue(down.access_rule(self.state_with("Shield")))
        self.assertFalse(down.access_rule(self.state_with("Beetle Dig")))

    def test_boulder_both_ways(self) -> None:
        horn = self.multiworld.get_location("Ancient Castle: Rock Room, Boulder Broken with Horn Dash", self.player)
        rock = self.multiworld.get_location("Ancient Castle: Rock Room, Boulder Crushed by the Rolling Rock",
                                            self.player)
        self.assertEqual(horn.parent_region.name, "SandCastleRockRoom (Top Left)")
        self.assertEqual(rock.parent_region.name, "SandCastleRockRoom (Top Right)")
        self.assertTrue(horn.access_rule(self.state_with("Progressive Dash", "Progressive Dash")))
        self.assertFalse(rock.access_rule(self.state_with("Progressive Beemerang")))
        self.assertTrue(rock.access_rule(self.state_with("Progressive Beemerang", "Jump")))

    def test_top_across_needs_the_boulder_broken(self) -> None:
        across = self.multiworld.get_entrance(
            "SandCastleRockRoom (Top Right) to SandCastleRockRoom (Top Left) (ledge)", self.player)
        self.assertFalse(across.access_rule(self.state_with("Bee Fly")))
        self.assertTrue(across.access_rule(self.state_with("Bee Fly", "Sand Castle Rock Room Boulder Broken")))
        back = self.multiworld.get_entrance(
            "SandCastleRockRoom (Top Left) to SandCastleRockRoom (Top Right) (flight)", self.player)
        self.assertFalse(back.access_rule(self.state_with("Progressive Beemerang", "Jump",
                                                          "Sand Castle Rock Room Boulder Broken")))

    def test_berry_waits(self) -> None:
        names = {spot.name for spot in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Ancient Castle: Rock Room, Alcove between the Rocks", names)


class TestCastleRoof(BugFablesTestBase):
    # SandCastleRoof (the user, 2026-10-09): both doors and the save crystal free; the boss door locked from the roof
    # until the Big Ancient Key, still the game's own pickup in the Boss Key Room, taken past a fight that needs Vi
    # (build step 67).
    options = {"starting_party_member": "kabbu"}

    def test_boss_door_needs_the_key_and_its_fight(self) -> None:
        door = self.multiworld.get_entrance(
            next(e.name for e in self.multiworld.get_region("SandCastleRoof", self.player).exits
                 if "SandCastleBossRoom" in e.connected_region.name), self.player)
        self.assertFalse(door.access_rule(self.state_with()))
        state = CollectionState(self.multiworld)
        self.collect_all_but(["Vi"], state)
        self.assertFalse(door.access_rule(state))

    def test_frost_bomb_waits(self) -> None:
        names = {spot.name for spot in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Ancient Castle: Roof, Behind the Left Statue", names)


class TestCastleBossRoom(BugFablesTestBase):
    # SandCastleBossRoom (the user, 2026-10-09): one region, both doors free, the Watcher's fight needing nothing; the
    # wall before the treasure room's door, never solid from that side, hidden (build step 68).
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_one_region_both_doors_free(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("SandCastleBossRoom")}
        self.assertEqual(parts, {"SandCastleBossRoom"})
        exits = self.multiworld.get_region("SandCastleBossRoom", self.player).exits
        reached = {e.connected_region.name for e in exits if e.access_rule(self.state_with())}
        self.assertIn("SandCastleRoof", reached)
        self.assertIn("SandCastleTreasureRoom", reached)

    def test_treasure_room_wall_hidden(self) -> None:
        self.assertIn({"map": "SandCastleBossRoom", "entity": "Base/CastlePlatform"},
                      self.world.fill_slot_data()["scenery_hidden"])


class TestCastleTreasureRoom(BugFablesTestBase):
    # SandCastleTreasureRoom (the user, 2026-10-09): one region, its door free; the artifact on its platform (Jump or
    # Bee Fly) held out with the castle (build step 67), so it never counts toward the goal.

    def test_one_region_door_free(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player)
                 if r.name.startswith("SandCastleTreasureRoom")}
        self.assertEqual(parts, {"SandCastleTreasureRoom"})
        exits = self.multiworld.get_region("SandCastleTreasureRoom", self.player).exits
        self.assertIn("SandCastleBossRoom",
                      {e.connected_region.name for e in exits if e.access_rule(self.state_with())})

    def test_artifact_waits(self) -> None:
        self.assertNotIn(345, {artifact.source.flag for artifact in ARTIFACTS})


class TestOutsideTheBeehive(BugFablesTestBase):
    # BeehiveOutside, Outside the Beehive (the user, 2026-10-09): the bottom (the elevator, the main door) and
    # the left bridge (the side door, the factory's) with no way between; the elevator bee sends the party down to
    # Defiant Root's for nothing, while the way up keeps its stand-in.

    def test_bridge_cut_off(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("BeehiveOutside")}
        self.assertEqual(parts, {"BeehiveOutside", "BeehiveOutside (Left)"})
        everything = self.state_with(*(item.name for item in self.multiworld.itempool if item.player == self.player))
        for way in ("BeehiveOutside to BeehiveOutside (Left)", "BeehiveOutside (Left) to BeehiveOutside"):
            self.assertFalse(self.multiworld.get_entrance(way, self.player).access_rule(everything), way)

    def test_elevator_down_free_up_not(self) -> None:
        down = self.multiworld.get_entrance("BeehiveOutside to DefiantRoot2 (Elevator) (elevator)", self.player)
        up = self.multiworld.get_entrance("DefiantRoot2 (Elevator) to BeehiveOutside (elevator)", self.player)
        self.assertTrue(down.access_rule(self.state_with()))
        self.assertFalse(up.access_rule(self.state_with()))

    def test_factory_door_open(self) -> None:
        # Build step 69: both halves present and both closed models hidden from the start.
        slot = self.world.fill_slot_data()
        for half in ({"map": "BeehiveOutside", "entity": "loadzone factory"},
                     {"map": "HoneyFactoryEntrance", "entity": "loadzoneoutside"}):
            self.assertIn(half, slot["kept_present"])
        for model in ({"map": "BeehiveOutside", "entity": "Base/Door"},
                      {"map": "HoneyFactoryEntrance", "entity": "Base/DoorE"}):
            self.assertIn(model, slot["scenery_hidden"])


class TestThroneRoom(BugFablesTestBase):
    # BeehiveThroneRoom, the Throne Room (the user, 2026-10-09): one region, its door free; the main area's half of
    # that door, made in the game only from 169, kept present with its closed model hidden and its open one shown
    # (build step 70).

    def test_one_region_door_free(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("BeehiveThroneRoom")}
        self.assertEqual(parts, {"BeehiveThroneRoom"})
        exits = self.multiworld.get_region("BeehiveThroneRoom", self.player).exits
        self.assertIn("BeehiveMainArea", {e.connected_region.name.split(" (")[0] for e in exits
                                          if e.access_rule(self.state_with())})

    def test_door_open(self) -> None:
        slot = self.world.fill_slot_data()
        self.assertIn({"map": "BeehiveMainArea", "entity": "loadzone throne"}, slot["kept_present"])
        self.assertIn({"map": "BeehiveMainArea", "entity": "Base/ThroneDoors"}, slot["scenery_hidden"])
        self.assertIn({"map": "BeehiveMainArea", "entity": "Base/ThroneDoors (1)"}, slot["scenery_present"])


class TestJaunesGallery(BugFablesTestBase):
    # JaunesGallery, Jaune's Gallery (the user, 2026-10-09): one region, its door free, the Bad Book behind the
    # paintings needing nothing; the main area's door to it, made in the game only from 299, open with its sign and
    # cube gone (build step 71).

    def test_bad_book_free(self) -> None:
        spot = self.multiworld.get_location("Bee Kingdom Hive: Jaune's Gallery, Behind the Paintings", self.player)
        self.assertEqual(spot.parent_region.name, "JaunesGallery")
        self.assertTrue(spot.access_rule(self.state_with()))

    def test_way_in_open(self) -> None:
        slot = self.world.fill_slot_data()
        self.assertIn({"map": "BeehiveMainArea", "entity": "loadzonejaune"}, slot["kept_present"])
        self.assertIn({"map": "BeehiveMainArea", "entity": "jaune sign"}, slot["kept_open"])
        self.assertIn({"map": "BeehiveMainArea", "entity": "Base/Cube"}, slot["scenery_hidden"])


class TestMainArea(BugFablesTestBase):
    # BeehiveMainArea, the Main Area (the user, 2026-10-09): one region, every door free; the clothing stall's two
    # spots, the Bee Hat and then the Pretty Ribbon, behind Mothiva's scene (flag 173), which needs nothing.

    def test_one_region(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("BeehiveMainArea")}
        self.assertEqual(parts, {"BeehiveMainArea"})

    def test_stall_after_mothiva(self) -> None:
        show = self.multiworld.get_location("Bee Kingdom Hive: Main Area, Mothiva's Show", self.player)
        self.assertTrue(show.access_rule(self.state_with()))
        for n in (1, 2):
            stall = self.multiworld.get_location(f"Bee Kingdom Hive: Main Area, Clothing Stall {n}", self.player)
            self.assertEqual(stall.parent_region.name, "BeehiveMainArea")
            self.assertFalse(stall.access_rule(self.state_with()))
            self.assertTrue(stall.access_rule(self.state_with("Mothiva's Show Seen")))


class TestHBsLab(BugFablesTestBase):
    # HBsLab, HB's Lab (the user, 2026-10-10): one region, its door free; HB asks for a crystal from the start, and
    # the Explorer Permit shown unlocks B.O.S.S. (build step 75).

    def test_one_region(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("HBsLab")}
        self.assertEqual(parts, {"HBsLab"})

    def test_boss_needs_the_permit(self) -> None:
        shown = self.multiworld.get_location("Bee Kingdom Hive: HB's Lab, Explorer Permit Shown", self.player)
        self.assertFalse(shown.access_rule(self.state_with()))
        self.assertTrue(shown.access_rule(self.state_with("Explorer Permit")))
        self.assertIn({"map": "HBsLab", "entity": "HB", "flag": 219, "to": 691},
                      self.world.fill_slot_data()["dialogue_flags"])


class TestBalcony(BugFablesTestBase):
    # BeehiveBalcony, the Balcony (the user, 2026-10-10): one region, its door free; Beette's sale needs nothing in the
    # room, at her price (build step 76), its berries with Next 63.

    def test_one_region(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("BeehiveBalcony")}
        self.assertEqual(parts, {"BeehiveBalcony"})

    def test_sale_free_to_reach(self) -> None:
        spot = self.multiworld.get_location("Bee Kingdom Hive: Balcony, Beette's Sale", self.player)
        self.assertEqual(spot.parent_region.name, "BeehiveBalcony")
        self.assertTrue(spot.access_rule(self.state_with()))


class TestHoneycombsLab(BugFablesTestBase):
    # HoneycombsLab, Honeycomb's Lab (the user, 2026-10-10): one region, its door free; no spot yet.

    def test_one_region(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("HoneycombsLab")}
        self.assertEqual(parts, {"HoneycombsLab"})


class TestLobby(BugFablesTestBase):
    # HoneyFactoryEntrance, the Lobby (the user, 2026-10-10): the bottom a drop from the upper area, Jump or Bee Fly
    # back up; the processing door locked until the Factory Pass (a stand-in until it's an item, Next 67); the storage
    # door open from the start (build step 77).
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_bottom_needs_jump_or_bee_fly_back_up(self) -> None:
        up = self.multiworld.get_entrance("HoneyFactoryEntrance (Bottom) to HoneyFactoryEntrance", self.player)
        self.assertFalse(up.access_rule(self.state_with()))
        self.assertTrue(up.access_rule(self.state_with("Jump")))
        self.assertTrue(up.access_rule(self.state_with("Bee Fly")))

    def test_processing_door_locked(self) -> None:
        door = self.multiworld.get_entrance("HoneyFactoryEntrance: loadzone processing", self.player)
        self.assertFalse(door.access_rule(self.state_with("Jump", "Bee Fly")))

    def test_storage_door_open(self) -> None:
        slot = self.world.fill_slot_data()
        self.assertIn({"map": "HoneyFactoryEntrance", "entity": "loadzonestorage"}, slot["kept_present"])
        self.assertIn({"map": "HoneyFactoryEntrance", "entity": "Base/DoorS"}, slot["scenery_hidden"])

    # The bottom's shop, opened by talking to the bee outside it: five item shop slots, nothing needed there.
    def test_shop_on_the_bottom(self) -> None:
        shops = self.world.fill_slot_data()["location_item_shops"]
        for slot, item in enumerate((10, 9, 20, 49, 43), start=1):
            spot = self.multiworld.get_location(f"Honey Factory: Lobby, Shop {slot}", self.player)
            self.assertEqual(spot.parent_region.name, "HoneyFactoryEntrance (Bottom)")
            self.assertTrue(spot.access_rule(self.state_with()))
            self.assertEqual(shops[str(spot.address)],
                             {"map": "HoneyFactoryEntrance", "keeper": "shopbee - Duplicate", "item": item})


class TestWorkerRooms(BugFablesTestBase):
    # HoneyFactoryWorkerRooms, the Worker Rooms (the user, 2026-10-10): the office and the sleeping quarters cut off
    # from each other; the Shock Candy on the office's desk, Jump or Bee Fly.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_two_parts_cut_off(self) -> None:
        for way in ("HoneyFactoryWorkerRooms to HoneyFactoryWorkerRooms (Sleeping Quarters)",
                    "HoneyFactoryWorkerRooms (Sleeping Quarters) to HoneyFactoryWorkerRooms"):
            self.assertFalse(self.multiworld.get_entrance(way, self.player).access_rule(
                self.state_with("Jump", "Bee Fly")))

    def test_desk_needs_jump_or_bee_fly(self) -> None:
        desk = self.multiworld.get_location("Honey Factory: Worker Rooms, On the Desk", self.player)
        self.assertEqual(desk.parent_region.name, "HoneyFactoryWorkerRooms")
        self.assertFalse(desk.access_rule(self.state_with()))
        self.assertTrue(desk.access_rule(self.state_with("Jump")))
        self.assertTrue(desk.access_rule(self.state_with("Bee Fly")))


class TestFirstRoom(BugFablesTestBase):
    # FactoryProcessingFirstRoom (the user, 2026-10-10): the switch on the right hit with a basic attack starts the
    # platforms for good; across to the left only on them, the Shield (Bee Fly works only before the switch, so never
    # counts); the bottom a drop from either side, Jump or Bee Fly up to the right only.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_switch_needs_a_basic_attack(self) -> None:
        switch = self.multiworld.get_location("Honey Factory: First Room, Switch", self.player)
        self.assertEqual(switch.parent_region.name, "FactoryProcessingFirstRoom")
        self.assertFalse(switch.access_rule(self.state_with("Jump", "Bee Fly", "Shield")))
        self.assertTrue(switch.access_rule(self.state_with("Horn Slash")))

    def test_left_only_on_the_platforms(self) -> None:
        for way in ("FactoryProcessingFirstRoom to FactoryProcessingFirstRoom (Left)",
                    "FactoryProcessingFirstRoom (Left) to FactoryProcessingFirstRoom"):
            rule = self.multiworld.get_entrance(way, self.player).access_rule
            self.assertFalse(rule(self.state_with("Bee Fly", "Jump", "First Room Platforms Running")))
            self.assertFalse(rule(self.state_with("Shield")))
            self.assertTrue(rule(self.state_with("Shield", "First Room Platforms Running")))

    def test_bottom_up_to_the_right_only(self) -> None:
        up = self.multiworld.get_entrance("FactoryProcessingFirstRoom (Bottom) to FactoryProcessingFirstRoom",
                                          self.player)
        self.assertTrue(up.access_rule(self.state_with("Jump")))
        self.assertTrue(up.access_rule(self.state_with("Bee Fly")))
        drop = self.multiworld.get_entrance(
            "FactoryProcessingFirstRoom (Left) to FactoryProcessingFirstRoom (Bottom) (drop)", self.player)
        self.assertFalse(drop.access_rule(self.state_with("Jump")))
        self.assertTrue(drop.access_rule(self.state_with("Jump", "Shield", "First Room Platforms Running")))


class TestProcessing2(BugFablesTestBase):
    # FactoryProcessing2, the Second Room (the user, 2026-10-10): up from the bottom right to the top left, Jump or Bee
    # Fly, a basic attack, the Shield, and Beemerang Halt or Bee Fly; down a drop, a one-way.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_up_to_the_pump_door(self) -> None:
        up = self.multiworld.get_entrance("FactoryProcessing2 to FactoryProcessing2 (Top Left)", self.player)
        self.assertFalse(up.access_rule(self.state_with("Jump", "Horn Slash", "Shield")))
        self.assertFalse(up.access_rule(self.state_with("Bee Fly", "Horn Slash")))
        self.assertTrue(up.access_rule(self.state_with("Bee Fly", "Horn Slash", "Shield")))
        self.assertTrue(up.access_rule(self.state_with("Jump", "Horn Slash", "Shield", "Progressive Beemerang",
                                                       "Progressive Beemerang")))

    def test_down_is_a_one_way(self) -> None:
        down = self.multiworld.get_entrance("FactoryProcessing2 (Top Left) to FactoryProcessing2", self.player)
        self.assertFalse(down.access_rule(self.state_with()))
        self.assertTrue(down.access_rule(self.state_with("Bee Fly", "Horn Slash", "Shield")))


class TestPumpRoom(BugFablesTestBase):
    # FactoryProcessingPump, the Pump Room (the user, 2026-10-10): the top left corner across the Shield or Bee Fly; up
    # to either upper side the cranks (Beemerang Halt), then the platform loop (Jump and the Shield), which joins the
    # two; down a drop, free from the upper left, Bee Fly from the upper right; Malbee's door behind the pass scanner.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}
    HALT = ("Progressive Beemerang", "Progressive Beemerang")

    def way(self, name: str):
        return self.multiworld.get_entrance(name, self.player).access_rule

    def test_top_left_corner(self) -> None:
        corner = self.way("FactoryProcessingPump to FactoryProcessingPump (Bottom Top Left)")
        self.assertFalse(corner(self.state_with("Jump")))
        self.assertTrue(corner(self.state_with("Shield")))
        self.assertTrue(corner(self.state_with("Bee Fly")))

    def test_up_on_the_cranks_and_the_loop(self) -> None:
        for side in ("Upper Left", "Upper Right"):
            up = self.way(f"FactoryProcessingPump to FactoryProcessingPump ({side})")
            self.assertFalse(up(self.state_with("Jump", "Shield")))
            self.assertFalse(up(self.state_with(*self.HALT, "Shield")))
            self.assertTrue(up(self.state_with(*self.HALT, "Jump", "Shield")))
        loop = self.way("FactoryProcessingPump (Upper Left) to FactoryProcessingPump (Upper Right) (platforms)")
        self.assertFalse(loop(self.state_with("Shield")))
        self.assertTrue(loop(self.state_with("Jump", "Shield")))

    def test_drops(self) -> None:
        left = self.way("FactoryProcessingPump (Upper Left) to FactoryProcessingPump")
        right = self.way("FactoryProcessingPump (Upper Right) to FactoryProcessingPump")
        back = (*self.HALT, "Jump", "Shield")
        self.assertTrue(left(self.state_with(*back)))
        self.assertFalse(right(self.state_with(*back)))
        self.assertTrue(right(self.state_with(*back, "Bee Fly")))

    def test_malbee_door_locked(self) -> None:
        door = self.way("FactoryProcessingPump: loadzonemalbee")
        self.assertFalse(door(self.state_with("Jump", "Shield", "Bee Fly")))

    # A respawning Shell Ointment behind boxes in the upper right's left part, across from its door: Shield or Bee Fly.
    def test_behind_the_boxes(self) -> None:
        spot = self.multiworld.get_location("Honey Factory: Pump Room, Behind the Boxes", self.player)
        self.assertEqual(spot.parent_region.name, "FactoryProcessingPump (Upper Right)")
        self.assertFalse(spot.access_rule(self.state_with("Jump")))
        self.assertTrue(spot.access_rule(self.state_with("Shield")))
        self.assertTrue(spot.access_rule(self.state_with("Bee Fly")))


class TestPuzzle1(BugFablesTestBase):
    # FactoryProcessingPuzzle1 (the user, 2026-10-10): one region, its door free; its Factory Pass not a location yet.
    def test_one_region_no_location(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player)
                 if r.name.startswith("FactoryProcessingPuzzle1")}
        self.assertEqual(parts, {"FactoryProcessingPuzzle1"})
        self.assertFalse(self.multiworld.get_region("FactoryProcessingPuzzle1", self.player).locations)


class TestCore(BugFablesTestBase):
    # HoneyFactoryCore (the user, 2026-10-10): one region, its door free; the gate to the boss arena the story's.
    def test_one_region(self) -> None:
        parts = {r.name for r in self.multiworld.get_regions(self.player) if r.name.startswith("HoneyFactoryCore")}
        self.assertEqual(parts, {"HoneyFactoryCore"})


class TestScannerRoom(BugFablesTestBase):
    # BeehiveScannerRoom, the Scanner Room (the user, 2026-10-09): one region, nothing needed across; kept between the
    # outside and the inside (build step 73), its gate open (72), its scan location 208 with flag 160 (74).

    def test_scan_free(self) -> None:
        spot = self.multiworld.get_location("Bee Kingdom Hive: Scanner Room, Scan", self.player)
        self.assertEqual(spot.parent_region.name, "BeehiveScannerRoom")
        self.assertTrue(spot.access_rule(self.state_with()))
        self.assertEqual(self.world.fill_slot_data()["flags_with"], [{"event": 84, "flag": 159, "also": 160}])

    def test_kept_between(self) -> None:
        slot = self.world.fill_slot_data()
        self.assertIn({"map": "BeehiveOutside", "entity": "loadzonecorridor"}, slot["kept_present"])
        for away in ({"map": "BeehiveOutside", "entity": "loadzoneinside"},
                     {"map": "BeehiveScannerRoom", "entity": "eventtrigger2"}):
            self.assertIn(away, slot["kept_open"])
        self.assertIn({"map": "BeehiveScannerRoom", "entity": "Base/Door"}, slot["scenery_hidden"])
        rows = {(r["map"], r["door"]): r for r in slot["door_rows"]}
        top = rows[("BeehiveScannerRoom", "loadzoneinside")]
        self.assertEqual(top["copy"], {"map": "BeehiveScannerRoom", "entity": "loadzoneoutside"})
        self.assertEqual(top["data"][0], 67)
        back = rows[("BeehiveMainArea", "loadzoneoutside - Duplicate")]
        self.assertEqual(back["data"][0], 64)
        self.assertNotIn("copy", back)
        # When a door's data has more than one entry, TransferMap reads data 1-3 and vectordata 3-6 (DoorRows refuses
        # a row that doesn't fit).
        for row in rows.values():
            self.assertTrue(len(row["data"]) == 1 or len(row["data"]) >= 4, row)
            self.assertTrue(len(row["data"]) == 1 or len(row["vectors"]) >= 7, row)
