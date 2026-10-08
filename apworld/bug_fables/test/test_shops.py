from unittest import TestCase

from BaseClasses import LocationProgressType
from worlds.generic.Rules import add_item_rule

from . import BugFablesTestBase, generate_like_main
from ..rules import fall_back_from_filler_only


class TestMedalShop(BugFablesTestBase):
    # Merab's full stock is 22 locations, one per copy (TP Plus and Ambusher twice).
    def test_stock_in_slot_data(self) -> None:
        data = self.world.fill_slot_data()
        first = str(self.world.location_name_to_id["Bugaria City: Commercial District, Medal Shop 1"])
        self.assertEqual(data["location_shops"][first], {"shop": 0, "medal": 0})
        self.assertEqual(data["location_gives"][first], {"map": "BugariaCommercial", "type": 2, "item": 0})
        self.assertEqual(len(data["location_shops"]), 22)

    def test_full_stock_with_duplicates(self) -> None:
        # The stock in the order the story builds it: the mod treats a shop's copies in location id order.
        shops = self.world.fill_slot_data()["location_shops"]
        medals = [shops[key]["medal"] for key in sorted(shops, key=int) if shops[key]["shop"] == 0]
        self.assertEqual(medals, [0, 1, 7, 12, 30, 86, 84, 87, 88, 81, 21, 22, 48, 33, 56, 74, 45, 1, 86, 62, 41, 85])

    def test_each_copy_puts_its_medal_in_the_pool(self) -> None:
        names = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertEqual(names.count("TP Plus"), 2)
        self.assertEqual(names.count("Ambusher"), 2)
        self.assertEqual(names.count("We Owe Ya!"), 1)


class TestItemShop(BugFablesTestBase):
    # Item shop checks carry no give entry, so no unrelated giveitem of that item is swapped.
    def test_slot_data(self) -> None:
        data = self.world.fill_slot_data()
        shops = {k: e for k, e in data["location_item_shops"].items() if e["keeper"] == "ButterflyShopkeeper"}
        self.assertEqual(sorted(entry["item"] for entry in shops.values()), [0, 1, 13, 17, 26])
        self.assertTrue(all(entry == {"map": "BugariaCommercial", "keeper": "ButterflyShopkeeper",
                                      "item": entry["item"]}
                            for entry in shops.values()))
        for key in shops:
            self.assertNotIn(key, data["location_gives"])

    def test_items_in_pool(self) -> None:
        names = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Aphid Egg", names)
        self.assertIn("Danger Shroom", names)

    def test_shop_contents_applies(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Item Shop 1")
        self.assertFalse(shop.item_rule(self.world.create_item("Explorer Permit")))


class TestItemShopsOff(BugFablesTestBase):
    options = {"shuffle_item_shops": False}

    def test_no_item_shop_locations(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["location_item_shops"], {})


class TestMedalShopsOff(BugFablesTestBase):
    options = {"shuffle_medal_shops": False}

    def test_no_shop_locations(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["location_shops"], {})


class TestShopContentsDefault(BugFablesTestBase):
    # By default shops refuse progression items.
    def test_shop_refuses_progression(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertFalse(shop.item_rule(self.world.create_item("Explorer Permit")))
        self.assertTrue(shop.item_rule(self.world.create_item("TP Plus")))

    def test_no_progression_placed_in_shops(self) -> None:
        for location in self.multiworld.get_locations(self.player):
            if "Medal Shop" in location.name and location.item is not None:
                with self.subTest(location=location.name):
                    self.assertFalse(location.item.advancement)


class TestShopContentsFillerOnly(BugFablesTestBase):
    # With discoveries and hidden items on, a solo seed with the story's party has filler enough for every shop
    # location, so Filler Only holds (the shops are excluded). Without hidden items it falls short since Crystal Berry
    # and Hard Mode became useful (2026-10-08).
    options = {"shop_contents": "filler_only", "shuffle_discoveries": True, "shuffle_hidden_items": True,
               "starting_party_member": "off", "filler_starting_checks": False}

    def test_enough_filler_with_discoveries_and_hidden_items(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertEqual(shop.progress_type, LocationProgressType.EXCLUDED)
        self.assertFalse(self.world.shops_fell_back)
        self.assertEqual(self.world.fill_slot_data()["options"]["shop_contents"], 2)


class TestShopContentsFillerOnlyAfterStartingChecks(BugFablesTestBase):
    # The same seed with Filler Starting Checks on: the opening's spots and the shops are all excluded, and the room
    # has filler enough for both, so nothing falls back.
    options = {"shop_contents": "filler_only", "shuffle_discoveries": True, "shuffle_hidden_items": True,
               "starting_party_member": "off"}

    def test_opening_and_shops_both_filler_only(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertEqual(shop.progress_type, LocationProgressType.EXCLUDED)
        self.assertFalse(self.world.shops_fell_back)
        gift = self.world.get_location("Outskirts: Maki and Eetl's Gift")
        self.assertEqual(gift.progress_type, LocationProgressType.EXCLUDED)


class TestShopContentsFillerOnlyFallsBack(BugFablesTestBase):
    # A solo seed without crystal berries is short of filler, and since Crystal Berry and Hard Mode became useful
    # (2026-10-08) the default seed is too: shops fall back to No Progression and still generate.
    options = {"shop_contents": "filler_only", "shuffle_crystal_berries": False}

    def test_shops_fall_back_to_no_progression(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertEqual(shop.progress_type, LocationProgressType.DEFAULT)
        self.assertFalse(shop.item_rule(self.world.create_item("Explorer Permit")))
        self.assertTrue(shop.item_rule(self.world.create_item("TP Plus")))

    def test_slot_data_sends_the_fallback(self) -> None:
        # Universal Tracker never runs pre_fill: slot_data tells it the shops ended up No Progression.
        self.assertTrue(self.world.shops_fell_back)
        self.assertEqual(self.world.fill_slot_data()["options"]["shop_contents"], 1)


class TestShopFallbackKeepsThePlayersRules(TestCase):
    # The fallback runs after Archipelago applied the player's exclusions (and, with other players, item locality): it
    # must leave both in place.
    def test_an_excluded_shop_stays_excluded(self) -> None:
        excluded = "Bugaria City: Commercial District, Medal Shop 2"
        world = generate_like_main({"shop_contents": "filler_only", "shuffle_crystal_berries": False,
                                    "exclude_locations": [excluded]}, seed=1)
        self.assertTrue(world.shops_fell_back)
        self.assertEqual(world.get_location(excluded).progress_type, LocationProgressType.EXCLUDED)
        other = world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertEqual(other.progress_type, LocationProgressType.DEFAULT)

    def test_an_item_rule_already_there_stays(self) -> None:
        world = generate_like_main({"shop_contents": "filler_only", "shuffle_crystal_berries": False}, seed=1, steps=(
            "generate_early", "create_regions", "create_items", "set_rules", "connect_entrances", "generate_basic"))
        shop = world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        add_item_rule(shop, lambda item: item.name != "TP Plus")
        fall_back_from_filler_only(world)
        self.assertTrue(world.shops_fell_back)
        self.assertFalse(shop.item_rule(world.create_item("TP Plus")))
        self.assertFalse(shop.item_rule(world.create_item("Explorer Permit")))
        self.assertTrue(shop.item_rule(world.create_item("Ambusher")))


class TestShopContentsAnything(BugFablesTestBase):
    options = {"shop_contents": "anything"}

    def test_shop_takes_progression(self) -> None:
        shop = self.world.get_location("Bugaria City: Commercial District, Medal Shop 1")
        self.assertTrue(shop.item_rule(self.world.create_item("Explorer Permit")))
