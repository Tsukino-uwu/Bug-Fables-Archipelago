"""The logic's area modules (logic/): how they fit together, and that every name a rule uses exists."""
from rule_builder.rules import Has

from . import BugFablesTestBase, logic_rules, rule_parts
from .. import logic
from ..abilities import ABILITIES
from ..custom_rules import CanUse, Member, MoveItem
from ..data_tables import ARTIFACTS, ITEMS, LOCATIONS, REGIONS, STORY_EVENTS
from ..regions import START_REGION


class TestAreas(BugFablesTestBase):
    options = {"shuffle_discoveries": True}

    def test_every_exit_leads_to_a_region(self) -> None:
        names = {region.name for region in REGIONS}
        for area in logic.AREAS:
            for region in area.REGIONS:
                for exit_data in region.exits:
                    with self.subTest(area=area.__name__, exit=f"{region.name} -> {exit_data.to}"):
                        self.assertIn(exit_data.to, names)

    def test_every_spot_is_in_a_region_of_its_own_module(self) -> None:
        # Only exits cross from one module into another, so a room's spots are always found with the room.
        for area in logic.AREAS:
            own = {region.name for region in area.REGIONS}
            spots = (*getattr(area, "LOCATIONS", ()), *getattr(area, "STORY_EVENTS", ()),
                     *getattr(area, "ARTIFACTS", ()))
            for spot in spots:
                with self.subTest(area=area.__name__, spot=spot.name):
                    self.assertIn(spot.region, own)

    def test_names_are_unique(self) -> None:
        regions = [region.name for region in REGIONS]
        self.assertEqual(len(regions), len(set(regions)))
        spots = [spot.name for spot in (*LOCATIONS, *STORY_EVENTS, *ARTIFACTS)]
        self.assertEqual(len(spots), len(set(spots)))

    def test_every_region_has_a_way_in(self) -> None:
        entered = {exit_data.to for region in REGIONS for exit_data in region.exits} | {START_REGION}
        for region in REGIONS:
            with self.subTest(region=region.name):
                self.assertIn(region.name, entered)

    def test_every_region_reachable_with_everything(self) -> None:
        state = self.multiworld.get_all_state()
        for region in REGIONS:
            with self.subTest(region=region.name):
                self.assertTrue(state.can_reach_region(region.name, self.player))

    def test_locations_in_id_order(self) -> None:
        # Moving a spot from one module to another must never change a seed.
        ids = [location.id for location in LOCATIONS]
        self.assertEqual(ids, sorted(ids))

    def test_every_name_a_rule_uses_exists(self) -> None:
        # A misspelt name in a rule would make a spot unreachable, or quietly need nothing.
        items = {item.name for item in ITEMS} | {event.item for event in STORY_EVENTS}
        members = {item.name for item in ITEMS if item.member}
        for where, rule in logic_rules():
            for part in rule_parts(rule):
                with self.subTest(where=where, rule=str(part)):
                    if isinstance(part, Has):
                        self.assertIn(part.item_name, items)
                    elif isinstance(part, (CanUse, MoveItem)):
                        self.assertIn(part.ability, ABILITIES)
                    elif isinstance(part, Member):
                        self.assertIn(part.name, members)
