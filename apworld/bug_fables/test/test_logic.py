from . import BugFablesTestBase


class TestPermitGate(BugFablesTestBase):
    def test_goal_needs_the_permit(self) -> None:
        # The gate is the Explorer Permit's whole purpose: if this passes with no items,
        # the rule is attached to nothing and fill could put the permit behind its own gate.
        self.assertFalse(self.can_reach_location("Open the Outskirts Gate"))
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Open the Outskirts Gate"))

    def test_outskirts_locations_open_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: Explorer Permit"))
        self.assertTrue(self.can_reach_location("Outskirts: Favor Reward"))

    def test_pool_matches_locations(self) -> None:
        pool = [item for item in self.multiworld.itempool if item.player == self.player]
        locations = [loc for loc in self.multiworld.get_locations(self.player) if loc.address is not None]
        self.assertEqual(len(pool), len(locations))
