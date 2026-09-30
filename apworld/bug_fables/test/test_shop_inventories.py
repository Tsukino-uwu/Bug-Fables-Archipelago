from collections import Counter
from random import Random
from typing import Any
from unittest import TestCase

from . import BugFablesTestBase
from ..data_tables import LOCATIONS
from ..shop_inventories import SPOTS, shuffle


def where(swap: dict[str, Any]) -> tuple[Any, ...]:
    return swap["map"], swap.get("keeper"), swap.get("regional"), swap["item"]


def sells_twice(swaps: list[dict[str, Any]]) -> bool:
    sold = [(swap["map"], swap["keeper"], swap["to"]) for swap in swaps if "keeper" in swap]
    return len(sold) != len(set(sold))


class TestShopInventoriesOff(BugFablesTestBase):
    options = {"shuffle_shop_inventories": False}

    def test_nothing_swapped(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["shop_inventories"], [])


class TestShopInventories(BugFablesTestBase):
    # On by default.

    def test_every_spot_once(self) -> None:
        swaps = self.world.fill_slot_data()["shop_inventories"]
        self.assertEqual([where(swap) for swap in swaps],
                         [(spot.map, spot.keeper, spot.regional, spot.item) for spot in SPOTS])

    def test_items_only_trade_places(self) -> None:
        swaps = self.world.fill_slot_data()["shop_inventories"]
        self.assertEqual(Counter(swap["to"] for swap in swaps), Counter(swap["item"] for swap in swaps))

    def test_items_move(self) -> None:
        swaps = self.world.fill_slot_data()["shop_inventories"]
        self.assertTrue(any(swap["to"] != swap["item"] for swap in swaps))

    def test_no_shop_sells_an_item_twice(self) -> None:
        self.assertFalse(sells_twice(self.world.fill_slot_data()["shop_inventories"]))


class TestShopInventorySpots(TestCase):
    def test_every_item_shop_slot_and_respawning_pickup(self) -> None:
        shops = [loc for loc in LOCATIONS if loc.source.item_shop is not None]
        respawning = [loc for loc in LOCATIONS if loc.source.regional is not None]
        self.assertEqual(len(SPOTS), len(shops) + len(respawning))
        self.assertTrue(shops and respawning)

    def test_no_repeat_in_many_rolls(self) -> None:
        for seed in range(500):
            swaps = shuffle(SPOTS, Random(seed))
            self.assertFalse(sells_twice(swaps), seed)
            self.assertEqual(Counter(swap["to"] for swap in swaps), Counter(spot.item for spot in SPOTS), seed)


class TestShopInventoriesWithItemShopsOff(BugFablesTestBase):
    # The shops aren't locations then, but still sell the seed's stock.
    options = {"shuffle_item_shops": False}

    def test_shops_still_shuffled(self) -> None:
        swaps = self.world.fill_slot_data()["shop_inventories"]
        self.assertEqual(len(swaps), len(SPOTS))
        self.assertTrue(any("keeper" in swap for swap in swaps))


class TestShopInventoriesChangeNothingElse(BugFablesTestBase):
    # No check, location or rule depends on it: with the same seed, everything else must be the same, music included.
    auto_construct = False

    def generated(self, options: dict[str, Any]) -> tuple[dict[str, Any], list[str], object]:
        self.options = options
        self.world_setup(seed=42)
        data = {key: value for key, value in self.world.fill_slot_data().items() if key != "shop_inventories"}
        return data, [item.name for item in self.multiworld.itempool], self.multiworld.random.getstate()

    def test_same_seed_same_world(self) -> None:
        base = {"enemy_shuffle": "enemies_only", "entrance_randomizer": "coupled", "starting_location": "anywhere",
                "music_shuffle": True}
        off = self.generated({**base, "shuffle_shop_inventories": False})
        on = self.generated(base)
        self.assertEqual(off, on)
