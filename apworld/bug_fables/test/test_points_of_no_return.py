"""Points of No Return: with it on, a one-way's way back (WayBack) needs nothing, since the Warp to Start is the way back
(room-logic.md, rules 4 and 9); with it off, the way back is needed."""
from rule_builder.rules import Has

from . import BugFablesTestBase
from ..custom_rules import WayBack, one_way


class TestPointsOfNoReturnOff(BugFablesTestBase):
    def test_off_by_default(self) -> None:
        self.assertFalse(self.world.fill_slot_data()["points_of_no_return"])

    def test_the_way_back_is_needed(self) -> None:
        rule = WayBack(Has("Jump")).resolve(self.world)
        self.assertFalse(rule(self.state_with()))
        self.assertTrue(rule(self.state_with("Jump")))

    def test_a_one_way_needs_its_own_rule_and_its_way_back(self) -> None:
        rule = one_way(Has("Explorer Permit"), Has("Jump")).resolve(self.world)
        self.assertFalse(rule(self.state_with("Explorer Permit")))
        self.assertFalse(rule(self.state_with("Jump")))
        self.assertTrue(rule(self.state_with("Explorer Permit", "Jump")))


class TestPointsOfNoReturnOn(BugFablesTestBase):
    options = {"points_of_no_return": True}

    def test_in_slot_data(self) -> None:
        self.assertTrue(self.world.fill_slot_data()["points_of_no_return"])

    def test_the_way_back_needs_nothing(self) -> None:
        self.assertTrue(WayBack(Has("Jump")).resolve(self.world)(self.state_with()))

    def test_a_one_way_needs_only_its_own_rule(self) -> None:
        rule = one_way(Has("Explorer Permit"), Has("Jump")).resolve(self.world)
        self.assertFalse(rule(self.state_with()))
        self.assertTrue(rule(self.state_with("Explorer Permit")))
        self.assertTrue(one_way(None, Has("Jump")).resolve(self.world)(self.state_with()))
