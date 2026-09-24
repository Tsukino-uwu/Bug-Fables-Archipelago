from . import BugFablesTestBase
from ..data_tables import ITEMS, LOCATIONS


class TestLocationNames(BugFablesTestBase):
    # A location's name says where it is, never what it gives: once items are shuffled, a hint reading
    # "your Hover is at Outskirts: Explorer Permit" points the player at the wrong thing.
    def test_no_location_is_named_after_its_vanilla_item(self) -> None:
        for location in LOCATIONS:
            give = location["source"].get("give")
            if give is None:
                continue
            for item in ITEMS:
                if item["game_id"] == give["item"] and item["kind"] == give["type"]:
                    with self.subTest(location=location["name"]):
                        self.assertNotIn(item["name"].lower(), location["name"].lower())
