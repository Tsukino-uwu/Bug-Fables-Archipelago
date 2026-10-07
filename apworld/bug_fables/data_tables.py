"""The world's tables: the logic's from logic/, the rest from data/ with pkgutil (a packaged .apworld is a zip with no
file paths to open())."""
from __future__ import annotations

import json
import pkgutil
from typing import Any

from .data_types import Area, Doors, Encounter, Item, Location, OneWayDoor, RoomStart, SavePoint
from .enemysanity import enemy_locations
from .logic import (ARTIFACTS, DAY_NIGHT, DIALOGUE_FLAGS, DOOR_RULES, ENTITIES_MOVED, FREE_SALES, HELD_UNTIL,
                    HELD_UNTIL_ITEM, KEPT_OPEN, KEPT_PRESENT, LOCATIONS, MAP_AREAS, PRESENT_FROM, PRESENT_WITH_ITEM,
                    ROADBLOCKS, SCENE_CAMERAS, SCENERY_HIDDEN, SCENERY_MOVED, SCENERY_OFF, SCENERY_PRESENT,
                    STORY_EVENTS, TIME_SWITCHES, TRACKER_ORDER, TRANSFERS)

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
# Story-only copies of rooms, there for one scene (chapter 3's attack on the city): their doors are never shuffled and
# they're never a start, so their door pairs join the fixed links (the user, 2026-10-05).
STORY_ONLY_MAPS = frozenset({"BugariaAssociationAttack", "BugariaPlazaAttack", "BugariaBridgeAttack",
                             "BugariaCastleAttack"})


def _story_doors_fixed(doors: Doors) -> Doors:
    story = [c for c in doors.connections if c.a.map in STORY_ONLY_MAPS or c.b.map in STORY_ONLY_MAPS]
    links = tuple((end.map, other.map) for c in story for end, other in ((c.a, c.b), (c.b, c.a)))
    return Doors(connections=tuple(c for c in doors.connections if c not in story), one_way=doors.one_way,
                 fixed=(*doors.fixed, *links), gated=doors.gated)


DOORS: Doors = _story_doors_fixed(Doors.from_json(_load("doors.json")))


def door_name(map_name: str, door: str) -> str:
    """A door's entrance, named where it is."""
    return f"{map_name}: {door}"


# Every two-way door the entrance randomizer shuffles, by its entrance's name.
DOOR_NAMES: frozenset[str] = frozenset(door_name(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b))
# Maps nothing leads into: an unused room and the debug room (MEASURED.md, "The door graph").
UNUSED_MAPS = frozenset({"SnakemouthEmpty", "TestRoom"})
# Every one-way door, those of unused maps left out.
ONE_WAYS: tuple[OneWayDoor, ...] = tuple(w for w in DOORS.one_way
                                         if w.map not in UNUSED_MAPS and w.to not in UNUSED_MAPS)


def one_way_landing(door: OneWayDoor) -> str:
    """A one-way door's landing, the entrance randomizer's target: named apart from the door, so a one-way may keep its
    own landing (coupled, the randomizer never joins an exit to a target of its own name)."""
    return f"{door.to} as from {door_name(door.map, door.door)}"


# The one-way doors and their landings, by name.
ONE_WAY_NAMES: frozenset[str] = frozenset(door_name(w.map, w.door) for w in ONE_WAYS)
ONE_WAY_LANDINGS: frozenset[str] = frozenset(one_way_landing(w) for w in ONE_WAYS)
# Every map a region: the door table's and those only a transfer reaches.
MAPS: tuple[str, ...] = tuple(sorted(
    ({end.map for c in DOORS.connections for end in (c.a, c.b)} | {m for w in ONE_WAYS for m in (w.map, w.to)}
     | {m for link in DOORS.fixed for m in link}
     | {m for t in TRANSFERS for m in (t.from_map, t.to_map)}) - UNUSED_MAPS))
# Every region past Menu: the maps, then the parts of a map a roadblock cuts off.
REGIONS: tuple[str, ...] = (*MAPS, *(area.region for area in MAP_AREAS))
_AREA_OF_DOOR: dict[tuple[str, str], str] = {(area.map, door): area.region for area in MAP_AREAS for door in area.doors}


def door_region(map_name: str, door: str) -> str:
    """The region a door stands in: its map's, or the map area's that holds it."""
    return _AREA_OF_DOOR.get((map_name, door), map_name)


_AREA_OF_LANDING: dict[tuple[str, str], Area] = {landing: area for area in MAP_AREAS for landing in area.landings}
for _w in ONE_WAYS:
    if (_w.map, _w.door) in _AREA_OF_LANDING and _AREA_OF_LANDING[(_w.map, _w.door)].map != _w.to:
        raise ValueError(f"bug_fables: {_w.map}/{_w.door} lands in {_w.to}, not in its landing's area")
if set(_AREA_OF_LANDING) - {(w.map, w.door) for w in ONE_WAYS}:
    raise ValueError("bug_fables: an area takes the landing of a door that is no one-way")


def landing_region(door: OneWayDoor) -> str:
    """The region a one-way door lands in: its target map's, or the map area's that takes its landing."""
    area = _AREA_OF_LANDING.get((door.map, door.door))
    return door.to if area is None else area.region


# Every room entered through a door: the map, and the map whose door leads in (both ways of each connection). A start
# there lands where walking in through that door ends, at the room's own door back: never one the game makes only from
# a story flag (absent on a new file, so the landing is over nothing) unless the seed keeps it present.
_PRESENT_FROM_START: frozenset[tuple[str, str]] = frozenset((e.map, e.entity) for e in KEPT_PRESENT)
ROOM_STARTS: tuple[RoomStart, ...] = tuple(sorted(
    {RoomStart(map=end.map, from_map=other.map) for c in DOORS.connections for end, other in ((c.a, c.b), (c.b, c.a))
     if end.map != other.map
     and ((end.map, end.door) not in DOORS.gated or (end.map, end.door) in _PRESENT_FROM_START)}))
# Every save point (map, entity index). Unused today (a random start picks from ROOM_STARTS); kept for named start
# spots, a planned option.
STARTS: tuple[SavePoint, ...] = tuple(SavePoint.from_json(s) for s in _load("starts.json")["starts"])
# Every map enemy (map, entity index) and the enemy ids its fight starts with.
ENCOUNTERS: tuple[Encounter, ...] = tuple(Encounter.from_json(e) for e in _load("enemies.json")["encounters"])
# Enemysanity's spots join the hand-written ones (by id, as the logic orders them).
ENEMY_LOCATIONS: tuple[Location, ...] = enemy_locations(ENCOUNTERS, LOCATIONS, MAPS)
LOCATIONS = tuple(sorted(LOCATIONS + ENEMY_LOCATIONS, key=lambda loc: loc.id))

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
# Termacade tokens: game_id is the amount.
TOKEN_KIND = 7
TOKEN_ID_OFFSET = 6_000


def item_id(item: Item) -> int:
    offset = {MEDAL_KIND: MEDAL_ID_OFFSET, MONEY_KIND: MONEY_ID_OFFSET, CRYSTAL_KIND: CRYSTAL_ID_OFFSET,
              MEMBER_KIND: MEMBER_ID_OFFSET, MOVE_KIND: MOVE_ID_OFFSET, TOKEN_KIND: TOKEN_ID_OFFSET}.get(item.kind, 0)
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
    if source.tokens is not None:
        return next(item.name for item in ITEMS if item.kind == TOKEN_KIND and item.game_id == source.tokens)
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

