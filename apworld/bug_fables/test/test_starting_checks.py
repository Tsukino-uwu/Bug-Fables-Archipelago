from BaseClasses import ItemClassification, LocationProgressType

from . import BugFablesTestBase
from ..items import BugFablesItem

OPENING = ["Outskirts: Maki and Eetl's Gift", "Outskirts: Outside the City, Opening",
           "Outskirts: Outside the City, Tutorial Battle"]
SPIDER = "Snakemouth Den: Fall Room, After the Spider"


def _plain_filler(item) -> bool:
    return not (item.advancement or item.useful or item.trap)


class TestFillerStartingChecksDefault(BugFablesTestBase):
    # On by default: the checks a new file sends by itself hold filler only. Written out, so the base's fill test runs.
    options = {"filler_starting_checks": True}

    def test_opening_checks_excluded(self) -> None:
        for name in OPENING:
            with self.subTest(location=name):
                self.assertEqual(self.world.get_location(name).progress_type, LocationProgressType.EXCLUDED)

    def test_trap_refused(self) -> None:
        trap = BugFablesItem("Crunchy Leaf", ItemClassification.trap, None, self.player)
        leaf = BugFablesItem("Crunchy Leaf", ItemClassification.filler, None, self.player)
        for name in OPENING:
            with self.subTest(location=name):
                location = self.world.get_location(name)
                self.assertFalse(location.item_rule(trap))
                self.assertTrue(location.item_rule(leaf))

    def test_filled_with_plain_filler(self) -> None:
        # The base's own fill test, which fills this multiworld and checks it can be beaten.
        self.test_fill()
        for name in OPENING:
            with self.subTest(location=name):
                item = self.world.get_location(name).item
                self.assertIsNotNone(item)
                self.assertTrue(_plain_filler(item), f"{name} holds {item.name}")

    def test_only_the_opening(self) -> None:
        excluded = {location.name for location in self.multiworld.get_locations(self.player)
                    if location.progress_type == LocationProgressType.EXCLUDED}
        self.assertEqual(excluded, set(OPENING))


class TestFillerStartingChecksOneMember(BugFablesTestBase):
    # With members as items the opening spot is one of them; Leif's spot after the spider is not.
    options = {"starting_party_member": "vi"}

    def test_opening_spot_excluded(self) -> None:
        location = self.world.get_location("Outskirts: Outside the City, Opening")
        self.assertEqual(location.progress_type, LocationProgressType.EXCLUDED)

    def test_spider_spot_untouched(self) -> None:
        location = self.world.get_location(SPIDER)
        self.assertEqual(location.progress_type, LocationProgressType.DEFAULT)
        self.assertTrue(location.item_rule(self.world.create_item("Kabbu")))
        self.assertTrue(location.can_fill(self.multiworld.state, self.world.create_item("Kabbu"), check_access=False))


class TestFillerStartingChecksStoryParty(BugFablesTestBase):
    # With the story's party the opening sends two checks, not three.
    options = {"starting_party_member": "off"}

    def test_two_spots(self) -> None:
        excluded = {location.name for location in self.multiworld.get_locations(self.player)
                    if location.progress_type == LocationProgressType.EXCLUDED}
        self.assertEqual(excluded, {OPENING[0], OPENING[2]})


class TestFillerStartingChecksOff(BugFablesTestBase):
    options = {"filler_starting_checks": False}

    def test_opening_checks_take_anything(self) -> None:
        for name in OPENING:
            with self.subTest(location=name):
                location = self.world.get_location(name)
                self.assertEqual(location.progress_type, LocationProgressType.DEFAULT)
                self.assertTrue(location.item_rule(self.world.create_item("Explorer Permit")))


class TestFillerStartingChecksCoupled(BugFablesTestBase):
    # Coupled doors can leave the start with only the opening's spots: the option stands down for the seed.
    options = {"entrance_randomizer": "coupled"}

    def test_stands_down(self) -> None:
        self.assertFalse(self.world.filler_starting_checks)
        for name in OPENING:
            with self.subTest(location=name):
                self.assertEqual(self.world.get_location(name).progress_type, LocationProgressType.DEFAULT)


class TestFillerStartingChecksRoomSwap(BugFablesTestBase):
    options = {"entrance_randomizer": "room_swap"}

    def test_stands_down(self) -> None:
        self.assertFalse(self.world.filler_starting_checks)
        self.assertEqual(self.world.get_location(OPENING[0]).progress_type, LocationProgressType.DEFAULT)


class TestFillerStartingChecksDecoupled(BugFablesTestBase):
    # Decoupled was measured with 0 failures: the option holds there.
    options = {"entrance_randomizer": "decoupled"}

    def test_holds(self) -> None:
        self.assertTrue(self.world.filler_starting_checks)
        self.assertEqual(self.world.get_location(OPENING[0]).progress_type, LocationProgressType.EXCLUDED)


class TestFillerStartingChecksSmallestPool(BugFablesTestBase):
    # Every category that can be left out is, so the pool is at its smallest: it still holds plain filler for each
    # opening spot, so the option alone can never fail a solo seed.
    options = {"shuffle_quests": False, "shuffle_crystal_berries": False, "shuffle_discoveries": False,
               "shuffle_medal_shops": False, "shuffle_item_shops": False, "starting_party_member": "vi"}

    def test_enough_plain_filler(self) -> None:
        plain = sum(1 for item in self.multiworld.itempool if item.player == self.player and _plain_filler(item))
        self.assertGreaterEqual(plain, len(OPENING))
