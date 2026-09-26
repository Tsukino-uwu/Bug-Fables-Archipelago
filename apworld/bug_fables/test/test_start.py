from . import BugFablesTestBase
from ..data_tables import DOORS, ROOM_STARTS


class TestStartOffByDefault(BugFablesTestBase):
    def test_game_start(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["start"], {})


class TestStartAnywhere(BugFablesTestBase):
    options = {"starting_location": "anywhere"}

    def test_start_is_a_room_entered_through_a_door(self) -> None:
        start = self.world.fill_slot_data()["start"]
        self.assertIn(start, ROOM_STARTS)
        self.assertIn({start["map"], start["from"]},
                      [{c["a"]["map"], c["b"]["map"]} for c in DOORS["connections"]])

    def test_every_room_can_be_picked_both_ways(self) -> None:
        # A connection between two maps gives an arrival into each.
        for c in DOORS["connections"]:
            if c["a"]["map"] != c["b"]["map"]:
                self.assertIn({"map": c["b"]["map"], "from": c["a"]["map"]}, ROOM_STARTS)
                self.assertIn({"map": c["a"]["map"], "from": c["b"]["map"]}, ROOM_STARTS)

    def test_same_seed_same_start(self) -> None:
        # generate_early ran once with the seed's random; asking twice gives what it chose, not a new pick.
        self.assertEqual(self.world.fill_slot_data()["start"], self.world.fill_slot_data()["start"])
