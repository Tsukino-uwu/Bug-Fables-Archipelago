"""The item class and the item pool."""
from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

from .abilities import item_copies
from .data_tables import ITEM_KIND, ITEM_NAME_TO_ID, ITEMS, MONEY_KIND, vanilla_item
from .data_types import GAME

if TYPE_CHECKING:
    from .world import BugFablesWorld

CLASSIFICATIONS = {
    "progression": ItemClassification.progression,
    "useful": ItemClassification.useful,
    "filler": ItemClassification.filler,
    "trap": ItemClassification.trap,
}
ITEMS_BY_NAME = {item.name: item for item in ITEMS}
# Only padding fills leftover slots; any other filler enters only as a location's vanilla item.
PADDING = [item.name for item in ITEMS if item.padding]
# By member number: 0 Vi, 1 Kabbu, 2 Leif.
MEMBERS = [item.name for item in sorted((item for item in ITEMS if item.member), key=lambda item: item.game_id)]


class BugFablesItem(Item):
    game = GAME


def create_item(world: BugFablesWorld, name: str) -> BugFablesItem:
    return BugFablesItem(name, CLASSIFICATIONS[ITEMS_BY_NAME[name].classification], ITEM_NAME_TO_ID[name], world.player)


def random_filler_name(world: BugFablesWorld) -> str:
    return world.random.choice(PADDING)


def own_copies(world: BugFablesWorld, name: str) -> int:
    """How many copies of one of the mod's own items this seed's pool holds: the boat's items by Progressive Boat."""
    item = ITEMS_BY_NAME[name]
    wanted = item.progressive_boat is None or item.progressive_boat == bool(world.options.progressive_boat.value)
    return item.always if wanted else 0


def create_all_items(world: BugFablesWorld) -> None:
    # The included locations' vanilla items (duplicates kept), then padding; an item whose spot is off stays vanilla.
    pool: list[Item] = [world.create_item(name) for name in
                        (vanilla_item(loc) for loc in world.included_locations) if name is not None]
    # With a starting member, it is start inventory (the client gets it too) and the others are in the pool (none with
    # All Three).
    if world.starting_member >= 0:
        for number, name in enumerate(MEMBERS):
            if world.starting_member in (number, world.ALL_MEMBERS):
                world.push_precollected(world.create_item(name))
            else:
                pool.append(world.create_item(name))
    # Field abilities: enough copies of each item for its highest level; a base level the party starts with isn't one.
    for item in ITEMS:
        if item.move:
            pool += [world.create_item(item.name) for _ in range(item_copies(world, item.name))]
    # The mod's own items (custom gates) enter in every seed, each copy in a filler slot: when every location already
    # has its vanilla item, one filler item (an ordinary item or berries, picked by the seed) makes room per copy.
    always = [world.create_item(item.name) for item in ITEMS for _ in range(own_copies(world, item.name))]
    unfilled = len(world.multiworld.get_unfilled_locations(world.player))
    while always and len(pool) + len(always) > unfilled:
        # Only an ordinary item or berries, never a medal or a token. A duplicate copy first; the last copy only when
        # none is left (few locations, many move items).
        names = [item.name for item in pool]
        ordinary = [item for item in pool if item.classification == ItemClassification.filler
                    and ITEMS_BY_NAME[item.name].kind in (ITEM_KIND, MONEY_KIND)]
        filler = [item for item in ordinary if names.count(item.name) > 1] or ordinary
        if not filler:
            raise Exception(f"Bug Fables: no filler item to make room for {always[0].name} "
                            f"in player {world.player_name}'s pool")
        pool.remove(world.random.choice(filler))
    pool += always
    pool += [world.create_filler() for _ in range(unfilled - len(pool))]
    world.multiworld.itempool += pool
