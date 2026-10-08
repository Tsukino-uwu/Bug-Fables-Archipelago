"""Rules from the room mapping that the generic tests don't reach: a trade's item, a gate kept shut, Riz's fight."""
from . import BugFablesTestBase
from ..custom_rules import ItemOnHand
from ..data_tables import door_name

PINK_SPIDER = "Forsaken Lands: Pink Spider, First Trade"
ITEM_SHOP_ROOMS = {"GoldenSettlement1", "GoldenSettlementEntrance", "BugariaCommercial", "BugariaOutskirtsOutsideCity"}


class TestItemOnHand(BugFablesTestBase):
    # An item to trade comes from an item shop reached: its slots sell their own item without end once bought.
    def test_it_waits_on_the_item_shops(self) -> None:
        self.assertEqual(set(ItemOnHand().resolve(self.world).region_dependencies()), ITEM_SHOP_ROOMS)

    def test_the_pink_spiders_trade_needs_one(self) -> None:
        rule = self.multiworld.get_location(PINK_SPIDER, self.player).access_rule
        self.assertTrue(ITEM_SHOP_ROOMS <= set(rule.region_dependencies()))


class TestWaspFrontGateShut(BugFablesTestBase):
    # The Wasp Kingdom's front gate (the lake to outside the hive) exists only after the story: shut for good, it is
    # left out of the door graph (SHUT_DOORS), so neither side is an entrance and the randomizer never shuffles it.
    def test_neither_side_opens(self) -> None:
        made = {entrance.name for entrance in self.multiworld.get_entrances(self.player)}
        for door in (door_name("FarGrasslandsLake", "loadzonewasp"), door_name("WaspKingdomOutside", "loadzonesouth")):
            with self.subTest(door=door):
                self.assertNotIn(door, made)


class TestRizOffersHisFight(BugFablesTestBase):
    # Maki never leaves (the swamp bridge kept up), so the client must always let Riz offer his fight.
    def test_the_client_is_told(self) -> None:
        self.assertIs(self.world.fill_slot_data()["riz_fight_with_follower"], True)
