from . import BugFablesTestBase
from ..abilities import ABILITIES

LEARNED = ["Progressive Dash", "Bee Fly", "Beetle Dig", "Shield"]


def _pool(test: BugFablesTestBase) -> list[str]:
    return [item.name for item in test.multiworld.itempool if item.player == test.player]


class TestLearnedAbilities(BugFablesTestBase):
    # Every ability the story teaches is an item, always; the Dash takes two copies.
    def test_always_in_the_pool(self) -> None:
        pool = _pool(self)
        self.assertEqual([pool.count(item) for item in LEARNED], [2, 1, 1, 1])
        self.assertTrue(self.world.fill_slot_data()["ability_items"])

    def test_one_location_per_ability(self) -> None:
        # Each unlock scene is its ability's one location, found by its flag: never a second check for the same one.
        from ..data_tables import LOCATIONS
        for name, ability in ABILITIES.items():
            if ability.flag is None:
                continue
            with self.subTest(ability=name):
                spots = [loc.name for loc in LOCATIONS if loc.source.flag == ability.flag]
                self.assertEqual(len(spots), 1, spots)

    def test_unlock_spots_are_silent(self) -> None:
        # A scene teaches, it shows no item: the client shows the player's own item arriving there.
        from ..data_tables import LOCATIONS
        silent = self.world.fill_slot_data()["silent_locations"]
        flags = {ability.flag for ability in ABILITIES.values() if ability.flag is not None}
        for loc in LOCATIONS:
            if loc.source.flag in flags:
                self.assertIn(self.world.location_name_to_id[loc.name], silent)

    def test_story_order(self) -> None:
        # Until chapters 2-7 get room-level logic, an unlock spot needs every ability taught before it.
        spot = "Swamplands: Bridge"
        self.collect_by_name(["Explorer Permit", "Boat Ticket"])
        self.collect(self.get_items_by_name("Progressive Beemerang"))
        self.collect(self.get_items_by_name("Progressive Dash")[0])
        self.collect_by_name("Shield")
        self.assertFalse(self.can_reach_location(spot))
        self.collect_by_name("Beetle Dig")
        self.assertTrue(self.can_reach_location(spot))

    def test_the_horn_dash_is_the_second_copy(self) -> None:
        # Bee Fly's spot comes after the Horn Dash's: it takes both copies of the Progressive Dash.
        spot = "Barren Lands: Fly Spot"
        self.collect_by_name(["Explorer Permit", "Boat Ticket", "Progressive Beemerang", "Shield", "Beetle Dig"])
        first, second = self.get_items_by_name("Progressive Dash")
        self.collect(first)
        self.assertFalse(self.can_reach_location(spot))
        self.collect(second)
        self.assertTrue(self.can_reach_location(spot))
