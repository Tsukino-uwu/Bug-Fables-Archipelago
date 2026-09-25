from . import BugFablesTestBase
from ..data_tables import ITEMS, LOCATIONS


class TestLocationNames(BugFablesTestBase):
    # A location's name says where it is, never what it gives: once items are shuffled, a hint reading
    # "your Hover is at Outskirts: Explorer Permit" points the player at the wrong thing.
    def test_no_location_is_named_after_its_vanilla_item(self) -> None:
        for location in LOCATIONS:
            give = location["source"].get("give") or location["source"].get("pickup")
            if give is None:
                continue
            for item in ITEMS:
                if item["game_id"] == give["item"] and item["kind"] == give["type"]:
                    with self.subTest(location=location["name"]):
                        self.assertNotIn(item["name"].lower(), location["name"].lower())


class TestOptionCounts(BugFablesTestBase):
    # Every toggle that adds locations says how many in its yaml description (the user, 2026-09-25).
    def test_toggles_state_their_check_count(self) -> None:
        from ..options import ShuffleCrystalBerries, ShuffleDiscoveries, ShuffleMedalShops, ShuffleQuests, category_count
        for option, category in ((ShuffleQuests, "quest"), (ShuffleCrystalBerries, "crystal_berry"),
                                 (ShuffleDiscoveries, "discovery"), (ShuffleMedalShops, "shop")):
            with self.subTest(option=option.__name__):
                self.assertGreater(category_count(category), 0)
                self.assertIn(f"Checks added in this version: {category_count(category)}.", option.__doc__)
