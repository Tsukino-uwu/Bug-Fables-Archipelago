from . import BugFablesTestBase

MOVES = ["Progressive Beemerang", "Horn Slash", "Progressive Freeze"]
PAST_THE_GATE = "Outskirts: Near Snakemouth Den, Horn Tutorial"


def _pool(test: BugFablesTestBase) -> list[str]:
    return [item.name for item in test.multiworld.itempool if item.player == test.player]


class TestMovesOffByDefault(BugFablesTestBase):
    def test_no_move_items(self) -> None:
        # The party starts with its attacks: no Horn Slash or Jump item, and one copy of each progressive attack, its
        # upgrade (the Halt, the Icicle).
        pool = _pool(self)
        for move in ("Horn Slash", "Jump"):
            self.assertNotIn(move, pool)
        self.assertEqual(pool.count("Progressive Beemerang"), 1)
        self.assertEqual(pool.count("Progressive Freeze"), 1)
        options = self.world.fill_slot_data()["options"]
        self.assertIs(options["shuffle_field_moves"], False)
        self.assertIs(options["shuffle_jump"], False)

    def test_nothing_needs_a_move_item(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location(PAST_THE_GATE))


class TestFieldMoves(BugFablesTestBase):
    options = {"shuffle_field_moves": True, "shuffle_hidden_items": True}

    def test_three_moves_in_the_pool(self) -> None:
        # The attack, then for the two progressive ones its upgrade.
        pool = _pool(self)
        self.assertEqual([pool.count(move) for move in MOVES], [2, 1, 2])
        self.assertNotIn("Jump", pool)
        self.assertIs(self.world.fill_slot_data()["options"]["shuffle_field_moves"], True)

    def test_past_the_gate_needs_every_move(self) -> None:
        self.collect_by_name("Explorer Permit")
        for move in MOVES:
            self.assertFalse(self.can_reach_location(PAST_THE_GATE))
            self.collect_by_name(move)
        self.assertTrue(self.can_reach_location(PAST_THE_GATE))

    def test_a_horn_spot_needs_the_horn(self) -> None:
        spot = "Outskirts: East Road, Boulder"
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Horn Slash")
        self.assertTrue(self.can_reach_location(spot))

    def test_an_ice_spot_needs_the_ice(self) -> None:
        spot = "Bugaria City: Residential District, Fountain Rooftop"
        self.assertFalse(self.can_reach_location(spot))
        self.collect(self.get_item_by_name("Progressive Freeze"))
        self.assertTrue(self.can_reach_location(spot))


class TestFieldMovesStoryParty(BugFablesTestBase):
    # With the story's party, Leif's ice needs Leif's own joining too.
    options = {"shuffle_field_moves": True, "starting_party_member": "off"}

    def test_ice_spot_needs_leif_and_the_ice(self) -> None:
        spot = "Bugaria City: Residential District, Fountain Rooftop"
        self.collect(self.get_item_by_name("Progressive Freeze"))
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Explorer Permit")
        self.collect_by_name(MOVES)
        self.assertTrue(self.can_reach_location(spot))


class TestJump(BugFablesTestBase):
    options = {"shuffle_jump": True}

    def test_jump_in_the_pool(self) -> None:
        self.assertEqual(_pool(self).count("Jump"), 1)
        self.assertIs(self.world.fill_slot_data()["options"]["shuffle_jump"], True)

    def test_measured_spots_need_no_jump(self) -> None:
        # Seen reachable without a jump: the opening, the ladybug siblings' house, the caravan and the town's shops.
        for spot in ("Outskirts: Maki and Eetl's Gift", "Outskirts: Ladybug Siblings' House",
                     "Outskirts: Caravan, Item Shop 1", "Bugaria City: Commercial District, Medal Shop 1"):
            with self.subTest(spot=spot):
                self.assertTrue(self.can_reach_location(spot))

    def test_everything_else_needs_jump(self) -> None:
        # Madeleine's house needs a jump; unmeasured spots are cautious.
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
        self.assertEqual([pool.count(move) for move in MOVES + ["Jump"]], [2, 1, 2, 1])
