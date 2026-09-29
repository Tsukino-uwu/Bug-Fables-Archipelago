"""The logic's area modules (logic/) on the map regions: how they fit together, and that every name a rule uses
exists."""
from rule_builder.rules import Has

from . import BugFablesTestBase, logic_rules, rule_parts
from ..abilities import ABILITIES
from ..custom_rules import CanUse, Member, MoveItem
from ..data_tables import ARTIFACTS, DOOR_RULES, DOORS, ITEMS, LOCATIONS, MAPS, STORY_EVENTS, TRANSFERS
from ..regions import door_name

ALL_SPOTS = (*LOCATIONS, *STORY_EVENTS, *ARTIFACTS)


class TestAreas(BugFablesTestBase):
    options = {"shuffle_discoveries": True}

    def test_every_spot_is_in_a_map(self) -> None:
        for spot in ALL_SPOTS:
            with self.subTest(spot=spot.name):
                self.assertIn(spot.region, MAPS)

    def test_every_spot_is_in_its_source_map(self) -> None:
        # Where the game data names the spot's map, the logic must put it there.
        for spot in ALL_SPOTS:
            for named in (spot.source.pickup, spot.source.give, spot.source.item_shop):
                if named is not None:
                    with self.subTest(spot=spot.name):
                        self.assertEqual(spot.region, named.map)

    def test_names_are_unique(self) -> None:
        spots = [spot.name for spot in ALL_SPOTS]
        self.assertEqual(len(spots), len(set(spots)))

    def test_every_door_is_an_entrance(self) -> None:
        # Where the game has it, with the doors as they are (Archipelago's entrance randomizer shuffles these).
        for connection in DOORS.connections:
            for end, other in ((connection.a, connection.b), (connection.b, connection.a)):
                with self.subTest(door=door_name(end)):
                    entrance = self.multiworld.get_entrance(door_name(end), self.player)
                    self.assertEqual(entrance.parent_region.name, end.map)
                    self.assertEqual(entrance.connected_region.name, other.map)

    def test_door_gates_and_transfers_name_real_places(self) -> None:
        doors = {(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)}
        for gate in DOOR_RULES:
            with self.subTest(gate=gate):
                self.assertIn((gate.map, gate.door), doors)
        for transfer in TRANSFERS:
            with self.subTest(transfer=transfer.name):
                self.assertIn(transfer.from_map, MAPS)
                self.assertIn(transfer.to_map, MAPS)

    def test_every_region_reachable_with_everything(self) -> None:
        state = self.multiworld.get_all_state()
        for name in MAPS:
            with self.subTest(region=name):
                self.assertTrue(state.can_reach_region(name, self.player))

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
