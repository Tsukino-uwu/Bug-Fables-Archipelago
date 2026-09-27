"""Field abilities and the items that give them.

Rules name the ability (the game's own name), never the item: this table turns it into its holder and the copies of
its item it takes. A progressive item gives its abilities in the game's order, one level per copy.
"""
from __future__ import annotations

from typing import TYPE_CHECKING, NamedTuple

if TYPE_CHECKING:
    from .world import BugFablesWorld


class Ability(NamedTuple):
    holder: str | None  # the member who uses it; None for the whole party
    item: str
    level: int  # copies of the item it takes, counting the base level
    # The option that makes the base level an item; with it off the base level is the party's from the start.
    base_option: str | None
    # The game flag the story sets where it teaches the ability: that spot is its location (build step 23).
    flag: int | None = None


ABILITIES: dict[str, Ability] = {
    "Beemerang Toss": Ability("Vi", "Progressive Beemerang", 1, "shuffle_field_moves"),
    "Beemerang Halt": Ability("Vi", "Progressive Beemerang", 2, "shuffle_field_moves", 21),
    "Bee Fly": Ability("Vi", "Bee Fly", 1, None, 19),
    "Horn Slash": Ability("Kabbu", "Horn Slash", 1, "shuffle_field_moves"),
    "Dash": Ability("Kabbu", "Progressive Dash", 1, None, 699),
    "Horn Dash": Ability("Kabbu", "Progressive Dash", 2, None, 39),
    "Beetle Dig": Ability("Kabbu", "Beetle Dig", 1, None, 18),
    "Freeze": Ability("Leif", "Progressive Freeze", 1, "shuffle_field_moves"),
    "Icicle": Ability("Leif", "Progressive Freeze", 2, "shuffle_field_moves", 171),
    "Shield": Ability("Leif", "Shield", 1, None, 20),
    "Jump": Ability(None, "Jump", 1, "shuffle_jump"),
}


def item_count(world: BugFablesWorld, ability: str) -> int:
    """Copies of its item an ability takes in this seed: 0 when the party has it from the start."""
    data = ABILITIES[ability]
    if data.base_option is not None and not getattr(world.options, data.base_option).value:
        return data.level - 1
    return data.level


def item_copies(world: BugFablesWorld, item: str) -> int:
    """How many of an ability item go in the pool: enough for its highest level."""
    return max((item_count(world, name) for name, data in ABILITIES.items() if data.item == item), default=0)
