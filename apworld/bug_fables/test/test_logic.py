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
        flags = self.world.fill_slot_data()["location_flags"]
        ids = {str(loc.address) for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        self.assertEqual(set(flags), ids)
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
