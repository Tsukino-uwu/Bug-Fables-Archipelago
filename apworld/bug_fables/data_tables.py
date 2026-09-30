"""The world's tables: the logic's from logic/, the rest from data/ with pkgutil (a packaged .apworld is a zip with no
file paths to open())."""
from __future__ import annotations

import json
import pkgutil
from typing import Any

from .data_types import Doors, Encounter, Item, Location, RoomStart, SavePoint
from .logic import (ARTIFACTS, DIALOGUE_FLAGS, DOOR_RULES, HELD_UNTIL, HELD_UNTIL_ITEM, KEPT_OPEN, KEPT_PRESENT,
                    LOCATIONS, PRESENT_FROM, PRESENT_WITH_ITEM, SCENERY_HIDDEN, SCENERY_PRESENT, STORY_EVENTS,
                    TRANSFERS)

ITEM_ID_BASE = 7_710_000
LOCATION_ID_BASE = 7_720_000


def _load_manifest() -> dict[str, Any]:
    raw = pkgutil.get_data(__name__, "archipelago.json")
    if raw is None:
        raise FileNotFoundError("bug_fables: archipelago.json is missing from the world package")
    return json.loads(raw.decode("utf-8"))


def _load(name: str) -> dict[str, Any]:
    raw = pkgutil.get_data(__name__, f"data/{name}")
    if raw is None:
        raise FileNotFoundError(f"bug_fables: data/{name} is missing from the world package")
    return json.loads(raw.decode("utf-8"))


WORLD_VERSION: str = _load_manifest()["world_version"]
ITEMS: tuple[Item, ...] = tuple(Item.from_json(item) for item in _load("items.json")["items"])
DOORS: Doors = Doors.from_json(_load("doors.json"))


def door_name(map_name: str, door: str) -> str:
    """A door's entrance, named where it is."""
    return f"{map_name}: {door}"


# Every door the entrance randomizer shuffles, by its entrance's name.
DOOR_NAMES: frozenset[str] = frozenset(door_name(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b))
# Maps nothing leads into: an unused room and the debug room (MEASURED.md, "The door graph").
UNUSED_MAPS = frozenset({"SnakemouthEmpty", "TestRoom"})
# Every map a region: the door table's and those only a transfer reaches.
MAPS: tuple[str, ...] = tuple(sorted(
    ({end.map for c in DOORS.connections for end in (c.a, c.b)} | {m for link in DOORS.fixed for m in link}
     | {m for t in TRANSFERS for m in (t.from_map, t.to_map)}) - UNUSED_MAPS))
# Every room entered through a door: the map, and the map whose door leads in (both ways of each connection). A start
# there lands where walking in through that door ends.
ROOM_STARTS: tuple[RoomStart, ...] = tuple(sorted(
    {RoomStart(map=end.map, from_map=other.map) for c in DOORS.connections for end, other in ((c.a, c.b), (c.b, c.a))
     if end.map != other.map}))
# Every save point (map, entity index). Unused today (a random start picks from ROOM_STARTS); kept for named start
# spots, a planned option.
STARTS: tuple[SavePoint, ...] = tuple(SavePoint.from_json(s) for s in _load("starts.json")["starts"])
# Every map enemy (map, entity index) and the enemy ids its fight starts with.
ENCOUNTERS: tuple[Encounter, ...] = tuple(Encounter.from_json(e) for e in _load("enemies.json")["encounters"])

# An item's kind: where the game puts it. Ordinary items and key items share the base range.
ITEM_KIND = 0
KEY_ITEM_KIND = 1
# Medal ids overlap item ids, so medals get their own range.
MEDAL_KIND = 2
MEDAL_ID_OFFSET = 1_000
# Money: giveitem type -1; game_id is the amount.
MONEY_KIND = 3
MONEY_ID_OFFSET = 2_000
# Crystal berries: one counted item (game_id 0).
CRYSTAL_KIND = 4
CRYSTAL_ID_OFFSET = 3_000
# Party members: game_id is the member (0 Vi, 1 Kabbu, 2 Leif).
MEMBER_KIND = 5
MEMBER_ID_OFFSET = 4_000
# Field moves: game_id 0 Beemerang (Vi), 1 Horn (Kabbu), 2 Ice (Leif), 3 Jump (the whole party).
MOVE_KIND = 6
MOVE_ID_OFFSET = 5_000


def item_id(item: Item) -> int:
    offset = {MEDAL_KIND: MEDAL_ID_OFFSET, MONEY_KIND: MONEY_ID_OFFSET, CRYSTAL_KIND: CRYSTAL_ID_OFFSET,
              MEMBER_KIND: MEMBER_ID_OFFSET, MOVE_KIND: MOVE_ID_OFFSET}.get(item.kind, 0)
    return ITEM_ID_BASE + offset + item.game_id


ITEM_NAME_TO_ID: dict[str, int] = {item.name: item_id(item) for item in ITEMS}
LOCATION_NAME_TO_ID: dict[str, int] = {loc.name: LOCATION_ID_BASE + loc.id for loc in LOCATIONS}

if len(set(ITEM_NAME_TO_ID.values())) != len(ITEMS):
    raise ValueError("bug_fables: two items share an id (same kind and game_id)")
if len(set(LOCATION_NAME_TO_ID.values())) != len(LOCATIONS):
    raise ValueError("bug_fables: two locations share an id")


# A give or pickup's type: the item kinds, except these two.
MONEY_TYPE = -1
CRYSTAL_TYPE = 3


def vanilla_item(location: Location) -> str | None:
    """The name of the item the game hands out at a location, or None."""
    source = location.source
    handed = source.give or source.pickup or source.added
    if handed is not None:
        give_type, give_item = handed.type, handed.item
    elif source.item_shop is not None:
        give_type, give_item = ITEM_KIND, source.item_shop.item
    else:
        return None
    if give_type == CRYSTAL_TYPE:
        kind, game_id = CRYSTAL_KIND, 0
    else:
        kind, game_id = (MONEY_KIND if give_type == MONEY_TYPE else give_type), give_item
    for item in ITEMS:
        if item.kind == kind and item.game_id == game_id:
            return item.name
    return None

