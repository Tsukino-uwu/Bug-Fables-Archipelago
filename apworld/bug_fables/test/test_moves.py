from . import BugFablesTestBase

MOVES = ["Beemerang", "Horn", "Ice"]
PAST_THE_GATE = "Outskirts: Near Snakemouth Den, Reward"


def _pool(test: BugFablesTestBase) -> list[str]:
    return [item.name for item in test.multiworld.itempool if item.player == test.player]


class TestMovesOffByDefault(BugFablesTestBase):
    def test_no_move_items(self) -> None:
        pool = _pool(self)
        for move in MOVES + ["Jump"]:
            self.assertNotIn(move, pool)
        data = self.world.fill_slot_data()
        self.assertFalse(data["shuffle_moves"])
        self.assertFalse(data["shuffle_jump"])

    def test_nothing_needs_a_move_item(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location(PAST_THE_GATE))


class TestFieldMoves(BugFablesTestBase):
    options = {"shuffle_field_moves": True}

    def test_three_moves_in_the_pool(self) -> None:
        pool = _pool(self)
        for move in MOVES:
            self.assertEqual(pool.count(move), 1)
        self.assertNotIn("Jump", pool)
        self.assertTrue(self.world.fill_slot_data()["shuffle_moves"])

    def test_past_the_gate_needs_every_move(self) -> None:
        self.collect_by_name("Explorer Permit")
        for move in MOVES:
            self.assertFalse(self.can_reach_location(PAST_THE_GATE))
            self.collect_by_name(move)
        self.assertTrue(self.can_reach_location(PAST_THE_GATE))

    def test_a_horn_spot_needs_the_horn(self) -> None:
        spot = "Outskirts: East Road, Stone"
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Horn")
        self.assertTrue(self.can_reach_location(spot))

    def test_an_ice_spot_needs_the_ice(self) -> None:
        spot = "Bugaria City: Residential District, Fountain Rooftop"
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Ice")
        self.assertTrue(self.can_reach_location(spot))


class TestFieldMovesStoryParty(BugFablesTestBase):
    # With the story's party, Leif's ice needs Leif's own joining too.
    options = {"shuffle_field_moves": True, "starting_party_member": "off"}

    def test_ice_spot_needs_leif_and_the_ice(self) -> None:
        spot = "Bugaria City: Residential District, Fountain Rooftop"
        self.collect_by_name("Ice")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Explorer Permit")
        self.collect_by_name(MOVES)
        self.assertTrue(self.can_reach_location(spot))


class TestJump(BugFablesTestBase):
    options = {"shuffle_jump": True}

    def test_jump_in_the_pool(self) -> None:
        self.assertEqual(_pool(self).count("Jump"), 1)
        self.assertTrue(self.world.fill_slot_data()["shuffle_jump"])

    def test_measured_spots_need_no_jump(self) -> None:
        # The user, 2026-09-27: the opening, the ladybug siblings' house, the caravan and the town's shops.
        for spot in ("Outskirts: Maki and Eetl's Gift", "Outskirts: Ladybug Siblings' House",
                     "Outskirts: Caravan, Item Shop 1", "Bugaria City: Commercial District, Medal Shop 1"):
            with self.subTest(spot=spot):
                self.assertTrue(self.can_reach_location(spot))

    def test_everything_else_needs_jump(self) -> None:
        # Madeleine's house needs a jump (the user); unmeasured spots are cautious.
        spot = "Outskirts: Madeleine's House, Table Right"
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Jump")
        self.assertTrue(self.can_reach_location(spot))

    def test_the_goal_needs_jump(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertFalse(self.can_reach_location("Artifact 1"))
        self.collect_by_name("Jump")
        self.assertTrue(self.can_reach_location("Artifact 1"))


class TestMovesAndJumpTogether(BugFablesTestBase):
    # Both on, one member: fill still finds room for every move early enough (the base tests generate it).
    options = {"shuffle_field_moves": True, "shuffle_jump": True, "starting_party_member": "random_member"}

    def test_all_four_in_the_pool(self) -> None:
        pool = _pool(self)
        for move in MOVES + ["Jump"]:
            self.assertEqual(pool.count(move), 1)
