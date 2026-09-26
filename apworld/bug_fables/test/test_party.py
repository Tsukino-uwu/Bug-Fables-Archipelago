from . import BugFablesTestBase

MEMBERS = ["Vi", "Kabbu", "Leif"]
JOINS = ["Outskirts: Outside the City, Opening", "Snakemouth Den: Fall Room, After the Spider"]


def _names(items) -> list[str]:
    return [item.name for item in items]


class TestPartyDefault(BugFablesTestBase):
    def test_all_three_by_default(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["starting_member"], 3)


class TestPartyOff(BugFablesTestBase):
    options = {"starting_party_member": "off"}

    def test_story_party(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["starting_member"], -1)
        pool = _names(item for item in self.multiworld.itempool if item.player == self.player)
        for member in MEMBERS:
            self.assertNotIn(member, pool)
        locations = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for name in JOINS:
            self.assertNotIn(name, locations)
        self.assertIn("Leif Joins", locations)
        self.assertEqual(self.world.fill_slot_data()["silent_locations"], [])

    def test_horn_spots_need_no_member(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: East Road, Stone"))


class _StartWith:
    # A mixin, not a test case: only the classes below, which name a start, run it.
    start = ""

    def test_start_member_is_start_inventory(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["starting_member"], MEMBERS.index(self.start))
        self.assertEqual(_names(self.multiworld.precollected_items[self.player]), [self.start])
        self.assertIsNotNone(self.multiworld.precollected_items[self.player][0].code)

    def test_other_two_are_in_the_pool(self) -> None:
        pool = _names(item for item in self.multiworld.itempool if item.player == self.player)
        for member in MEMBERS:
            self.assertEqual(pool.count(member), 0 if member == self.start else 1)

    def test_joining_moments_are_locations(self) -> None:
        locations = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for name in JOINS:
            self.assertIn(name, locations)
        # Leif's story event would hand him out for free.
        self.assertNotIn("Leif Joins", locations)
        flags = self.world.fill_slot_data()["location_flags"]
        self.assertEqual(flags[str(self.world.location_name_to_id[JOINS[0]])], 15)
        self.assertEqual(flags[str(self.world.location_name_to_id[JOINS[1]])], 27)

    def test_joining_moments_are_silent(self) -> None:
        # Neither shows an item of its own, so the client shows the player's own item arriving there.
        self.assertEqual(self.world.fill_slot_data()["silent_locations"],
                         sorted(self.world.location_name_to_id[name] for name in JOINS))

    def test_past_the_gate_needs_all_three(self) -> None:
        self.collect_by_name("Explorer Permit")
        others = [member for member in MEMBERS if member != self.start]
        self.collect_by_name(others[0])
        self.assertFalse(self.can_reach_location("Outskirts: Near Snakemouth Den, Reward"))
        self.collect_by_name(others[1])
        self.assertTrue(self.can_reach_location("Outskirts: Near Snakemouth Den, Reward"))

    def test_horn_spots_need_kabbu(self) -> None:
        spots = ["Outskirts: East Road, Stone", "Bugaria City: Residential District, Rooftop"]
        for spot in spots:
            self.assertEqual(self.can_reach_location(spot), self.start == "Kabbu")
        self.collect_by_name("Kabbu")
        for spot in spots:
            self.assertTrue(self.can_reach_location(spot))

    def test_pool_matches_locations(self) -> None:
        pool = [item for item in self.multiworld.itempool if item.player == self.player]
        locations = [loc for loc in self.multiworld.get_locations(self.player) if loc.address is not None]
        self.assertEqual(len(pool), len(locations))


class TestStartVi(_StartWith, BugFablesTestBase):
    options = {"starting_party_member": "vi"}
    start = "Vi"


class TestStartKabbu(_StartWith, BugFablesTestBase):
    options = {"starting_party_member": "kabbu"}
    start = "Kabbu"


class TestStartLeif(_StartWith, BugFablesTestBase):
    options = {"starting_party_member": "leif"}
    start = "Leif"


class TestStartRandomMember(BugFablesTestBase):
    options = {"starting_party_member": "random_member"}

    def test_one_member_picked(self) -> None:
        start = self.world.fill_slot_data()["starting_member"]
        self.assertIn(start, range(3))
        self.assertEqual(_names(self.multiworld.precollected_items[self.player]), [MEMBERS[start]])


class TestAbilities(BugFablesTestBase):
    # Rules name a field move; until moves are items, each attack is its member's and Jump the whole party's.
    options = {"starting_party_member": "leif"}

    def test_every_ability_in_the_data_is_known(self) -> None:
        from ..data_tables import LOCATIONS, REGIONS
        named = {ability for loc in LOCATIONS for ability in loc.get("abilities", [])}
        named |= {ability for region in REGIONS for exit_data in region["exits"] for ability in exit_data.get("abilities", [])}
        self.assertLessEqual(named, set(self.world._ability_holders))

    def test_each_attack_needs_its_member(self) -> None:
        for ability, member in (("Horn", "Kabbu"), ("Beemerang", "Vi"), ("Ice", "Leif")):
            with self.subTest(ability=ability):
                self.assertEqual(self.world._requires({"abilities": [ability]}), [member])

    def test_jump_needs_no_member(self) -> None:
        self.assertEqual(self.world._requires({"abilities": ["Jump"]}), [])

    def test_story_party_needs_nothing_for_a_move(self) -> None:
        self.world.starting_member = -1
        self.assertEqual(self.world._requires({"abilities": ["Horn"], "requires": ["Explorer Permit"]}), ["Explorer Permit"])

    def test_the_den_needs_the_horn(self) -> None:
        # Grass on the way in and the door room's puzzle down the trapdoor (the user, 2026-09-26).
        from ..data_tables import LOCATIONS, REGIONS
        gate = next(region for region in REGIONS if region["name"] == "Past the Outskirts Gate")
        into_den = next(exit_data for exit_data in gate["exits"] if exit_data["to"] == "Snakemouth Den")
        self.assertIn("Kabbu", self.world._requires(into_den))
        trapdoor = next(loc for loc in LOCATIONS if loc["name"] == "Snakemouth Den: Door Room, Trapdoor")
        self.assertIn("Kabbu", self.world._requires(trapdoor))


class TestStartAllThree(BugFablesTestBase):
    options = {"starting_party_member": "all_three"}

    def test_all_three_are_start_inventory(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["starting_member"], 3)
        self.assertEqual(sorted(_names(self.multiworld.precollected_items[self.player])), sorted(MEMBERS))
        pool = _names(item for item in self.multiworld.itempool if item.player == self.player)
        for member in MEMBERS:
            self.assertNotIn(member, pool)

    def test_joining_moments_are_locations(self) -> None:
        locations = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for name in JOINS:
            self.assertIn(name, locations)
        self.assertNotIn("Leif Joins", locations)

    def test_nothing_waits_on_a_member(self) -> None:
        self.collect_by_name("Explorer Permit")
        self.assertTrue(self.can_reach_location("Outskirts: Near Snakemouth Den, Reward"))
        self.assertTrue(self.can_reach_location("Outskirts: East Road, Stone"))

    def test_pool_matches_locations(self) -> None:
        pool = [item for item in self.multiworld.itempool if item.player == self.player]
        locations = [loc for loc in self.multiworld.get_locations(self.player) if loc.address is not None]
        self.assertEqual(len(pool), len(locations))
