from . import BugFablesTestBase
from ..data_tables import STARTS


class TestStartOffByDefault(BugFablesTestBase):
    def test_game_start(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["start"], {})


class TestStartAnywhere(BugFablesTestBase):
    options = {"starting_location": "anywhere"}

    def test_start_is_a_save_point(self) -> None:
        start = self.world.fill_slot_data()["start"]
        self.assertIn({"map": start["map"], "entity": start["entity"]}, STARTS)

    def test_same_seed_same_start(self) -> None:
        # generate_early ran once with the seed's random; asking twice gives what it chose, not a new pick.
        self.assertEqual(self.world.fill_slot_data()["start"], self.world.fill_slot_data()["start"])
