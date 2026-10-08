"""Bug Fables' own Rule Builder rules: the needs that depend on the seed's options.

Everything else a rule says is Archipelago's own: Has for an item or story event, & for "and", | for "or".
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING, Any

from rule_builder.rules import CanReachRegion, Has, HasAllCounts, Or, Rule, True_, WrapperRule

from .abilities import ABILITIES, item_count
from .data_types import GAME

if TYPE_CHECKING:
    from .world import BugFablesWorld

# The one member the story's party lacks at first.
LATE_MEMBER = "Leif"


@dataclass()
class CanUse(Rule["BugFablesWorld"], game=GAME):
    """An ability, by the game's name (abilities.py): its item's copies when that's an item, and the member who uses it
    when members are items (with the story's party, only Leif, who joins late)."""

    ability: str

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        data = ABILITIES[self.ability]
        needed: dict[str, int] = {}
        if data.holder is not None and (world.starting_member >= 0 or data.holder == LATE_MEMBER):
            needed[data.holder] = 1
        if item_count(world, self.ability):
            needed[data.item] = item_count(world, self.ability)
        return (HasAllCounts(needed) if needed else True_()).resolve(world)


@dataclass()
class Member(Rule["BugFablesWorld"], game=GAME):
    """A party member, needed only when members are items (Starting Party Member); the story's party is always there."""

    name: str

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        return (Has(self.name) if world.starting_member >= 0 else True_()).resolve(world)


@dataclass()
class ItemOnHand(Rule["BugFablesWorld"], game=GAME):
    """An ordinary item to hand over: any item shop reached, whose slots sell their own item without end once bought
    (the user, 2026-10-07). By the shop's region, so it holds with item shops left out of the seed too."""

    def _ways(self) -> list[Rule]:
        from .logic import LOCATIONS

        ways: list[Rule] = []
        for location in LOCATIONS:
            if location.category == "item_shop":
                region = location.region if location.area is None else f"{location.region} ({location.area})"
                ways.append(CanReachRegion(region) if location.rule is None else CanReachRegion(region) & location.rule)
        return ways

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        return Or(*self._ways()).resolve(world)

    # Serialized as the Or it stands for (rule builder.md, Custom serialization), so a tracker reads only built-in rules.
    def to_dict(self) -> dict[str, Any]:
        return Or(*self._ways(), options=self.options, filtered_resolution=self.filtered_resolution).to_dict()


@dataclass()
class MoveItem(Rule["BugFablesWorld"], game=GAME):
    """An ability's item alone, not who uses it: the blanket rule for ground not measured yet."""

    ability: str

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        count = item_count(world, self.ability)
        return (Has(ABILITIES[self.ability].item, count) if count else True_()).resolve(world)


# With Progressive Boat off, each level is its own item.
BOAT_LEVELS = {1: "Boat Ticket", 2: "Subaquatic Maritime Neotransport"}


@dataclass()
class Boat(Rule["BugFablesWorld"], game=GAME):
    """A way across the water, by level (1 the Boat Ticket, 2 the submarine): that many copies of the Progressive Boat
    with the option on, the level's own item with it off."""

    level: int

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        if world.options.progressive_boat:
            return Has("Progressive Boat", self.level).resolve(world)
        return Has(BOAT_LEVELS[self.level]).resolve(world)


@dataclass()
class WayBack(WrapperRule["BugFablesWorld"], game=GAME):
    """What getting back from a one-way needs (room-logic.md, rule 4): its child rule, or nothing with Points of No
    Return on, where the Warp to Start is the way back."""

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        if world.options.points_of_no_return:
            return True_().resolve(world)
        return self.child.resolve(world)


def one_way(rule: Rule | None, way_back: Rule) -> Rule:
    """A one-way's rule: its own need, and what it takes to get back (WayBack), never joined beforehand."""
    return WayBack(way_back) if rule is None else rule & WayBack(way_back)


# The cautious stand-in for ground not measured yet: every member, and every attack's item.
WHOLE_PARTY = Member("Vi") & Member("Kabbu") & Member("Leif")
ALL_ATTACKS = MoveItem("Beemerang Toss") & MoveItem("Horn Slash") & MoveItem("Freeze")
# A switch takes any of the three: the Beemerang, a hit tagged BeetleHorn or BeetleDash, or ice (NPCControl, Switch).
ANY_ATTACK = CanUse("Beemerang Toss") | CanUse("Horn Slash") | CanUse("Freeze")
BOAT_TICKET = Boat(1)
SUBMARINE = Boat(2)
# The submarine's key item in the bag, the Subaquatic Maritime Neotransport (CustomItems.cs), whichever item gives it.
SUBMARINE_KEY = 212
# The Ant Palace's rooms and the city districts, once the big Inner City region: chapter 2 (flag 67).
INNER_CITY = Has("Chapter 2 Started")
# Chapters 2-7 until they get room-level logic: past chapter 2's start, with everything the story used before (the
# permit, the Boat Ticket, the first boss, the whole party and its attacks). More cautious than the game.
LATER_CHAPTERS = (INNER_CITY & Has("Explorer Permit") & BOAT_TICKET & Has("Snakemouth Den Cleared")
                  & WHOLE_PARTY & ALL_ATTACKS)
