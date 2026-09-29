"""Bug Fables' own Rule Builder rules (custom_rules.py), and "and" and "or" as the logic writes them."""
from rule_builder.rules import Has

from . import BugFablesTestBase
from ..custom_rules import CanUse, Member, MoveItem


class TestStoryParty(BugFablesTestBase):
    # The story's party: members are never items, only Leif joins late.
    options = {"starting_party_member": "off"}

    def test_an_attack_needs_nothing(self) -> None:
        self.assertTrue(CanUse("Horn Slash").resolve(self.world).always_true)

    def test_leifs_ability_needs_leif(self) -> None:
        rule = CanUse("Freeze").resolve(self.world)
        self.assertFalse(rule(self.state_with()))
        self.assertTrue(rule(self.state_with("Leif")))

    def test_a_member_needs_nothing(self) -> None:
        self.assertTrue(Member("Vi").resolve(self.world).always_true)


class TestMembersAndMovesAsItems(BugFablesTestBase):
    options = {"starting_party_member": "vi", "shuffle_field_moves": True}

    def test_an_ability_needs_its_member_and_its_item(self) -> None:
        rule = CanUse("Horn Slash").resolve(self.world)
        self.assertFalse(rule(self.state_with("Kabbu")))
        self.assertFalse(rule(self.state_with("Horn Slash")))
        self.assertTrue(rule(self.state_with("Kabbu", "Horn Slash")))

    def test_a_second_level_needs_two_copies(self) -> None:
        rule = CanUse("Icicle").resolve(self.world)
        self.assertFalse(rule(self.state_with("Leif", "Progressive Freeze")))
        self.assertTrue(rule(self.state_with("Leif", "Progressive Freeze", "Progressive Freeze")))

    def test_a_move_item_needs_no_member(self) -> None:
        rule = MoveItem("Horn Slash").resolve(self.world)
        self.assertFalse(rule(self.state_with("Kabbu")))
        self.assertTrue(rule(self.state_with("Horn Slash")))

    def test_a_member_needs_that_member(self) -> None:
        rule = Member("Kabbu").resolve(self.world)
        self.assertFalse(rule(self.state_with("Vi", "Leif")))
        self.assertTrue(rule(self.state_with("Kabbu")))


class TestMovesNotShuffled(BugFablesTestBase):
    # The attacks' base level is the party's from the start; later levels are still items.
    options = {"starting_party_member": "vi"}

    def test_an_attack_needs_only_its_member(self) -> None:
        rule = CanUse("Horn Slash").resolve(self.world)
        self.assertTrue(rule(self.state_with("Kabbu")))

    def test_a_move_item_needs_nothing(self) -> None:
        self.assertTrue(MoveItem("Horn Slash").resolve(self.world).always_true)

    def test_a_later_level_needs_one_copy(self) -> None:
        rule = CanUse("Beemerang Halt").resolve(self.world)
        self.assertFalse(rule(self.state_with("Vi")))
        self.assertTrue(rule(self.state_with("Vi", "Progressive Beemerang")))


class TestAndOr(BugFablesTestBase):
    # "and" is &, "or" is |: Archipelago's own And and Or, as a spot with two ways to it writes them.
    options = {"starting_party_member": "vi", "shuffle_field_moves": True}

    def test_a_base_and_either_way(self) -> None:
        rule = (Has("Explorer Permit") & (Has("Boat Ticket") | Has("Quest Book"))).resolve(self.world)
        self.assertTrue(rule(self.state_with("Explorer Permit", "Boat Ticket")))
        self.assertTrue(rule(self.state_with("Explorer Permit", "Quest Book")))
        self.assertFalse(rule(self.state_with("Explorer Permit")))
        self.assertFalse(rule(self.state_with("Boat Ticket", "Quest Book")))

    def test_either_ability(self) -> None:
        rule = (CanUse("Horn Slash") | CanUse("Beetle Dig")).resolve(self.world)
        self.assertTrue(rule(self.state_with("Kabbu", "Horn Slash")))
        self.assertTrue(rule(self.state_with("Kabbu", "Beetle Dig")))
        self.assertFalse(rule(self.state_with("Kabbu")))
        self.assertFalse(rule(self.state_with("Horn Slash", "Beetle Dig")))


class TestAFreeWay(BugFablesTestBase):
    # A way that needs nothing in this seed makes the whole "or" free.
    options = {"starting_party_member": "off"}

    def test_or_with_a_way_that_needs_nothing(self) -> None:
        self.assertTrue((Member("Vi") | Has("Boat Ticket")).resolve(self.world).always_true)
        rule = (Has("Explorer Permit") & (Member("Vi") | Has("Boat Ticket"))).resolve(self.world)
        self.assertTrue(rule(self.state_with("Explorer Permit")))
