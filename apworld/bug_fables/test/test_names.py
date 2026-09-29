from unittest import TestCase

from . import BugFablesTestBase
from ..data_tables import LOCATIONS, vanilla_item
from ..data_types import Item


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
    # A misspelt key in a data file would otherwise be ignored. (The logic is Python: a misspelt keyword fails itself.)
    def test_unknown_key_refused(self) -> None:
        entry = {"name": "Test Item", "game_id": 999, "kind": 0, "classification": "filler", "paddnig": True}
        with self.assertRaises(ValueError):
            Item.from_json(entry)
