from BaseClasses import LocationProgressType

from . import BugFablesTestBase

GIFT = "Bugaria City: Termacade, Arcade Gift"
PRIZES = [f"Bugaria City: Termacade, Prize {n}" for n in range(1, 14)]
PRIZE_ITEMS = ["Bag of Flour", "Magic Seed", "Spicy Fries", "Tangy Berry", "Empower+", "Enfeeble+", "Fortify+",
               "Break+", "Charge Up+", "Venom Ribbon", "Shocking Ribbon", "Drowsy Ribbon", "Helper Boost"]


def _pool(test: BugFablesTestBase) -> list[str]:
    return [item.name for item in test.multiworld.itempool if item.player == test.player]


class TestTermacade(BugFablesTestBase):
    # On by default: the gift and every prize are locations kept to filler, and the prizes are in the pool.

    def test_gift_and_prizes_are_filler_locations(self) -> None:
        for name in (GIFT, *PRIZES):
            with self.subTest(location=name):
                location = self.multiworld.get_location(name, self.player)
                self.assertEqual(location.progress_type, LocationProgressType.EXCLUDED)
                self.assertTrue(location.item.excludable)

    def test_vanilla_items_in_the_pool(self) -> None:
        pool = _pool(self)
        for name in ("15 Tokens", *PRIZE_ITEMS):
            with self.subTest(item=name):
                self.assertIn(name, pool)

    def test_slot_data(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIs(data["options"]["shuffle_termacade"], True)
        self.assertEqual(sorted(data["location_prizes"].values()), list(range(13)))
        gifts = [give for give in data["location_gives"].values() if give.get("npc") == "termiteoutside"]
        self.assertEqual(gifts, [{"map": "BugariaCommercial", "type": 1, "item": 110, "npc": "termiteoutside"}])


class TestTermacadeOff(BugFablesTestBase):
    # Off: the stand is the game's own, but the gift stays.
    options = {"shuffle_termacade": False}

    def test_only_the_gift(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        self.assertIn(GIFT, names)
        self.assertFalse(names & set(PRIZES))
        self.assertEqual(self.world.fill_slot_data()["location_prizes"], {})

    def test_prizes_not_in_the_pool(self) -> None:
        pool = _pool(self)
        self.assertIn("15 Tokens", pool)
        for name in ("Empower+", "Helper Boost", "Venom Ribbon"):
            with self.subTest(item=name):
                self.assertNotIn(name, pool)
