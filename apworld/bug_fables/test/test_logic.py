from BaseClasses import CollectionState

from . import BugFablesTestBase
from ..data_tables import STORY_EVENTS


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
