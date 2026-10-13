"""Rules from the room mapping that the generic tests don't reach: a trade's item, a gate kept shut, Riz's fight."""
from BaseClasses import CollectionState

from ..custom_rules import ItemOnHand
from ..data_tables import DOOR_LABELS, game_door_name
from ..logic import PRESENT_FROM
from . import BugFablesTestBase

PINK_SPIDER = "Forsaken Lands: Pink Spider, First Trade"
ITEM_SHOP_ROOMS = {"GoldenSettlement1", "GoldenSettlementEntrance", "BugariaCommercial", "BugariaOutskirtsOutsideCity",
                   "HoneyFactoryEntrance (Bottom)", "DefiantRoot3", "GiantLairRoachVillage", "MetalIsland1",
                   "TermiteMainPlaza", "TermiteIndustrial", "TermiteColiseum1", "WaspKingdomJayde"}


class TestItemOnHand(BugFablesTestBase):
    # An item to trade comes from an item shop reached: its slots sell their own item without end once bought.
    def test_it_waits_on_the_item_shops(self) -> None:
        self.assertEqual(set(ItemOnHand().resolve(self.world).region_dependencies()), ITEM_SHOP_ROOMS)

    def test_serialized_as_the_item_shops(self) -> None:
        # A tracker reads it as built-in rules: the Or of the item shops' regions, which reads back as the same rule.
        data = ItemOnHand().to_dict()
        self.assertEqual(data["rule"], "Or")
        regions = set()
        for child in data["children"]:
            reach = child if child["rule"] == "CanReachRegion" else child["children"][0]
            regions.add(reach["args"]["region_name"])
        self.assertEqual(regions, ITEM_SHOP_ROOMS)
        rule = self.world.rule_from_dict(data)
        self.assertEqual(rule.to_dict(), data)
        self.assertEqual(set(rule.resolve(self.world).region_dependencies()), ITEM_SHOP_ROOMS)

    def test_the_pink_spiders_trade_needs_one(self) -> None:
        rule = self.multiworld.get_location(PINK_SPIDER, self.player).access_rule
        self.assertTrue(ITEM_SHOP_ROOMS <= set(rule.region_dependencies()))


class TestRustyKeyFromTheWell(BugFablesTestBase):
    # The Rusty Key's seller, in the game only from chapter 4's start, is at the well from the start: the hideout's
    # badlands door counts once the well is reached.
    def test_astotheles_is_there_from_the_start(self) -> None:
        self.assertIn(("DefiantRootWell", "astotheles"), {(e.map, e.entity) for e in PRESENT_FROM})

    def test_the_hideout_door_needs_the_well(self) -> None:
        rule = self.multiworld.get_entrance("DesertBadlands to DesertBadlands (Hideout Door)", self.player).access_rule
        self.assertIn("DefiantRootWell", set(rule.region_dependencies()))


class TestBigCrankFromBothHalves(BugFablesTestBase):
    # The Big Crank is the game's own, its top half across the right crank room: without the Shield and Bee Fly, the
    # elevator it works never counts.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_the_elevator_needs_the_top_half(self) -> None:
        state = CollectionState(self.multiworld)
        for item in self.multiworld.itempool:
            if item.player == self.player and item.name not in ("Shield", "Bee Fly"):
                state.collect(item, prevent_sweep=True)
        state.sweep_for_advancements()
        self.assertFalse(state.can_reach_region("GoldenHillsDungeonUpperMain", self.player))
        state.collect(self.world.create_item("Bee Fly"))
        self.assertTrue(state.can_reach_region("GoldenHillsDungeonUpperMain", self.player))


class TestWaspFrontGateShut(BugFablesTestBase):
    # The Wasp Kingdom's front gate (the lake to outside the hive) exists only after the story: shut for good, it is
    # left out of the door graph (SHUT_DOORS), so neither side is an entrance and the randomizer never shuffles it.
    def test_neither_side_opens(self) -> None:
        made = {entrance.name for entrance in self.multiworld.get_entrances(self.player)}
        for door in (("FarGrasslandsLake", "loadzonewasp"), ("WaspKingdomOutside", "loadzonesouth")):
            with self.subTest(door=game_door_name(*door)):
                self.assertNotIn(door, DOOR_LABELS)
                self.assertNotIn(game_door_name(*door), made)


class TestRizOffersHisFight(BugFablesTestBase):
    # Maki never leaves (the swamp bridge kept up), so the client must always let Riz offer his fight.
    def test_the_client_is_told(self) -> None:
        self.assertIs(self.world.fill_slot_data()["riz_fight_with_follower"], True)
