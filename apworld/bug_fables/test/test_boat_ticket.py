from . import BugFablesTestBase


class TestBoatTicket(BugFablesTestBase):
    def test_in_the_pool_once(self) -> None:
        tickets = [item for item in self.multiworld.itempool if item.player == self.player and item.name == "Boat Ticket"]
        self.assertEqual(len(tickets), 1)
        self.assertTrue(tickets[0].advancement)

    def test_metal_island_needs_the_ticket(self) -> None:
        self.collect_all_but(["Boat Ticket"])
        self.assertFalse(self.can_reach_region("Metal Island"))
        self.collect_by_name(["Boat Ticket"])
        self.assertTrue(self.can_reach_region("Metal Island"))
