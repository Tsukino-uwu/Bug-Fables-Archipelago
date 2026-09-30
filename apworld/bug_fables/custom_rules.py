"""Bug Fables' own Rule Builder rules: the needs that depend on the seed's options.

Everything else a rule says is Archipelago's own: Has for an item or story event, & for "and", | for "or".
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING

from rule_builder.rules import HasAllCounts, Rule

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
        return HasAllCounts(needed).resolve(world)


@dataclass()
class Member(Rule["BugFablesWorld"], game=GAME):
    """A party member, needed only when members are items (Starting Party Member); the story's party is always there."""

    name: str

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        return HasAllCounts({self.name: 1} if world.starting_member >= 0 else {}).resolve(world)


@dataclass()
class MoveItem(Rule["BugFablesWorld"], game=GAME):
    """An ability's item alone, not who uses it: the blanket rule for ground not measured yet."""

    ability: str

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        count = item_count(world, self.ability)
        return HasAllCounts({ABILITIES[self.ability].item: count} if count else {}).resolve(world)


# With Progressive Boat off, each level is its own item.
BOAT_LEVELS = {1: "Boat Ticket", 2: "Subaquatic Maritime Neotransport"}


@dataclass()
class Boat(Rule["BugFablesWorld"], game=GAME):
    """A way across the water, by level (1 the Boat Ticket, 2 the submarine): that many copies of the Progressive Boat
    with the option on, the level's own item with it off."""

    level: int

    def _instantiate(self, world: BugFablesWorld) -> Rule.Resolved:
        if world.options.progressive_boat:
            return HasAllCounts({"Progressive Boat": self.level}).resolve(world)
        return HasAllCounts({BOAT_LEVELS[self.level]: 1}).resolve(world)


# The cautious stand-in for ground not measured yet: every member, and every attack's item.
WHOLE_PARTY = Member("Vi") & Member("Kabbu") & Member("Leif")
ALL_ATTACKS = MoveItem("Beemerang Toss") & MoveItem("Horn Slash") & MoveItem("Freeze")
BOAT_TICKET = Boat(1)
SUBMARINE = Boat(2)
