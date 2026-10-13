"""Every rule serializes as Rule Builder's to_dict writes it and reads back with from_dict: the PopTracker pack exports
the logic this way (rule builder.md, Serialization)."""
import json
from unittest import TestCase

from rule_builder.rules import Has, Rule

from ..custom_rules import one_way
from ..data_tables import LOCATIONS, STORY_EVENTS
from ..regions import logic_entrances
from ..rules import spot_rule
from ..world import BugFablesWorld
from . import BugFablesTestBase, logic_rules


class TestRuleExport(TestCase):
    def assert_round_trip(self, where: str, rule: Rule) -> None:
        # As a tracker receives it, through JSON: a tuple of names arrives as a list.
        data = json.loads(json.dumps(rule.to_dict()))
        self.assertEqual(json.loads(json.dumps(BugFablesWorld.rule_from_dict(data).to_dict())), data, where)

    def test_every_logic_rule(self) -> None:
        for where, rule in logic_rules():
            if rule is not None:
                self.assert_round_trip(where, rule)

    def test_every_spot_rule(self) -> None:
        for spot in (*LOCATIONS, *STORY_EVENTS):
            rule = spot_rule(spot)
            if rule is not None:
                self.assert_round_trip(spot.name, rule)

    def test_every_entrance_rule(self) -> None:
        for entrance in logic_entrances():
            if entrance.rule is not None:
                self.assert_round_trip(entrance.name, entrance.rule)

    def test_a_way_back_keeps_its_child(self) -> None:
        rule = one_way(Has("Explorer Permit"), Has("Jump"))
        self.assert_round_trip("a one-way", rule)
        self.assertEqual(rule.to_dict()["children"][1]["child"], Has("Jump").to_dict())


class TestExportedEntrancesAreTheWorlds(BugFablesTestBase):
    def test_every_entrance(self) -> None:
        made = {(e.name, e.parent_region.name, e.connected_region.name)
                for e in self.multiworld.get_entrances(self.player)}
        # Archipelago never makes an entrance whose rule can't ever pass (AutoWorld.create_entrance).
        possible = [e for e in logic_entrances() if e.rule is None or not e.rule.resolve(self.world).always_false]
        self.assertEqual(made, {(e.name, e.from_map, e.to_map) for e in possible})
