from unittest import TestCase

from . import BugFablesTestBase
from ..data_tables import LOCATIONS, vanilla_item
from ..data_types import Location


class TestLocationNames(BugFablesTestBase):
    # A location named after its vanilla item misleads hints once items are shuffled.
    def test_no_location_is_named_after_its_vanilla_item(self) -> None:
        for location in LOCATIONS:
            item = vanilla_item(location)
            if item is None:
                continue
            with self.subTest(location=location.name):
                self.assertNotIn(item.lower(), location.name.lower())


class TestOptionCounts(BugFablesTestBase):
    # Every toggle that adds locations states how many in its player-visible description.
    def test_toggles_state_their_check_count(self) -> None:
        from ..options import (ShuffleCrystalBerries, ShuffleDiscoveries, ShuffleItemShops, ShuffleMedalShops,
                               ShuffleQuests, category_count)
        for option, category in ((ShuffleQuests, "quest"), (ShuffleCrystalBerries, "crystal_berry"),
                                 (ShuffleDiscoveries, "discovery"), (ShuffleMedalShops, "shop"),
                                 (ShuffleItemShops, "item_shop")):
            with self.subTest(option=option.__name__):
                self.assertGreater(category_count(category), 0)
                self.assertIn(f"Checks added in this version: {category_count(category)}.", option.__doc__)


class TestDataRecords(TestCase):
    # A misspelt key in the data would otherwise be ignored, and its rule silently lost.
    def test_unknown_key_refused(self) -> None:
        entry = {"name": "Test Spot", "id": 999, "region": "Menu", "source": {"flag": 1},
                 "requirse": ["Explorer Permit"]}
        with self.assertRaises(ValueError):
            Location.from_json(entry)

    def test_unknown_source_key_refused(self) -> None:
        with self.assertRaises(ValueError):
            Location.from_json({"name": "Test Spot", "id": 999, "region": "Menu", "source": {"flga": 1}})
