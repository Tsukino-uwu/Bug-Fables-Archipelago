"""The item class and the item pool."""
from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

from .data_tables import ITEM_KIND, ITEM_NAME_TO_ID, ITEMS, MONEY_KIND, vanilla_item

if TYPE_CHECKING:
    from .world import BugFablesWorld

GAME = "Bug Fables"
CLASSIFICATIONS = {
    "progression": ItemClassification.progression,
    "useful": ItemClassification.useful,
    "filler": ItemClassification.filler,
    "trap": ItemClassification.trap,
}
ITEMS_BY_NAME = {item["name"]: item for item in ITEMS}
# Only padding fills leftover slots; other filler (the Hard Mode medal) enters only as a location's vanilla item.
PADDING = [item["name"] for item in ITEMS if item.get("padding")]
# By member number: 0 Vi, 1 Kabbu, 2 Leif.
MEMBERS = [item["name"] for item in sorted((item for item in ITEMS if item.get("member")), key=lambda item: item["game_id"])]


class BugFablesItem(Item):
    game = GAME


def create_item(world: BugFablesWorld, name: str) -> BugFablesItem:
    return BugFablesItem(name, CLASSIFICATIONS[ITEMS_BY_NAME[name]["classification"]], ITEM_NAME_TO_ID[name], world.player)


def random_filler_name(world: BugFablesWorld) -> str:
    return world.random.choice(PADDING)


def create_all_items(world: BugFablesWorld) -> None:
    # The included locations' vanilla items (duplicates kept), then padding; an item whose spot is off stays vanilla.
    pool: list[Item] = [world.create_item(name) for name in
                        (vanilla_item(loc) for loc in world.included_locations) if name is not None]
    # With a starting member, it is start inventory (the client gets it too) and the other two are in the pool.
    if world.starting_member >= 0:
        for number, name in enumerate(MEMBERS):
            if world.starting_member in (number, world.ALL_MEMBERS):
                world.push_precollected(world.create_item(name))
            else:
                pool.append(world.create_item(name))
    # Field moves are items only with their option: the three attacks, and Jump on its own.
    for item in ITEMS:
        if item.get("move") and (world.jump_shuffled() if item["name"] == "Jump" else world.moves_shuffled()):
            pool.append(world.create_item(item["name"]))
    # The mod's own items (custom gates) enter once in every seed, in a filler slot: when every location already
    # has its vanilla item, one filler item (an ordinary item or berries, picked by the seed) makes room.
    always = [world.create_item(item["name"]) for item in ITEMS if item.get("always")]
    unfilled = len(world.multiworld.get_unfilled_locations(world.player))
    while always and len(pool) + len(always) > unfilled:
        # Only an ordinary item or berries, and only one with a copy left in the pool: every location's own item
        # stays in the pool at least once, and a filler medal (the Hard Mode medal) is never taken.
        names = [item.name for item in pool]
        filler = [item for item in pool if item.classification == ItemClassification.filler
                  and ITEMS_BY_NAME[item.name]["kind"] in (ITEM_KIND, MONEY_KIND) and names.count(item.name) > 1]
        if not filler:
            raise Exception(f"Bug Fables: no filler item to make room for {always[0].name} in player {world.player_name}'s pool")
        pool.remove(world.random.choice(filler))
    pool += always
    pool += [world.create_filler() for _ in range(unfilled - len(pool))]
    world.multiworld.itempool += pool
